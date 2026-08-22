using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MulliganMadness.Cards;
using MulliganMadness.Stats;
using Photon.Pun;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;
using UnityEngine.EventSystems;
using CardsApi = ModdingUtils.Utils.Cards;

namespace MulliganMadness.Utils
{
    internal static class DraftSniperManager
    {
        private static readonly MethodInfo SpawnMethod =
            AccessTools.Method(typeof(CardChoice), "Spawn", new[] { typeof(GameObject), typeof(Vector3), typeof(Quaternion) });
        private static readonly FieldInfo IsHoveredField = AccessTools.Field(typeof(CardVisuals), "isHovered");
        private static readonly HashSet<int> BlockedViews = new HashSet<int>();
        private static readonly Dictionary<int, int> UsesConsumed = new Dictionary<int, int>();
        private static readonly Queue<PendingBan> HostQueue = new Queue<PendingBan>();

        private static bool _hostBusy;
        private static float _clickLockUntil;
        private static int _hintHandKey = int.MinValue;

        private struct PendingBan
        {
            public int SniperId;
            public int ViewId;
        }

        internal static void ResetForNewGame()
        {
            BlockedViews.Clear();
            UsesConsumed.Clear();
            HostQueue.Clear();
            _hostBusy = false;
            _clickLockUntil = 0f;
            _hintHandKey = int.MinValue;
        }

        internal static int CountOwned(Player player)
        {
            var cards = player?.data?.currentCards;
            if (cards == null) return 0;
            var count = 0;
            foreach (var card in cards)
            {
                if (card == null) continue;
                if (DraftSniper.Card != null && card == DraftSniper.Card) count++;
                else if (string.Equals(card.cardName, DraftSniper.Title, System.StringComparison.OrdinalIgnoreCase)) count++;
            }

            return count;
        }

        internal static int Remaining(Player player)
        {
            if (player == null) return 0;
            UsesConsumed.TryGetValue(player.playerID, out var used);
            return Mathf.Max(0, CountOwned(player) - used);
        }

        internal static bool IsBlocked(GameObject card)
        {
            if (card == null) return true;
            var view = card.GetComponent<PhotonView>();
            return view != null && BlockedViews.Contains(view.ViewID);
        }

        internal static void NotifyGained(Player player)
        {
            if (player?.data?.view == null || !player.data.view.IsMine) return;
            var left = Remaining(player);
            if (left <= 0) return;
            var extra = left == 1
                ? "Click a card during someone else's pick to replace it."
                : $"Stacked. {left} snipes ready. Click a card during someone else's pick.";
            PlayerNotice.Show(player, extra);
        }

        internal static void Tick()
        {
            if (CardChoice.instance == null || !CardChoice.instance.IsPicking)
            {
                _hintHandKey = int.MinValue;
                return;
            }

            var local = PlayerStatsSnapshot.LocalPlayer();
            if (!CanLocalSnipe(local)) return;
            MaybeHint(local);

            if (Time.unscaledTime < _clickLockUntil) return;
            if (!Input.GetMouseButtonDown(0)) return;
            if (IsPointerOverUi()) return;

            var card = CardUnderCursor();
            if (card == null || IsBlocked(card)) return;
            var view = card.GetComponent<PhotonView>();
            if (view == null || view.ViewID == 0) return;

            _clickLockUntil = Time.unscaledTime + 0.35f;
            NetworkingManager.RPC(typeof(DraftSniperManager), nameof(RPCA_BanCard), local.playerID, view.ViewID);
        }

        [UnboundRPC]
        public static void RPCA_BanCard(int sniperId, int viewId)
        {
            BlockedViews.Add(viewId);
            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;

            if (!IsValidSnipe(sniperId))
            {
                NetworkingManager.RPC(typeof(DraftSniperManager), nameof(RPCA_Unblock), viewId);
                return;
            }

            HostQueue.Enqueue(new PendingBan { SniperId = sniperId, ViewId = viewId });
            TryStartNextReplace();
        }

        [UnboundRPC]
        public static void RPCA_Unblock(int viewId) => BlockedViews.Remove(viewId);

        [UnboundRPC]
        public static void RPCA_ConsumeUse(int sniperId)
        {
            UsesConsumed.TryGetValue(sniperId, out var used);
            UsesConsumed[sniperId] = used + 1;
        }

