using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ui
{
    [RequireComponent(typeof(Button))]
    public class ButtonClickAnimationDOTween : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerExitHandler
    {
        [Header("Animation Settings")]
        [SerializeField]
        private float pressedScale = 0.9f;

        [SerializeField] private float tweenDuration = 0.1f;
        [SerializeField] private Ease easeType = Ease.OutBack;
        [SerializeField] private bool useUnscaledTime = true;

        private RectTransform rt;
        private Vector3 originalScale;
        private Tween currentTween;
        private Button button;

        void Awake()
        {
            rt = transform as RectTransform;
            button = GetComponent<Button>();
            originalScale = rt.localScale;
        }

        void OnEnable()
        {
            KillTween();
            rt.localScale = originalScale;
        }

        void OnDisable()
        {
            KillTween();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractableOrRaycastable()) return;
            AnimateTo(originalScale * pressedScale, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsInteractableOrRaycastable()) return;
            AnimateTo(originalScale, easeType);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!IsInteractableOrRaycastable()) return;
            AnimateTo(originalScale, easeType);
        }

        private bool IsInteractableOrRaycastable()
        {
            return button != null && button.interactable;
        }

        private void AnimateTo(Vector3 targetScale, Ease ease)
        {
            KillTween();
            currentTween = rt
                .DOScale(targetScale, tweenDuration)
                .SetEase(ease)
                .SetUpdate(useUnscaledTime); // more reliable with timescale changes
        }

        private void KillTween()
        {
            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
                currentTween = null;
            }
        }
    }
}
