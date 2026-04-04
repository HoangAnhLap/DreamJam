// ============================================================
//  Popups.cs  —  Concrete popup implementations
//
//  Each class:
//  1. Inherits BasePopup (gets fade, CanvasGroup, lifecycle).
//  2. Overrides OnShow / OnHide for its own data refresh logic.
//  3. Exposes button-handler methods wired up in the Inspector.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameFramework.Core;

namespace GameFramework.UI
{
    // ══════════════════════════════════════════════════════════
    //  SETTINGS POPUP
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Shown whenever the game enters the <see cref="GameState.Paused"/> state.
    /// Provides volume sliders, sound toggles, and a resume button.
    /// </summary>
    public class SettingsPopup : BasePopup
    {
        [Header("Settings Controls")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle muteToggle;

        protected override void OnShow()
        {
            // Refresh UI to match current settings (read from a SettingsService, etc.)
            if (musicVolumeSlider) musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
            if (sfxVolumeSlider)   sfxVolumeSlider.value   = PlayerPrefs.GetFloat("SFXVolume", 1f);
            if (muteToggle)        muteToggle.isOn          = PlayerPrefs.GetInt("Muted", 0) == 1;
        }

        protected override void OnHide()
        {
            // Persist any changes the player made.
            if (musicVolumeSlider) PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
            if (sfxVolumeSlider)   PlayerPrefs.SetFloat("SFXVolume",   sfxVolumeSlider.value);
            PlayerPrefs.Save();
        }

        // ── Button handlers (wire these in the Inspector) ─────

        /// <summary>Called by the Resume button.</summary>
        public void OnResumeClicked()
        {
            // Resume transitions: Paused → Playing.
            GameStateManager.Instance?.ChangeState(GameState.Playing);
            // UIManager will call HideAllPopups via the state-change event.
        }

        /// <summary>Called by the Main Menu button inside Settings.</summary>
        public void OnMainMenuClicked()
        {
            GameStateManager.Instance?.ChangeState(GameState.MainMenu);
            SceneLoader.Instance?.LoadScene("MainMenu");
        }
    }


    // ══════════════════════════════════════════════════════════
    //  WIN POPUP
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Shown automatically when <see cref="GameState.Win"/> is entered.
    /// Displays score, stars earned, and navigation buttons.
    /// </summary>
    public class WinPopup : BasePopup
    {
        [Header("Win UI References")]
        [SerializeField] private TextMeshProUGUI scoreLabel;
        [SerializeField] private TextMeshProUGUI titleLabel;

        // Injected data — your game code calls SetResults before the state changes.
        private int _finalScore;
        private int _starsEarned;

        /// <summary>
        /// Call this from your game logic BEFORE changing state to Win.
        /// e.g.  UIManager.Instance.GetPopup<WinPopup>()?.SetResults(score, stars);
        ///       GameStateManager.Instance.ChangeState(GameState.Win);
        /// </summary>
        public void SetResults(int score, int stars)
        {
            _finalScore  = score;
            _starsEarned = stars;
        }

        protected override void OnShow()
        {
            if (scoreLabel) scoreLabel.text = $"Score: {_finalScore:N0}";
            if (titleLabel) titleLabel.text = _starsEarned == 3 ? "Perfect!" : "Level Complete!";
        }

        // ── Button handlers ───────────────────────────────────

        /// <summary>Load the next level.</summary>
        public void OnNextLevelClicked()
        {
            string nextScene = LevelRegistry.GetNextSceneName(); // your own service
            GameStateManager.Instance?.ChangeState(GameState.Loading);
            SceneLoader.Instance?.LoadScene(nextScene);
        }

        /// <summary>Replay the current level.</summary>
        public void OnReplayClicked()
        {
            string current = SceneLoader.Instance?.CurrentSceneName ?? "Level_01";
            GameStateManager.Instance?.ChangeState(GameState.Loading);
            SceneLoader.Instance?.LoadScene(current);
        }

        /// <summary>Return to the main menu.</summary>
        public void OnMainMenuClicked()
        {
            GameStateManager.Instance?.ChangeState(GameState.MainMenu);
            SceneLoader.Instance?.LoadScene("MainMenu");
        }
    }


    // ══════════════════════════════════════════════════════════
    //  LOSE POPUP
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Shown automatically when <see cref="GameState.Lose"/> is entered.
    /// </summary>
    public class LosePopup : BasePopup
    {
        [Header("Lose UI References")]
        [SerializeField] private TextMeshProUGUI messageLabel;
        [SerializeField] private TextMeshProUGUI scoreLabel;

        private int _finalScore;

        public void SetResults(int score)
        {
            _finalScore = score;
        }

        protected override void OnShow()
        {
            if (scoreLabel)   scoreLabel.text   = $"Score: {_finalScore:N0}";
            if (messageLabel) messageLabel.text = "Better luck next time!";
        }

        // ── Button handlers ───────────────────────────────────

        public void OnRetryClicked()
        {
            string current = SceneLoader.Instance?.CurrentSceneName ?? "Level_01";
            GameStateManager.Instance?.ChangeState(GameState.Loading);
            SceneLoader.Instance?.LoadScene(current);
        }

        public void OnMainMenuClicked()
        {
            GameStateManager.Instance?.ChangeState(GameState.MainMenu);
            SceneLoader.Instance?.LoadScene("MainMenu");
        }
    }


    // ══════════════════════════════════════════════════════════
    //  PLACEHOLDER — remove/replace with your real level registry
    // ══════════════════════════════════════════════════════════
    internal static class LevelRegistry
    {
        public static string GetNextSceneName() => "Level_02";
    }
}
