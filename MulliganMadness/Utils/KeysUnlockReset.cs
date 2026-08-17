using System;
using System.Collections.Generic;
using CardChoiceSpawnUniqueCardPatch.CustomCategories;
using ModdingUtils.Extensions;
using UnityEngine;

namespace MulliganMadness.Utils
{
    // KeysCards only re-blacklists hidden categories if Treasure is missing. If a previous
    // game left Treasure in the list and unlocked Iron via Metamorphosis, Iron Pincers
    // (and similar) leak into the next game's regular draws.
    internal static class KeysUnlockReset
    {
        private static readonly string[] HiddenCategories =
        {
            "Treasure",
            "Iron",
            "Hatchling",
            "Blessing",
            "Alchemy",
            "Automatic",
            "GroveSlow",
            "HexiLoad",
            "Heavens",
            "ThornS",
            "OverC",
            "BurnB",
            "CatT"
        };

        internal static void Reapply()
        {
            if (PlayerManager.instance?.players == null) return;
            if (CustomCardCategories.instance == null) return;

            foreach (var player in PlayerManager.instance.players)
            {
                if (player?.data?.stats == null) continue;

                List<CardCategory> list;
                try
                {
                    list = player.data.stats.GetAdditionalData().blacklistedCategories;
                }
                catch
                {
                    continue;
                }

                if (list == null) continue;

                foreach (var name in HiddenCategories)
                {
                    var category = CustomCardCategories.instance.CardCategory(name);
                    if (category != null && !list.Contains(category))
                    {
                        list.Add(category);
                    }
                }

                if (HasCardNamed(player, "Metamorphosis"))
                {
                    var iron = CustomCardCategories.instance.CardCategory("Iron");
                    if (iron != null) list.Remove(iron);
                }
            }
        }

        private static bool HasCardNamed(Player player, string name)
        {
            var cards = player.data?.currentCards;
            if (cards == null) return false;

            foreach (var card in cards)
            {
                if (card == null) continue;
                if (string.Equals(card.cardName, name, StringComparison.OrdinalIgnoreCase)) return true;
                var objectName = card.gameObject != null ? card.gameObject.name : "";
                if (objectName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }

            return false;
        }
    }
}
