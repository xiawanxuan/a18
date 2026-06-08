using UnityEngine;

namespace SoftFluidPuzzle.Core
{
    public static class GameEvents
    {
        public const string OnLevelComplete = "OnLevelComplete";
        public const string OnLevelStart = "OnLevelStart";
        public const string OnSoftBodyDeformed = "OnSoftBodyDeformed";
        public const string OnFluidVolumeChanged = "OnFluidVolumeChanged";
        public const string OnPlayerMoved = "OnPlayerMoved";
        public const string OnPuzzleSolved = "OnPuzzleSolved";
        public const string OnSaveDataLoaded = "OnSaveDataLoaded";
        public const string OnSaveDataSaved = "OnSaveDataSaved";
    }

    public struct LevelCompleteArgs
    {
        public int LevelIndex;
        public float CompletionTime;
        public int StarsEarned;
    }

    public struct FluidVolumeChangedArgs
    {
        public float CurrentVolume;
        public float TargetVolume;
    }

    public struct PuzzleSolvedArgs
    {
        public string PuzzleId;
        public float SolveTime;
    }
}
