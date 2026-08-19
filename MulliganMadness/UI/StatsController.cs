using System.Collections;
using HarmonyLib;
using MulliganMadness.Stats;
using UnboundLib;
using UnboundLib.GameModes;
using UnityEngine;

namespace MulliganMadness.UI
{
    internal sealed class StatsController : MonoBehaviour
    {
        internal static StatsController Instance { get; private set; }
        internal static bool InPickPhase { get; private set; }
        internal static bool InBattlePhase { get; private set; }
        internal static bool MatchSessionActive { get; private set; }
        internal static int CurrentRound { get; private set; }
        internal static int CurrentPoint { get; private set; }
        internal static bool TabIsOpen => Instance?._tab != null && Instance._tab.IsOpen;

        private StatsHudPanel _hud;
        private CompactComparePanel _compare;
        private StatsTabOverlay _tab;
        private CardInfo _hoveredCard;
        private CardInfo _hoveredVisual;
        private PlayerStatsSnapshot _previewDelta;
        private float _refreshTimer;

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _hud = gameObject.AddComponent<StatsHudPanel>();
            _compare = gameObject.AddComponent<CompactComparePanel>();
            _tab = gameObject.AddComponent<StatsTabOverlay>();
        }

        internal static void RegisterHooks()
        {
            GameModeManager.AddHook(GameModeHooks.HookGameStart, OnGameStart);
            GameModeManager.AddHook(GameModeHooks.HookGameEnd, OnGameEnd);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, OnPickStart);
            GameModeManager.AddHook(GameModeHooks.HookPickEnd, OnPickEnd);
            GameModeManager.AddHook(GameModeHooks.HookRoundStart, OnRoundStart);
            GameModeManager.AddHook(GameModeHooks.HookPointStart, OnPointStart);
        }

        private static System.Collections.IEnumerator OnGameStart(IGameModeHandler gm)
        {
            MatchSessionActive = true;
            CurrentRound = 0;
            CurrentPoint = 0;
            InPickPhase = false;
            InBattlePhase = false;
            Instance?._compare?.ResetBaseline();
            Instance?._tab?.SetOpen(false);
            yield break;
        }

        private static System.Collections.IEnumerator OnGameEnd(IGameModeHandler gm)
        {
            MatchSessionActive = false;
            InPickPhase = false;
            InBattlePhase = false;
            Instance?._tab?.SetOpen(false);
            yield break;
        }

        private static System.Collections.IEnumerator OnPickStart(IGameModeHandler gm)
        {
            InPickPhase = true;
            InBattlePhase = false;
            yield break;
        }

        private static System.Collections.IEnumerator OnPickEnd(IGameModeHandler gm)
        {
            InPickPhase = false;
            Instance?.ClearPreview();
            yield break;
        }

        private static System.Collections.IEnumerator OnRoundStart(IGameModeHandler gm)
        {
            CurrentRound += 1;
            CurrentPoint = 0;
            InBattlePhase = true;
            InPickPhase = false;
            yield break;
        }

        private static System.Collections.IEnumerator OnPointStart(IGameModeHandler gm)
        {
            CurrentPoint += 1;
            yield break;
        }

        internal void NotifyHoveredCard(CardInfo cardInfo, CardInfo pickVisual = null)
        {
            if (!Plugin.Configs.EnableCardHoverPreview.Value) return;
            if (!Utils.TakeAllManager.IsLocalPlayersTurn())
            {
                ClearPreview();
                return;
            }

            if (_hoveredCard == cardInfo && _hoveredVisual == pickVisual) return;
            _hoveredCard = cardInfo;
            _hoveredVisual = pickVisual;

            var local = PlayerStatsSnapshot.LocalPlayer();
            if (local == null || cardInfo == null)
            {
                ClearPreview();
                return;
            }

            if (CardStatPreview.TryPreview(local, cardInfo, out var delta, pickVisual))
            {
                _previewDelta = delta;
            }
            else
            {
                _previewDelta = null;
            }

            _hud?.SetPreviewDelta(_previewDelta);
        }

        internal void ClearPreview()
        {
            _hoveredCard = null;
            _hoveredVisual = null;
            _previewDelta = null;
            _hud?.SetPreviewDelta(null);
        }

        internal static bool InActiveMatch()
        {
            if (!HasReadyPlayers()) return false;
            if (MatchSessionActive) return true;
            return IsSandboxMode();
        }

        private static bool HasReadyPlayers()
        {
            var players = PlayerManager.instance?.players;
            if (players == null || players.Count == 0) return false;

            foreach (var player in players)
            {
                if (PlayerStatsSnapshot.TryFrom(player, out _)) return true;
            }

            return false;
        }

        private static bool IsSandboxMode()
        {
            try
            {
                if (GameModeManager.CurrentHandler is SandboxHandler) return true;
                var id = GameModeManager.CurrentHandlerID;
                if (!string.IsNullOrEmpty(id) &&
                    id.IndexOf("Sandbox", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            catch
            {
                // handler not ready
            }

            return false;
        }

        private void Update()
        {
            if (Unbound.Instance?.canvas == null) return;

            HandleTabToggle();
            HandleHudToggle();
            HandleCompareShortcuts();
            PollHoveredCard();

            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = 0.12f;

            _hud?.Refresh(_compare?.GetLocalBaseline());
            _compare?.Refresh();
            if (_tab != null && _tab.IsOpen) _tab.Refresh();
        }

        private static void HandleTabToggle()
        {
            if (Instance?._tab == null) return;

            if (!Plugin.Configs.EnableStatsTab.Value || !InActiveMatch())
            {
                if (Instance._tab.IsOpen) Instance._tab.SetOpen(false);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Instance._tab.Toggle();
            }
        }

        private static void HandleHudToggle()
        {
            if (!Plugin.Configs.EnableStatsHud.Value || Instance == null) return;
            if (Input.GetKeyDown(Plugin.Configs.StatsHudToggleKey.Value))
            {
                Plugin.Configs.StatsHudVisible.Value = !Plugin.Configs.StatsHudVisible.Value;
            }
        }

        private static void HandleCompareShortcuts()
        {
            if (Instance?._compare == null || !Plugin.Configs.EnableCompactCompare.Value) return;
            if (!TabIsOpen || !InActiveMatch()) return;
            if (Input.GetKeyDown(KeyCode.P)) Instance._compare.PinBaseline();
            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete)) Instance._compare.ResetBaseline();
        }

        private void PollHoveredCard()
        {
            if (!Plugin.Configs.EnableCardHoverPreview.Value)
            {
                ClearPreview();
                return;
            }

            if (CardChoice.instance == null || !CardChoice.instance.IsPicking || !Utils.TakeAllManager.IsLocalPlayersTurn())
            {
                ClearPreview();
                return;
            }

            var spawnedField = AccessTools.Field(typeof(CardChoice), "spawnedCards");
            var spawned = spawnedField?.GetValue(CardChoice.instance) as IList;
            if (spawned == null)
            {
                ClearPreview();
                return;
            }

            CardInfo hovered = null;
            CardInfo visual = null;
            foreach (var item in spawned)
            {
                if (!(item is GameObject go) || go == null) continue;

                var visuals = go.GetComponentInChildren<CardVisuals>(true);
                if (visuals == null) continue;
                if (!Traverse.Create(visuals).Field("isHovered").GetValue<bool>()) continue;

                var cardInfo = go.GetComponent<CardInfo>();
                if (cardInfo == null) continue;
                visual = cardInfo;
                hovered = CardChoice.instance.GetSourceCard(cardInfo) ?? cardInfo.sourceCard ?? cardInfo;
                break;
            }

            if (hovered == null)
            {
                ClearPreview();
                return;
            }

            NotifyHoveredCard(hovered, visual);
        }
    }
}
