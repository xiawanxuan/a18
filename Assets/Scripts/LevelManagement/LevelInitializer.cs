using UnityEngine;
using SoftFluidPuzzle.PhysicsSimulation;
using SoftFluidPuzzle.FluidRendering;
using SoftFluidPuzzle.PlayerControl;

namespace SoftFluidPuzzle.LevelManagement
{
    public class LevelInitializer : MonoBehaviour
    {
        [Header("Level Setup")]
        public int levelIndex = 0;
        public Transform playerSpawnPoint;

        [Header("References")]
        public GameObject playerPrefab;
        public PlayerController playerController;

        [Header("Soft Body Settings")]
        public SoftBody[] levelSoftBodies;

        [Header("Fluid Settings")]
        public FluidSystem[] levelFluidSystems;

        [Header("Objectives")]
        public LevelObjective[] levelObjectives;

        private void Start()
        {
            InitializeLevel();
        }

        private void InitializeLevel()
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.currentLevelIndex = levelIndex;

                if (levelObjectives != null && levelObjectives.Length > 0)
                {
                    if (LevelManager.Instance.CurrentLevel != null)
                    {
                        LevelManager.Instance.CurrentLevel.objectives = levelObjectives;
                    }
                }
            }

            SpawnPlayer();
            RegisterSoftBodies();
            RegisterFluidSystems();

            if (playerSpawnPoint != null && LevelManager.Instance != null)
            {
                LevelManager.Instance.SetSpawnPoint(playerSpawnPoint.position);
            }

            Debug.Log("Level " + levelIndex + " initialized.");
        }

        private void SpawnPlayer()
        {
            if (playerController == null && playerPrefab != null)
            {
                Vector3 spawnPos = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
                Quaternion spawnRot = playerSpawnPoint != null ? playerSpawnPoint.rotation : Quaternion.identity;

                GameObject playerObj = Instantiate(playerPrefab, spawnPos, spawnRot);
                playerController = playerObj.GetComponent<PlayerController>();
            }
            else if (playerController != null && playerSpawnPoint != null)
            {
                playerController.transform.position = playerSpawnPoint.position;
                playerController.transform.rotation = playerSpawnPoint.rotation;
            }

            SetupCamera();
        }

        private void SetupCamera()
        {
            PlayerCamera playerCamera = FindObjectOfType<PlayerCamera>();
            if (playerCamera != null && playerController != null)
            {
                playerCamera.SetTarget(playerController);
            }
            else if (playerController != null)
            {
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.tag = "MainCamera";

                Camera cam = cameraObj.AddComponent<Camera>();
                cam.fieldOfView = 60f;

                AudioListener listener = cameraObj.AddComponent<AudioListener>();

                PlayerCamera pc = cameraObj.AddComponent<PlayerCamera>();
                pc.SetTarget(playerController);
                pc.collisionLayers = LayerMask.GetMask("Default");
            }
        }

        private void RegisterSoftBodies()
        {
            if (levelSoftBodies == null) return;

            foreach (SoftBody softBody in levelSoftBodies)
            {
                if (softBody != null && PhysicsSimulationManager.Instance != null)
                {
                    PhysicsSimulationManager.Instance.RegisterSoftBody(softBody);
                }
            }
        }

        private void RegisterFluidSystems()
        {
            if (levelFluidSystems == null) return;

            foreach (FluidSystem fluidSystem in levelFluidSystems)
            {
                if (fluidSystem != null && FluidRenderingManager.Instance != null)
                {
                    FluidRenderingManager.Instance.RegisterFluidSystem(fluidSystem);

                    FluidRenderer renderer = fluidSystem.GetComponent<FluidRenderer>();
                    if (renderer != null)
                    {
                        FluidRenderingManager.Instance.RegisterFluidRenderer(renderer);
                    }
                }
            }
        }

        public void ResetLevel()
        {
            if (playerController != null)
            {
                playerController.transform.position = playerSpawnPoint.position;
                playerController.ResetVelocity();
            }

            foreach (SoftBody softBody in levelSoftBodies)
            {
                if (softBody != null)
                {
                    softBody.ResetSoftBody();
                }
            }
        }
    }
}
