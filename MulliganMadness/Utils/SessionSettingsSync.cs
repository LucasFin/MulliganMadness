using Photon.Pun;
using UnboundLib;
using UnboundLib.Networking;

namespace MulliganMadness.Utils
{
    internal static class SessionSettingsSync
    {
        private const string SyncEvent = "MM_SessionSettings_v1";

        internal static void Register()
        {
            NetworkingManager.RegisterEvent(SyncEvent, OnReceived);
        }

        internal static void BroadcastIfHost()
        {
            if (!SessionSettings.IsHost) return;
            var payload = SessionSettings.Current.Serialize();
            if (PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode)
            {
                NetworkingManager.RaiseEventOthers(SyncEvent, payload);
            }
        }

        internal static void BroadcastToAllIfHost()
        {
            if (!SessionSettings.IsHost) return;
            var payload = SessionSettings.Current.Serialize();
            NetworkingManager.RaiseEvent(SyncEvent, payload);
        }

        private static void OnReceived(object[] data)
        {
            if (data == null || data.Length == 0 || data[0] == null) return;
            var payload = data[0] as string;
            if (string.IsNullOrWhiteSpace(payload)) return;
            SessionSettings.ApplyFromNetwork(payload);
        }
    }
}
