// ============================================================
//  BasePopup.cs  —  Abstract base for every UI popup/panel.
//
//  Each concrete popup (SettingsPopup, WinPopup, LosePopup, …)
//  inherits from this class.  UIManager interacts only with this
//  interface — it never knows the concrete type.
//
//  Pattern: Template Method  (Show/Hide logic lives here;
//           subclasses override OnShow / OnHide hooks).
// ============================================================
using System;
using UnityEngine;

namespace GameFramework.UI
{
    /// <summary>
    /// Attach this to the root GameObject of every popup prefab.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePopup : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────
        [Header("Popup Identity")]
        [Tooltip("Must match the key used in UIManager.RegisterPopup.")]
        [SerializeField] private string popupKey;

        [Header("Animation")]
        [Tooltip("Use a simple alpha fade if true; otherwise snap instantly.")]
        [SerializeField] private bool useFade = true;
        [SerializeField] private float fadeDuration = 0.2f;

        // ── State ─────────────────────────────────────────────
        public string PopupKey => popupKey;
        public bool   IsVisible { get; private set; }

        // Callbacks supplied by UIManager so it can chain logic.
        private Action _onShowComplete;
        private Action _onHideComplete;

        private CanvasGroup _canvasGroup;
        private Coroutine   _fadeCoroutine;

        // ── Unity lifecycle ───────────────────────────────────
        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            HideImmediate(); // Always start hidden.
        }

        // ── Public API (called by UIManager) ──────────────────

        /// <summary>Make this popup visible.  Fires OnShow hook.</summary>
        public void Show(Action onComplete = null)
        {
            _onShowComplete = onComplete;
            gameObject.SetActive(true);
            IsVisible = true;

            OnShow();

            if (useFade)
                StartFade(0f, 1f, fadeDuration, () =>
                {
                    _canvasGroup.interactable    = true;
                    _canvasGroup.blocksRaycasts  = true;
                    _onShowComplete?.Invoke();
                });
            else
            {
                _canvasGroup.alpha           = 1f;
                _canvasGroup.interactable    = true;
                _canvasGroup.blocksRaycasts  = true;
                _onShowComplete?.Invoke();
            }
        }

        /// <summary>Hide this popup.  Fires OnHide hook.</summary>
        public void Hide(Action onComplete = null)
        {
            _onHideComplete = onComplete;
            IsVisible = false;

            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            OnHide();

            if (useFade)
                StartFade(1f, 0f, fadeDuration, () =>
                {
                    gameObject.SetActive(false);
                    _onHideComplete?.Invoke();
                });
            else
            {
                HideImmediate();
                _onHideComplete?.Invoke();
            }
        }

        // ── Template-method hooks ─────────────────────────────

        /// <summary>Called at the start of Show, before any animation.</summary>
        protected virtual void OnShow() { }

        /// <summary>Called at the start of Hide, before any animation.</summary>
        protected virtual void OnHide() { }

        // ── Private helpers ───────────────────────────────────

        private void HideImmediate()
        {
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            IsVisible = false;
        }

        private void StartFade(float from, float to, float duration, Action onDone)
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeRoutine(from, to, duration, onDone));
        }

        private System.Collections.IEnumerator FadeRoutine(
            float from, float to, float duration, Action onDone)
        {
            float elapsed = 0f;
            _canvasGroup.alpha = from;

            while (elapsed < duration)
            {
                elapsed            += Time.unscaledDeltaTime; // unscaled: works while paused
                _canvasGroup.alpha  = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = to;
            onDone?.Invoke();
        }
    }
}
