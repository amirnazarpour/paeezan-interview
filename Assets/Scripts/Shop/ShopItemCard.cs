using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    public sealed class ShopItemCard : MonoBehaviour
    {
        private enum CardState
        {
            Powerup,
            Locked,
            Unlocked,
            Equipped
        }

        [SerializeField] private string itemId;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private TextMeshProUGUI actionText;
        [SerializeField] private Image previewImage;
        [SerializeField] private Button actionButton;

        private ShopPanelController panel;
        public string ItemId => itemId;

        private void Awake()
        {
            actionButton.onClick.AddListener(OnAction);
        }

        private void OnDestroy()
        {
            actionButton.onClick.RemoveListener(OnAction);
        }

        public void SetItemId(string id)
        {
            itemId = id;
        }

        public void Bind(ShopPanelController owner, ShopItemData item, int balance)
        {
            panel = owner;
            bool owned = ShopStateService.IsOwned(item.id);
            bool theme = ShopCatalog.IsTheme(item.kind);
            CardState state = !theme ? CardState.Powerup : !owned ? CardState.Locked :
                ShopStateService.IsEquipped(item) ? CardState.Equipped : CardState.Unlocked;

            titleText.text = item.title;
            descriptionText.text = item.description;
            priceText.text = state == CardState.Unlocked || state == CardState.Equipped ?
                "OWNED" : item.price + " COINS";
            if (theme && ColorUtility.TryParseHtmlString(item.color, out Color color))
                previewImage.color = color;
            else
                previewImage.color = item.kind == ShopItemKind.Shield ? new Color(0.31f, 0.78f, 0.88f) :
                    new Color(1f, 0.77f, 0.3f);

            switch (state)
            {
                case CardState.Equipped:
                    stateText.text = "EQUIPPED";
                    actionText.text = "EQUIPPED";
                    actionButton.interactable = false;
                    break;
                case CardState.Unlocked:
                    stateText.text = "UNLOCKED";
                    actionText.text = "EQUIP";
                    actionButton.interactable = true;
                    break;
                case CardState.Locked:
                    stateText.text = "LOCKED";
                    actionText.text = "BUY";
                    actionButton.interactable = true;
                    break;
                default:
                    stateText.text = "OWNED  x" + ShopStateService.GetCount(item.id);
                    actionText.text = "BUY +1";
                    actionButton.interactable = true;
                    break;
            }
        }

        private void OnAction()
        {
            panel.HandleAction(itemId);
        }
    }
}
