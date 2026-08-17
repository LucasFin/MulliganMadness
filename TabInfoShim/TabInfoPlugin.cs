using System.Collections;
using BepInEx;
using HarmonyLib;
using TabInfo.Utils;
using UnboundLib;
using UnboundLib.GameModes;

namespace TabInfo
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModId, ModName, Version)]
    [BepInProcess("Rounds.exe")]
    public class TabInfoPlugin : BaseUnityPlugin
    {
        public const string ModId = "com.willuwontu.rounds.tabinfo";
        public const string ModName = "Tab Info (MM Compat)";
        public const string Version = "0.0.6";
        public const string ModInitials = "TI";

        private void Awake()
        {
            new Harmony(ModId).PatchAll();
            Unbound.RegisterClientSideMod(ModId);
        }

        private void Start()
        {
            GameModeManager.AddHook(GameModeHooks.HookRoundStart, OnRoundStart);
            GameModeManager.AddHook(GameModeHooks.HookPointStart, OnPointStart);
            GameModeManager.AddHook(GameModeHooks.HookGameStart, OnGameStart);
        }

        private static IEnumerator OnRoundStart(IGameModeHandler gm)
        {
            TabInfoManager.CurrentRound += 1;
            TabInfoManager.CurrentPoint = 0;
            yield break;
        }

        private static IEnumerator OnPointStart(IGameModeHandler gm)
        {
            TabInfoManager.CurrentPoint += 1;
            yield break;
        }

        private static IEnumerator OnGameStart(IGameModeHandler gm)
        {
            TabInfoManager.CurrentRound = 0;
            TabInfoManager.CurrentPoint = 0;
            yield break;
        }
    }
}
