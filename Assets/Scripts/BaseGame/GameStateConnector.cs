// ============================================================
//  GameStateConnector.cs
//
//  This small class is the ONLY bridge between the puzzle game
//  logic (TrayManager) and your core framework (GameStateManager).
//
//  Why a separate class?
//  ─────────────────────
//  TrayManager knows nothing about GameStateManager.
//  GameStateManager knows nothing about TrayManager.
//  This connector lives in between, subscribing to TrayManager's
//  static events and calling GameStateManager to change state.
//
//  This keeps both systems independently testable and reusable.
//
//  Attach this to any persistent GameObject in your gameplay scene
//  (e.g. a "GameplayController" empty object).
// ============================================================
using UnityEngine;
using GameFramework.Core;   // your existing framework namespace

namespace PuzzleGame
{
    public class GameStateConnector : MonoBehaviour
    {
        private void OnEnable()
        {
            TrayManager.OnLevelComplete += HandleLevelComplete;
            TrayManager.OnLevelFailed   += HandleLevelFailed;
        }

        private void OnDisable()
        {
            TrayManager.OnLevelComplete -= HandleLevelComplete;
            TrayManager.OnLevelFailed   -= HandleLevelFailed;
        }

        private void HandleLevelComplete()
        {
            Debug.Log("[GameStateConnector] Level complete — transitioning to Win.");
            GameStateManager.Instance?.ChangeState(GameState.Win);
        }

        private void HandleLevelFailed()
        {
            Debug.Log("[GameStateConnector] Level failed — transitioning to Lose.");
            GameStateManager.Instance?.ChangeState(GameState.Lose);
        }
    }
}
