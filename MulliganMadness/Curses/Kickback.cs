using MulliganMadness.Utils;
using UnityEngine;

namespace MulliganMadness.Curses
{
    public class Kickback : AutoPickCurse
    {
        internal static CardInfo Card;

        protected override string GetArtName() => "kickback";

        protected override string GetTitle() => "Kickback";

        protected override string GetDescription() =>
            "Your gun hits 25% harder and kicks you backward when you fire.";

        protected override CardInfo.Rarity GetRarity() => CardInfo.Rarity.Uncommon;

        protected override CardInfoStat[] GetStats() => new[]
        {
            new CardInfoStat
            {
                positive = true,
                stat = "Damage",
                amount = "+25%",
                simepleAmount = CardInfoStat.SimpleAmount.Some
            },
            new CardInfoStat
            {
                positive = false,
                stat = "Recoil",
                amount = "Self knockback",
                simepleAmount = CardInfoStat.SimpleAmount.notAssigned
            }
        };

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers)
        {
            base.SetupCard(cardInfo, gun, cardStats, statModifiers);
            gun.damage = CurseOwnership.KickbackDamageMultiplier;
            gun.recoil = 2.2f;
            gun.recoilMuiltiplier = 2.4f;
            gun.bodyRecoil = 3.5f;
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            base.OnAddCard(player, gun, gunAmmo, data, health, gravity, block, characterStats);
            KickbackBehaviour.Ensure(player);
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var behaviour = player != null ? player.GetComponent<KickbackBehaviour>() : null;
            if (behaviour != null) UnityEngine.Object.Destroy(behaviour);
        }
    }

    internal sealed class KickbackBehaviour : MonoBehaviour
    {
        private Player _player;
        private Gun _hookedGun;

        internal static void Ensure(Player player)
        {
            if (player == null) return;
            if (player.GetComponent<KickbackBehaviour>() != null) return;
            player.gameObject.AddComponent<KickbackBehaviour>();
        }

        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        private void Start() => TryHook();

        private void Update() => TryHook();

        private void TryHook()
        {
            var gun = _player?.data?.weaponHandler?.gun;
            if (gun == null || gun == _hookedGun) return;
            gun.AddAttackAction(OnShoot);
            _hookedGun = gun;
        }

        private void OnShoot()
        {
            if (_player?.data?.view == null || !_player.data.view.IsMine) return;
            if (!CurseOwnership.Has(_player, Kickback.Card)) return;

            var gun = _player.data.weaponHandler?.gun;
            var health = _player.data.healthHandler;
            if (gun == null || health == null) return;

            var aim = (Vector2)gun.transform.right;
            if (aim.sqrMagnitude < 0.01f) aim = Vector2.right;
            var force = -aim.normalized * CurseOwnership.KickbackForce;
            // 5th arg is setFlying (float). Leave 0 so this is a shove, not a launch.
            health.CallTakeForce(force, ForceMode2D.Impulse, true, true, 0f);
        }
    }
}
