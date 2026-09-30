using System;
using System.Collections.Generic;
using ScriptableObjects.Services;
using UnityEngine;

namespace Shop
{
    public enum ShopPurchaseFailure
    {
        None,
        Unavailable,
        AlreadyOwned,
        InventoryFull,
        InsufficientFunds
    }

    [Serializable]
    public sealed class PowerupStack
    {
        public string id;
        public int count;
    }

    [Serializable]
    public sealed class ShopSaveData
    {
        public List<string> ownedThemes = new List<string>();
        public string equippedBallThemeId = ShopItemIds.BallDefault;
        public string equippedWorldThemeId = ShopItemIds.WorldDefault;
        public List<PowerupStack> powerups = new List<PowerupStack>();
    }

    public static class ShopStateService
    {
        private const string SaveKey = "ShopStateV1";
        private static bool reportedBadSave;
        public static event Action StateChanged;

        private static ShopSaveData Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            ShopSaveData state;
            try
            {
                state = string.IsNullOrEmpty(json) ? new ShopSaveData() :
                    JsonUtility.FromJson<ShopSaveData>(json);
            }
            catch (Exception)
            {
                if (!reportedBadSave)
                {
                    Debug.LogError("ShopStateV1 save data is invalid; the shop is using default inventory until the next purchase.");
                    reportedBadSave = true;
                }
                state = new ShopSaveData();
            }
            if (state == null)
                state = new ShopSaveData();
            if (state.ownedThemes == null)
                state.ownedThemes = new List<string>();
            if (state.powerups == null)
                state.powerups = new List<PowerupStack>();
            return state;
        }

        private static void Save(ShopSaveData state)
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(state));
            PlayerPrefs.Save();
            StateChanged?.Invoke();
        }

        public static string EquippedBallThemeId => ValidEquipped(Load().equippedBallThemeId,
            ShopItemKind.BallTheme, ShopItemIds.BallDefault);
        public static string EquippedWorldThemeId => ValidEquipped(Load().equippedWorldThemeId,
            ShopItemKind.WorldTheme, ShopItemIds.WorldDefault);

        private static string ValidEquipped(string id, ShopItemKind kind, string fallback)
        {
            ShopItemData item = ShopCatalog.Find(id);
            return item != null && item.kind == kind && IsOwned(id) ? id : fallback;
        }

        public static bool IsOwned(string id)
        {
            return id == ShopItemIds.BallDefault || id == ShopItemIds.WorldDefault ||
                Load().ownedThemes.Contains(id);
        }

        public static bool IsEquipped(ShopItemData item)
        {
            switch (item.kind)
            {
                case ShopItemKind.BallTheme: return item.id == EquippedBallThemeId;
                case ShopItemKind.WorldTheme: return item.id == EquippedWorldThemeId;
                default: return false;
            }
        }

        public static int GetCount(string id)
        {
            foreach (PowerupStack stack in Load().powerups)
                if (stack.id == id)
                    return Mathf.Max(0, stack.count);
            return 0;
        }

        public static bool TryPurchase(string id, CoinWallet wallet, out string message)
        {
            return TryPurchase(id, wallet, out message, out _);
        }

        public static bool TryPurchase(string id, CoinWallet wallet, out string message,
            out ShopPurchaseFailure failure)
        {
            failure = ShopPurchaseFailure.None;
            ShopItemData item = ShopCatalog.Find(id);
            if (item == null || wallet == null)
            {
                failure = ShopPurchaseFailure.Unavailable;
                message = "Shop is unavailable. Check its catalog and Coin Wallet.";
                return false;
            }

            bool theme = ShopCatalog.IsTheme(item.kind);
            ShopSaveData state = Load();
            if (theme && (id == ShopItemIds.BallDefault || id == ShopItemIds.WorldDefault ||
                state.ownedThemes.Contains(id)))
            {
                failure = ShopPurchaseFailure.AlreadyOwned;
                message = "Already owned.";
                return false;
            }

            PowerupStack stack = null;
            if (!theme)
            {
                stack = state.powerups.Find(x => x.id == id);
                if (stack != null && stack.count == int.MaxValue)
                {
                    failure = ShopPurchaseFailure.InventoryFull;
                    message = "Inventory is full.";
                    return false;
                }
            }

            if (wallet.Balance < item.price)
            {
                failure = ShopPurchaseFailure.InsufficientFunds;
                message = "Not enough coins.";
                return false;
            }
            if (!wallet.TrySpendCoins(item.price))
            {
                failure = ShopPurchaseFailure.Unavailable;
                message = "Shop is unavailable. Check its Coin Wallet.";
                return false;
            }

            if (theme)
                state.ownedThemes.Add(id);
            else if (stack == null)
                state.powerups.Add(new PowerupStack { id = id, count = 1 });
            else
                stack.count++;

            Save(state);
            message = theme ? "Unlocked " + item.title + "." : "Added one " + item.title + ".";
            return true;
        }

        public static bool TryEquip(string id)
        {
            ShopItemData item = ShopCatalog.Find(id);
            if (item == null || !IsOwned(id))
                return false;
            ShopSaveData state = Load();
            if (item.kind == ShopItemKind.BallTheme)
                state.equippedBallThemeId = id;
            else if (item.kind == ShopItemKind.WorldTheme)
                state.equippedWorldThemeId = id;
            else
                return false;
            Save(state);
            return true;
        }

        public static bool TryConsume(string id)
        {
            ShopSaveData state = Load();
            PowerupStack stack = state.powerups.Find(x => x.id == id);
            if (stack == null || stack.count <= 0)
                return false;
            stack.count--;
            Save(state);
            return true;
        }
    }
}
