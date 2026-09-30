using System;
using UnityEngine;

namespace ScriptableObjects.Services
{
    [CreateAssetMenu(menuName = "Services/Coin Wallet", fileName = "CoinWallet")]
    public sealed class CoinWallet : ScriptableObject
    {
        private const string BalanceKey = "CoinBalance";
        [SerializeField] private PlayerPrefsSaveService saveService;
        private bool _reportedMissingSaveService;

        public event Action<int> CoinsChanged;

        public int Balance
        {
            get
            {
                return HasSaveService() ? Mathf.Max(0, saveService.LoadInt(BalanceKey)) : 0;
            }
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
                return;

            if (!HasSaveService())
                return;

            int current = Mathf.Max(0, saveService.LoadInt(BalanceKey));
            int updated = current > int.MaxValue - amount ? int.MaxValue : current + amount;
            if (updated == current)
                return;

            saveService.SaveInt(BalanceKey, updated);
            CoinsChanged?.Invoke(updated);
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount < 0 || !HasSaveService())
                return false;

            int current = Mathf.Max(0, saveService.LoadInt(BalanceKey));
            if (current < amount)
                return false;

            if (amount == 0)
                return true;

            int updated = current - amount;
            saveService.SaveInt(BalanceKey, updated);
            CoinsChanged?.Invoke(updated);
            return true;
        }

        private bool HasSaveService()
        {
            if (!saveService && !_reportedMissingSaveService)
            {
                Debug.LogError("CoinWallet requires a PlayerPrefsSaveService asset assigned to its Save Service field.", this);
                _reportedMissingSaveService = true;
            }

            return saveService;
        }
    }
}
