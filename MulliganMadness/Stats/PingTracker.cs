using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

namespace MulliganMadness.Stats
{
    internal static class PingTracker
    {
        private const string PropKey = "MM_P";
        private const float PublishInterval = 2f;
        private const float HeartbeatInterval = 8f;
        private const int MinDeltaToPublish = 5;

        private static float _nextPublish;
        private static float _lastPublishTime = -999f;
        private static int _lastPublished = int.MinValue;

        internal static bool Online
        {
            get
            {
                try
                {
                    return PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode && PhotonNetwork.InRoom;
                }
                catch
                {
                    return false;
                }
            }
        }

        internal static int LocalPing()
        {
            if (!Online) return -1;
            try
            {
                var ping = PhotonNetwork.GetPing();
                if (ping > 0) return ping;

                var rtt = PhotonNetwork.NetworkingClient?.LoadBalancingPeer?.RoundTripTime ?? 0;
                return rtt > 0 ? rtt : ping;
            }
            catch
            {
                return -1;
            }
        }

        internal static int Read(Player player)
        {
            if (!Online || player == null) return -1;

            try
            {
                var owner = player.data?.view?.Owner;
                if (owner == null) return -1;
                if (owner.IsLocal) return LocalPing();

                var props = owner.CustomProperties;
                if (props == null || !props.ContainsKey(PropKey)) return -1;
                return ToInt(props[PropKey]);
            }
            catch
            {
                return -1;
            }
        }

        internal static void Tick()
        {
            if (!Online) return;
            if (Time.unscaledTime < _nextPublish) return;
            _nextPublish = Time.unscaledTime + PublishInterval;

            var ping = LocalPing();
            if (ping < 0) return;

            var stale = Time.unscaledTime - _lastPublishTime >= HeartbeatInterval;
            if (!stale && Mathf.Abs(ping - _lastPublished) < MinDeltaToPublish) return;

            try
            {
                PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { PropKey, ping } });
                _lastPublished = ping;
                _lastPublishTime = Time.unscaledTime;
            }
            catch
            {
                // Photon not ready
            }
        }

        private static int ToInt(object raw)
        {
            switch (raw)
            {
                case int i: return i;
                case byte b: return b;
                case short s: return s;
                case long l: return (int)l;
                default: return -1;
            }
        }
    }
}
