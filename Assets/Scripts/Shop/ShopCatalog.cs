using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shop
{
    public enum ShopItemKind
    {
        Unknown = 0,
        Shield = 1,
        DoubleScore = 2,
        BallTheme = 3,
        WorldTheme = 4
    }

    public static class ShopItemIds
    {
        public const string Shield = "shield";
        public const string DoubleScore = "double_score";
        public const string BallDefault = "ball_default";
        public const string WorldDefault = "world_default";
    }

    [Serializable]
    public sealed class ShopCatalogData
    {
        public ShopSectionData[] sections;
    }

    [Serializable]
    public sealed class ShopSectionData
    {
        public string id;
        public string title;
        public ShopItemData[] items;
    }

    [Serializable]
    public sealed class ShopItemData
    {
        public string id;
        public string title;
        public string description;
        public ShopItemKind kind;
        public int price;
        public string color;
        public float duration;
        public int shieldHits;
        public int scoreMultiplier;
    }

    public static class ShopCatalog
    {
        private static ShopCatalogData _current;
        private static bool _loaded;

        public static ShopCatalogData Current
        {
            get
            {
                if (!_loaded)
                {
                    _loaded = true;
                    TextAsset json = Resources.Load<TextAsset>("ShopCatalog");
                    string error;
                    if (json != null)
                        _current = Parse(json.text, out error);
                    else
                        error = "Assets/Resources/ShopCatalog.json is missing.";

                    if (_current == null)
                        Debug.LogError("Shop cannot load its catalog: " + error);
                }
                return _current;
            }
        }

        public static void Reload()
        {
            _loaded = false;
            _current = null;
        }

        public static ShopCatalogData Parse(string json, out string error)
        {
            error = null;
            ShopCatalogData data;
            try
            {
                data = JsonUtility.FromJson<ShopCatalogData>(json);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }

            if (data == null || data.sections == null || data.sections.Length == 0)
            {
                error = "The catalog needs a sections array.";
                return null;
            }

            var ids = new HashSet<string>();
            var sectionIds = new HashSet<string>();
            foreach (ShopSectionData section in data.sections)
            {
                if (section == null || !ValidId(section.id) ||
                    string.IsNullOrWhiteSpace(section.title) || !sectionIds.Add(section.id) ||
                    section.items == null)
                {
                    error = "A section has a missing or duplicate ID, title, or items array.";
                    return null;
                }

                foreach (ShopItemData item in section.items)
                {
                    if (item == null || !ValidId(item.id) ||
                        string.IsNullOrWhiteSpace(item.title) || !ids.Add(item.id) || item.price < 0)
                    {
                        error = "An item has a missing or duplicate ID/title, or a negative price.";
                        return null;
                    }

                    bool isTheme = IsTheme(item.kind);
                    bool isPowerup = IsPowerup(item.kind);
                    if (!isTheme && !isPowerup)
                    {
                        error = "Unsupported item kind for " + item.id + ".";
                        return null;
                    }

                    if (isPowerup && item.id != PowerupId(item.kind))
                    {
                        error = "Only the shield and double_score powerup IDs have gameplay handlers.";
                        return null;
                    }

                    if (isTheme && !ColorUtility.TryParseHtmlString(item.color, out _))
                    {
                        error = "Invalid color for " + item.id + ".";
                        return null;
                    }

                    if (item.kind == ShopItemKind.DoubleScore &&
                        (item.duration <= 0f || float.IsNaN(item.duration) || float.IsInfinity(item.duration)))
                    {
                        error = "Double score needs a positive duration.";
                        return null;
                    }
                    if (item.kind == ShopItemKind.Shield && item.shieldHits <= 0)
                    {
                        error = "Shield needs a positive hit count.";
                        return null;
                    }
                    if (item.kind == ShopItemKind.DoubleScore && item.scoreMultiplier <= 0)
                    {
                        error = "Double score needs a positive score multiplier.";
                        return null;
                    }
                }
            }

            foreach (string required in new[] { ShopItemIds.Shield, ShopItemIds.DoubleScore,
                ShopItemIds.BallDefault, ShopItemIds.WorldDefault })
            {
                if (!ids.Contains(required))
                {
                    error = "Required item is missing: " + required + ".";
                    return null;
                }
            }
            if (FindKind(data, ShopItemIds.Shield) != ShopItemKind.Shield ||
                FindKind(data, ShopItemIds.DoubleScore) != ShopItemKind.DoubleScore ||
                FindKind(data, ShopItemIds.BallDefault) != ShopItemKind.BallTheme ||
                FindKind(data, ShopItemIds.WorldDefault) != ShopItemKind.WorldTheme ||
                FindPrice(data, ShopItemIds.BallDefault) != 0 || FindPrice(data, ShopItemIds.WorldDefault) != 0)
            {
                error = "Required powerups and free default themes must keep their kinds and default prices.";
                return null;
            }
            return data;
        }

        private static bool ValidId(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            foreach (char c in id)
                if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '_')
                    return false;
            return true;
        }

        public static bool IsTheme(ShopItemKind kind)
        {
            return kind == ShopItemKind.BallTheme || kind == ShopItemKind.WorldTheme;
        }

        public static bool IsPowerup(ShopItemKind kind)
        {
            return kind == ShopItemKind.Shield || kind == ShopItemKind.DoubleScore;
        }

        public static string PowerupId(ShopItemKind kind)
        {
            switch (kind)
            {
                case ShopItemKind.Shield: return ShopItemIds.Shield;
                case ShopItemKind.DoubleScore: return ShopItemIds.DoubleScore;
                default: return null;
            }
        }

        private static ShopItemKind FindKind(ShopCatalogData data, string id)
        {
            foreach (ShopSectionData section in data.sections)
                foreach (ShopItemData item in section.items)
                    if (item.id == id) return item.kind;
            return ShopItemKind.Unknown;
        }

        private static int FindPrice(ShopCatalogData data, string id)
        {
            foreach (ShopSectionData section in data.sections)
                foreach (ShopItemData item in section.items)
                    if (item.id == id) return item.price;
            return -1;
        }

        public static ShopItemData Find(string id)
        {
            if (Current == null || string.IsNullOrEmpty(id))
                return null;
            foreach (ShopSectionData section in Current.sections)
                foreach (ShopItemData item in section.items)
                    if (item.id == id)
                        return item;
            return null;
        }

        public static ShopItemData FindPowerup(ShopItemKind kind)
        {
            return Find(PowerupId(kind));
        }

        public static Color GetEquippedColor(ShopItemKind kind, Color fallback)
        {
            string id;
            switch (kind)
            {
                case ShopItemKind.BallTheme: id = ShopStateService.EquippedBallThemeId; break;
                case ShopItemKind.WorldTheme: id = ShopStateService.EquippedWorldThemeId; break;
                default: return fallback;
            }
            ShopItemData item = Find(id);
            return item != null && item.kind == kind && ColorUtility.TryParseHtmlString(item.color, out Color color)
                ? color : fallback;
        }
    }
}
