// ============================================================
//  TrayManager.cs
//
//  This is the heart of the puzzle game.
//
//  Responsibilities
//  ────────────────
//  • Maintain an ordered list of BallControllers in the tray
//    (max 7 slots, left-to-right).
//  • Accept balls from BallController.OnPointerClick.
//  • Run the match-3 check + compact (shift-left) algorithm.
//  • Track remaining balls on the board to detect win condition.
//  • Broadcast OnLevelComplete / OnLevelFailed static events so
//    GameStateManager can react with ZERO coupling to this class.
//
//  No reference to UIManager or SceneManager is held here.
//
//  Algorithm summary (see detailed comments inside CheckAndResolveMatches)
//  ────────────────────────────────────────────────────────────
//  After each ball is added we:
//  1. Sort the tray list by color so same-color balls cluster.
//  2. Scan the sorted list for any run of exactly 3+ same-color balls.
//  3. If found, remove those 3 from the list AND destroy their GameObjects.
//  4. Compact: the List<T> itself becomes the logical "left-shift" — no
//     explicit shifting is needed; we just re-render slot positions.
//  5. Repeat from step 1 until no more matches (chain reactions).
//  6. After all chains resolve, test win/lose conditions.
// ============================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame
{
    public class TrayManager : MonoBehaviour
    {
        // ── Static events — the ONLY coupling to GameStateManager ──
        /// <summary>Fired when the board is cleared.  GameStateManager listens.</summary>
        public static event Action OnLevelComplete;

        /// <summary>Fired when the tray fills with no match. GameStateManager listens.</summary>
        public static event Action OnLevelFailed;

        // ── Singleton ─────────────────────────────────────────
        public static TrayManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────
        [Header("Tray Configuration")]
        [Tooltip("World-space positions of the 7 tray slots, left to right.")]
        [SerializeField] private Transform[] slotTransforms;   // length must be 7

        [Header("Timing")]
        [Tooltip("Seconds to wait between checking chain reactions.")]
        [SerializeField] private float chainReactionDelay = 0.35f;

        [Tooltip("Seconds balls take to slide into their new positions after a match.")]
        [SerializeField] private float shiftDuration = 0.15f;

        // ── Constants ─────────────────────────────────────────
        private const int TrayCapacity  = 7;
        private const int MatchCount    = 3;    // change to 2 or 4 for variants

        // ── State ─────────────────────────────────────────────
        /// <summary>Ordered list of balls currently in the tray (index 0 = leftmost).</summary>
        private readonly List<BallController> _tray = new(TrayCapacity);

        /// <summary>All balls that were on the board at level start.</summary>
        private readonly HashSet<BallController> _boardBalls = new();

        private bool _isProcessing;     // lock taps while match animation plays
        private bool _gameOver;         // prevents double-fire of end events

        // ── Unity lifecycle ───────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (slotTransforms == null || slotTransforms.Length != TrayCapacity)
                Debug.LogError($"[TrayManager] slotTransforms must have exactly " +
                               $"{TrayCapacity} entries. Found: {slotTransforms?.Length}");
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>
        /// Register a ball that exists on the board at level start.
        /// Call this from your level-setup code (e.g. a LevelBuilder).
        /// </summary>
        public void RegisterBoardBall(BallController ball)
        {
            if (ball != null)
                _boardBalls.Add(ball);
        }

        /// <summary>
        /// Called by BallController when the player taps a board ball.
        /// Returns false if the tray is full or if processing is locked.
        /// </summary>
        public bool AddBall(BallController ball)
        {
            if (_isProcessing || _gameOver) return false;
            if (_tray.Count >= TrayCapacity) return false;

            // ── Step 1: Move the ball from the board into the tray list ──
            _boardBalls.Remove(ball);
            _tray.Add(ball);

            // ── Step 2: Sort tray so matching colors cluster together ──
            //  Sorting is what makes the match-3 scan O(n) instead of O(n²).
            //  We sort by color enum value (stable integer comparison).
            //  Balls of the same color will always be adjacent after sort.
            SortTray();

            // ── Step 3: Move ball GameObject to its new (sorted) tray slot ──
            //  We animate ALL balls to their current logical slot positions
            //  because the sort may have reshuffled them.
            int newIndex = _tray.IndexOf(ball);
            ball.MoveToSlot(GetSlotPosition(newIndex), onComplete: null);

            // Slide any balls that were displaced by the sort.
            RefreshAllSlotPositions(except: ball);

            // ── Step 4: Run match-check pipeline (coroutine for timing) ──
            StartCoroutine(ProcessMatchPipeline());
            return true;
        }

        // ── Core Algorithm ────────────────────────────────────

        /// <summary>
        /// Master pipeline: check → destroy → compact → repeat until stable.
        /// Runs as a coroutine so animations have time to play between steps.
        /// </summary>
        private IEnumerator ProcessMatchPipeline()
        {
            _isProcessing = true;

            // Keep resolving until no more matches exist (handles chain reactions).
            bool matchFound;
            do
            {
                yield return new WaitForSeconds(chainReactionDelay);

                matchFound = CheckAndResolveMatches();

                if (matchFound)
                {
                    // Let destruction animations finish before the next iteration.
                    yield return new WaitForSeconds(chainReactionDelay);

                    // Compact the tray: re-render remaining balls to fill gaps.
                    // The List<BallController> is already compact (no nulls) because
                    // we called _tray.RemoveRange() during destruction.
                    // We only need to re-animate positions.
                    yield return StartCoroutine(AnimateShift());
                }
            }
            while (matchFound);

            // ── Step 5: Post-resolution condition checks ──────
            _isProcessing = false;
            EvaluateEndConditions();
        }

        /// <summary>
        /// THE CORE ALGORITHM
        /// ──────────────────
        /// Scans the sorted tray for a run of ≥ MatchCount (3) same-color balls.
        ///
        /// Because the tray was sorted before this call, all balls of the same
        /// color are contiguous.  A single left-to-right scan (O(n)) is enough:
        ///
        ///   Index:  0    1    2    3    4
        ///   Color: [R]  [R]  [R]  [B]  [G]
        ///           ↑ run start           ↑ run breaks here → remove indices 0-2
        ///
        /// The moment a run of exactly 3 is found we break immediately and return
        /// true.  The outer coroutine calls us again until we return false.
        ///
        /// Returns true if a match was found and removed.
        /// </summary>
        private bool CheckAndResolveMatches()
        {
            if (_tray.Count < MatchCount) return false;

            int     runStart = 0;
            BallColor runColor = _tray[0].Color;

            for (int i = 1; i <= _tray.Count; i++)
            {
                // Either we hit the end of the list, or the color changed.
                bool endOfRun = (i == _tray.Count) || (_tray[i].Color != runColor);

                if (endOfRun)
                {
                    int runLength = i - runStart;

                    if (runLength >= MatchCount)
                    {
                        // Found a match!  Destroy exactly MatchCount balls
                        // starting from runStart.
                        DestroyMatchedBalls(runStart, MatchCount);
                        return true;
                    }

                    // No match in this run — advance the run window.
                    if (i < _tray.Count)
                    {
                        runStart = i;
                        runColor = _tray[i].Color;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Remove <paramref name="count"/> balls from the tray list starting at
        /// <paramref name="startIndex"/> and trigger their destroy animations.
        ///
        /// LEFT-SHIFT (compact) explanation
        /// ─────────────────────────────────
        /// We use List<BallController> not a fixed array.
        /// List.RemoveRange(start, count) internally copies all elements after
        /// the removed range one slot to the left — exactly the "shift left"
        /// mechanic the game requires.  No manual loop is needed.
        ///
        ///   Before: [R, R, R, B, G]   (indices 0-4)
        ///   RemoveRange(0, 3)
        ///   After:  [B, G]            (B is now index 0, G is index 1)
        ///
        /// The visual slide to new positions is handled by AnimateShift().
        /// </summary>
        private void DestroyMatchedBalls(int startIndex, int count)
        {
            // Grab references BEFORE removing from list.
            var toDestroy = new List<BallController>(count);
            for (int i = startIndex; i < startIndex + count; i++)
                toDestroy.Add(_tray[i]);

            // Compact the list immediately (logical shift-left).
            _tray.RemoveRange(startIndex, count);

            // Trigger visual destruction on each ball.
            foreach (var ball in toDestroy)
                ball.PlayDestroyAnimation();
        }

        // ── End conditions ────────────────────────────────────

        private void EvaluateEndConditions()
        {
            if (_gameOver) return;

            // WIN: no balls left on the board AND tray is empty (or cleared).
            if (_boardBalls.Count == 0 && _tray.Count == 0)
            {
                _gameOver = true;
                Debug.Log("[TrayManager] Level Complete!");
                OnLevelComplete?.Invoke();
                return;
            }

            // Also win if board is clear even if tray has some balls
            // (not a game-over scenario, remaining tray balls are fine).
            if (_boardBalls.Count == 0)
            {
                _gameOver = true;
                Debug.Log("[TrayManager] Level Complete! (board cleared)");
                OnLevelComplete?.Invoke();
                return;
            }

            // LOSE: tray is completely full after all chains resolved.
            if (_tray.Count >= TrayCapacity)
            {
                _gameOver = true;
                Debug.Log("[TrayManager] Level Failed — tray is full.");
                OnLevelFailed?.Invoke();
            }
        }

        // ── Visual / Animation helpers ────────────────────────

        /// <summary>
        /// Sort the logical tray list by color.
        /// This is the key that makes the match-3 scan simple and fast.
        /// </summary>
        private void SortTray()
        {
            _tray.Sort((a, b) => ((int)a.Color).CompareTo((int)b.Color));
        }

        /// <summary>
        /// Animate all remaining tray balls sliding to their correct slot positions.
        /// Called after a DestroyMatchedBalls to close the gaps visually.
        /// </summary>
        private IEnumerator AnimateShift()
        {
            // Kick off all moves in parallel.
            int remaining = _tray.Count;
            if (remaining == 0) yield break;

            int doneCount = 0;
            for (int i = 0; i < _tray.Count; i++)
            {
                int capturedIndex = i;
                _tray[i].MoveToSlot(GetSlotPosition(capturedIndex),
                    onComplete: () => doneCount++);
            }

            // Wait until every ball has finished sliding.
            float timeout = shiftDuration + 0.5f;
            float elapsed = 0f;
            while (doneCount < remaining && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Immediately teleport all tray balls to their correct positions
        /// (used for instant sort when a new ball is added — excludes the
        /// just-added ball which is already being animated by MoveToSlot).
        /// </summary>
        private void RefreshAllSlotPositions(BallController except = null)
        {
            for (int i = 0; i < _tray.Count; i++)
            {
                if (_tray[i] == except) continue;
                _tray[i].MoveToSlot(GetSlotPosition(i));
            }
        }

        private Vector3 GetSlotPosition(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slotTransforms.Length)
            {
                Debug.LogError($"[TrayManager] Invalid slot index: {slotIndex}");
                return Vector3.zero;
            }
            return slotTransforms[slotIndex].position;
        }
    }
}
