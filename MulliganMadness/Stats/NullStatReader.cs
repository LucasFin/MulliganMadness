using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MulliganMadness.Stats
{
    internal static class NullStatReader
    {
        private static MethodInfo _getNulls;
        private static MethodInfo _getNullCount;
        private static MethodInfo _getNullValue;
        private static Type _nullCardInfo;
        private static bool _resolved;

        internal static float GetRemaining(Player player)
        {
            if (player?.data?.stats == null) return 0f;
            EnsureResolved();
            if (_getNulls == null) return 0f;

            try
            {
                var value = _getNulls.Invoke(null, new object[] { player.data.stats });
                return ToFloat(value);
            }
            catch
            {
                return 0f;
            }
        }

        internal static int GetOwned(Player player)
        {
            if (player?.data?.currentCards == null) return 0;
            EnsureResolved();

            if (_getNullCount != null)
            {
                try
                {
                    var value = _getNullCount.Invoke(null, new object[] { player });
                    var count = Mathf.RoundToInt(ToFloat(value));
                    if (count > 0) return count;
                }
                catch
                {
                    // fall through to card-bar scan
                }
            }

            var owned = 0;
            foreach (var card in player.data.currentCards)
            {
                if (IsNullPlaceholder(card)) owned++;
            }

            return owned;
        }

        internal static bool IsPlaceholder(CardInfo card)
        {
            EnsureResolved();
            return IsNullPlaceholder(card);
        }

        internal static void ApplyPickPreview(Player player, CardInfo card, PlayerStatsSnapshot after)
        {
            if (player == null || card == null || after == null) return;
            if (!IsPlaceholder(card)) return;

            var remaining = after.TryGetNumeric("Nulls", out var current) ? current : GetRemaining(player);
            var owned = after.TryGetNumeric("NullCards", out var ownedNow) ? Mathf.RoundToInt(ownedNow) : GetOwned(player);
            var cost = GetPickCost(card);

            after.WriteCount("Nulls", Mathf.Max(0f, remaining - cost));
            after.WriteCount("NullCards", owned + 1);
        }

        internal static float GetPickCost(CardInfo card)
        {
            EnsureResolved();
            if (card == null) return 1f;

            var source = GetNulledSource(card) ?? card;
            if (_getNullValue != null)
            {
                try
                {
                    var result = InvokeNullValue(source);
                    var cost = ToFloat(result);
                    if (cost > 0.05f) return cost;
                }
                catch
                {
                    // fall through to rarity default
                }
            }

            return DefaultCost(source.rarity);
        }

        private static void EnsureResolved()
        {
            if (_resolved) return;
            _resolved = true;

            try
            {
                var ext = AccessTools.TypeByName("Nullmanager.CharacterStatModifiersExtension");
                _getNulls = ext == null
                    ? null
                    : AccessTools.Method(ext, "GetNulls", new[] { typeof(CharacterStatModifiers) });
            }
            catch
            {
                _getNulls = null;
            }

            try
            {
                var playerExt = AccessTools.TypeByName("Nullmanager.PlayerExtensions");
                _getNullCount = playerExt == null
                    ? null
                    : AccessTools.Method(playerExt, "GetNullCount", new[] { typeof(Player) });
            }
            catch
            {
                _getNullCount = null;
            }

            _nullCardInfo = AccessTools.TypeByName("Nullmanager.NullCardInfo");

            foreach (var typeName in new[]
                     {
                         "Nullmanager.NullManager",
                         "Nullmanager.Main",
                         "Nullmanager.CharacterStatModifiersExtension",
                         "Nullmanager.NullSpawnLogic"
                     })
            {
                try
                {
                    var type = AccessTools.TypeByName(typeName);
                    _getNullValue = type == null ? null : AccessTools.Method(type, "GetNullValue");
                    if (_getNullValue != null) break;
                }
                catch
                {
                    _getNullValue = null;
                }
            }
        }

        private static object InvokeNullValue(CardInfo source)
        {
            if (_getNullValue == null || source == null) return null;
            var parms = _getNullValue.GetParameters();
            object target = _getNullValue.IsStatic ? null : AccessTools.Property(_getNullValue.DeclaringType, "instance")?.GetValue(null);

            if (parms.Length == 0) return _getNullValue.Invoke(target, null);
            if (parms.Length == 1)
            {
                if (parms[0].ParameterType == typeof(CardInfo)) return _getNullValue.Invoke(target, new object[] { source });
                if (parms[0].ParameterType == typeof(CardInfo.Rarity)) return _getNullValue.Invoke(target, new object[] { source.rarity });
            }

            if (parms.Length == 2 && parms[1].ParameterType == typeof(CardInfo.Rarity))
            {
                return _getNullValue.Invoke(target, new object[] { parms[0].ParameterType.IsInstanceOfType(source) ? (object)source : null, source.rarity });
            }

            return _getNullValue.Invoke(target, new object[] { source.rarity });
        }

        private static CardInfo GetNulledSource(CardInfo card)
        {
            if (_nullCardInfo == null || card == null) return null;
            object info = _nullCardInfo.IsInstanceOfType(card) ? card : null;
            if (info == null && card.gameObject != null)
            {
                info = card.gameObject.GetComponent(_nullCardInfo)
                       ?? card.gameObject.GetComponentInChildren(_nullCardInfo);
            }

            if (info == null) return null;
            try
            {
                return AccessTools.Field(info.GetType(), "NulledSorce")?.GetValue(info) as CardInfo;
            }
            catch
            {
                return null;
            }
        }

        private static float DefaultCost(CardInfo.Rarity rarity)
        {
            switch (rarity)
            {
                case CardInfo.Rarity.Rare: return 3f;
                case CardInfo.Rarity.Uncommon: return 2f;
                default: return 1f;
            }
        }

        private static bool IsNullPlaceholder(CardInfo card)
        {
            if (card == null) return false;
            if (_nullCardInfo != null && _nullCardInfo.IsInstanceOfType(card)) return true;

            var name = (card.cardName ?? "").Trim();
            if (name.StartsWith("[]", StringComparison.Ordinal)) return true;
            if (name.Equals("null", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Equals("NullCard", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static float ToFloat(object value)
        {
            if (value == null) return 0f;
            try { return Convert.ToSingle(value); }
            catch { return 0f; }
        }
    }
}
