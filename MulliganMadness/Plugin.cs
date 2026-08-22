using System;
using System.Collections;
using BepInEx;
using HarmonyLib;
using MulliganMadness.Cards;
using MulliganMadness.Curses;
using MulliganMadness.UI;
using MulliganMadness.Utils;
using Photon.Pun;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;
using WillsWackyManagers.Utils;

namespace MulliganMadness
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.cardchoicespawnuniquecardpatch", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.willuwontu.rounds.managers", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModId, ModName, Version)]
    [BepInProcess("Rounds.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModId = "com.bukey.rounds.mulliganmadness";
        public const string ModName = "Mulligan Madness";
        public const string Version = "0.3.13";
        public const string ModInitials = "MM";
        public const string CurseInitials = "MMC";

        public static Plugin Instance { get; private set; }

        internal static Configs Configs;

        private void Awake()
        {
            Instance = this;
            Configs = new Configs(Config);
            SessionSettings.InitializeFromConfig();
            SessionSettingsSync.Register();
            SessionRulesBanner.RegisterHooks();
            RoundWinTracker.RegisterHooks();
            try
            {
                new Harmony(ModId).PatchAll();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Harmony PatchAll failed: {ex}");
            }
        }

        private void Start()
        {
            AutoPickCurse.RegisterAll();
            CardRegistration.RegisterAll();
            TakebacksiesBlacklist.EnsureGlobalBlacklist();

            Unbound.RegisterMenu(ModName, () => { }, DrawSettingsMenu, null, true);
            Unbound.RegisterHandshake(ModId, OnHandshake);

            GameModeManager.AddHook(GameModeHooks.HookGameStart, OnGameStart);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickStart, OnPlayerPickStart);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickEnd, OnPlayerPickEnd);
            GameModeManager.AddHook(GameModeHooks.HookPickEnd, OnPickEnd);

            gameObject.GetOrAddComponent<TakeAllButton>();
            gameObject.GetOrAddComponent<AutoPickController>();
            gameObject.GetOrAddComponent<StatsController>();
            gameObject.GetOrAddComponent<SessionVoteTicker>();
            StatsController.RegisterHooks();
        }

        private static void OnHandshake()
        {
            if (SessionSettings.IsHost)
            {
                SessionSettingsSync.BroadcastToAllIfHost();
            }
        }

        private static IEnumerator OnGameStart(IGameModeHandler gm)
        {
            TakeAllManager.ResetForNewGame();
            TakeAllVoteManager.ResetForNewGame();
            MercyTakeAllManager.ResetForNewGame();
            RoundWinTracker.Reset();
            AutoPickController.ResetForNewGame();
            StealLedger.ResetForNewGame();
            SandbagManager.ResetForNewGame();
            DefaultAppearance.ResetForNewGame();
            KeysUnlockReset.Reapply();
            Instance.ExecuteAfterSeconds(0.35f, KeysUnlockReset.Reapply);
            Instance.ExecuteAfterSeconds(0.5f, TakebacksiesBlacklist.EnsureGlobalBlacklist);
            Instance.ExecuteAfterSeconds(0.6f, () => DefaultAppearance.TryApply());
            Instance.ExecuteAfterSeconds(1.2f, () => DefaultAppearance.TryApply());
            yield break;
        }

        private static IEnumerator OnPlayerPickStart(IGameModeHandler gm)
        {
            TakeAllManager.ApplyDeferredKnowledge();
            TakeAllButton.RefreshVisibility();
            AutoPickController.NotifyPlayerPickStarted();
            StealLedger.TryOpenDeferredThiefPrompt();

            var picker = TakeAllManager.GetCurrentPicker();
            if (picker != null)
            {
                Instance.ExecuteAfterSeconds(0.5f, () => MercyTakeAllManager.TryOfferMercy(picker));
            }

            yield break;
        }

        private static IEnumerator OnPlayerPickEnd(IGameModeHandler gm)
        {
            TakeAllVoteManager.CancelIfActive("Take All vote cancelled — pick ended.");
            TakeAllButton.RefreshVisibility();
            yield break;
        }

        private static IEnumerator OnPickEnd(IGameModeHandler gm)
        {
            TakeAllVoteManager.CancelIfActive("Take All vote cancelled — pick ended.");
            TakeAllButton.Hide();
            yield break;
        }

        private static void DrawSettingsMenu(GameObject menu)
        {
            MenuBuilder.Build(menu);
        }

        internal void Log(string message) => Logger.LogInfo($"[{ModName}] {message}");
        internal void LogWarn(string message) => Logger.LogWarning($"[{ModName}] {message}");
    }

    internal sealed class SessionVoteTicker : MonoBehaviour
    {
        private void Update() => TakeAllVoteManager.Tick();
    }
}
