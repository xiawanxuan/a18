using UnityEngine;
using System;

namespace SoftFluidPuzzle.LevelManagement
{
    [Serializable]
    public class LevelData
    {
        public int levelIndex;
        public string levelName;
        public string sceneName;
        public string description;

        [Header("Objectives")]
        public LevelObjective[] objectives;

        [Header("Scoring")]
        public float threeStarTime;
        public float twoStarTime;

        public LevelData(int index, string name, string scene)
        {
            levelIndex = index;
            levelName = name;
            sceneName = scene;
            description = "";
            threeStarTime = 60f;
            twoStarTime = 120f;
        }
    }

    [Serializable]
    public class LevelObjective
    {
        public string objectiveId;
        public string description;
        public ObjectiveType type;
        public float targetValue;
        public bool isOptional;

        public LevelObjective(string id, string desc, ObjectiveType objType, float target)
        {
            objectiveId = id;
            description = desc;
            type = objType;
            targetValue = target;
            isOptional = false;
        }
    }

    public enum ObjectiveType
    {
        CollectItems,
        ReachDestination,
        SolvePuzzle,
        TimeLimit,
        FillVolume,
        TriggerSwitch,
        MoveObject
    }
}
