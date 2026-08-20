using HarmonyLib;
using MulliganMadness.Stats;
using Photon.Pun;
using UnityEngine;

namespace MulliganMadness.Utils
{
    internal static class DefaultAppearance
    {
        private static bool _appliedThisGame;

        internal static void ResetForNewGame() => _appliedThisGame = false;

        internal static bool TryCaptureFromLocal(out string notice)
        {
            notice = null;
            if (!TryGetLocalPlayer(out var player, out var playerId))
            {
                notice = "Join a lobby or start a game to capture your current look.";
                return false;
            }

            if (!TryReadFace(playerId, out var face))
            {
                notice = "Could not read your face — customize in character select first.";
                return false;
            }

            WriteFaceToConfig(face);
            Plugin.Configs.DefaultColorIndex.Value = TryReadColorIndex(player, playerId);
            notice = $"Saved face + color #{Plugin.Configs.DefaultColorIndex.Value}.";
            return true;
        }

        internal static void TryApply(bool force = false)
        {
            if (!Plugin.Configs.DefaultAppearanceEnabled.Value) return;
            if (_appliedThisGame && !force) return;
            if (!TryGetLocalPlayer(out var player, out var playerId)) return;

            var face = ReadFaceFromConfig();
            ApplyFace(player, playerId, face);
            ApplyColor(player, Plugin.Configs.DefaultColorIndex.Value);
            _appliedThisGame = true;
        }

        private static bool TryGetLocalPlayer(out Player player, out int playerId)
        {
            player = PlayerStatsSnapshot.LocalPlayer();
            if (player != null)
            {
                playerId = player.playerID;
                return true;
            }

            if (PhotonNetwork.OfflineMode && PlayerManager.instance?.players != null && PlayerManager.instance.players.Count > 0)
            {
                player = PlayerManager.instance.players[0];
                playerId = player.playerID;
                return player != null;
            }

            player = null;
            playerId = 0;
            return false;
        }

        private static bool TryReadFace(int playerId, out PlayerFace face)
        {
            face = default;
            var handler = CharacterCreatorHandler.instance;
            if (handler == null) return false;

            try
            {
                face = handler.GetFacePreset(playerId);
                return true;
            }
            catch
            {
                if (handler.selectedPlayerFaces != null && playerId >= 0 && playerId < handler.selectedPlayerFaces.Length)
                {
                    face = handler.selectedPlayerFaces[playerId];
                    return true;
                }
            }

            return false;
        }

        private static int TryReadColorIndex(Player player, int playerId)
        {
            var selectors = Traverse.Create(typeof(CharacterSelectionInstance)).Field("selectors").GetValue<CharacterSelectionInstance[]>();
            if (selectors != null)
            {
                foreach (var selector in selectors)
                {
                    if (selector == null) continue;
                    var current = Traverse.Create(selector).Field("currentPlayer").GetValue<Player>();
                    if (current == player)
                    {
                        return Traverse.Create(selector).Field("currentlySelectedFace").GetValue<int>();
                    }
                }
            }

            if (playerId >= 0 && CharacterCreatorHandler.instance?.selectedFaceID != null &&
                playerId < CharacterCreatorHandler.instance.selectedFaceID.Length)
            {
                return CharacterCreatorHandler.instance.selectedFaceID[playerId];
            }

            return Plugin.Configs.DefaultColorIndex.Value;
        }

        private static PlayerFace ReadFaceFromConfig()
        {
            return new PlayerFace
            {
                eyeID = Plugin.Configs.DefaultEyeId.Value,
                eyeOffset = new Vector2(Plugin.Configs.DefaultEyeOffsetX.Value, Plugin.Configs.DefaultEyeOffsetY.Value),
                mouthID = Plugin.Configs.DefaultMouthId.Value,
                mouthOffset = new Vector2(Plugin.Configs.DefaultMouthOffsetX.Value, Plugin.Configs.DefaultMouthOffsetY.Value),
                detailID = Plugin.Configs.DefaultDetailId.Value,
                detailOffset = new Vector2(Plugin.Configs.DefaultDetailOffsetX.Value, Plugin.Configs.DefaultDetailOffsetY.Value),
                detail2ID = Plugin.Configs.DefaultDetail2Id.Value,
                detail2Offset = new Vector2(Plugin.Configs.DefaultDetail2OffsetX.Value, Plugin.Configs.DefaultDetail2OffsetY.Value)
            };
        }

        private static void WriteFaceToConfig(PlayerFace face)
        {
            Plugin.Configs.DefaultEyeId.Value = face.eyeID;
            Plugin.Configs.DefaultEyeOffsetX.Value = face.eyeOffset.x;
            Plugin.Configs.DefaultEyeOffsetY.Value = face.eyeOffset.y;
            Plugin.Configs.DefaultMouthId.Value = face.mouthID;
            Plugin.Configs.DefaultMouthOffsetX.Value = face.mouthOffset.x;
            Plugin.Configs.DefaultMouthOffsetY.Value = face.mouthOffset.y;
            Plugin.Configs.DefaultDetailId.Value = face.detailID;
            Plugin.Configs.DefaultDetailOffsetX.Value = face.detailOffset.x;
            Plugin.Configs.DefaultDetailOffsetY.Value = face.detailOffset.y;
            Plugin.Configs.DefaultDetail2Id.Value = face.detail2ID;
            Plugin.Configs.DefaultDetail2OffsetX.Value = face.detail2Offset.x;
            Plugin.Configs.DefaultDetail2OffsetY.Value = face.detail2Offset.y;
        }

        private static void ApplyFace(Player player, int playerId, PlayerFace face)
        {
            var handler = CharacterCreatorHandler.instance;
            if (handler != null)
            {
                AccessTools.Method(typeof(CharacterCreatorHandler), "SetFacePreset")?.Invoke(handler, new object[] { playerId, face });
                if (handler.selectedPlayerFaces != null && playerId >= 0 && playerId < handler.selectedPlayerFaces.Length)
                {
                    handler.selectedPlayerFaces[playerId] = face;
                }
            }

            if (player?.data?.view != null && (PhotonNetwork.OfflineMode || player.data.view.IsMine))
            {
                player.RPCA_SetFace(
                    face.eyeID, face.eyeOffset,
                    face.mouthID, face.mouthOffset,
                    face.detailID, face.detailOffset,
                    face.detail2ID, face.detail2Offset);
            }
        }

        private static void ApplyColor(Player player, int colorIndex)
        {
            if (player == null) return;

            var bank = Traverse.Create(typeof(PlayerSkinBank)).Field("instance").GetValue<PlayerSkinBank>();
            if (bank?.skins == null || bank.skins.Length == 0) return;

            var index = Mathf.Clamp(colorIndex, 0, bank.skins.Length - 1);
            var source = PlayerSkinBank.GetPlayerSkinColors(index);
            if (source == null) return;

            var handler = CharacterCreatorHandler.instance;
            if (handler?.selectedFaceID != null && player.playerID >= 0 && player.playerID < handler.selectedFaceID.Length)
            {
                handler.selectedFaceID[player.playerID] = index;
            }

            var target = player.GetTeamColors();
            if (target == null) return;

            target.color = source.color;
            target.backgroundColor = source.backgroundColor;
            target.particleEffect = source.particleEffect;
            target.winText = source.winText;
            player.SetColors();
        }

        internal static int MaxColorIndex()
        {
            var bank = Traverse.Create(typeof(PlayerSkinBank)).Field("instance").GetValue<PlayerSkinBank>();
            if (bank?.skins == null || bank.skins.Length == 0) return 15;
            return bank.skins.Length - 1;
        }
    }
}
