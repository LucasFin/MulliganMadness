using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MulliganMadness.Stats;
using MulliganMadness.Utils;
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
        internal static string HoveredCardName => Instance?._hoveredCard?.cardName;

        internal static bool IsCardPickActive =>
            InPickPhase && CardChoice.instance != null && CardChoice.instance.IsPicking;

        private StatsHudPanel _hud;
        private StatsTabOverlay _tab;
        private CardInfo _hoveredCard;
        private CardInfo _hoveredVisual;
        private PlayerStatsSnapshot _previewDelta;
        private readonly Dictionary<int, PlayerStatsSnapshot> _pickBaselines = new Dictionary<int, PlayerStatsSnapshot>();
        private float _refreshTimer;
        private static float _readyCacheUntil;
        private static bool _readyCache;
        private static readonly FieldInfo SpawnedCardsField = AccessTools.Field(typeof(CardChoice), "spawnedCards");
        private static readonly FieldInfo IsHoveredField = AccessTools.Field(typeof(CardVisuals), "isHovered");
        private static readonly FieldInfo IsSelectedField = AccessTools.Field(typeof(CardVisuals), "isSelected");
        private static readonly FieldInfo CurrentlySelectedCardField = AccessTools.Field(typeof(CardChoice), "currentlySelectedCard");
        private static readonly PropertyInfo CurrentlySelectedCardProp = AccessTools.Property(typeof(CardChoice), "currentlySelectedCard");

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _hud = gameObject.AddComponent<StatsHudPanel>();
            _tab = gameObject.AddComponent<StatsTabOverlay>();
        }

        internal static void RegisterHooks()
        {
            GameModeManager.AddHook(GameModeHooks.HookGameStart, OnGameStart);
            GameModeManager.AddHook(GameModeHooks.HookGameEnd, OnGameEnd);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, OnPickStart);
            GameModeManager.AddHook(GameModeHooks.HookPickEnd, OnPickEnd);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickStart, OnPlayerPickStart);
            GameModeManager.AddHook(GameModeHooks.HookRoundStart, OnRoundStart);
            GameModeManager.AddHook(GameModeHooks.HookPointStart, OnPointStart);
            GameModeManager.AddHook(GameModeHooks.HookBattleStart, OnBattleStart);
        }

        private static IEnumerator OnGameStart(IGameModeHandler gm)
        {
            MatchSessionActive = true;
            CurrentRound = 0;
            CurrentPoint = 0;
            InPickPhase = false;
            InBattlePhase = false;
            Instance?._tab?.SetOpen(false);
            Instance?.ClearPickBaselines();
            yield break;
        }

        private static IEnumerator OnGameEnd(IGameModeHandler gm)
        {
            MatchSessionActive = false;
            InPickPhase = false;
            InBattlePhase = false;
            Instance?._tab?.SetOpen(false);
            Instance?.ClearPickBaselines();
            yield break;
        }

        private static IEnumerator OnPickStart(IGameModeHandler gm)
        {
            InPickPhase = true;
            InBattlePhase = false;
            Instance?.CaptureAllPickBaselines();
            yield break;
        }

        private static IEnumerator OnPickEnd(IGameModeHandler gm)
        {
            InPickPhase = false;
            Instance?.ClearPreview();
            yield break;
        }

        private static IEnumerator OnPlayerPickStart(IGameModeHandler gm)
        {
            Instance?.CapturePickBaseline();
            Instance?.ClearPreview();
            yield return null;
            Instance?.CapturePickBaseline();
        }

        private static IEnumerator OnRoundStart(IGameModeHandler gm)
        {
            CurrentRound += 1;
            CurrentPoint = 0;
            InPickPhase = false;
            yield break;
        }

        private static IEnumerator OnPointStart(IGameModeHandler gm)
        {
            CurrentPoint += 1;
            yield break;
        }

        private static IEnumerator OnBattleStart(IGameModeHandler gm)
        {
            InBattlePhase = true;
            InPickPhase = false;
            yield break;
        }

        private void CapturePickBaseline()
        {
            var picker = TakeAllManager.GetCurrentPicker() ?? PlayerStatsSnapshot.LocalPlayer();
            if (picker == null || !PlayerStatsSnapshot.TryFrom(picker, out var snap)) return;
            _pickBaselines[picker.playerID] = snap;
        }

        private void CaptureAllPickBaselines()
        {
            foreach (var player in PlayerStatsSnapshot.ActivePlayers())
            {
                if (PlayerStatsSnapshot.TryFrom(player, out var snap))
                {
                    _pickBaselines[player.playerID] = snap;
                }
            }
        }

        private void CaptureMissingPickBaselines()
        {
            foreach (var player in PlayerStatsSnapshot.ActivePlayers())
            {
                if (_pickBaselines.ContainsKey(player.playerID)) continue;
                if (PlayerStatsSnapshot.TryFrom(player, out var snap))
                {
                    _pickBaselines[player.playerID] = snap;
                }
            }
        }

        private void ClearPickBaselines() => _pickBaselines.Clear();

        private PlayerStatsSnapshot GetPickBaselineFor(Player player)
        {
            if (player == null) return null;
            return _pickBaselines.TryGetValue(player.playerID, out var snap) ? snap : null;
        }

        internal static Player GetHudPlayer()
        {
            if (InPickPhase && CardChoice.instance != null && CardChoice.instance.IsPicking)
            {
                return TakeAllManager.GetCurrentPicker() ?? PlayerStatsSnapshot.LocalPlayer();
            }

            return PlayerStatsSnapshot.LocalPlayer();
        }

        internal static bool IsWatchingOtherPicker()
        {
            if (!InPickPhase || CardChoice.instance == null || !CardChoice.instance.IsPicking) return false;
            var picker = TakeAllManager.GetCurrentPicker();
            var local = PlayerStatsSnapshot.LocalPlayer();
            if (picker == null || local == null) return false;
            return picker.playerID != local.playerID;
        }

        internal void NotifyHoveredCard(Player picker, CardInfo cardInfo, CardInfo pickVisual = null)
        {
            if (picker == null || cardInfo == null)
            {
                ClearPreview();
                return;
            }

            if (_hoveredCard == cardInfo && _hoveredVisual == pickVisual && _previewDelta != null) return;
            _hoveredCard = cardInfo;
            _hoveredVisual = pickVisual;

            if (CardStatPreview.TryPreview(picker, cardInfo, out var delta, pickVisual))
            {
                _previewDelta = delta;
            }
            else
            {
                _previewDelta = null;
            }

            _hud?.SetPreviewDelta(_previewDelta);
            _refreshTimer = 0f;
        }

        internal void ClearPreview()
        {
            var had = _hoveredCard != null || _previewDelta != null;
            _hoveredCard = null;
            _hoveredVisual = null;
            _previewDelta = null;
            _hud?.SetPreviewDelta(null);
            if (had) _refreshTimer = 0f;
        }

        internal static bool InActiveMatch()
        {
            if (!HasReadyPlayers()) return false;
            if (MatchSessionActive) return true;
            return IsSandboxMode();
        }

        private static bool HasReadyPlayers()
        {
            if (Time.unscaledTime < _readyCacheUntil) return _readyCache;

            var players = PlayerManager.instance?.players;
            var ready = false;
            if (players != null && players.Count > 0)
            {
                foreach (var player in players)
                {
                    if (player?.data?.weaponHandler?.gun != null && player.data.block != null)
                    {
                        ready = true;
                        break;
                    }
                }
            }

            _readyCache = ready;
            _readyCacheUntil = Time.unscaledTime + 0.25f;
            return ready;
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

        internal void RebuildHud() => _hud?.Rebuild();

        internal void RebuildTab() => _tab?.RebuildLayout();

        private void Update()
        {
            HandleTabToggle();
            HandleHudToggle();
            _tab?.HandleShortcuts();
            PollHoveredCard();
            PingTracker.Tick();

            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = _tab != null && _tab.IsOpen ? 0.25f : 0.2f;

            if (InPickPhase) CaptureMissingPickBaselines();

            var hudPlayer = GetHudPlayer();
            _hud?.Refresh(
                GetPickBaselineFor(hudPlayer),
                hudPlayer,
                IsWatchingOtherPicker());
            if (_tab != null && _tab.IsOpen) _tab.Refresh();
        }

        private static void HandleTabToggle()
        {
            if (Instance?._tab == null) return;

            if (!InActiveMatch())
            {
                if (Instance._tab.IsOpen) Instance._tab.SetOpen(false);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Instance._tab.Toggle();
            }

            if (Input.GetKeyDown(KeyCode.Escape) && Instance._tab.IsOpen)
            {
                Instance._tab.SetOpen(false);
            }
        }

        internal static void HandleHudToggle()
        {
            if (Instance == null) return;
            if (Input.GetKeyDown(KeyCode.O))
            {
                Plugin.Configs.StatsHudVisible.Value = !Plugin.Configs.StatsHudVisible.Value;
            }
        }

        private void PollHoveredCard()
        {
            if (!InPickPhase)
            {
                if (_previewDelta != null) ClearPreview();
                return;
            }

            if (CardChoice.instance == null || !CardChoice.instance.IsPicking)
            {
                if (_previewDelta != null) ClearPreview();
                return;
            }

            var picker = TakeAllManager.GetCurrentPicker();
            if (picker == null)
            {
                if (_previewDelta != null) ClearPreview();
                return;
            }

            if (!TryGetHighlightedCard(out var hovered, out var visual))
            {
                if (_previewDelta != null || _hoveredCard != null) ClearPreview();
                return;
            }

            NotifyHoveredCard(picker, hovered, visual);
        }

        private static bool TryGetHighlightedCard(out CardInfo source, out CardInfo visual)
        {
            source = null;
            visual = null;
            var choice = CardChoice.instance;
            if (choice == null) return false;

            var selected = CurrentlySelectedCardField?.GetValue(choice) ?? CurrentlySelectedCardProp?.GetValue(choice);
            if (TryResolveCard(choice, selected, out source, out visual)) return true;

            var spawned = SpawnedCardsField?.GetValue(choice) as IList;
            if (spawned == null) return false;

            CardInfo scaledSource = null;
            CardInfo scaledVisual = null;
            var bestScale = 1.04f;
            foreach (var item in spawned)
            {
                if (!(item is GameObject go) || go == null) continue;
                var cardInfo = go.GetComponent<CardInfo>();
                if (cardInfo == null) continue;

                var visuals = go.GetComponentInChildren<CardVisuals>(true);
                if (visuals != null && (ReadFlag(visuals, IsHoveredField) || ReadFlag(visuals, IsSelectedField)))
                {
                    visual = cardInfo;
                    source = choice.GetSourceCard(cardInfo) ?? cardInfo.sourceCard ?? cardInfo;
                    return source != null;
                }

                var scale = go.transform.localScale.x;
                if (scale < bestScale) continue;
                bestScale = scale;
                scaledVisual = cardInfo;
                scaledSource = choice.GetSourceCard(cardInfo) ?? cardInfo.sourceCard ?? cardInfo;
            }

            if (scaledSource == null) return false;
            source = scaledSource;
            visual = scaledVisual;
            return true;
        }

        private static bool TryResolveCard(CardChoice choice, object raw, out CardInfo source, out CardInfo visual)
        {
            source = null;
            visual = null;
            if (raw == null) return false;

            GameObject go = raw as GameObject;
            if (raw is CardInfo info)
            {
                visual = info;
                go ??= info.gameObject;
            }

            if (visual == null && go != null)
            {
                visual = go.GetComponent<CardInfo>() ?? go.GetComponentInChildren<CardInfo>();
            }

            if (visual == null) return false;
            source = choice.GetSourceCard(visual) ?? visual.sourceCard ?? visual;
            return source != null;
        }

        private static bool ReadFlag(CardVisuals visuals, FieldInfo field)
        {
            if (field == null || visuals == null) return false;
            try
            {
                return field.GetValue(visuals) is true;
            }
            catch
            {
                return false;
            }
        }
    }
}
