using MulliganMadness.Cards;
using UnityEngine;

namespace MulliganMadness.Utils
{
    internal sealed class SelfKickOnFire : MonoBehaviour
    {
        private Player _player;
        private Gun _hookedGun;
        private float _force;

        internal static void Ensure(Player player, float force)
        {
            if (player == null) return;
            var existing = player.GetComponent<SelfKickOnFire>();
            if (existing != null)
            {
                if (force > existing._force) existing._force = force;
                return;
            }

            var added = player.gameObject.AddComponent<SelfKickOnFire>();
            added._force = force;
        }

        private void Awake() => _player = GetComponent<Player>();

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
            if (!CurseOwnership.Has(_player, YeetCannon.Card)) return;

            var gun = _player.data.weaponHandler?.gun;
            var health = _player.data.healthHandler;
            if (gun == null || health == null) return;

            var aim = (Vector2)gun.transform.right;
            if (aim.sqrMagnitude < 0.01f) aim = Vector2.right;
            health.CallTakeForce(-aim.normalized * _force, ForceMode2D.Impulse, true, true, 0f);
        }
    }
}
