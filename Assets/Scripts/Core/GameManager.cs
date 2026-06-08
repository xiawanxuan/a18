using UnityEngine;
using SoftFluidPuzzle.Core;
using SoftFluidPuzzle.PhysicsSimulation;
using SoftFluidPuzzle.FluidRendering;
using SoftFluidPuzzle.PlayerControl;
using SoftFluidPuzzle.LevelManagement;

namespace SoftFluidPuzzle.Core
{
    public class GameManager : Singleton<GameManager>
    {
        [Header("Game State")]
        public GameState currentState = GameState.MainMenu;

        [Header("References")]
        public GameObject mainMenuUI;
        public GameObject gameUI;
        public GameObject pauseMenuUI;
        public GameObject levelCompleteUI;

        public enum GameState
        {
            MainMenu,
            LevelSelect,
            Playing,
            Paused,
            LevelComplete,
            GameComplete
        }

        protected override void Awake()
        {
            base.Awake();
            InitializeManagers();
        }

        private void Start()
        {
            SetState(GameState.MainMenu);
        }

        private void InitializeManagers()
        {
            if (PhysicsSimulationManager.Instance == null)
            {
                GameObject obj = new GameObject("PhysicsSimulationManager");
                obj.AddComponent<PhysicsSimulationManager>();
            }

            if (FluidRenderingManager.Instance == null)
            {
                GameObject obj = new GameObject("FluidRenderingManager");
                obj.AddComponent<FluidRenderingManager>();
            }

            if (LevelManager.Instance == null)
            {
                GameObject obj = new GameObject("LevelManager");
                obj.AddComponent<LevelManager>();
            }

            if (SaveSystem.Instance == null)
            {
                GameObject obj = new GameObject("SaveSystem");
                obj.AddComponent<SaveSystem>();
            }
        }

        public void SetState(GameState newState)
        {
            currentState = newState;

            switch (newState)
            {
                case GameState.MainMenu:
                    ShowMainMenu();
                    break;
                case GameState.LevelSelect:
                    ShowLevelSelect();
                    break;
                case GameState.Playing:
                    StartGameplay();
                    break;
                case GameState.Paused:
                    PauseGame();
                    break;
                case GameState.LevelComplete:
                    ShowLevelComplete();
                    break;
                case GameState.GameComplete:
                    ShowGameComplete();
                    break;
            }
        }

        private void ShowMainMenu()
        {
            if (mainMenuUI != null) mainMenuUI.SetActive(true);
            if (gameUI != null) gameUI.SetActive(false);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
            if (levelCompleteUI != null) levelCompleteUI.SetActive(false);

            Time.timeScale = 1f;
        }

        private void ShowLevelSelect()
        {
            if (mainMenuUI != null) mainMenuUI.SetActive(false);
            if (gameUI != null) gameUI.SetActive(false);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
            if (levelCompleteUI != null) levelCompleteUI.SetActive(false);

            Time.timeScale = 1f;
        }

        private void StartGameplay()
        {
            if (mainMenuUI != null) mainMenuUI.SetActive(false);
            if (gameUI != null) gameUI.SetActive(true);
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
            if (levelCompleteUI != null) levelCompleteUI.SetActive(false);

            Time.timeScale = 1f;
        }

        private void PauseGame()
        {
            if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
            Time.timeScale = 0f;
        }

        private void ShowLevelComplete()
        {
            if (levelCompleteUI != null) levelCompleteUI.SetActive(true);
            Time.timeScale = 0f;
        }

        private void ShowGameComplete()
        {
            Time.timeScale = 1f;
        }

        public void StartNewGame()
        {
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.CreateNewSave();
            }

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.StartLevel(0);
                SetState(GameState.Playing);
            }
        }

        public void ContinueGame()
        {
            if (SaveSystem.Instance != null && SaveSystem.Instance.HasSaveData)
            {
                int levelToLoad = SaveSystem.Instance.CurrentSaveData.playerData.highestUnlockedLevel;
                LevelManager.Instance.StartLevel(levelToLoad);
                SetState(GameState.Playing);
            }
        }

        public void SelectLevel(int levelIndex)
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.StartLevel(levelIndex);
                SetState(GameState.Playing);
            }
        }

        public void TogglePause()
        {
            if (currentState == GameState.Playing)
            {
                SetState(GameState.Paused);
                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.PauseGame();
                }
            }
            else if (currentState == GameState.Paused)
            {
                SetState(GameState.Playing);
                if (LevelManager.Instance != null)
                {
                    LevelManager.Instance.ResumeGame();
                }
            }
        }

        public void ReturnToMainMenu()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            SetState(GameState.MainMenu);
        }

        public void RestartLevel()
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RestartLevel();
                SetState(GameState.Playing);
            }
        }

        public void NextLevel()
        {
            if (LevelManager.Instance != null && LevelManager.Instance.HasNextLevel())
            {
                LevelManager.Instance.LoadNextLevel();
                SetState(GameState.Playing);
            }
            else
            {
                SetState(GameState.GameComplete);
            }
        }

        public void QuitGame()
        {
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.SaveGame();
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnEnable()
        {
            EventBus.Subscribe<LevelCompleteArgs>(GameEvents.OnLevelComplete, OnLevelComplete);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LevelCompleteArgs>(GameEvents.OnLevelComplete, OnLevelComplete);
        }

        private void OnLevelComplete(LevelCompleteArgs args)
        {
            SetState(GameState.LevelComplete);
        }
    }
}
