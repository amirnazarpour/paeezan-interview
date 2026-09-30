using GameCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    public sealed class GamePowerupCard : MonoBehaviour
    {
        [SerializeField] private Button actionButton;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image background;

        private GamePowerupHud owner;
        private ShopItemData item;
        private void Awake()
        {
            actionButton.onClick.AddListener(Use);
        }

        private void OnDestroy()
        {
            actionButton.onClick.RemoveListener(Use);
        }

        public void Bind(GamePowerupHud hud, ShopItemData catalogItem, BallController ball)
        {
            owner = hud;
            item = catalogItem;
            background.color = item.kind == ShopItemKind.Shield ? new Color(0.22f, 0.5f, 0.62f) :
                new Color(0.72f, 0.52f, 0.18f);
            Refresh(ball);
        }

        public void Refresh(BallController ball)
        {
            if (item == null) return;
            int count = ShopStateService.GetCount(item.id);
            bool shield = item.kind == ShopItemKind.Shield;
            bool active = ball && (shield ? ball.ShieldActive : ball.DoubleScoreActive);
            string state = active ?
                (shield ? "BLOCKS " + ball.ShieldHitsRemaining :
                    ball.ScoreMultiplier + "X  " + Mathf.CeilToInt(ball.DoubleScoreRemaining) + "s") :
                "OWNED  x" + count;
            label.text = item.title + "\n" + state;
            actionButton.interactable = ball && ball.IsRunActive && count > 0 && !active;
        }

        private void Use()
        {
            if (item != null && owner) owner.UsePowerup(item.kind);
        }
    }
}
