using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnboundLib;
using UnboundLib.Networking;
using UnityEngine;

namespace MulliganMadness.Utils
{
    public static class SandbagManager
    {
        private static readonly HashSet<int> UsedThisGame = new HashSet<int>();

        internal static void ResetForNewGame() => UsedThisGame.Clear();

        internal static bool HasRemaining(Player player) =>
            player != null
            && SessionSettings.Current.EnableSandbagSimulator
            && (!SessionSettings.Current.SandbagOncePerGame || !UsedThisGame.Contains(player.playerID));

        internal static void TryPromptSandbag(Player user)
        {
            if (user == null || !HasRemaining(user)) return;
            if (ItemShopGuard.AnyPlayerInShop())
            {
                PlayerNotice.Show(user, "Can't sandbag during a shop.");
                return;
            }

            CardTargetUi.OpenSandbag(user, target =>
            {
                if (target == null) return;
                NetworkingManager.RPC(typeof(SandbagManager), nameof(RPCA_RerollTarget), user.playerID, target.playerID);
            });
        }

        [UnboundRPC]
        public static void RPCA_RerollTarget(int userId, int targetId)
        {
            var user = PlayerManager.instance.players.FirstOrDefault(p => p.playerID == userId);
            var target = PlayerManager.instance.players.FirstOrDefault(p => p.playerID == targetId);
            if (user == null || target == null) return;

            if (SessionSettings.Current.SandbagOncePerGame)
            {
                UsedThisGame.Add(userId);
            }

            if (!(PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)) return;

            if (ItemShopGuard.AnyPlayerInShop())
            {
                Plugin.Instance.LogWarn("Sandbag blocked — shop open.");
                return;
            }

            Plugin.Instance.StartCoroutine(RerollTargetRoutine(target));
            Plugin.Instance.Log($"Player {userId} sandbagged player {targetId}'s hand.");
        }

        private static IEnumerator RerollTargetRoutine(Player target)
        {
            var managerType = AccessTools.TypeByName("WillsWackyManagers.Utils.RerollManager");
            var instanceProp = managerType == null ? null : AccessTools.Property(managerType, "instance");
            var manager = instanceProp?.GetValue(null);
            if (manager == null)
            {
                Plugin.Instance.LogWarn("Sandbag failed — RerollManager missing.");
                yield break;
            }

            var rerollMethod = AccessTools.Method(managerType, "Reroll", new[] { typeof(Player), typeof(bool) });
            if (rerollMethod == null)
            {
                Plugin.Instance.LogWarn("Sandbag failed — Reroll method missing.");
                yield break;
            }

            var routine = rerollMethod.Invoke(manager, new object[] { target, false }) as IEnumerator;
            if (routine == null)
            {
                QueuePendingReroll(manager, managerType, target);
                yield break;
            }

            while (routine.MoveNext())
            {
                yield return routine.Current;
            }
        }

        private static void QueuePendingReroll(object manager, Type managerType, Player target)
        {
            try
            {
                var listField = AccessTools.Field(managerType, "rerollPlayers");
                if (listField?.GetValue(manager) is IList list)
                {
                    if (!list.Contains(target)) list.Add(target);
                }

                var flagField = AccessTools.Field(managerType, "reroll");
                if (flagField != null) flagField.SetValue(manager, true);
            }
            catch (Exception ex)
            {
                Plugin.Instance.LogWarn($"Sandbag queue fallback failed: {ex.Message}");
            }
        }
    }
}
