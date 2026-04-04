// ============================================================
//  GameStateManager.cs  —  System 1: Game State Manager
//
//  Responsibilities
//  ────────────────
//  • Own the single source of truth for the current GameState.
//  • Validate legal state transitions (guard clauses).
//  • Broadcast changes through GameEvents so every other system
//    can react without coupling back to this class.
//
//  Pattern: Singleton MonoBehaviour (DontDestroyOnLoad)
// ============================================================
using UnityEngine;

namespace GameFramework.Core
{
    public class GameStateManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────
        public static GameStateManager Instance { get; private set; }

        // ── State ─────────────────────────────────────────────
        /// <summary>Read-only access to the current state.</summary>
        public GameState CurrentState { get; private set; } = GameState.None;

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

            // Kick off the very first state transition.
            ChangeState(GameState.Initializing);
        }

        private void Start()
        {
            // After the first frame (all Awake() calls are done),
            // transition from Initializing → MainMenu.
            // In a real project you'd wait for async boot tasks here.
            ChangeState(GameState.MainMenu);
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>
        /// Request a state change.  Guards prevent illegal transitions.
        /// </summary>
        public void ChangeState(GameState newState)
        {
            if (!IsTransitionAllowed(CurrentState, newState))
            {
                Debug.LogWarning($"[GameStateManager] Illegal transition: " +
                                 $"{CurrentState} → {newState}. Request ignored.");
                return;
            }

            GameState previous = CurrentState;
            CurrentState = newState;

            Debug.Log($"[GameStateManager] {previous} → {CurrentState}");

            // Notify every listener (UIManager, SceneManager, Analytics, etc.)
            // without knowing any of them.
            GameEvents.RaiseGameStateChanged(CurrentState);

            // Handle any internal side-effects that belong
            // purely to state management (e.g. Time.timeScale).
            HandleStateEntered(CurrentState);
        }

        // ── Private helpers ───────────────────────────────────

        /// <summary>
        /// Define which state transitions are legal.
        /// Extend this table as your game grows.
        /// </summary>
        private bool IsTransitionAllowed(GameState from, GameState to)
        {
            // Allow everything from None (first boot).
            if (from == GameState.None) return true;

            return (from, to) switch
            {
                (GameState.Initializing, GameState.MainMenu)   => true,
                (GameState.MainMenu,     GameState.Loading)    => true,
                (GameState.Loading,      GameState.Playing)    => true,
                (GameState.Playing,      GameState.Paused)     => true,
                (GameState.Playing,      GameState.Win)        => true,
                (GameState.Playing,      GameState.Lose)       => true,
                (GameState.Paused,       GameState.Playing)    => true,
                (GameState.Paused,       GameState.MainMenu)   => true,
                (GameState.Win,          GameState.Loading)    => true,  // next level
                (GameState.Win,          GameState.MainMenu)   => true,
                (GameState.Lose,         GameState.Loading)    => true,  // retry
                (GameState.Lose,         GameState.MainMenu)   => true,
                _                                              => false
            };
        }

        /// <summary>
        /// Pure side-effects that belong inside the manager itself
        /// (e.g. pausing Unity's time scale).
        /// </summary>
        private void HandleStateEntered(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;

                // Win / Lose: keep time flowing so UI animations play.
                case GameState.Win:
                case GameState.Lose:
                    Time.timeScale = 1f;
                    break;

                default:
                    Time.timeScale = 1f;
                    break;
            }
        }
    }
}
