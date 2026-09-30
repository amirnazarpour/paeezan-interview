using System.Collections.Generic;
using GameCore;
using UnityEngine;

namespace Shop
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class GamePowerupHud : MonoBehaviour
    {
        [SerializeField] private BallController ball;
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private GamePowerupCard cardPrefab;

        private readonly Dictionary<ShopItemKind, GamePowerupCard> cardsByKind =
            new Dictionary<ShopItemKind, GamePowerupCard>();

        private void OnEnable()
        {
            ball.PowerupStateChanged += Refresh;
            ShopStateService.StateChanged += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            ShopStateService.StateChanged -= Refresh;
            ball.PowerupStateChanged -= Refresh;
        }

        private void Update()
        {
            if (ball.DoubleScoreActive && cardsByKind.TryGetValue(ShopItemKind.DoubleScore, out GamePowerupCard card))
                card.Refresh(ball);
        }

        public void UsePowerup(ShopItemKind kind)
        {
            switch (kind)
            {
                case ShopItemKind.Shield: ball.TryActivateShield(); break;
                case ShopItemKind.DoubleScore: ball.TryActivateDoubleScore(); break;
            }
            Refresh();
        }

        public void Refresh()
        {
            CanvasGroup group = GetComponent<CanvasGroup>();
            bool visible = ball.IsRunActive;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;

            ShopCatalogData catalog = ShopCatalog.Current;
            if (catalog == null)
            {
                Debug.LogError("GamePowerupHud requires a valid ShopCatalog.json.", this);
                return;
            }

            var catalogKinds = new HashSet<ShopItemKind>();
            int cardIndex = 0;
            foreach (ShopSectionData section in catalog.sections)
            {
                foreach (ShopItemData item in section.items)
                {
                    if (!ShopCatalog.IsPowerup(item.kind)) continue;
                    catalogKinds.Add(item.kind);
                    if (!cardsByKind.TryGetValue(item.kind, out GamePowerupCard card) || !card)
                    {
                        card = Instantiate(cardPrefab, cardContainer);
                        card.gameObject.name = "Powerup_" + item.id;
                        cardsByKind[item.kind] = card;
                    }
                    card.transform.SetSiblingIndex(cardIndex++);
                    card.Bind(this, item, ball);
                }
            }

            foreach (ShopItemKind kind in new List<ShopItemKind>(cardsByKind.Keys))
            {
                if (catalogKinds.Contains(kind)) continue;
                if (cardsByKind[kind]) Destroy(cardsByKind[kind].gameObject);
                cardsByKind.Remove(kind);
            }
        }
    }
}
