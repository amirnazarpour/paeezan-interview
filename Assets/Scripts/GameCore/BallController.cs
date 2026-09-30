using DG.Tweening;
using Enums;
using UnityEngine;
using ScriptableObjects.GameEvents;
using ScriptableObjects.Services;
using Shop;
using System;
using UnityEngine.EventSystems;

namespace GameCore
{
    public class BallController : MonoBehaviour
    {
        [SerializeField] private IntEvent OnScoreChanged;
        [SerializeField] private NullEvent OnGameEnded;
        [SerializeField] private NullEvent OnStartMoving;
        [SerializeField] private ParticleSystem dieParticle;
        [SerializeField] private CoinWallet coinWallet;

        [SerializeField] private Transform center;
        [SerializeField] private float outsideRadius = 2f;
        [SerializeField] private float insideRadius = 1.3f;
        [SerializeField] private float moveTime = 0.2f;

        [Header("Rotation")]
        [SerializeField] private float speed = 360f;       
        [SerializeField] private float maxSpeed = 1200f;  
        [SerializeField] private float speedIncreaseRate = 50f; 

        [SerializeField] private int increasedScoreByPickingStars = 1;

        [Header("Death Animation")]
        [SerializeField] private float dieScaleTime = 0.5f;
        [SerializeField] private float dieRotationTime = 0.5f;
        [SerializeField] private float dieFadeTime = 0.5f;

        private int _score;
        private float angle;
        private float lapProgress;
        private float radius;
        private bool isInside;
        private bool canMove; 
        private int shieldHitsRemaining;
        private float doubleScoreRemaining;
        private int activeScoreMultiplier = 1;

        public event Action PowerupStateChanged;
        public event Action<int> CoinsEarned;
        public event Action<PowerupFeedbackType> PowerupFeedback;
        public bool IsRunActive => canMove && gameObject.activeInHierarchy;
        public bool ShieldActive => shieldHitsRemaining > 0;
        public int ShieldHitsRemaining => shieldHitsRemaining;
        public bool DoubleScoreActive => doubleScoreRemaining > 0f;
        public float DoubleScoreRemaining => doubleScoreRemaining;
        public int ScoreMultiplier => DoubleScoreActive ? activeScoreMultiplier : 1;

        private SpriteRenderer _spriteRenderer;
        private Vector3 _firstLocalScale;
        private float _firstSpeed ;
        private Color _firstColor;

        private void Start()
        {
            if (!coinWallet)
                Debug.LogError("BallController requires a CoinWallet asset assigned to its Coin Wallet field.", this);

            _firstSpeed = speed;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _firstColor = _spriteRenderer.color;
            _firstLocalScale = transform.localScale;
            radius = outsideRadius* transform.parent.localScale.x;
        }

        private void OnEnable()
        {
            OnGameEnded.OnEventRaised += Die;
            OnStartMoving.OnEventRaised += StartMovement; 
        }

        private void OnDisable()
        {
            OnGameEnded.OnEventRaised -= Die;
            OnStartMoving.OnEventRaised -= StartMovement;
        }

        private void StartMovement()
        {
            lapProgress = 0f;
            shieldHitsRemaining = 0;
            doubleScoreRemaining = 0f;
            activeScoreMultiplier = 1;
            canMove = true;
            PowerupStateChanged?.Invoke();
        }

        public bool TryActivateShield()
        {
            ShopItemData item = ShopCatalog.FindPowerup(ShopItemKind.Shield);
            if (!IsRunActive || ShieldActive || item == null || item.shieldHits <= 0 ||
                !ShopStateService.TryConsume(item.id))
                return false;
            shieldHitsRemaining = item.shieldHits;
            PowerupStateChanged?.Invoke();
            PowerupFeedback?.Invoke(PowerupFeedbackType.ShieldActivated);
            return true;
        }

        public bool TryActivateDoubleScore()
        {
            ShopItemData item = ShopCatalog.FindPowerup(ShopItemKind.DoubleScore);
            if (!IsRunActive || DoubleScoreActive || item == null || item.duration <= 0f ||
                item.scoreMultiplier <= 0 ||
                !ShopStateService.TryConsume(item.id))
                return false;
            doubleScoreRemaining = item.duration;
            activeScoreMultiplier = item.scoreMultiplier;
            PowerupStateChanged?.Invoke();
            PowerupFeedback?.Invoke(PowerupFeedbackType.DoubleScoreActivated);
            return true;
        }

