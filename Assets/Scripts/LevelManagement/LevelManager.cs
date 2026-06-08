using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using SoftFluidPuzzle.Core;

namespace SoftFluidPuzzle.LevelManagement
{
    public class LevelManager : Singleton<LevelManager>
    {
        [Header("Level Settings")]
        public LevelData[] levels;
        public int currentLevelIndex = 0;
        public float levelStartTime;

        [Header("Game State")]
        public bool isLevelActive = false;
        public bool isPaused = false;

        private List<string> _completedObjectives = new List<string>();
        private int _currentCheckpointIndex = 0;
        private Vector3 _spawnPoint;

        public LevelData CurrentLevel => levels != null && currentLevelIndex < levels.Length ? levels[currentLevelIndex] : null;
        public float ElapsedTime => isLevelActive ? Time.time - levelStartTime : 0f;
        public int CompletedObjectiveCount => _completedObjectives.Count;

        protected override void Awake()
        {
            base.Awake();
            InitializeLevels();
        }

        private void InitializeLevels()
        {
            if (levels == null || levels.Length == 0)
            {
                levels = new LevelData[]
                {
                    new LevelData(0, "入门教学", "Level_01"),
                    new LevelData(1, "流体引导", "Level_02"),
                    new LevelData(2, "软体变形", "Level_03"),
                    new LevelData(3, "综合挑战", "Level_04"),
                    new LevelData(4, "终极考验", "Level_05"),
                };
            }
        }

        public void StartLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= levels.Length)
            {
                Debug.LogError("Invalid level index: " + levelIndex);
                return;
            }

            if (!SaveSystem.Instance.IsLevelUnlocked(levelIndex))
            {
                Debug.LogWarning("Level " + levelIndex + " is locked!");
                return;
            }

            currentLevelIndex = levelIndex;
            isLevelActive = true;
            isPaused = false;
            levelStartTime = Time.time;
            _completedObjectives.Clear();
            _currentCheckpointIndex = 0;

            Time.timeScale = 1f;

            LevelData level = levels[levelIndex];
            if (!string.IsNullOrEmpty(level.sceneName))
            {
                SceneManager.LoadScene(level.sceneName);
            }

            EventBus.Publish(GameEvents.OnLevelStart);
            Debug.Log("Started level: " + level.levelName);
        }

        public void RestartLevel()
        {
            StartLevel(currentLevelIndex);
        }

        public void CompleteLevel()
        {
            if (!isLevelActive) return;

            isLevelActive = false;
            float completionTime = ElapsedTime;
            int starsEarned = CalculateStars(completionTime);

            LevelData level = CurrentLevel;
            if (level != null)
            {
                SaveSystem.Instance.SaveLevelProgress(
                    currentLevelIndex,
                    starsEarned,
                    completionTime,
                    _completedObjectives
                );

                EventBus.Publish(GameEvents.OnLevelComplete, new LevelCompleteArgs
                {
                    LevelIndex = currentLevelIndex,
                    CompletionTime = completionTime,
                    StarsEarned = starsEarned
                });

                Debug.Log(string.Format(
                    "Level {0} completed! Time: {1:F2}s, Stars: {2}",
                    level.levelName,
                    completionTime,
                    starsEarned
                ));
            }
        }

        public int CalculateStars(float completionTime)
        {
            if (CurrentLevel == null) return 0;

            if (completionTime <= CurrentLevel.threeStarTime)
                return 3;
            if (completionTime <= CurrentLevel.twoStarTime)
                return 2;

            return 1;
        }

        public void CompleteObjective(string objectiveId)
        {
            if (!_completedObjectives.Contains(objectiveId))
            {
                _completedObjectives.Add(objectiveId);
                CheckLevelCompletion();
            }
        }

        public bool IsObjectiveCompleted(string objectiveId)
        {
            return _completedObjectives.Contains(objectiveId);
        }

        private void CheckLevelCompletion()
        {
            if (CurrentLevel == null || CurrentLevel.objectives == null) return;

            int requiredObjectives = 0;
            int completedRequired = 0;

            foreach (LevelObjective obj in CurrentLevel.objectives)
            {
                if (!obj.isOptional)
                {
                    requiredObjectives++;
                    if (_completedObjectives.Contains(obj.objectiveId))
                    {
                        completedRequired++;
                    }
                }
            }

            if (requiredObjectives > 0 && completedRequired >= requiredObjectives)
            {
                CompleteLevel();
            }
        }

        public void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
        }

        public void TogglePause()
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        public void SetSpawnPoint(Vector3 position)
        {
            _spawnPoint = position;
        }

        public Vector3 GetSpawnPoint()
        {
            return _spawnPoint;
        }

        public void AddCheckpoint(Vector3 position)
        {
            _currentCheckpointIndex++;
            SaveSystem.Instance.AddCheckpointPosition(currentLevelIndex, position);
            _spawnPoint = position;
        }

        public void LoadNextLevel()
        {
            int nextLevel = currentLevelIndex + 1;
            if (nextLevel < levels.Length)
            {
                StartLevel(nextLevel);
            }
            else
            {
                Debug.Log("All levels completed!");
            }
        }

        public bool HasNextLevel()
        {
            return currentLevelIndex + 1 < levels.Length;
        }

        public int GetTotalLevels()
        {
            return levels != null ? levels.Length : 0;
        }

        public LevelData GetLevelData(int index)
        {
            if (index >= 0 && index < levels.Length)
            {
                return levels[index];
            }
            return null;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }
    }
}
