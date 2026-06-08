using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using SoftFluidPuzzle.Core;

namespace SoftFluidPuzzle.LevelManagement
{
    public class SaveSystem : Singleton<SaveSystem>
    {
        [Header("Settings")]
        public string saveFileName = "game_save.dat";
        public bool autoSave = true;
        public float autoSaveInterval = 30f;

        private SaveData _currentSaveData;
        private string _saveFilePath;
        private float _autoSaveTimer;

        public SaveData CurrentSaveData => _currentSaveData;
        public bool HasSaveData => _currentSaveData != null;

        protected override void Awake()
        {
            base.Awake();
            InitializeSavePath();
            LoadOrCreateSave();
        }

        private void InitializeSavePath()
        {
            _saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
            Debug.Log("Save path: " + _saveFilePath);
        }

        private void Update()
        {
            if (autoSave && _currentSaveData != null)
            {
                _autoSaveTimer += Time.deltaTime;
                if (_autoSaveTimer >= autoSaveInterval)
                {
                    SaveGame();
                    _autoSaveTimer = 0f;
                }
            }
        }

        public void LoadOrCreateSave()
        {
            if (File.Exists(_saveFilePath))
            {
                LoadGame();
            }
            else
            {
                CreateNewSave();
            }
        }

        public void CreateNewSave()
        {
            _currentSaveData = new SaveData();
            SaveGame();
            EventBus.Publish(GameEvents.OnSaveDataLoaded);
        }

        public bool SaveGame()
        {
            if (_currentSaveData == null)
            {
                Debug.LogError("No save data to save!");
                return false;
            }

            try
            {
                _currentSaveData.lastSavedTime = System.DateTime.Now.ToString();

                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream stream = new FileStream(_saveFilePath, FileMode.Create))
                {
                    formatter.Serialize(stream, _currentSaveData);
                }

                EventBus.Publish(GameEvents.OnSaveDataSaved);
                Debug.Log("Game saved successfully to: " + _saveFilePath);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to save game: " + e.Message);
                return false;
            }
        }

        public bool LoadGame()
        {
            try
            {
                if (!File.Exists(_saveFilePath))
                {
                    Debug.LogWarning("No save file found at: " + _saveFilePath);
                    return false;
                }

                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream stream = new FileStream(_saveFilePath, FileMode.Open))
                {
                    _currentSaveData = formatter.Deserialize(stream) as SaveData;
                }

                if (_currentSaveData != null)
                {
                    ApplySettings(_currentSaveData.settingsData);
                    EventBus.Publish(GameEvents.OnSaveDataLoaded);
                    Debug.Log("Game loaded successfully from: " + _saveFilePath);
                    return true;
                }

                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to load game: " + e.Message);
                CreateNewSave();
                return false;
            }
        }

        public void DeleteSave()
        {
            if (File.Exists(_saveFilePath))
            {
                File.Delete(_saveFilePath);
                _currentSaveData = null;
                CreateNewSave();
                Debug.Log("Save file deleted and new save created.");
            }
        }

        public void SaveLevelProgress(int levelIndex, int starsEarned, float completionTime, List<string> completedObjectives)
        {
            if (_currentSaveData == null) return;

            LevelSaveData levelSave = _currentSaveData.GetLevelSave(levelIndex);
            levelSave.isCompleted = true;
            levelSave.starsEarned = Mathf.Max(levelSave.starsEarned, starsEarned);
            levelSave.bestTime = Mathf.Min(levelSave.bestTime, completionTime);

            foreach (string obj in completedObjectives)
            {
                if (!levelSave.completedObjectives.Contains(obj))
                {
                    levelSave.completedObjectives.Add(obj);
                }
            }

            _currentSaveData.playerData.totalStars = CalculateTotalStars();

            int nextLevel = levelIndex + 1;
            LevelSaveData nextLevelSave = _currentSaveData.GetLevelSave(nextLevel);
            nextLevelSave.isUnlocked = true;

            if (nextLevel > _currentSaveData.playerData.highestUnlockedLevel)
            {
                _currentSaveData.playerData.highestUnlockedLevel = nextLevel;
            }

            _currentSaveData.playerData.totalPlayTime += completionTime;

            SaveGame();
        }

        public bool IsLevelUnlocked(int levelIndex)
        {
            if (_currentSaveData == null) return levelIndex == 0;

            LevelSaveData levelSave = _currentSaveData.GetLevelSave(levelIndex);
            return levelSave.isUnlocked;
        }

        public int GetStarsForLevel(int levelIndex)
        {
            if (_currentSaveData == null) return 0;

            LevelSaveData levelSave = _currentSaveData.GetLevelSave(levelIndex);
            return levelSave.starsEarned;
        }

        public float GetBestTimeForLevel(int levelIndex)
        {
            if (_currentSaveData == null) return float.MaxValue;

            LevelSaveData levelSave = _currentSaveData.GetLevelSave(levelIndex);
            return levelSave.bestTime;
        }

        public int CalculateTotalStars()
        {
            if (_currentSaveData == null) return 0;

            int total = 0;
            foreach (LevelSaveData levelSave in _currentSaveData.levelSaves)
            {
                total += levelSave.starsEarned;
            }
            return total;
        }

        public void SaveSettings(SettingsSaveData settings)
        {
            if (_currentSaveData == null) return;

            _currentSaveData.settingsData = settings;
            ApplySettings(settings);
            SaveGame();
        }

        private void ApplySettings(SettingsSaveData settings)
        {
            if (settings == null) return;

            QualitySettings.SetQualityLevel(settings.qualityLevel);

            if (settings.resolutionIndex >= 0 && settings.resolutionIndex < Screen.resolutions.Length)
            {
                Resolution res = Screen.resolutions[settings.resolutionIndex];
                Screen.SetResolution(res.width, res.height, settings.fullscreen);
            }
        }

        public void AddCheckpointPosition(int levelIndex, Vector3 position)
        {
            if (_currentSaveData == null) return;

            LevelSaveData levelSave = _currentSaveData.GetLevelSave(levelIndex);
            SerializableVector3 serializablePos = position;

            if (!levelSave.checkpointPositions.Contains(serializablePos))
            {
                levelSave.checkpointPositions.Add(serializablePos);
            }
        }

        public Vector3 GetLastCheckpointPosition(int levelIndex)
        {
            if (_currentSaveData == null) return Vector3.zero;

            LevelSaveData levelSave = _currentSaveData.GetLevelSave(levelIndex);
            if (levelSave.checkpointPositions.Count > 0)
            {
                return levelSave.checkpointPositions[levelSave.checkpointPositions.Count - 1];
            }

            return Vector3.zero;
        }

        public string GetSaveFilePath()
        {
            return _saveFilePath;
        }
    }
}