        [UnboundRPC]
        public static void RPCA_SwapSpawned(int oldViewId, int newViewId)
        {
            BlockedViews.Remove(oldViewId);
            if (Plugin.Instance == null)
            {
                SwapList(TakeAllManager.GetSpawnedCards(), oldViewId, newViewId);
                return;
            }

            Plugin.Instance.StartCoroutine(SwapWhenReady(oldViewId, newViewId));
        }

        [UnboundRPC]
        public static void RPCA_AnnounceBan(string sniperName, string cardName, string targetName)
        {
            CardTargetUi.ShowToast($"{sniperName} sniped {cardName} from {targetName}.");
        }

        private static void TryStartNextReplace()
        {
            if (_hostBusy || HostQueue.Count == 0) return;
            var next = HostQueue.Dequeue();
            _hostBusy = true;
            Plugin.Instance.StartCoroutine(ReplaceCardRoutine(next.SniperId, next.ViewId));
        }

        private static IEnumerator SwapWhenReady(int oldViewId, int newViewId)
        {
            for (var i = 0; i < 30; i++)
            {
                if (PhotonView.Find(newViewId) != null) break;
                yield return null;
            }

            SwapList(TakeAllManager.GetSpawnedCards(), oldViewId, newViewId);
        }

        private static void SwapList(List<GameObject> list, int oldViewId, int newViewId)
        {
            if (list == null) return;

            GameObject neu = null;
            var found = PhotonView.Find(newViewId);
            if (found != null) neu = found.gameObject;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                var go = list[i];
                if (go == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                var view = go.GetComponent<PhotonView>();
                if (view != null && view.ViewID == oldViewId)
                {
                    if (neu != null) list[i] = neu;
                    else list.RemoveAt(i);
                }
            }

            if (neu != null && !list.Contains(neu)) list.Add(neu);
        }

        private static IEnumerator ReplaceCardRoutine(int sniperId, int viewId)
        {
            yield return ReplaceOnce(sniperId, viewId);
            _hostBusy = false;
            TryStartNextReplace();
        }

        private static IEnumerator ReplaceOnce(int sniperId, int viewId)
        {
            var sniper = FindPlayer(sniperId);
            if (Remaining(sniper) <= 0)
            {
                NetworkingManager.RPC(typeof(DraftSniperManager), nameof(RPCA_Unblock), viewId);
                yield break;
            }

            var oldView = PhotonView.Find(viewId);
            var old = oldView != null ? oldView.gameObject : null;
            if (old == null)
            {
                NetworkingManager.RPC(typeof(DraftSniperManager), nameof(RPCA_Unblock), viewId);
                yield break;
            }

            var picker = TakeAllManager.GetCurrentPicker();
            var choice = CardChoice.instance;
            if (choice == null || picker == null || SpawnMethod == null)
            {
                NetworkingManager.RPC(typeof(DraftSniperManager), nameof(RPCA_Unblock), viewId);
                yield break;
            }

            var banned = TakeAllManager.SourceOf(old);
            var replacement = PickReplacement(picker, banned);
            if (replacement == null || replacement.gameObject == null)
            {
                NetworkingManager.RPC(typeof(DraftSniperManager), nameof(RPCA_Unblock), viewId);
                yield break;
            }

            var pos = old.transform.position;
            var rot = old.transform.rotation;
            var oldName = banned != null && !string.IsNullOrEmpty(banned.cardName) ? banned.cardName : "a card";

            GameObject spawned = null;
            try
            {
                spawned = SpawnMethod.Invoke(choice, new object[] { replacement.gameObject, pos, rot }) as GameObject;
            }
            catch
            {
                spawned = null;
            }

            yield return null;
            var newView = spawned != null ? spawned.GetComponent<PhotonView>() : null;
            if (newView == null)
            {
                NetworkingManager.RPC(typeof(DraftSniperManager), nameof(RPCA_Unblock), viewId);
                yield break;
            }

            PhotonNetwork.Destroy(old);
            yield return null;

            NetworkingManager.RPC(typeof(DraftSniperManager), nameof(RPCA_ConsumeUse), sniperId);
            NetworkingManager.RPC(typeof(DraftSniperManager), nameof(RPCA_SwapSpawned), viewId, newView.ViewID);
            NetworkingManager.RPC(
                typeof(DraftSniperManager),
                nameof(RPCA_AnnounceBan),
                PlayerLabel(sniper, sniperId),
                oldName,
                PlayerLabel(picker, picker.playerID));
        }

