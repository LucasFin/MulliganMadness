using System;
using System.Globalization;
using Photon.Pun;
using UnityEngine;

namespace MulliganMadness.Utils
{
    public enum TakeAllMode
    {
        Disabled = 0,
        OncePerGame = 1,
        MultiUse = 2,
        Vote = 3
    }

    public enum TakeAllCurseOnExisting
    {
        ReplaceExisting = 0,
        SkipCurse = 1
    }

    [Serializable]
    public sealed class SessionSettingsData
    {
        public const int SchemaVersion = 2;

        public TakeAllMode TakeAllMode = TakeAllMode.OncePerGame;
        public int TakeAllUsesPerPlayer = 1;
        public float VoteThreshold = 0.5f;
        public float VoteTimeoutSeconds = 15f;
        public bool VoteConsumesUse = true;
        public bool TakeAllCurseCost;
        public TakeAllCurseOnExisting CurseOnExisting = TakeAllCurseOnExisting.ReplaceExisting;
        public bool EnableMercyVote;
        public int MercyRoundDeficit = 2;
        public bool MercyOncePerGame = true;
        public bool FixPristineHealth = true;
        public bool EnableAutoPickCurses = true;
        public float PanicTimerSeconds = 3f;
        public bool EnableThiefCard = true;
        public bool EnableTakebacksies = true;
        public bool EnableSandbagSimulator = true;
        public bool EnableJarOfDirt = true;
        public bool SandbagOncePerGame = true;

        public bool EnableTakeAll => TakeAllMode != TakeAllMode.Disabled && TakeAllUsesPerPlayer > 0;

        public static SessionSettingsData CreateDefault()
        {
            var data = new SessionSettingsData();
            if (Plugin.Configs != null) data.LoadFromConfigDefaults(Plugin.Configs);
            return data;
        }

        public void LoadFromConfigDefaults(Configs cfg)
        {
            if (cfg == null) return;
            TakeAllMode = cfg.DefaultTakeAllMode.Value;
            TakeAllUsesPerPlayer = cfg.DefaultTakeAllUsesPerPlayer.Value;
            VoteThreshold = cfg.DefaultVoteThreshold.Value;
            VoteTimeoutSeconds = cfg.DefaultVoteTimeoutSeconds.Value;
            VoteConsumesUse = cfg.DefaultVoteConsumesUse.Value;
            TakeAllCurseCost = cfg.DefaultTakeAllCurseCost.Value;
            CurseOnExisting = cfg.DefaultCurseOnExisting.Value;
            FixPristineHealth = cfg.FixPristineHealth.Value;
            EnableAutoPickCurses = cfg.EnableAutoPickCurses.Value;
            PanicTimerSeconds = cfg.PanicTimerSeconds.Value;
            EnableThiefCard = cfg.EnableThiefCard.Value;
            EnableTakebacksies = cfg.EnableTakebacksies.Value;
            EnableSandbagSimulator = cfg.EnableSandbagSimulator.Value;
            EnableJarOfDirt = cfg.EnableJarOfDirt.Value;
            SandbagOncePerGame = cfg.SandbagOncePerGame.Value;
            EnableMercyVote = cfg.DefaultEnableMercyVote.Value;
            MercyRoundDeficit = cfg.DefaultMercyRoundDeficit.Value;
            MercyOncePerGame = cfg.DefaultMercyOncePerGame.Value;
        }

        public void SaveToConfigDefaults(Configs cfg)
        {
            if (cfg == null) return;
            cfg.DefaultTakeAllMode.Value = TakeAllMode;
            cfg.DefaultTakeAllUsesPerPlayer.Value = TakeAllUsesPerPlayer;
            cfg.DefaultVoteThreshold.Value = VoteThreshold;
            cfg.DefaultVoteTimeoutSeconds.Value = VoteTimeoutSeconds;
            cfg.DefaultVoteConsumesUse.Value = VoteConsumesUse;
            cfg.DefaultTakeAllCurseCost.Value = TakeAllCurseCost;
            cfg.DefaultCurseOnExisting.Value = CurseOnExisting;
            cfg.FixPristineHealth.Value = FixPristineHealth;
            cfg.EnableAutoPickCurses.Value = EnableAutoPickCurses;
            cfg.PanicTimerSeconds.Value = PanicTimerSeconds;
            cfg.EnableThiefCard.Value = EnableThiefCard;
            cfg.EnableTakebacksies.Value = EnableTakebacksies;
            cfg.EnableSandbagSimulator.Value = EnableSandbagSimulator;
            cfg.EnableJarOfDirt.Value = EnableJarOfDirt;
            cfg.SandbagOncePerGame.Value = SandbagOncePerGame;
            cfg.DefaultEnableMercyVote.Value = EnableMercyVote;
            cfg.DefaultMercyRoundDeficit.Value = MercyRoundDeficit;
            cfg.DefaultMercyOncePerGame.Value = MercyOncePerGame;
        }

