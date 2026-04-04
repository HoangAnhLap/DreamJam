// ============================================================
//  SceneLoader.cs  —  System 2: Scene Manager
//
//  Responsibilities
//  ────────────────
//  • Load / unload scenes asynchronously with progress callbacks.
//  • Coordinate with GameStateManager (via GameEvents, not direct ref)
//    so it changes state to Loading before work starts and to Playing
//    when work finishes.
//  • Optionally keep a persistent "base" scene (the Bootstrap scene)
//    loaded at all times.
//
//  Pattern: Singleton MonoBehaviour (DontDestroyOnLoad)
// ============================================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFramework.Core
{
    public class SceneLoader : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────
        public static SceneLoader Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────
        [Header("Transition")]
        [Tooltip("Minimum seconds the loading screen is shown " +
                 "(prevents a jarring flash for fast loads).")]
        [SerializeField] private float minimumLoadTime = 0.5f;

        // ── State ─────────────────────────────────────────────
        /// <summary>Name of the scene that is currently active.</summary>
        public string CurrentSceneName { get; private set; }

        /// <summary>True while an async load or unload is in progress.</summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// Raised every frame during a load.  Value is 0..1.
        /// Subscribe in your loading screen UI to drive a progress bar.
        /// </summary>
        public event Action<float> OnLoadProgress;

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

            // Track the scene that was active when we booted.
            CurrentSceneName = SceneManager.GetActiveScene().name;
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>
        /// Load <paramref name="sceneName"/> asynchronously, replacing the
        /// current active scene.  GameState is driven automatically:
        ///   Playing → Loading → Playing (after load).
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning("[SceneLoader] Load request ignored: " +
                                 "a load is already in progress.");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        /// <summary>
        /// Additively load <paramref name="sceneName"/> without unloading
        /// the current scene.  Useful for overlay scenes (e.g. HUD).
        /// </summary>
        public void LoadSceneAdditive(string sceneName)
        {
            if (IsLoading) return;
            StartCoroutine(LoadSceneAdditiveRoutine(sceneName));
        }

        /// <summary>
        /// Unload an additively loaded scene.
        /// </summary>
        public void UnloadScene(string sceneName)
        {
            if (IsLoading) return;
            StartCoroutine(UnloadSceneRoutine(sceneName));
        }

        // ── Coroutines ────────────────────────────────────────

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            IsLoading = true;

            // 1. Tell everyone a load has started.
            GameEvents.RaiseSceneLoadStarted(sceneName);

            // 2. Ask GameStateManager to switch to Loading.
            //    We call it through the singleton because this IS a
            //    valid direct dependency (both are core infrastructure).
            GameStateManager.Instance?.ChangeState(GameState.Loading);

            // 3. Start the actual async operation (don't auto-activate).
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            float elapsed = 0f;

            // 4. Drive progress until Unity signals 0.9 (its internal cap).
            while (op.progress < 0.9f || elapsed < minimumLoadTime)
            {
                elapsed += Time.unscaledDeltaTime;

                // Normalise: Unity reports 0..0.9, we report 0..1.
                float progress = Mathf.Clamp01(op.progress / 0.9f);
                OnLoadProgress?.Invoke(progress);

                yield return null;
            }

            // 5. Activate the scene.
            OnLoadProgress?.Invoke(1f);
            op.allowSceneActivation = true;

            // Wait one frame for Unity to finish the swap.
            yield return null;

            CurrentSceneName = sceneName;
            IsLoading        = false;

            // 6. Notify listeners that the load completed.
            GameEvents.RaiseSceneLoadCompleted(sceneName);

            // 7. Transition back to Playing.
            GameStateManager.Instance?.ChangeState(GameState.Playing);
        }

        private IEnumerator LoadSceneAdditiveRoutine(string sceneName)
        {
            IsLoading = true;
            GameEvents.RaiseSceneLoadStarted(sceneName);

            AsyncOperation op = SceneManager.LoadSceneAsync(
                sceneName, LoadSceneMode.Additive);

            while (!op.isDone)
            {
                OnLoadProgress?.Invoke(op.progress);
                yield return null;
            }

            IsLoading = false;
            GameEvents.RaiseSceneLoadCompleted(sceneName);
        }

        private IEnumerator UnloadSceneRoutine(string sceneName)
        {
            IsLoading = true;

            AsyncOperation op = SceneManager.UnloadSceneAsync(sceneName);

            while (!op.isDone)
            {
                yield return null;
            }

            IsLoading = false;
            GameEvents.RaiseSceneUnloaded(sceneName);
        }
    }
}
