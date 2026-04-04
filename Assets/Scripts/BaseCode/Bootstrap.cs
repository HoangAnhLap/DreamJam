// ============================================================
//  Bootstrap.cs  —  Scene Entry Point  (attach to one empty
//                   GameObject in your "Bootstrap" scene)
//
//  The Bootstrap scene is the FIRST scene Unity loads (Build
//  Settings index 0).  It instantiates all persistent managers,
//  then immediately loads the Main Menu scene.
//
//  All subsequent scene loads go through SceneLoader, which
//  means this Bootstrap scene itself is never unloaded.
// ============================================================
using UnityEngine;

namespace GameFramework.Core
{
    public class Bootstrap : MonoBehaviour
    {
        [Header("Manager Prefabs")]
        [Tooltip("Prefab containing GameStateManager.")]
        [SerializeField] private GameObject gameStateManagerPrefab;

        [Tooltip("Prefab containing SceneLoader.")]
        [SerializeField] private GameObject sceneLoaderPrefab;

        [Tooltip("Prefab containing UIManager (with popup canvas).")]
        [SerializeField] private GameObject uiManagerPrefab;

        private void Awake()
        {
            // Instantiate managers that don't yet exist.
            // Each manager's Awake() handles its own DontDestroyOnLoad.
            EnsureManager(gameStateManagerPrefab, GameStateManager.Instance == null);
            EnsureManager(sceneLoaderPrefab,      SceneLoader.Instance == null);
            EnsureManager(uiManagerPrefab,        GameFramework.UI.UIManager.Instance == null);
        }

        private void Start()
        {
            // Hand off to the Main Menu.  SceneLoader will set GameState → Loading → Playing.
            // For the main menu we actually want MainMenu state, so we override after load.
            // A cleaner approach: subscribe to OnSceneLoadCompleted and set state there.
            SceneLoader.Instance.LoadScene("MainMenu");
        }

        // ── Helper ────────────────────────────────────────────

        private void EnsureManager(GameObject prefab, bool shouldCreate)
        {
            if (!shouldCreate || prefab == null) return;
            Instantiate(prefab);
        }
    }
}