        public string Serialize()
        {
            return string.Join("|",
                SchemaVersion.ToString(CultureInfo.InvariantCulture),
                ((int)TakeAllMode).ToString(CultureInfo.InvariantCulture),
                TakeAllUsesPerPlayer.ToString(CultureInfo.InvariantCulture),
                VoteThreshold.ToString(CultureInfo.InvariantCulture),
                VoteTimeoutSeconds.ToString(CultureInfo.InvariantCulture),
                VoteConsumesUse ? "1" : "0",
                TakeAllCurseCost ? "1" : "0",
                ((int)CurseOnExisting).ToString(CultureInfo.InvariantCulture),
                FixPristineHealth ? "1" : "0",
                EnableAutoPickCurses ? "1" : "0",
                PanicTimerSeconds.ToString(CultureInfo.InvariantCulture),
                EnableThiefCard ? "1" : "0",
                EnableTakebacksies ? "1" : "0",
                EnableSandbagSimulator ? "1" : "0",
                EnableJarOfDirt ? "1" : "0",
                SandbagOncePerGame ? "1" : "0",
                EnableMercyVote ? "1" : "0",
                MercyRoundDeficit.ToString(CultureInfo.InvariantCulture),
                MercyOncePerGame ? "1" : "0");
        }

        public static SessionSettingsData Deserialize(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return CreateDefault();
            var parts = payload.Split('|');
            if (parts.Length < 16) return CreateDefault();

            var data = new SessionSettingsData();
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var version) || version < 1)
            {
                return CreateDefault();
            }

            data.TakeAllMode = (TakeAllMode)Mathf.Clamp(ParseInt(parts[1], 1), 0, 3);
            data.TakeAllUsesPerPlayer = Mathf.Clamp(ParseInt(parts[2], 1), 0, 3);
            data.VoteThreshold = Mathf.Clamp(ParseFloat(parts[3], 0.5f), 0.01f, 1f);
            data.VoteTimeoutSeconds = Mathf.Clamp(ParseFloat(parts[4], 15f), 5f, 60f);
            data.VoteConsumesUse = parts[5] != "0";
            data.TakeAllCurseCost = parts[6] != "0";
            data.CurseOnExisting = (TakeAllCurseOnExisting)Mathf.Clamp(ParseInt(parts[7], 0), 0, 1);
            data.FixPristineHealth = parts[8] != "0";
            data.EnableAutoPickCurses = parts[9] != "0";
            data.PanicTimerSeconds = Mathf.Clamp(ParseFloat(parts[10], 3f), 1f, 10f);
            data.EnableThiefCard = parts[11] != "0";
            data.EnableTakebacksies = parts[12] != "0";
            data.EnableSandbagSimulator = parts[13] != "0";
            data.EnableJarOfDirt = parts[14] != "0";
            data.SandbagOncePerGame = parts[15] != "0";
            if (parts.Length >= 19 && version >= 2)
            {
                data.EnableMercyVote = parts[16] != "0";
                data.MercyRoundDeficit = Mathf.Clamp(ParseInt(parts[17], 2), 1, 10);
                data.MercyOncePerGame = parts[18] != "0";
            }

            return data;
        }

        private static int ParseInt(string value, int fallback) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

        private static float ParseFloat(string value, float fallback) =>
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }

    public static class SessionSettings
    {
        public static SessionSettingsData Current { get; private set; } = SessionSettingsData.CreateDefault();

        public static bool IsHost =>
            PhotonNetwork.OfflineMode || !PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient;

        public static bool CanEditSession => IsHost;

        public static void InitializeFromConfig()
        {
            Current = SessionSettingsData.CreateDefault();
        }

        public static void ApplyFromNetwork(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return;
            Current = SessionSettingsData.Deserialize(payload);
            Plugin.Instance?.Log("Session settings synced from host.");
        }

        public static void SetHostSession(SessionSettingsData data, bool broadcast)
        {
            if (!CanEditSession || data == null) return;
            Current = data;
            Current.SaveToConfigDefaults(Plugin.Configs);
            if (broadcast) SessionSettingsSync.BroadcastIfHost();
        }
    }
}
