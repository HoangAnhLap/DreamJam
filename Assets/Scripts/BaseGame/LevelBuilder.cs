// ============================================================
//  LevelBuilder.cs
//
//  Spawns the ball layout for a level and registers every ball
//  with TrayManager so it can track the board-clear win condition.
//
//  In a real project this would read from a ScriptableObject or
//  JSON level file.  This example hard-codes a sample layout so
//  you can run the scene immediately.
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame
{
    public class LevelBuilder : MonoBehaviour
    {
        [System.Serializable]
        public struct BallSpawnData
        {
            public BallColor   color;
            public Vector3     worldPosition;
        }

        [Header("Prefab")]
        [SerializeField] private GameObject ballPrefab;

        [Header("Level Layout")]
        [SerializeField] private List<BallSpawnData> ballSpawns;

        private void Start()
        {
            BuildLevel();
        }

        private void BuildLevel()
        {
            if (ballPrefab == null)
            {
                Debug.LogError("[LevelBuilder] ballPrefab is not assigned.");
                return;
            }

            foreach (var data in ballSpawns)
            {
                GameObject go   = Instantiate(ballPrefab, data.worldPosition,
                                              Quaternion.identity);
                var ball        = go.GetComponent<BallController>();

                if (ball == null)
                {
                    Debug.LogError("[LevelBuilder] ballPrefab is missing BallController.");
                    continue;
                }

                // The BallColor field is serialized on BallController.
                // Since we're spawning at runtime we need to set it via reflection
                // OR expose a setter.  Simplest: expose an Init method.
                ball.Init(data.color);

                // Register with TrayManager for board-clear tracking.
                TrayManager.Instance?.RegisterBoardBall(ball);
            }
        }
    }
}
