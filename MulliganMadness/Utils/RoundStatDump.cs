using System.Collections;
using System.Globalization;
using System.Text;
using Photon.Pun;
using UnboundLib.GameModes;
using UnityEngine;

namespace MulliganMadness.Utils
{
    /// <summary>
    /// Dumps every player's gun and body stats at each round boundary so a host log and a
    /// client log can be diffed directly.
    ///
    /// This exists because the reported desync — "the host lost projectile velocity and it
    /// never registered on my client" — is invisible in the current logs: nothing records
    /// what the stats actually were on either machine, so there is no way to tell a genuine
    /// stat divergence from a HUD that simply stopped refreshing.
    ///
    /// Each player line ends with sig=, a checksum of every value on that line. Same round,
    /// same player, different sig between two logs means the machines genuinely disagree;
    /// matching sigs mean the stats are fine and the problem is presentation or physics.
    /// </summary>
    internal static class RoundStatDump
    {
        internal static void RegisterHooks()
        {
            GameModeManager.AddHook(GameModeHooks.HookRoundStart, OnRoundStart);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, OnRoundEnd);
            GameModeManager.AddHook(GameModeHooks.HookGameEnd, OnGameEnd);
        }

        private static IEnumerator OnRoundStart(IGameModeHandler gm)
        {
            Diag.Refresh();
            Dump("round-start");
            yield break;
        }

        private static IEnumerator OnRoundEnd(IGameModeHandler gm)
        {
            Diag.Flush("round-end");
            yield break;
        }

        private static IEnumerator OnGameEnd(IGameModeHandler gm)
        {
            Diag.Flush("game-end");
            Diag.FlushTotals("game-end");
            yield break;
        }

        internal static void Dump(string reason)
        {
            if (!Diag.Enabled) return;

            try
            {
                var players = PlayerManager.instance?.players;
                if (players == null) return;

                foreach (var player in players)
                {
                    if (player == null) continue;
                    Plugin.Instance?.Log(Describe(reason, player));
                }
            }
            catch
            {
                // Never let diagnostics take down a round transition.
            }
        }

        private static string Describe(string reason, Player player)
        {
            var sb = new StringBuilder();
            var sig = 17;

            sb.Append("DIAG STATS ").Append(reason).Append(" peer=").Append(Diag.Peer);
            sb.Append(" pid=").Append(SafeInt(() => player.playerID));
            sb.Append(" team=").Append(SafeInt(() => player.teamID));
            sb.Append(" mine=").Append(IsMine(player));

            var gun = SafeGun(player);
            if (gun != null)
            {
                // projectileSpeed and speedMOnBounce are the two that matter most here:
                // the first is muzzle velocity, the second is the multiplier vanilla applies
                // to velocity every time a bullet bounces.
                Add(sb, ref sig, "spd", gun.projectileSpeed);
                Add(sb, ref sig, "bounceM", gun.speedMOnBounce);
                Add(sb, ref sig, "dmgBounceM", gun.dmgMOnBounce);
                Add(sb, ref sig, "simSpd", gun.projectielSimulatonSpeed);
                Add(sb, ref sig, "grav", gun.gravity);
                Add(sb, ref sig, "drag", gun.drag);
                Add(sb, ref sig, "dragMin", gun.dragMinSpeed);
                Add(sb, ref sig, "dmg", gun.damage);
                Add(sb, ref sig, "atk", gun.attackSpeed);
                Add(sb, ref sig, "knock", gun.knockback);
                Add(sb, ref sig, "reflects", gun.reflects);
                Add(sb, ref sig, "smartBounce", gun.smartBounce);
                Add(sb, ref sig, "randBounce", gun.randomBounces);
                Add(sb, ref sig, "nproj", gun.numberOfProjectiles);
                Add(sb, ref sig, "bursts", gun.bursts);
                Add(sb, ref sig, "ammo", gun.ammo);
                Add(sb, ref sig, "projSize", gun.projectileSize);
            }
            else
            {
                sb.Append(" gun=<null>");
            }

            var stats = SafeStats(player);
            if (stats != null)
            {
                Add(sb, ref sig, "hp", stats.health);
                Add(sb, ref sig, "move", stats.movementSpeed);
                Add(sb, ref sig, "jump", stats.jump);
                Add(sb, ref sig, "bodyGrav", stats.gravity);
                Add(sb, ref sig, "size", stats.sizeMultiplier);
                Add(sb, ref sig, "atkMult", stats.attackSpeedMultiplier);
            }
            else
            {
                sb.Append(" stats=<null>");
            }

            sb.Append(" sig=").Append(sig.ToString("X8"));
            return sb.ToString();
        }

        private static void Add(StringBuilder sb, ref int sig, string key, float value)
        {
            // Round before hashing so float jitter below display precision cannot make two
            // otherwise-identical machines look divergent.
            var rounded = Mathf.Round(value * 1000f) / 1000f;
            sb.Append(' ').Append(key).Append('=').Append(rounded.ToString("0.###", CultureInfo.InvariantCulture));
            unchecked { sig = sig * 31 + rounded.GetHashCode(); }
        }

        private static void Add(StringBuilder sb, ref int sig, string key, int value)
        {
            sb.Append(' ').Append(key).Append('=').Append(value.ToString(CultureInfo.InvariantCulture));
            unchecked { sig = sig * 31 + value; }
        }

        private static Gun SafeGun(Player player)
        {
            try { return player?.data?.weaponHandler?.gun; }
            catch { return null; }
        }

        private static CharacterStatModifiers SafeStats(Player player)
        {
            try { return player?.data?.stats; }
            catch { return null; }
        }

        private static string IsMine(Player player)
        {
            try
            {
                if (PhotonNetwork.OfflineMode) return "offline";
                var view = player?.data?.view ?? player?.GetComponent<PhotonView>();
                return view == null ? "?" : (view.IsMine ? "yes" : "no");
            }
            catch
            {
                return "?";
            }
        }

        private static int SafeInt(System.Func<int> get)
        {
            try { return get(); }
            catch { return -1; }
        }
    }
}
