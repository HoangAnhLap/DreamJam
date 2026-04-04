// ============================================================
//  GameState.cs  —  All possible top-level game states.
//  Add new states here as your project grows.
// ============================================================
namespace GameFramework.Core
{
    public enum GameState
    {
        None,           // Default / unset
        Initializing,   // Boot: loading configs, ads SDK, etc.
        MainMenu,       // Main menu is active
        Loading,        // Async scene load in progress
        Playing,        // Gameplay is running
        Paused,         // Gameplay paused (Settings popup, etc.)
        Win,            // Level/match won
        Lose            // Level/match lost
    }
}
