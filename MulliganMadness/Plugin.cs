using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using MulliganMadness.Cards;
using MulliganMadness.Curses;
using MulliganMadness.Patches;
using MulliganMadness.Stats;
using MulliganMadness.UI;
using MulliganMadness.Utils;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;

namespace MulliganMadness
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.cardchoicespawnuniquecardpatch", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.willuwontu.rounds.managers", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.pickncards", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rsmind.rounds.fancycardbar", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(ModId, ModName, Version)]
    [BepInProcess("Rounds.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModId = "com.bukey.rounds.mulliganmadness";
        public const string ModName = "Mulligan Madness";
        public const string Version = "0.4.3";
        public const string ModInitials = "MM";
        public const string CurseInitials = "MMC";
        public const string CardsMenuName = "MulliganMadness";

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

            // Patch per type so one bad/unloadable type cannot abort every MM patch
            // (Unity Mono historically chokes on IsReadOnlyAttribute from readonly structs).
            try
            {
                var harmony = new Harmony(ModId);
                Type[] types;
                try
                {
                    types = typeof(Plugin).Assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                    Logger.LogWarning($"Harmony GetTypes partial load: {ex.LoaderExceptions?.Length ?? 0} loader error(s)");
                }

                foreach (var type in types)
                {
                    if (type == null) continue;
                    try
                    {
                        harmony.CreateClassProcessor(type).Patch();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Harmony skip {type.FullName}: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Harmony patching failed: {ex}");
            }
        }

        private void Start()
        {
            AutoPickCurse.RegisterAll();
            CardRegistration.RegisterAll();
            CardArtFactory.BindLoadedCardInfos();
            NestEggManager.RegisterHooks();
            MmStatus.Register();

            Unbound.RegisterMenu(ModName, () => { }, DrawSettingsMenu, null, true);
            Unbound.RegisterHandshake(ModId, OnHandshake);

            GameModeManager.AddHook(GameModeHooks.HookGameStart, OnGameStart);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickStart, OnPlayerPickStart);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickEnd, OnPlayerPickEnd);
            GameModeManager.AddHook(GameModeHooks.HookPickEnd, OnPickEnd);

            gameObject.GetOrAddComponent<TakeAllButton>();
            gameObject.GetOrAddComponent<AutoPickController>();
            gameObject.GetOrAddComponent<SessionVoteTicker>();
            gameObject.GetOrAddComponent<FumbleController>();
            gameObject.GetOrAddComponent<BlindDraftController>();
        }

        private static void OnHandshake()
        {
            // Only meaningful in a room. Raising an Unbound event outside one logs
            // "RaiseEvent(69) failed" and drops the payload.
            if (!SessionSettings.IsHost) return;
            if (Photon.Pun.PhotonNetwork.OfflineMode) return;
            if (!Photon.Pun.PhotonNetwork.InRoom) return;

            SessionSettingsSync.BroadcastToAllIfHost();
        }

        private static IEnumerator OnGameStart(IGameModeHandler gm)
        {
            TakeAllManager.ResetForNewGame();
            TakeAllVoteManager.ResetForNewGame();
            MercyTakeAllManager.ResetForNewGame();
            RoundWinTracker.Reset();
            AutoPickController.ResetForNewGame();
            NestEggManager.ResetForNewGame();
            KeysUnlockReset.Reapply();
            Instance.ExecuteAfterSeconds(0.35f, KeysUnlockReset.Reapply);
            yield break;
        }

        private static IEnumerator OnPlayerPickStart(IGameModeHandler gm)
        {
            FumbleController.ResetForPick();
            TakeAllManager.ClearAuthorization();
            TakeAllManager.ClearPickTransientState();
            TakeAllManager.ApplyDeferredKnowledge();
            TakeAllButton.RefreshVisibility();
            AutoPickController.NotifyPlayerPickStarted();

            var picker = TakeAllManager.GetCurrentPicker();
            if (picker != null)
            {
                Instance.ExecuteAfterSeconds(0.5f, () => MercyTakeAllManager.TryOfferMercy(picker));
            }

            yield break;
        }

        private static IEnumerator OnPlayerPickEnd(IGameModeHandler gm)
        {
            FumbleController.ResetForPick();
            TakeAllVoteManager.CancelIfActive("Take All vote cancelled - pick ended.");
            TakeAllManager.ClearAuthorization();
            TakeAllManager.ClearActingPicker();
            TakeAllManager.ClearPickTransientState();
            PickAnnounceUi.HidePanic();
            TakeAllButton.RefreshVisibility();
            yield break;
        }

        private static IEnumerator OnPickEnd(IGameModeHandler gm)
        {
            FumbleController.ResetForPick();
            TakeAllVoteManager.CancelIfActive("Take All vote cancelled - pick ended.");
            TakeAllManager.ClearAuthorization();
            TakeAllManager.ClearActingPicker();
            TakeAllManager.ClearPickTransientState();
            PickAnnounceUi.HidePanic();
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
