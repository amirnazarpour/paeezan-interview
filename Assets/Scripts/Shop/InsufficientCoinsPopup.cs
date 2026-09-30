using Enums;
using ScriptableObjects.GameEvents;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    public sealed class InsufficientCoinsPopup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI itemText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI balanceText;
        [SerializeField] private TextMeshProUGUI neededText;
        [SerializeField] private Button goPlayButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private IntEvent onTabSelected;

        private void Awake()
        {
            goPlayButton.onClick.AddListener(GoPlay);
            closeButton.onClick.AddListener(Hide);
        }

        private void OnEnable()
        {
            onTabSelected.OnEventRaised += OnTabChanged;
        }

        private void OnDisable()
        {
            onTabSelected.OnEventRaised -= OnTabChanged;
        }

        private void OnDestroy()
        {
            goPlayButton.onClick.RemoveListener(GoPlay);
            closeButton.onClick.RemoveListener(Hide);
        }

        public void Show(ShopItemData item, int balance)
        {
            if (item == null) return;
            itemText.text = item.title;
            priceText.text = "Price: " + item.price + " coins";
            balanceText.text = "You have: " + balance + " coins";
            neededText.text = "Earn " + Mathf.Max(0, item.price - balance) + " more coins to buy it.";
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void GoPlay()
        {
            Hide();
            onTabSelected.Raise((int)TabType.Game);
        }

        private void OnTabChanged(int _)
        {
            Hide();
        }

    }
}