        private static CardInfo PickReplacement(Player picker, CardInfo banned)
        {
            var all = CardsApi.all;
            if (all == null || all.Count == 0) return null;

            var options = new List<CardInfo>();
            foreach (var card in all)
            {
                if (card == null || card == banned) continue;
                if (banned != null && string.Equals(card.cardName, banned.cardName, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    if (!CardsApi.instance.PlayerIsAllowedCard(picker, card)) continue;
                }
                catch
                {
                    continue;
                }

                options.Add(card);
            }

            if (options.Count == 0) return null;
            return options[Random.Range(0, options.Count)];
        }

        private static bool CanLocalSnipe(Player local)
        {
            if (local == null || Remaining(local) <= 0) return false;
            if (TakeAllManager.CollectingAll) return false;
            if (!TakeAllManager.IsOfferedHandReady()) return false;
            var picker = TakeAllManager.GetCurrentPicker();
            if (picker == null) return false;
            if (SameTeam(local, picker)) return false;
            return !PlayerStatsSnapshot.IsLocallyControlled(picker);
        }

        private static bool IsValidSnipe(int sniperId)
        {
            var sniper = FindPlayer(sniperId);
            if (sniper == null || Remaining(sniper) <= 0) return false;
            if (TakeAllManager.CollectingAll) return false;
            var picker = TakeAllManager.GetCurrentPicker();
            if (picker == null || picker.playerID == sniperId) return false;
            return !SameTeam(sniper, picker);
        }

        private static bool SameTeam(Player a, Player b)
        {
            return a != null && b != null && a.teamID == b.teamID;
        }

        private static void MaybeHint(Player local)
        {
            var spawned = TakeAllManager.GetReadySpawnedCards();
            if (spawned == null || spawned.Count == 0) return;
            var key = spawned.Count;
            foreach (var go in spawned)
            {
                if (go != null) key ^= go.GetInstanceID();
            }

            if (key == _hintHandKey) return;
            _hintHandKey = key;
            var left = Remaining(local);
            CardTargetUi.ShowToast(left == 1
                ? "Draft Sniper: click a card to replace it."
                : $"Draft Sniper: click a card to replace it ({left} left).");
        }

        private static GameObject CardUnderCursor()
        {
            var spawned = TakeAllManager.GetReadySpawnedCards();
            if (spawned == null) return null;

            foreach (var go in spawned)
            {
                if (go == null) continue;
                var visuals = go.GetComponentInChildren<CardVisuals>(true);
                if (visuals == null || IsHoveredField == null) continue;
                try
                {
                    if (IsHoveredField.GetValue(visuals) is true) return go;
                }
                catch
                {
                    // hover flag layout changed
                }
            }

            var cam = Camera.main;
            if (cam == null) return null;

            GameObject best = null;
            var bestDist = 160f;
            var mouse = (Vector2)Input.mousePosition;
            foreach (var go in spawned)
            {
                if (go == null) continue;
                var screen = cam.WorldToScreenPoint(go.transform.position);
                if (screen.z < 0f) continue;
                var dist = Vector2.Distance(mouse, new Vector2(screen.x, screen.y));
                if (dist >= bestDist) continue;
                bestDist = dist;
                best = go;
            }

            return best;
        }

        private static bool IsPointerOverUi()
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }

        private static Player FindPlayer(int playerId)
        {
            if (PlayerManager.instance?.players == null) return null;
            foreach (var player in PlayerManager.instance.players)
            {
                if (player != null && player.playerID == playerId) return player;
            }

            return null;
        }

        private static string PlayerLabel(Player player, int id)
        {
            var name = player?.data?.view?.Owner?.NickName;
            return string.IsNullOrEmpty(name) ? "Player " + (id + 1) : name;
        }
    }

    internal sealed class DraftSniperTicker : MonoBehaviour
    {
        private void Update() => DraftSniperManager.Tick();
    }
}
