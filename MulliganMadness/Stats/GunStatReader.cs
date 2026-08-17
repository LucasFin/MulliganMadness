using System;
using HarmonyLib;
using UnityEngine;

namespace MulliganMadness.Stats
{
    internal static class GunStatReader
    {
        internal static float ComputeDamage(Gun gun)
        {
            if (gun == null) return 0f;

            try
            {
                if (gun.projectiles != null && gun.projectiles.Length > 0)
                {
                    var spawn = gun.projectiles[0]?.objectToSpawn;
                    var hit = spawn != null ? spawn.GetComponent<ProjectileHit>() : null;
                    if (hit != null)
                    {
                        return gun.damage * gun.bulletDamageMultiplier * hit.damage;
                    }
                }
            }
            catch
            {
                // Fall back to Infoholic-style scaling below.
            }

            return gun.damage * 55f * gun.bulletDamageMultiplier;
        }

        internal static float ComputeReloadSeconds(GunAmmo ammo)
        {
            if (ammo == null) return float.NaN;

            try
            {
                var reloadMethod = AccessTools.Method(typeof(GunAmmo), "ReloadTime");
                if (reloadMethod != null)
                {
                    var value = reloadMethod.Invoke(ammo, null);
                    if (value is float f) return f;
                }
            }
            catch
            {
                // Fall back to composed fields below.
            }

            return (ammo.reloadTime + ammo.reloadTimeAdd) * ammo.reloadTimeMultiplier;
        }

        internal static bool LowerIsBetter(string key)
        {
            switch (key)
            {
                case "BlockCD":
                case "AttackSPD":
                case "Reload":
                case "BulletSlow":
                    return true;
                default:
                    return false;
            }
        }

        internal static bool IsPositiveChange(string key, float diff)
        {
            if (Mathf.Abs(diff) < 0.005f) return false;
            return LowerIsBetter(key) ? diff < 0f : diff > 0f;
        }
    }

    internal sealed class PlayerStatRawBackup
    {
        public float Health;
        public float MaxHealth;
        public float MovementSpeed;
        public float Jump;
        public float SizeMultiplier;
        public float LifeSteal;
        public int Respawns;

        public float GunDamage;
        public float BulletDamageMultiplier;
        public float Knockback;
        public float DamageAfterDistanceMultiplier;
        public float Slow;
        public float AttackSpeed;
        public float AttackSpeedMultiplier;
        public float ProjectileSpeed;
        public float ProjectileSimulationSpeed;
        public int NumberOfProjectiles;
        public float DestroyBulletAfter;
        public int Reflects;
        public int Bursts;
        public float Gravity;

        public int AdditionalBlocks;

        public int MaxAmmo;
        public float ReloadTime;
        public float ReloadTimeAdd;
        public float ReloadTimeMultiplier;

        public static PlayerStatRawBackup Capture(Player player)
        {
            var gun = player.data.weaponHandler.gun;
            var block = player.data.block;
            var data = player.data;
            var ammo = gun.GetComponentInChildren<GunAmmo>();

            return new PlayerStatRawBackup
            {
                Health = data.health,
                MaxHealth = data.maxHealth,
                MovementSpeed = data.stats.movementSpeed,
                Jump = data.stats.jump,
                SizeMultiplier = data.stats.sizeMultiplier,
                LifeSteal = data.stats.lifeSteal,
                Respawns = data.stats.respawns,
                GunDamage = gun.damage,
                BulletDamageMultiplier = gun.bulletDamageMultiplier,
                Knockback = gun.knockback,
                DamageAfterDistanceMultiplier = gun.damageAfterDistanceMultiplier,
                Slow = gun.slow,
                AttackSpeed = gun.attackSpeed,
                AttackSpeedMultiplier = gun.attackSpeedMultiplier,
                ProjectileSpeed = gun.projectileSpeed,
                ProjectileSimulationSpeed = gun.projectielSimulatonSpeed,
                NumberOfProjectiles = gun.numberOfProjectiles,
                DestroyBulletAfter = gun.destroyBulletAfter,
                Reflects = gun.reflects,
                Bursts = gun.bursts,
                Gravity = gun.gravity,
                AdditionalBlocks = block.additionalBlocks,
                MaxAmmo = ammo?.maxAmmo ?? 0,
                ReloadTime = ammo?.reloadTime ?? 0f,
                ReloadTimeAdd = ammo?.reloadTimeAdd ?? 0f,
                ReloadTimeMultiplier = ammo?.reloadTimeMultiplier ?? 1f
            };
        }

        public void Apply(Player player)
        {
            var gun = player.data.weaponHandler.gun;
            var block = player.data.block;
            var data = player.data;
            var ammo = gun.GetComponentInChildren<GunAmmo>();

            data.health = Health;
            data.maxHealth = MaxHealth;
            data.stats.movementSpeed = MovementSpeed;
            data.stats.jump = Jump;
            data.stats.sizeMultiplier = SizeMultiplier;
            data.stats.lifeSteal = LifeSteal;
            data.stats.respawns = Respawns;

            gun.damage = GunDamage;
            gun.bulletDamageMultiplier = BulletDamageMultiplier;
            gun.knockback = Knockback;
            gun.damageAfterDistanceMultiplier = DamageAfterDistanceMultiplier;
            gun.slow = Slow;
            gun.attackSpeed = AttackSpeed;
            gun.attackSpeedMultiplier = AttackSpeedMultiplier;
            gun.projectileSpeed = ProjectileSpeed;
            gun.projectielSimulatonSpeed = ProjectileSimulationSpeed;
            gun.numberOfProjectiles = NumberOfProjectiles;
            gun.destroyBulletAfter = DestroyBulletAfter;
            gun.reflects = Reflects;
            gun.bursts = Bursts;
            gun.gravity = Gravity;

            block.additionalBlocks = AdditionalBlocks;

            if (ammo != null)
            {
                ammo.maxAmmo = MaxAmmo;
                ammo.reloadTime = ReloadTime;
                ammo.reloadTimeAdd = ReloadTimeAdd;
                ammo.reloadTimeMultiplier = ReloadTimeMultiplier;
            }

            AccessTools.Method(typeof(CharacterStatModifiers), "ConfigureMassAndSize")?.Invoke(data.stats, null);
        }
    }
}
