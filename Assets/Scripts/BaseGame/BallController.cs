// ============================================================
//  BallController.cs
//
//  Responsibilities
//  ────────────────
//  • Stores the ball's color identity.
//  • Listens for player tap/click via Unity's IPointerClickHandler.
//  • On click, tells TrayManager to accept this ball and animates
//    itself from board position to the assigned tray slot.
//  • Does NOT know about game state, win/lose conditions, or UI.
//
//  Architecture note
//  ─────────────────
//  BallController → TrayManager (one-way call: AddBall)
//  TrayManager → GameStateManager (via static events, no reference)
// ============================================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PuzzleGame
{
    [RequireComponent(typeof(Collider2D))]
    public class BallController : MonoBehaviour, IPointerClickHandler
    {
        // ── Inspector ─────────────────────────────────────────
        [Header("Identity")]
        [SerializeField] private BallColor ballColor = BallColor.Red;

        [Header("Visual")]
        [Tooltip("Assign the SpriteRenderer so we can tint it at runtime.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Animation")]
        [SerializeField] private float moveDuration = 0.25f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // ── Public read-only data ─────────────────────────────
        public BallColor Color   => ballColor;
        public bool      IsInert => _isInert;   // true once added to tray

        // ── Private ───────────────────────────────────────────
        private bool _isInert;          // prevents double-tap
        private Coroutine _moveRoutine;

        // ── Unity lifecycle ───────────────────────────────────
        private void Awake()
        {
            ApplyColorTint();
        }

        // ── Runtime initialisation (called by LevelBuilder) ───
        /// <summary>Set color after Instantiate. Triggers tint refresh.</summary>
        public void Init(BallColor color)
        {
            ballColor = color;
            ApplyColorTint();
        }

        // ── IPointerClickHandler ──────────────────────────────
        /// <summary>
        /// Called by Unity's EventSystem when the player taps/clicks this ball.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isInert) return;   // already in tray or mid-flight

            // Delegate to TrayManager.  If the tray refuses (e.g. it's full
            // and it's a lose state), AddBall returns false and we ignore.
            TrayManager.Instance?.AddBall(this);
        }

        // ── Public API (called by TrayManager) ────────────────

        /// <summary>
        /// Animate this ball from its current position to <paramref name="targetWorldPos"/>
        /// then invoke <paramref name="onComplete"/>.
        /// Called by TrayManager immediately after accepting the ball.
        /// </summary>
        public void MoveToSlot(Vector3 targetWorldPos, Action onComplete = null)
        {
            _isInert = true;    // lock input during flight

            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            _moveRoutine = StartCoroutine(MoveRoutine(targetWorldPos, onComplete));
        }

        /// <summary>
        /// Play a pop/destroy animation then remove the GameObject.
        /// Called by TrayManager when a match-3 is confirmed.
        /// </summary>
        public void PlayDestroyAnimation(Action onComplete = null)
        {
            StartCoroutine(DestroyRoutine(onComplete));
        }

        // ── Helpers ───────────────────────────────────────────

        private void ApplyColorTint()
        {
            if (spriteRenderer == null) return;

            spriteRenderer.color = ballColor switch
            {
                BallColor.Red    => new Color(0.90f, 0.25f, 0.25f),
                BallColor.Blue   => new Color(0.20f, 0.50f, 0.90f),
                BallColor.Green  => new Color(0.20f, 0.75f, 0.30f),
                BallColor.Yellow => new Color(0.95f, 0.85f, 0.10f),
                BallColor.Purple => new Color(0.60f, 0.25f, 0.85f),
                BallColor.Orange => new Color(0.95f, 0.55f, 0.10f),
                BallColor.Pink   => new Color(0.95f, 0.45f, 0.70f),
                _                => UnityEngine.Color.white
            };
        }

        private IEnumerator MoveRoutine(Vector3 target, Action onComplete)
        {
            Vector3 start   = transform.position;
            float   elapsed = 0f;

            // Lift the ball above other scene elements while in flight.
            Vector3 sortingOffset = new Vector3(0, 0, -1f);
            transform.position += sortingOffset;

            while (elapsed < moveDuration)
            {
                elapsed           += Time.deltaTime;
                float t            = moveCurve.Evaluate(elapsed / moveDuration);
                transform.position = Vector3.LerpUnclamped(start, target + sortingOffset, t);
                yield return null;
            }

            transform.position = target;
            onComplete?.Invoke();
        }

        private IEnumerator DestroyRoutine(Action onComplete)
        {
            // Simple scale-down pop.  Replace with your particle FX as needed.
            float duration = 0.15f;
            float elapsed  = 0f;
            Vector3 start  = transform.localScale;

            while (elapsed < duration)
            {
                elapsed             += Time.deltaTime;
                float t              = elapsed / duration;
                transform.localScale = Vector3.Lerp(start, Vector3.zero, t);
                yield return null;
            }

            onComplete?.Invoke();
            Destroy(gameObject);
        }
    }
}
