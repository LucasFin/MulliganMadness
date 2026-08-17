using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ModdingUtils.Utils;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;

namespace MulliganMadness.Utils
{
    public static class TakeAllManager
    {
        private static readonly HashSet<int> UsedThisGame = new HashSet<int>();
        private static bool _busy;

        public static void ResetForNewGame()
        {
            UsedThisGame.Clear();
            _busy = false;
        }

        public static bool IsEnabled => Plugin.Configs == null || Plugin.Configs.EnableTakeAll.Value;

        public static bool HasRemaining(Player player)
        {
            if (player == null) return false;
            return IsEnabled && !UsedThisGame.Contains(player.playerID);
        }

        public static bool IsLocalPlayersTurn()
        {
            if (CardChoice.instance == null || !CardChoice.instance.IsPicking) return false;
            var picker = GetCurrentPicker();
            return picker != null && picker.data != null && picker.data.view != null && picker.data.view.IsMine;
        }

        public static Player GetCurrentPicker()
        {
            var choice = CardChoice.instance;
            if (choice == null) return null;

            var pickerType = (PickerType)AccessTools.Field(typeof(CardChoice), "pickerType").GetValue(choice);
            if (pickerType == PickerType.Team)
            {
                var team = PlayerManager.instance.GetPlayersInTeam(choice.pickrID);
                if (team == null || team.Length == 0) return null;
                return team.FirstOrDefault(p => p.data?.view != null && p.data.view.IsMine) ?? team[0];
            }

            return PlayerManager.instance.players.FirstOrDefault(p => p.playerID == choice.pickrID);
        }

        public static bool TryTakeAll()
        {
            if (_busy || !IsEnabled) return false;
            if (!IsLocalPlayersTurn()) return false;

            var picker = GetCurrentPicker();
            if (picker == null || !HasRemaining(picker)) return false;

            var spawned = GetSpawnedCards();
            if (spawned == null || spawned.Count == 0) return false;

            _busy = true;
            NetworkingManager.RPC(typeof(TakeAllManager), nameof(RPCA_MarkUsed), picker.playerID);

            var sources = new List<CardInfo>();
            foreach (var go in spawned)
            {
                if (go == null) continue;
                var visual = go.GetComponent<CardInfo>();
                if (visual == null) continue;
                var source = CardChoice.instance.GetSourceCard(visual) ?? visual;
                if (source != null) sources.Add(source);
            }

            if (sources.Count == 0)
            {
                _busy = false;
                return false;
            }

            // Add every card except the one we will Pick with, so pick-end logic still runs once.
            if (sources.Count > 1)
            {
                var extras = sources.Skip(1).ToArray();
                var zeros = new float[extras.Length];
                Cards.instance.AddCardsToPlayer(picker, extras, false, null, zeros, zeros, true);
            }

            var pickVisual = spawned[0];
            Plugin.Instance.ExecuteAfterFrames(1, () =>
            {
                try
                {
                    if (CardChoice.instance != null && CardChoice.instance.IsPicking && pickVisual != null)
                    {
                        CardChoice.instance.Pick(pickVisual, true);
                    }
                }
                finally
                {
                    _busy = false;
                }
            });

            Plugin.Instance.Log($"Player {picker.playerID} used Take All ({sources.Count} cards).");
            return true;
        }

        public static List<GameObject> GetSpawnedCards()
        {
            var choice = CardChoice.instance;
            if (choice == null) return null;
            var field = AccessTools.Field(typeof(CardChoice), "spawnedCards");
            return field?.GetValue(choice) as List<GameObject>;
        }

        [UnboundRPC]
        public static void RPCA_MarkUsed(int playerID)
        {
            UsedThisGame.Add(playerID);
            UI.TakeAllButton.RefreshVisibility();
        }
    }
}
