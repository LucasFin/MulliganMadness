namespace MulliganMadness.Utils
{
    /// <summary>
    /// Shows a short message to one player, on their machine only.
    /// </summary>
    internal static class PlayerNotice
    {
        internal static void Show(Player player, string message)
        {
            if (player?.data?.view == null || !player.data.view.IsMine) return;
            Plugin.Instance?.Log(message);
            CardTargetUi.ShowToast(message);
        }
    }
}
