using DG.Tweening;
using UnityEngine;

namespace GameCore
{
    [RequireComponent(typeof(BallController))]
    public sealed class BallGameplayFeedback : MonoBehaviour
    {
        [SerializeField] private ParticleSystem coinBurst;
        [SerializeField] private SpriteRenderer shieldRing;
        [SerializeField] private SpriteRenderer doubleScoreRing;
        [SerializeField] private AudioSource[] cueSources;
        [SerializeField] private AudioClip coinClip;
        [SerializeField] private AudioClip activationClip;
        [SerializeField] private AudioClip shieldHitClip;

        private BallController ball;
        private Vector3 shieldScale;
        private Vector3 doubleScoreScale;
        private Tween shieldImpactTween;
        private int nextSource;

        private void Awake()
        {
            ball = GetComponent<BallController>();
            if (!coinBurst || !shieldRing || !doubleScoreRing || cueSources == null || cueSources.Length == 0 ||
                !coinClip || !activationClip || !shieldHitClip)
                Debug.LogError("BallGameplayFeedback needs its Coin Burst, Shield Ring, Double Score Ring, audio sources, and existing cue clips assigned on the ball prefab.", this);

            if (shieldRing) shieldScale = shieldRing.transform.localScale;
            if (doubleScoreRing) doubleScoreScale = doubleScoreRing.transform.localScale;
            ResetVisuals();
        }

        private void OnEnable()
        {
            ball.CoinsEarned += OnCoinsEarned;
            ball.PowerupFeedback += OnPowerupFeedback;
            ball.PowerupStateChanged += OnPowerupStateChanged;
        }

        private void OnDisable()
        {
            ball.CoinsEarned -= OnCoinsEarned;
            ball.PowerupFeedback -= OnPowerupFeedback;
            ball.PowerupStateChanged -= OnPowerupStateChanged;
            ResetVisuals();
            if (cueSources == null) return;
            foreach (AudioSource source in cueSources)
                if (source) source.Stop();
        }

        private void OnCoinsEarned(int amount)
        {
            if (amount <= 0) return;
            if (coinBurst)
            {
                coinBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                coinBurst.Play(true);
            }
            PlayCue(coinClip, Random.Range(1.02f, 1.12f), 0.45f);
        }

        private void OnPowerupStateChanged()
        {
            if (!ball.IsRunActive)
                ResetVisuals();
        }

        private void OnPowerupFeedback(PowerupFeedbackType type)
        {
            switch (type)
            {
                case PowerupFeedbackType.ShieldActivated:
                    ShowRing(shieldRing, shieldScale, 0.9f);
                    PlayCue(activationClip, 1.1f, 0.55f);
                    break;
                case PowerupFeedbackType.ShieldBlocked:
                    FlashShield();
                    PlayCue(shieldHitClip, 0.92f, 0.6f);
                    break;
                case PowerupFeedbackType.ShieldDepleted:
                    if (shieldImpactTween == null || !shieldImpactTween.IsActive())
                        FadeRing(shieldRing, shieldScale);
                    break;
                case PowerupFeedbackType.DoubleScoreActivated:
                    ShowRing(doubleScoreRing, doubleScoreScale, 0.8f);
                    if (doubleScoreRing)
                        doubleScoreRing.DOFade(0.55f, 0.65f).SetLoops(-1, LoopType.Yoyo);
                    PlayCue(activationClip, 1.22f, 0.55f);
                    break;
                case PowerupFeedbackType.DoubleScoreExpired:
                    FadeRing(doubleScoreRing, doubleScoreScale);
                    break;
            }
        }

        private void FlashShield()
        {
            if (!shieldRing) return;
            shieldImpactTween?.Kill();
            shieldRing.DOKill();
            shieldRing.transform.DOKill();
            shieldRing.enabled = true;
            shieldRing.color = Color.white;
            shieldRing.transform.localScale = shieldScale * 1.24f;
            shieldImpactTween = DOTween.Sequence()
                .Append(shieldRing.transform.DOScale(shieldScale, 0.2f).SetEase(Ease.OutBack))
                .Join(shieldRing.DOColor(new Color(0.3f, 0.9f, 1f, 0.9f), 0.2f))
                .OnComplete(() =>
                {
                    shieldImpactTween = null;
                    if (!ball.ShieldActive) FadeRing(shieldRing, shieldScale);
                });
        }

        private static void ShowRing(SpriteRenderer ring, Vector3 baseScale, float alpha)
        {
            if (!ring) return;
            ring.DOKill();
            ring.transform.DOKill();
            ring.enabled = true;
            Color color = ring.color;
            color.a = alpha;
            ring.color = color;
            ring.transform.localScale = baseScale * 0.75f;
            ring.transform.DOScale(baseScale, 0.28f).SetEase(Ease.OutBack);
        }

        private static void FadeRing(SpriteRenderer ring, Vector3 baseScale)
        {
            if (!ring) return;
            ring.DOKill();
            ring.transform.DOKill();
            ring.DOFade(0f, 0.25f).OnComplete(() =>
            {
                if (ring) ring.enabled = false;
            });
            ring.transform.DOScale(baseScale * 1.18f, 0.25f);
        }

        private void ResetVisuals()
        {
            shieldImpactTween?.Kill();
            shieldImpactTween = null;
            ResetRing(shieldRing, shieldScale);
            ResetRing(doubleScoreRing, doubleScoreScale);
            if (coinBurst)
                coinBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private static void ResetRing(SpriteRenderer ring, Vector3 baseScale)
        {
            if (!ring) return;
            ring.DOKill();
            ring.transform.DOKill();
            ring.transform.localScale = baseScale;
            ring.enabled = false;
        }

        private void PlayCue(AudioClip clip, float pitch, float volume)
        {
            if (!clip || cueSources == null || cueSources.Length == 0) return;
            AudioSource source = cueSources[nextSource++ % cueSources.Length];
            if (!source) return;
            source.Stop();
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }
    }
}
