using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    public sealed class ShopSectionView : MonoBehaviour
    {
        [SerializeField] private string sectionId;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Transform cardContainer;

        public string SectionId => sectionId;
        public Transform CardContainer => cardContainer;

        public void SetSectionId(string id)
        {
            sectionId = id;
        }

        public void SetTitle(string title)
        {
            titleText.text = title;
        }

        public void Configure(string title, int itemCount)
        {
            SetTitle(title);
            float cardsHeight = itemCount * 130f;
            float sectionHeight = 56f + cardsHeight;
            if (TryGetComponent(out LayoutElement layout))
                layout.preferredHeight = sectionHeight;
            if (transform is RectTransform sectionRect)
                sectionRect.sizeDelta = new Vector2(sectionRect.sizeDelta.x, sectionHeight);
            if (CardContainer is RectTransform cardsRect)
                cardsRect.sizeDelta = new Vector2(cardsRect.sizeDelta.x, cardsHeight);
        }
    }
}
