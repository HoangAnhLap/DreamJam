// ============================================================
//  ExampleLevelController.cs
//
//  Drop this on any GameObject in a gameplay scene.
//  It shows the minimum code needed to interact with all three
//  core systems without importing UIManager or SceneLoader
//  directly (only GameStateManager is needed).
// ============================================================
using UnityEngine;
using GameFramework.Core;
using GameFramework.UI;

namespace GameFramework.Example
{
    public class ExampleLevelController : MonoBehaviour
    {
        // ── Simulated game state ──────────────────────────────
        private int  _score       = 0;
        private bool _levelActive = false;

        // ── Unity lifecycle ───────────────────────────────────
        private void OnEnable()
        {
            // React to state changes if you need to.
            GameEvents.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= OnGameStateChanged;
        }

        private void Update()
        {
            if (!_levelActive) return;

            // ── Demo: press W to win, L to lose, P to pause ──
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.W)) TriggerWin();
            if (Input.GetKeyDown(KeyCode.L)) TriggerLose();
            if (Input.GetKeyDown(KeyCode.P)) TriggerPause();
#endif
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>
        /// Call this when the player meets the win condition.
        ///
        /// Flow:
        ///   1. Prepare popup data (score).
        ///   2. Change state → Win.
        ///   3. GameStateManager fires OnGameStateChanged.
        ///   4. UIManager receives the event → shows WinPopup.
        ///
        /// Note: the level controller never references UIManager.
        /// </summary>
        public void TriggerWin()
        {
            if (!_levelActive) return;
            _levelActive = false;

            _score += 500; // final bonus

            // Optionally pre-populate the popup with data.
            // We ask UIManager for the popup instance only here — still decoupled from show/hide logic.
            if (UIManager.Instance != null)
            {
                // This is a safe opt-in: if UIManager isn't present, nothing breaks.
                // Alternatively, expose a static WinPopup.SetResults() method.
                // For now we call it through a helper that resolves the registered popup.
                SetWinPopupData(_score, starsEarned: 2);
            }

            // ONE LINE to drive the entire system:
            GameStateManager.Instance.ChangeState(GameState.Win);
        }

        /// <summary>
        /// Call this when the player fails the level.
        /// Identical flow to TriggerWin() but ends in GameState.Lose.
        /// </summary>
        public void TriggerLose()
        {
            if (!_levelActive) return;
            _levelActive = false;

            SetLosePopupData(_score);
            GameStateManager.Instance.ChangeState(GameState.Lose);
        }

        /// <summary>Open Settings (pauses the game).</summary>
        public void TriggerPause()
        {
            if (GameStateManager.Instance.CurrentState != GameState.Playing) return;
            GameStateManager.Instance.ChangeState(GameState.Paused);
        }

        // ── Private ───────────────────────────────────────────

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Playing)
                _levelActive = true;
        }

        private void SetWinPopupData(int score, int starsEarned)
        {
            // Resolve from registry without caching a hard reference.
            // Pattern: ask UIManager for the popup only when needed.
            // (WinPopup.SetResults is called before the state change triggers Show.)
            // In a more advanced setup, pass data through a dedicated WinData ScriptableObject.
            if (UIManager.Instance == null) return;

            // We expose a method on the concrete popup type.
            // The cast is intentional: the level controller DOES know about WinPopup.
            // What it does NOT do is call Show() or Hide() — that belongs to UIManager.
            var winPopupGO = GameObject.FindWithTag("WinPopup"); // or use a typed reference
            if (winPopupGO && winPopupGO.TryGetComponent<WinPopup>(out var winPopup))
                winPopup.SetResults(score, starsEarned);
        }

        private void SetLosePopupData(int score)
        {
            var losePopupGO = GameObject.FindWithTag("LosePopup");
            if (losePopupGO && losePopupGO.TryGetComponent<LosePopup>(out var losePopup))
                losePopup.SetResults(score);
        }
    }
}
