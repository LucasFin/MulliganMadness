using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MulliganMadness.Cards;
using Photon.Pun;
using UnboundLib.GameModes;
using UnityEngine;

namespace MulliganMadness.Utils
{
    internal static class DynamiteBlast
    {
        private const string EffectName = "MM_DynamiteCharge";

        internal static void RegisterHooks()
        {
            GameModeManager.AddHook(GameModeHooks.HookGameStart, OnClearLive);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, OnClearLive);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, OnClearLive);
        }

        internal static void Warmup()
        {
            // No-op: charges are spawned on hit, not via ObjectsToSpawn cloning.
        }

        internal static void ApplyToGun(Gun gun)
        {
            // Kept for card SetupCard/OnAddCard call sites; hit patch does the work.
        }

        private static float _lastPlantTime;
        private static Vector3 _lastPlantPos;

        internal static void TryPlantFromHit(ProjectileHit hit)
        {
            if (hit == null) return;
            var owner = hit.ownPlayer;
            if (owner == null || !CurseOwnership.Has(owner, Dynamite.Card)) return;
            if (!OwnerIsMine(hit, owner)) return;

            var pos = hit.transform != null ? hit.transform.position : owner.transform.position;
            if ((pos - _lastPlantPos).sqrMagnitude < 0.05f && Time.time - _lastPlantTime < 0.08f) return;
            _lastPlantTime = Time.time;
            _lastPlantPos = pos;
            SpawnCharge(pos, owner);
        }

        internal static void SpawnCharge(Vector3 position, Player owner)
        {
            var go = new GameObject(EffectName);
            go.transform.position = position;
            var charge = go.AddComponent<DynamiteCharge>();
            charge.Bind(owner);
        }

        private static bool OwnerIsMine(ProjectileHit hit, Player owner)
        {
            try
            {
                var attack = hit.GetComponent<SpawnedAttack>() ?? hit.GetComponentInParent<SpawnedAttack>();
                if (attack != null) return attack.IsMine();
            }
            catch
            {
            }

            if (owner?.data?.view != null) return owner.data.view.IsMine;
            return PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient;
        }

        private static IEnumerator OnClearLive(IGameModeHandler gm)
        {
            ClearLive();
            yield break;
        }

        internal static void ClearLive()
        {
            foreach (var charge in Object.FindObjectsOfType<DynamiteCharge>())
            {
                if (charge != null) Object.Destroy(charge.gameObject);
            }
        }

        internal static void KnockPhysics(Vector2 origin, Player owner)
        {
            var radiusSq = Dynamite.BlastRadius * Dynamite.BlastRadius;
            var seen = new HashSet<int>();
            var ownerData = owner != null ? owner.data : null;

            try
            {
                foreach (var npo in Object.FindObjectsOfType<NetworkPhysicsObject>())
                {
                    if (npo == null) continue;
                    var rb = npo.GetComponent<Rigidbody2D>() ?? npo.GetComponentInChildren<Rigidbody2D>();
                    var pos = rb != null ? rb.worldCenterOfMass : (Vector2)npo.transform.position;
                    var delta = pos - origin;
                    if (delta.sqrMagnitude > radiusSq) continue;
                    var dir = BlastDir(delta);
                    var push = dir * (Dynamite.BlastForce * 0.09f);
                    if (ownerData != null)
                    {
                        npo.RequestOwnership(ownerData);
                        npo.BulletPush(push, pos, ownerData);
                    }
                    else
                    {
                        npo.RPCA_SendForce(push, pos);
                    }

                    if (rb != null) seen.Add(rb.GetInstanceID());
                }
            }
            catch
            {
            }

            var cols = Physics2D.OverlapCircleAll(origin, Dynamite.BlastRadius);
            if (cols == null) return;
            foreach (var col in cols)
            {
                if (col == null) continue;
                if (col.GetComponentInParent<Player>() != null) continue;
                if (col.GetComponentInParent<ProjectileHit>() != null) continue;
                var rb = col.attachedRigidbody;
                if (rb == null) rb = col.GetComponentInParent<Rigidbody2D>();
                if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) continue;
                if (!seen.Add(rb.GetInstanceID())) continue;
                if (rb.GetComponentInParent<NetworkPhysicsObject>() != null) continue;

                var delta = rb.worldCenterOfMass - origin;
                var dir = BlastDir(delta);
                var view = rb.GetComponentInParent<PhotonView>();
                if (view != null && !view.IsMine)
                {
                    try { view.RequestOwnership(); }
                    catch { }
                }

                rb.WakeUp();
                rb.AddForce(dir * Mathf.Clamp(rb.mass * 28f, 18f, 160f), ForceMode2D.Impulse);
            }
        }

        private static Vector2 BlastDir(Vector2 delta)
        {
            var dir = delta.sqrMagnitude < 0.04f ? Vector2.up : delta.normalized;
            return (dir + new Vector2(0f, 0.55f)).normalized;
        }
    }

    internal sealed class DynamiteCharge : MonoBehaviour
    {
        private static Sprite _flash;
        private Player _owner;
        private bool _running;

        internal void Bind(Player owner) => _owner = owner;

        private void Start()
        {
            if (_running) return;
            _running = true;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            var pulse = MakePulse();
            var stick = MakeStick();
            var elapsed = 0f;
            while (elapsed < Dynamite.BlastDelay)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / Dynamite.BlastDelay;
                // Bomb fuse: slow blink early, frantic white flashes near boom.
                var hz = Mathf.Lerp(3.5f, 22f, t * t);
                var on = Mathf.PingPong(elapsed * hz, 1f) > 0.45f;
                var hot = t > 0.55f;

                if (pulse != null)
                {
                    var scale = Mathf.Lerp(0.55f, 1.85f, t);
                    if (on && hot) scale *= 1.18f;
                    pulse.transform.localScale = new Vector3(scale, scale, 1f);
                    var sr = pulse.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        if (on)
                            sr.color = hot ? Color.white : new Color(1f, 0.85f, 0.25f, 1f);
                        else
                            sr.color = new Color(1f, 0.12f, 0.08f, 0.9f);
                    }
                }

                if (stick != null)
                {
                    var sr = stick.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.color = on ? new Color(1f, 0.95f, 0.75f, 1f) : Color.white;
                    stick.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 40f) * (8f + t * 18f));
                }

                yield return null;
            }

            Detonate();
            if (pulse != null) Object.Destroy(pulse);
            if (stick != null) Object.Destroy(stick);
            Object.Destroy(gameObject);
        }

        private void Detonate()
        {
            var origin = (Vector2)transform.position;
            var players = PlayerManager.instance?.players;
            if (players != null)
            {
                foreach (var player in players)
                {
                    if (player?.data?.healthHandler == null) continue;
                    if (_owner != null && player.teamID == _owner.teamID && player.playerID != _owner.playerID) continue;

                    var delta = (Vector2)player.transform.position - origin;
                    if (delta.sqrMagnitude > Dynamite.BlastRadius * Dynamite.BlastRadius) continue;

                    var dir = delta.sqrMagnitude < 0.04f ? Vector2.up : delta.normalized;
                    // Bias hard upward so grounded players actually leave the floor.
                    dir = (dir + new Vector2(0f, 0.85f)).normalized;
                    var force = dir * Dynamite.BlastForce;
                    force.y = Mathf.Max(force.y, Dynamite.BlastForce * 0.55f);
                    player.data.healthHandler.CallTakeForce(
                        force,
                        ForceMode2D.Impulse,
                        true,
                        true,
                        Dynamite.BlastFlying);
                    player.data.healthHandler.CallTakeDamage(
                        dir * Dynamite.BlastDamage,
                        (Vector2)player.transform.position,
                        gameObject,
                        _owner,
                        true);
                }
            }

            DynamiteBlast.KnockPhysics(origin, _owner);
        }

        private GameObject MakePulse()
        {
            var go = new GameObject("MM_DynamitePulse");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FlashSprite();
            sr.color = new Color(1f, 0.18f, 0.12f, 0.95f);
            sr.sortingOrder = 40;
            go.transform.localScale = Vector3.one * 0.55f;
            return go;
        }

        private GameObject MakeStick()
        {
            var mini = CardArtFactory.GetMiniSprite("dynamite");
            if (mini == null) return null;
            var go = new GameObject("MM_DynamiteStick");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one * 0.85f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = mini;
            sr.sortingOrder = 41;
            sr.color = Color.white;
            return go;
        }

        private static Sprite FlashSprite()
        {
            if (_flash != null) return _flash;
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var c = (s - 1) * 0.5f;
            var cream = new Color(1f, 0.93f, 0.82f, 1f);
            var red = new Color(0.93f, 0.12f, 0.14f, 1f);
            for (var y = 0; y < s; y++)
            {
                for (var x = 0; x < s; x++)
                {
                    var nx = (x - c) / (c * 0.92f);
                    var ny = (y - c) / (c * 0.92f);
                    var d = nx * nx + ny * ny;
                    tex.SetPixel(x, y, d > 1f ? Color.clear : (d > 0.72f ? cream : red));
                }
            }

            tex.Apply();
            Object.DontDestroyOnLoad(tex);
            _flash = Sprite.Create(tex, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.5f), 64f);
            return _flash;
        }
    }

    [HarmonyPatch(typeof(ProjectileHit), "Hit")]
    internal static class DynamiteHitPatch
    {
        private static void Postfix(ProjectileHit __instance)
        {
            DynamiteBlast.TryPlantFromHit(__instance);
        }
    }

    [HarmonyPatch(typeof(ProjectileHit), "RPCA_DoHit")]
    internal static class DynamiteRpcHitPatch
    {
        private static void Postfix(ProjectileHit __instance)
        {
            DynamiteBlast.TryPlantFromHit(__instance);
        }
    }
}
