using System.Collections.Generic;
using ScriptableObjects.Services;
using TMPro;
using UnityEngine;

namespace Shop
{
    public sealed class ShopPanelController : MonoBehaviour
    {
        [SerializeField] private CoinWallet coinWallet;
        [SerializeField] private TextMeshProUGUI balanceText;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private Transform sectionContainer;
        [SerializeField] private ShopItemCard itemCardPrefab;
        [SerializeField] private InsufficientCoinsPopup insufficientCoinsPopup;
        private readonly Dictionary<string, ShopItemCard> cardsById = new Dictionary<string, ShopItemCard>();
        private bool reportedOutOfSync;

        private void OnEnable()
        {
            coinWallet.CoinsChanged += OnCoinsChanged;
            ShopStateService.StateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            coinWallet.CoinsChanged -= OnCoinsChanged;
            ShopStateService.StateChanged -= Refresh;
        }

        private void OnCoinsChanged(int _) => Refresh();

        public void HandleAction(string id)
        {
            ShopItemData item = ShopCatalog.Find(id);
            if (item == null)
                return;

            bool theme = ShopCatalog.IsTheme(item.kind);
            string message;
            bool showFooter = true;
            if (theme && ShopStateService.IsOwned(id))
                message = ShopStateService.TryEquip(id) ? "Equipped " + item.title + "." : "Cannot equip this theme.";
            else
            {
                ShopStateService.TryPurchase(id, coinWallet, out message, out ShopPurchaseFailure failure);
                if (failure == ShopPurchaseFailure.InsufficientFunds)
                {
                    insufficientCoinsPopup.Show(item, coinWallet.Balance);
                    showFooter = false;
                }
            }

            if (showFooter) feedbackText.text = message;
            Refresh();
        }

        public void Refresh()
        {
            ShopCatalogData catalog = ShopCatalog.Current;
            if (catalog == null)
            {
                feedbackText.text = "SHOP UNAVAILABLE";
                return;
            }

            int balance = coinWallet.Balance;
            balanceText.text = balance + " COINS";
            ShopSectionView[] views = sectionContainer.GetComponentsInChildren<ShopSectionView>(true);
            foreach (ShopSectionView existing in views)
            {
                bool exists = System.Array.Exists(catalog.sections, x => x.id == existing.SectionId);
                existing.gameObject.SetActive(exists);
            }

            int contentHeight = 36 + (catalog.sections.Length - 1) * 20;
            var catalogIds = new HashSet<string>();
            for (int sectionIndex = 0; sectionIndex < catalog.sections.Length; sectionIndex++)
            {
                ShopSectionData section = catalog.sections[sectionIndex];
                contentHeight += 56 + section.items.Length * 130;
                ShopSectionView view = System.Array.Find(views, x => x.SectionId == section.id);
                if (!view)
                {
                    ReportOutOfSync();
                    continue;
                }
                view.transform.SetSiblingIndex(sectionIndex);
                view.Configure(section.title, section.items.Length);
                for (int itemIndex = 0; itemIndex < section.items.Length; itemIndex++)
                {
                    ShopItemData item = section.items[itemIndex];
                    catalogIds.Add(item.id);
                    if (!cardsById.TryGetValue(item.id, out ShopItemCard card) || !card)
                    {
                        card = Instantiate(itemCardPrefab, view.CardContainer);
                        card.gameObject.name = "Card_" + item.id;
                        card.SetItemId(item.id);
                        cardsById[item.id] = card;
                    }
                    else if (card.transform.parent != view.CardContainer)
                        card.transform.SetParent(view.CardContainer, false);
                    card.transform.SetSiblingIndex(itemIndex);
                    card.Bind(this, item, balance);
                }
            }
            foreach (string id in new List<string>(cardsById.Keys))
            {
                if (catalogIds.Contains(id)) continue;
                if (cardsById[id]) Destroy(cardsById[id].gameObject);
                cardsById.Remove(id);
            }
            if (sectionContainer is RectTransform content)
                content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);
        }

        private void ReportOutOfSync()
        {
            if (reportedOutOfSync) return;
            Debug.LogError("Shop scene sections do not match ShopCatalog.json. Open Tools > Shop > Catalog Editor and click Save and Sync Scenes.", this);
            reportedOutOfSync = true;
        }
    }
}
