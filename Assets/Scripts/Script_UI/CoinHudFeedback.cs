using DG.Tweening;
using GameCore;
using TMPro;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class CoinHudFeedback : MonoBehaviour
    {
        [SerializeField] private BallController ball;

        private TextMeshProUGUI coinText;
        private TextMeshProUGUI gainText;
        private RectTransform gainRect;
        private Vector3 baseScale;
        private Color baseColor;
        private Tween pulseTween;
        private Tween gainTween;
        private int pendingGain;
        private bool reportedMissingBall;

        private void Awake()
        {
            coinText = GetComponent<TextMeshProUGUI>();
            baseScale = transform.localScale;
            baseColor = coinText.color;

            var gainObject = new GameObject("CoinGainToast", typeof(RectTransform), typeof(TextMeshProUGUI));
            gainRect = gainObject.GetComponent<RectTransform>();
            gainRect.SetParent(transform, false);
            gainRect.anchorMin = new Vector2(1f, 0.5f);
            gainRect.anchorMax = new Vector2(1f, 0.5f);
            gainRect.pivot = new Vector2(0f, 0.5f);
            gainRect.sizeDelta = new Vector2(150f, 60f);
            gainText = gainObject.GetComponent<TextMeshProUGUI>();
            gainText.font = coinText.font;
            gainText.fontSize = coinText.fontSize * 0.8f;
            gainText.alignment = TextAlignmentOptions.MidlineLeft;
            gainText.raycastTarget = false;
            gainText.color = new Color(1f, 0.73f, 0.18f, 1f);
            gainObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (!ball)
                ball = FindFirstObjectByType<BallController>(FindObjectsInactive.Include);
            if (!ball)
            {
                if (!reportedMissingBall)
                {
                    Debug.LogError("CoinHudFeedback requires a BallController in the game scene to show lap coin gains.", this);
                    reportedMissingBall = true;
                }
                return;
            }

            ball.CoinsEarned += OnCoinsEarned;
            ball.PowerupStateChanged += OnPowerupStateChanged;
        }

        private void OnDisable()
        {
            if (ball)
            {
                ball.CoinsEarned -= OnCoinsEarned;
                ball.PowerupStateChanged -= OnPowerupStateChanged;
            }
            ResetFeedback();
        }

        private void OnPowerupStateChanged()
        {
            if (!ball.IsRunActive)
                ResetFeedback();
        }

        private void OnCoinsEarned(int amount)
        {
            if (amount <= 0) return;
            pendingGain += amount;
            pulseTween?.Kill();
            gainTween?.Kill();
            transform.localScale = baseScale;
            coinText.color = baseColor;

            Color gold = new Color(1f, 0.73f, 0.18f, baseColor.a);
            pulseTween = DOTween.Sequence()
                .Append(coinText.DOColor(gold, 0.12f))
                .Join(transform.DOPunchScale(Vector3.one * 0.18f, 0.32f, 4, 0.4f))
                .Append(coinText.DOColor(baseColor, 0.2f))
                .OnComplete(() => transform.localScale = baseScale);

            gainText.text = "+" + pendingGain;
            gainRect.anchoredPosition = new Vector2(12f, 0f);
            gainText.color = new Color(1f, 0.73f, 0.18f, 1f);
            gainText.gameObject.SetActive(true);
            gainTween = DOTween.Sequence()
                .Append(gainRect.DOAnchorPosY(28f, 0.65f).SetEase(Ease.OutCubic))
                .Join(gainText.DOFade(0f, 0.65f))
                .OnComplete(() =>
                {
                    pendingGain = 0;
                    gainText.gameObject.SetActive(false);
                });
        }

        private void ResetFeedback()
        {
            pulseTween?.Kill();
            gainTween?.Kill();
            pulseTween = null;
            gainTween = null;
            pendingGain = 0;
            if (coinText) coinText.color = baseColor;
            transform.localScale = baseScale;
            if (gainText) gainText.gameObject.SetActive(false);
        }
    }
}
