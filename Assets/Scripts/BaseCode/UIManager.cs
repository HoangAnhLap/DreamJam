// ============================================================
//  UIManager.cs  —  System 3: UI / Popup Manager
//
//  Responsibilities
//  ────────────────
//  • Maintain a registry of all known popups (by string key).
//  • React to GameEvents.OnGameStateChanged to auto-show the
//    correct popup for Win / Lose / Pause states.
//  • Provide a public API so any gameplay script can show/hide
//    popups without holding a direct reference to them.
//
//  Pattern: Singleton MonoBehaviour + Observer (GameEvents)
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using GameFramework.Core;

namespace GameFramework.UI
{
    public class UIManager : MonoBehaviour
    {
        // ── Well-known popup keys (constants prevent typo bugs) ──
        public const string KEY_SETTINGS = "Settings";
        public const string KEY_WIN      = "Win";
        public const string KEY_LOSE     = "Lose";

        // ── Singleton ─────────────────────────────────────────
        public static UIManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────
        [Header("Auto-Registered Popups")]
        [Tooltip("Drag popup GameObjects that live in this scene here. " +
                 "They are registered automatically on Awake.")]
        [SerializeField] private BasePopup[] startupPopups;

        // ── Registry ─────────────────────────────────────────
        private readonly Dictionary<string, BasePopup> _registry = new();

        // ── Unity lifecycle ───────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Register any popups wired up in the Inspector.
            foreach (var popup in startupPopups)
            {
                if (popup != null)
                    RegisterPopup(popup);
            }
        }

        private void OnEnable()
        {
            // Subscribe to the event bus — no direct reference to GameStateManager needed.
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>
        /// Add a popup to the registry at runtime.
        /// Useful for popups spawned from prefabs during gameplay.
        /// </summary>
        public void RegisterPopup(BasePopup popup)
        {
            if (popup == null)
            {
                Debug.LogError("[UIManager] Tried to register a null popup.");
                return;
            }

            if (_registry.ContainsKey(popup.PopupKey))
            {
                Debug.LogWarning($"[UIManager] Popup '{popup.PopupKey}' is already " +
                                 "registered. Overwriting.");
            }

            _registry[popup.PopupKey] = popup;
            Debug.Log($"[UIManager] Registered popup: '{popup.PopupKey}'");
        }

        /// <summary>
        /// Remove a popup from the registry (e.g. when its scene unloads).
        /// </summary>
        public void UnregisterPopup(string key)
        {
            _registry.Remove(key);
        }

        /// <summary>Show the popup with the given key.</summary>
        public void ShowPopup(string key)
        {
            if (!TryGetPopup(key, out BasePopup popup)) return;
            if (popup.IsVisible) return;

            popup.Show();
            GameEvents.RaisePopupShown(key);
            Debug.Log($"[UIManager] Showing popup: '{key}'");
        }

        /// <summary>Hide the popup with the given key.</summary>
        public void HidePopup(string key)
        {
            if (!TryGetPopup(key, out BasePopup popup)) return;
            if (!popup.IsVisible) return;

            popup.Hide();
            GameEvents.RaisePopupHidden(key);
            Debug.Log($"[UIManager] Hiding popup: '{key}'");
        }

        /// <summary>Hide every currently visible popup.</summary>
        public void HideAllPopups()
        {
            foreach (var kvp in _registry)
            {
                if (kvp.Value.IsVisible)
                    HidePopup(kvp.Key);
            }
        }

        /// <summary>Returns true if the named popup is currently visible.</summary>
        public bool IsPopupVisible(string key)
            => _registry.TryGetValue(key, out var popup) && popup.IsVisible;

        // ── Event handler ─────────────────────────────────────

        /// <summary>
        /// This is the bridge between GameStateManager and the UI layer.
        /// GameStateManager raises the event; UIManager reacts.
        /// Neither holds a reference to the other.
        /// </summary>
        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Playing:
                    // Ensure no stale popups are visible when gameplay starts.
                    HideAllPopups();
                    break;

                case GameState.Paused:
                    ShowPopup(KEY_SETTINGS);
                    break;

                case GameState.Win:
                    ShowPopup(KEY_WIN);
                    break;

                case GameState.Lose:
                    ShowPopup(KEY_LOSE);
                    break;

                case GameState.MainMenu:
                    HideAllPopups();
                    break;
            }
        }

        // ── Private helpers ───────────────────────────────────

        private bool TryGetPopup(string key, out BasePopup popup)
        {
            if (_registry.TryGetValue(key, out popup)) return true;

            Debug.LogError($"[UIManager] No popup registered under key '{key}'. " +
                           "Did you forget to call RegisterPopup?");
            return false;
        }
    }
}
