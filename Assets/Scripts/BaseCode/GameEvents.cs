// ============================================================
//  GameEvents.cs  —  Central Event Bus
//  Decouples all managers via C# Action delegates.
//  No system needs a direct reference to another.
// ============================================================
using System;

namespace GameFramework.Core
{
    /// <summary>
    /// Static event hub.  Any system can raise or subscribe to
    /// these events without holding a reference to the publisher.
    /// </summary>
    public static class GameEvents
    {
        // ── Game State ────────────────────────────────────────
        /// <summary>Fired by GameStateManager whenever the state changes.</summary>
        public static event Action<GameState> OnGameStateChanged;

        // ── Scene ─────────────────────────────────────────────
        /// <summary>Fired just before a new scene begins loading.</summary>
        public static event Action<string> OnSceneLoadStarted;

        /// <summary>Fired when a scene has finished loading (progress = 1).</summary>
        public static event Action<string> OnSceneLoadCompleted;

        /// <summary>Fired when a scene has fully unloaded.</summary>
        public static event Action<string> OnSceneUnloaded;

        // ── UI / Popup ────────────────────────────────────────
        /// <summary>Fired by UIManager after a popup becomes visible.</summary>
        public static event Action<string> OnPopupShown;

        /// <summary>Fired by UIManager after a popup is hidden.</summary>
        public static event Action<string> OnPopupHidden;

        // ── Internal raise helpers (only managers should call these) ──

        internal static void RaiseGameStateChanged(GameState newState)
            => OnGameStateChanged?.Invoke(newState);

        internal static void RaiseSceneLoadStarted(string sceneName)
            => OnSceneLoadStarted?.Invoke(sceneName);

        internal static void RaiseSceneLoadCompleted(string sceneName)
            => OnSceneLoadCompleted?.Invoke(sceneName);

        internal static void RaiseSceneUnloaded(string sceneName)
            => OnSceneUnloaded?.Invoke(sceneName);

        internal static void RaisePopupShown(string popupKey)
            => OnPopupShown?.Invoke(popupKey);

        internal static void RaisePopupHidden(string popupKey)
            => OnPopupHidden?.Invoke(popupKey);
    }
}
