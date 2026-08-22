using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MulliganMadness.Cards;
using Photon.Pun;
using UnboundLib.GameModes;
using UnityEngine;
using CardsApi = ModdingUtils.Utils.Cards;

namespace MulliganMadness.Utils
{
    internal static class DynamiteBlast
    {
        private const string EffectName = "MM_Dynamite";
        private static ObjectsToSpawn[] _spawns;
        private static bool _built;
        private static bool _usedFallback;
        private static readonly HashSet<GameObject> Templates = new HashSet<GameObject>();
        internal static GameObject Template;

        internal static void RegisterHooks()
        {
            GameModeManager.AddHook(GameModeHooks.HookGameStart, OnClearLive);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, OnClearLive);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, OnClearLive);
        }

        internal static void Warmup()
        {
            EnsureBuilt();
            if (Dynamite.Card == null) return;
            var gun = Dynamite.Card.GetComponent<Gun>() ?? Dynamite.Card.GetComponentInChildren<Gun>(true);
            ReplaceOnGun(gun);
        }

        internal static void ApplyToGun(Gun gun)
        {
            EnsureBuilt();
            if (gun == null || _spawns == null || _spawns.Length == 0) return;
            if (HasOurs(gun)) return;
            AddOurs(gun);
        }

        internal static void ReplaceOnGun(Gun gun)
        {
            EnsureBuilt();
            if (gun == null || _spawns == null || _spawns.Length == 0) return;
            RemoveOurs(gun);
            AddOurs(gun);
        }

        private static void AddOurs(Gun gun)
        {
            var list = gun.objectsToSpawn != null
                ? new List<ObjectsToSpawn>(gun.objectsToSpawn)
                : new List<ObjectsToSpawn>();
            list.AddRange(_spawns);
            gun.objectsToSpawn = list.ToArray();
        }

        private static bool HasOurs(Gun gun)
        {
            if (gun?.objectsToSpawn == null) return false;
            foreach (var spawn in gun.objectsToSpawn)
            {
                if (IsOurs(spawn)) return true;
            }

            return false;
        }

        private static void RemoveOurs(Gun gun)
        {
            if (gun?.objectsToSpawn == null) return;
            var kept = new List<ObjectsToSpawn>();
            foreach (var spawn in gun.objectsToSpawn)
            {
                if (!IsOurs(spawn)) kept.Add(spawn);
            }

            gun.objectsToSpawn = kept.ToArray();
        }

        private static bool IsOurs(ObjectsToSpawn spawn)
        {
            return spawn?.effect != null
                   && spawn.effect.name.IndexOf(EffectName, StringComparison.Ordinal) >= 0;
        }

        private static void EnsureBuilt()
        {
            if (_built && _spawns != null && _spawns.Length > 0 && !_usedFallback) return;

            var vanilla = BuildFromTimedDetonation();
            if (vanilla != null)
            {
                _spawns = vanilla;
                _usedFallback = false;
                _built = true;
                return;
            }

            if (_built && _spawns != null && _spawns.Length > 0) return;
            _spawns = new[] { BuildFallback() };
            _usedFallback = true;
            _built = true;
        }

        private static ObjectsToSpawn[] BuildFromTimedDetonation()
        {
            var sourceGun = FindTimedDetonationGun();
            if (sourceGun?.objectsToSpawn == null || sourceGun.objectsToSpawn.Length == 0) return null;

            var copies = new List<ObjectsToSpawn>();
            foreach (var source in sourceGun.objectsToSpawn)
            {
                if (source == null) continue;
                var copy = CloneSpawn(source);
                if (source.effect != null)
                {
                    copy.effect = TuneEffect(source.effect);
                }

                copy.scaleFromDamage = 0f;
                copy.scaleStacks = false;
                copies.Add(copy);
            }

            return copies.Count > 0 ? copies.ToArray() : null;
        }

        private static Gun FindTimedDetonationGun()
        {
            CardInfo info = null;
            try
            {
                info = CardsApi.instance?.GetCardWithName("Timed Detonation");
            }
            catch
            {
                info = null;
            }

            if (info == null)
            {
                var all = CardsApi.all;
                if (all != null)
                {
                    foreach (var card in all)
                    {
                        if (IsTimedDetonation(card))
                        {
                            info = card;
                            break;
                        }
                    }
                }
            }

            if (info == null)
            {
                foreach (var card in Resources.FindObjectsOfTypeAll<CardInfo>())
                {
                    if (!IsTimedDetonation(card)) continue;
                    info = card;
                    break;
                }
            }

            return info == null ? null : info.GetComponent<Gun>() ?? info.GetComponentInChildren<Gun>(true);
        }

        private static bool IsTimedDetonation(CardInfo card)
        {
            if (card == null) return false;
            if (!string.IsNullOrEmpty(card.cardName)
                && card.cardName.IndexOf("Timed Detonation", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            var objectName = card.gameObject != null ? card.gameObject.name : "";
            return objectName.IndexOf("TimedDetonation", StringComparison.OrdinalIgnoreCase) >= 0
                   || objectName.IndexOf("Timed Detonation", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static GameObject TuneEffect(GameObject source)
        {
            var holder = new GameObject(EffectName + "_Hold");
            holder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(holder);

            var effect = UnityEngine.Object.Instantiate(source, holder.transform);
            effect.name = EffectName;
            StripPhoton(effect);

            foreach (var explosion in effect.GetComponentsInChildren<Explosion>(true))
            {
                explosion.auto = false;
                explosion.range = Dynamite.BlastRadius;
                explosion.damage = 0.18f;
                explosion.force = Dynamite.BlastForce;
                explosion.forceIgnoreMass = true;
                explosion.flyingFor = Dynamite.BlastFlying;
                explosion.scaleDmg = false;
                explosion.scaleRadius = false;
                explosion.scaleForce = false;
                explosion.objectForceMultiplier = 1f;
                explosion.ignoreWalls = true;
                explosion.ignoreTeam = true;
            }

            foreach (var delay in effect.GetComponentsInChildren<DelayEvent>(true))
            {
                delay.auto = false;
            }

            if (effect.GetComponent<DynamiteCharge>() == null)
            {
                effect.AddComponent<DynamiteCharge>();
            }

            NoteTemplate(effect);
            holder.SetActive(true);
            return effect;
        }

        private static void StripPhoton(GameObject go)
        {
            if (go == null) return;
            foreach (var view in go.GetComponentsInChildren<PhotonView>(true))
            {
                if (view != null) UnityEngine.Object.DestroyImmediate(view);
            }
        }

        private static ObjectsToSpawn CloneSpawn(ObjectsToSpawn source)
        {
            var copy = new ObjectsToSpawn();
            foreach (var field in typeof(ObjectsToSpawn).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                field.SetValue(copy, field.GetValue(source));
            }

            return copy;
        }

        private static ObjectsToSpawn BuildFallback()
        {
            var holder = new GameObject(EffectName + "_Hold");
            holder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(holder);

            var effect = new GameObject(EffectName);
            effect.transform.SetParent(holder.transform, false);
            effect.AddComponent<DynamiteCharge>();
            NoteTemplate(effect);
            holder.SetActive(true);

            return new ObjectsToSpawn
            {
                effect = effect,
                spawnOn = ObjectsToSpawn.SpawnOn.all,
                numberOfSpawns = 1,
                spawnAsChild = false,
                stickToAllTargets = true,
                stickToBigTargets = true,
                zeroZ = true,
                scaleFromDamage = 0f,
                scaleStacks = false
            };
        }

        private static IEnumerator OnClearLive(IGameModeHandler gm)
        {
            ClearLive();
            yield break;
        }

        internal static bool IsTemplate(GameObject go)
        {
            if (go == null) return false;
            if (Templates.Contains(go)) return true;
            var parent = go.transform != null ? go.transform.parent : null;
            return parent != null && parent.name.IndexOf(EffectName + "_Hold", StringComparison.Ordinal) >= 0;
        }

        private static void NoteTemplate(GameObject effect)
        {
            Template = effect;
            if (effect != null) Templates.Add(effect);
        }

        internal static void ClearLive()
        {
            var found = UnityEngine.Object.FindObjectsOfType<DynamiteCharge>();
            foreach (var charge in found)
            {
                if (charge == null) continue;
                if (IsTemplate(charge.gameObject)) continue;
                UnityEngine.Object.Destroy(charge.gameObject);
            }
        }
    }

    internal sealed class DynamiteCharge : MonoBehaviour
    {
        private static Sprite _flash;
        private bool _running;

        private void Start()
        {
            if (DynamiteBlast.IsTemplate(gameObject)) return;
            if (_running) return;
            _running = true;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            var pulse = MakePulse();
            var elapsed = 0f;
            while (elapsed < Dynamite.BlastDelay)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / Dynamite.BlastDelay;
                if (pulse != null)
                {
                    var scale = Mathf.Lerp(0.35f, 1.15f, t);
                    pulse.transform.localScale = new Vector3(scale, scale, 1f);
                    var sr = pulse.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        var flash = t > 0.7f && Mathf.PingPong(elapsed * 12f, 1f) > 0.5f;
                        sr.color = flash ? Color.white : new Color(1f, 0.18f, 0.12f, 0.95f);
                    }
                }

                yield return null;
            }

            Detonate();
            if (pulse != null) UnityEngine.Object.Destroy(pulse);
            UnityEngine.Object.Destroy(gameObject);
        }

        private void Detonate()
        {
            var explosion = GetComponent<Explosion>() ?? GetComponentInChildren<Explosion>(true);
            if (explosion != null)
            {
                explosion.auto = false;
                var attack = GetComponent<SpawnedAttack>() ?? GetComponentInParent<SpawnedAttack>();
                AccessTools.Field(typeof(Explosion), "spawned")?.SetValue(explosion, attack);
                explosion.Explode();
                return;
            }

            if (!ShouldApply()) return;
            ApplyFallbackBlast();
        }

        private void ApplyFallbackBlast()
        {
            var origin = (Vector2)transform.position;
            var players = PlayerManager.instance?.players;
            if (players == null) return;

            var owner = Owner();
            foreach (var player in players)
            {
                if (player?.data?.healthHandler == null) continue;
                if (owner != null && player.teamID == owner.teamID && player.playerID != owner.playerID) continue;

                var delta = (Vector2)player.transform.position - origin;
                if (delta.sqrMagnitude > Dynamite.BlastRadius * Dynamite.BlastRadius) continue;

                var dir = delta.sqrMagnitude < 0.04f ? Vector2.up : delta.normalized;
                dir = (dir + new Vector2(0f, 0.35f)).normalized;
                player.data.healthHandler.CallTakeForce(
                    dir * Dynamite.BlastForce,
                    ForceMode2D.Impulse,
                    true,
                    true,
                    Dynamite.BlastFlying);
                player.data.healthHandler.CallTakeDamage(dir * Dynamite.BlastDamage, (Vector2)player.transform.position, gameObject, owner, true);
            }
        }

        private bool ShouldApply()
        {
            var attack = GetComponent<SpawnedAttack>() ?? GetComponentInParent<SpawnedAttack>();
            if (attack != null)
            {
                try { return attack.IsMine(); }
                catch { /* view missing */ }
            }

            var owner = Owner();
            if (owner?.data?.view != null) return owner.data.view.IsMine;
            return PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient;
        }

        private Player Owner()
        {
            var attack = GetComponent<SpawnedAttack>() ?? GetComponentInParent<SpawnedAttack>();
            return attack != null ? attack.spawner : null;
        }

        private GameObject MakePulse()
        {
            if (GetComponentInChildren<Explosion>(true) != null) return null;

            var go = new GameObject("MM_DynamitePulse");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FlashSprite();
            sr.color = new Color(1f, 0.18f, 0.12f, 0.95f);
            sr.sortingOrder = 40;
            go.transform.localScale = Vector3.one * 0.35f;
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
            UnityEngine.Object.DontDestroyOnLoad(tex);
            _flash = Sprite.Create(tex, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.5f), 64f);
            return _flash;
        }
    }
}
