using UnityEngine;
using System;
using System.Collections.Generic;

namespace SoftFluidPuzzle.LevelManagement
{
    [Serializable]
    public class SaveData
    {
        public PlayerSaveData playerData;
        public List<LevelSaveData> levelSaves;
        public SettingsSaveData settingsData;
        public string lastSavedTime;
        public int saveVersion;

        public SaveData()
        {
            playerData = new PlayerSaveData();
            levelSaves = new List<LevelSaveData>();
            settingsData = new SettingsSaveData();
            lastSavedTime = DateTime.Now.ToString();
            saveVersion = 1;
        }

        public LevelSaveData GetLevelSave(int levelIndex)
        {
            LevelSaveData levelSave = levelSaves.Find(ls => ls.levelIndex == levelIndex);
            if (levelSave == null)
            {
                levelSave = new LevelSaveData(levelIndex);
                levelSaves.Add(levelSave);
            }
            return levelSave;
        }

        public void SetLevelSave(LevelSaveData levelSave)
        {
            int index = levelSaves.FindIndex(ls => ls.levelIndex == levelSave.levelIndex);
            if (index >= 0)
            {
                levelSaves[index] = levelSave;
            }
            else
            {
                levelSaves.Add(levelSave);
            }
        }
    }

    [Serializable]
    public class PlayerSaveData
    {
        public int totalStars;
        public int highestUnlockedLevel;
        public float totalPlayTime;
        public int totalPuzzlesSolved;

        public PlayerSaveData()
        {
            totalStars = 0;
            highestUnlockedLevel = 0;
            totalPlayTime = 0f;
            totalPuzzlesSolved = 0;
        }
    }

    [Serializable]
    public class LevelSaveData
    {
        public int levelIndex;
        public bool isUnlocked;
        public bool isCompleted;
        public int starsEarned;
        public float bestTime;
        public int bestScore;
        public List<string> completedObjectives;
        public List<SerializableVector3> checkpointPositions;

        public LevelSaveData(int index)
        {
            levelIndex = index;
            isUnlocked = index == 0;
            isCompleted = false;
            starsEarned = 0;
            bestTime = float.MaxValue;
            bestScore = 0;
            completedObjectives = new List<string>();
            checkpointPositions = new List<SerializableVector3>();
        }
    }

    [Serializable]
    public class SettingsSaveData
    {
        public float masterVolume;
        public float musicVolume;
        public float sfxVolume;
        public float mouseSensitivity;
        public bool invertYAxis;
        public int qualityLevel;
        public int resolutionIndex;
        public bool fullscreen;

        public SettingsSaveData()
        {
            masterVolume = 1f;
            musicVolume = 0.8f;
            sfxVolume = 1f;
            mouseSensitivity = 2f;
            invertYAxis = false;
            qualityLevel = 3;
            resolutionIndex = 0;
            fullscreen = true;
        }
    }

    [Serializable]
    public struct SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3(SerializableVector3 sv)
        {
            return new Vector3(sv.x, sv.y, sv.z);
        }

        public static implicit operator SerializableVector3(Vector3 v)
        {
            return new SerializableVector3(v.x, v.y, v.z);
        }
    }
}