        private void Update()
        {
            if (!canMove)
                return;

            if (doubleScoreRemaining > 0f)
            {
                doubleScoreRemaining = Mathf.Max(0f, doubleScoreRemaining - Time.deltaTime);
                if (doubleScoreRemaining == 0f)
                {
                    PowerupStateChanged?.Invoke();
                    PowerupFeedback?.Invoke(PowerupFeedbackType.DoubleScoreExpired);
                }
            }

            if (speed < maxSpeed)
                speed = Mathf.Min(speed + speedIncreaseRate * Time.deltaTime, maxSpeed);

            float degreesMoved = speed * Time.deltaTime;
            angle = (angle + degreesMoved) % 360f;
            lapProgress += degreesMoved;

            if (lapProgress >= 360f)
            {
                int completedLaps = Mathf.FloorToInt(lapProgress / 360f);
                lapProgress -= completedLaps * 360f;

                if (coinWallet)
                {
                    int previousBalance = coinWallet.Balance;
                    coinWallet.AddCoins(completedLaps);
                    int earned = coinWallet.Balance - previousBalance;
                    if (earned > 0)
                        CoinsEarned?.Invoke(earned);
                }
            }

            if (Input.GetMouseButtonDown(0) && !IsPointerOverUi())
            {
                AudioManger.AudioManager.Instance.PlaySFX(SoundType.Move);
                isInside = !isInside;
                float target = isInside ? insideRadius * transform.parent.localScale.x : outsideRadius* transform.parent.localScale.x;
                DOTween.To(() => radius, r => radius = r, target, moveTime)
                       .SetEase(Ease.OutBack);
            }

            float rad = angle * Mathf.Deg2Rad;
            transform.position = center.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * radius;
        }

        private static bool IsPointerOverUi()
        {
            if (!EventSystem.current)
                return false;
            if (Input.touchCount > 0)
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            return EventSystem.current.IsPointerOverGameObject();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!canMove)
                return;

            if (other.CompareTag("Obsticle"))
            {
                if (ShieldActive)
                {
                    shieldHitsRemaining--;
                    PowerupStateChanged?.Invoke();
                    PowerupFeedback?.Invoke(PowerupFeedbackType.ShieldBlocked);
                    if (!ShieldActive)
                        PowerupFeedback?.Invoke(PowerupFeedbackType.ShieldDepleted);
                }
                else
                    OnGameEnded.Raise();
            }
            else
            {
                _score += increasedScoreByPickingStars * ScoreMultiplier;
                OnScoreChanged.Raise(_score);
                AudioManger.AudioManager.Instance.PlaySFX(SoundType.Score);
            }
        }

        private void Die()
        {
            canMove = false;
            shieldHitsRemaining = 0;
            doubleScoreRemaining = 0f;
            activeScoreMultiplier = 1;
            PowerupStateChanged?.Invoke();
            AudioManger.AudioManager.Instance.PlaySFX(SoundType.Explosion);
            speed = 0;

            if (dieParticle)
                dieParticle.Play();
            
            Sequence dieSequence = DOTween.Sequence();
            dieSequence.Append(transform.DOScale(Vector3.zero, dieScaleTime).SetEase(Ease.InBack));
            dieSequence.Join(transform.DORotate(new Vector3(0, 0, 720f), dieRotationTime, RotateMode.FastBeyond360));
            
            if (_spriteRenderer)
                dieSequence.Join(_spriteRenderer.DOFade(0f, dieFadeTime));

            dieSequence.OnComplete(() =>
            {
                float rad = 0f;
                transform.position = center.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * radius;
                
                gameObject.SetActive(false);
                DOTween.Kill(transform);
                
                if (_spriteRenderer)
                    _spriteRenderer.color = _firstColor;
                
                angle = 0f;
                lapProgress = 0f;
                radius = outsideRadius* transform.parent.localScale.x;
                transform.localScale = _firstLocalScale;
                isInside = false;
                canMove = false;
                speed = _firstSpeed;
                _score = 0;
            });
        }
    }
    
}
