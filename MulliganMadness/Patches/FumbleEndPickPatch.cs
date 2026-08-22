using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MulliganMadness.Curses;
using MulliganMadness.Utils;
using Photon.Pun;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;

namespace MulliganMadness.Patches
{
    internal sealed class FumbleController : MonoBehaviour
    {
        private static bool _fumble;
        private static int _delta;
        private static int _rollPickerId = -1;
        private static int _rollHandKey = int.MinValue;
        private static int _lastHandKey = int.MinValue;
        private static bool _sentThisHand;

        internal static bool HasRoll(int pickerId) =>
            _rollPickerId == pickerId
            && _rollHandKey != int.MinValue
            && NetworkHandKey(TakeAllManager.GetSpawnedCards()) == _rollHandKey;

        internal static bool ShouldFumble(int pickerId) => HasRoll(pickerId) && _fumble;

        internal static int Delta => _delta == 0 ? 1 : _delta;

        internal static void ResetForPick()
        {
            _fumble = false;
            _delta = 0;
            _rollPickerId = -1;
            _rollHandKey = int.MinValue;
            _lastHandKey = int.MinValue;
            _sentThisHand = false;
        }

        private void Update()
        {
            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;
            if (CardChoice.instance == null || !CardChoice.instance.IsPicking)
            {
                ResetForPick();
                return;
            }

            var picker = TakeAllManager.GetCurrentPicker();
            if (picker == null || !CurseOwnership.Has(picker, Fumble.Card))
            {
                _sentThisHand = false;
                return;
            }

            if (!TakeAllManager.IsOfferedHandReady()) return;
            if (TakeAllManager.CollectingAll) return;

            var spawned = TakeAllManager.GetSpawnedCards();
            var count = spawned?.Count ?? 0;
            if (count <= 1) return;

            var handKey = HandKey(spawned);
            if (_sentThisHand && handKey == _lastHandKey) return;
            _lastHandKey = handKey;
            _sentThisHand = true;

            var fumble = Random.value < CurseOwnership.FumbleChance;
            var delta = Random.value < 0.5f ? -1 : 1;
            NetworkingManager.RPC(
                typeof(FumbleController),
                nameof(RPCA_FumbleRoll),
                picker.playerID,
                fumble,
                delta,
                NetworkHandKey(spawned));
        }

        [UnboundRPC]
        public static void RPCA_FumbleRoll(int pickerId, bool fumble, int delta, int handKey)
        {
            _rollPickerId = pickerId;
            _fumble = fumble;
            _delta = delta == 0 ? 1 : (delta > 0 ? 1 : -1);
            _rollHandKey = handKey;
        }

        [UnboundRPC]
        public static void RPCA_FumbleAnnounce(int pickerId)
        {
            var name = PlayerName(pickerId);
            CardTargetUi.ShowToast($"{name} fumbled - neighbor card.");
        }

        internal static GameObject FindNeighbor(GameObject picked, int delta)
        {
            var spawned = TakeAllManager.GetSpawnedCards();
            if (spawned == null || spawned.Count < 2) return null;

            var ordered = spawned
                .Where(go => go != null)
                .OrderBy(go => go.transform.position.x)
                .ToList();
            if (ordered.Count < 2) return null;

            var index = ordered.IndexOf(picked);
            if (index < 0) index = 0;
            var next = index + delta;
            if (next < 0 || next >= ordered.Count) next = index - delta;
            if (next < 0 || next >= ordered.Count) next = index == 0 ? 1 : index - 1;
            return ordered[Mathf.Clamp(next, 0, ordered.Count - 1)];
        }

        internal static void NoteHandConsumed()
        {
            _sentThisHand = false;
            _lastHandKey = int.MinValue;
            _rollPickerId = -1;
            _rollHandKey = int.MinValue;
            _fumble = false;
        }

        private static int HandKey(List<GameObject> spawned) => NetworkHandKey(spawned);

        private static int NetworkHandKey(List<GameObject> spawned)
        {
            if (spawned == null) return int.MinValue;
            var key = spawned.Count * 397;
            foreach (var go in spawned)
            {
                if (go == null) continue;
                var view = go.GetComponent<PhotonView>();
                if (view != null) key ^= view.ViewID;
            }

            return key;
        }

        private static string PlayerName(int pickerId)
        {
            var players = PlayerManager.instance?.players;
            if (players != null)
            {
                foreach (var player in players)
                {
                    if (player == null || player.playerID != pickerId) continue;
                    var name = player.data?.view?.Owner?.NickName;
                    if (!string.IsNullOrEmpty(name)) return name;
                    return "Player " + (pickerId + 1);
                }
            }

            return "Player " + (pickerId + 1);
        }
    }

    // ApplyCardStats runs inside Pick() on the clicking client, then RPCA_DoEndPick
    // sends that card's ViewID to everyone. Remap here so the granted card and the
    // Photon animation target stay the same. Do not also remap in IDoEndPick.
    [HarmonyPatch(typeof(CardChoice), nameof(CardChoice.Pick))]
    internal static class FumblePickPatch
    {
        private static void Prefix(ref GameObject pickedCard)
        {
            if (TakeAllManager.CollectingAll) return;
            if (pickedCard == null) return;

            var picker = TakeAllManager.GetCurrentPicker();
            if (picker == null || !CurseOwnership.Has(picker, Fumble.Card)) return;
            if (!FumbleController.ShouldFumble(picker.playerID)) return;

            var neighbor = FumbleController.FindNeighbor(pickedCard, FumbleController.Delta);
            if (neighbor == null || neighbor == pickedCard) return;

            pickedCard = neighbor;
            FumbleController.NoteHandConsumed();
            NetworkingManager.RPC(typeof(FumbleController), nameof(FumbleController.RPCA_FumbleAnnounce), picker.playerID);
        }
    }
}
