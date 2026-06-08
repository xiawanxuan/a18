using UnityEngine;
using SoftFluidPuzzle.PhysicsSimulation;
using SoftFluidPuzzle.FluidRendering;
using SoftFluidPuzzle.PlayerControl;

namespace SoftFluidPuzzle.LevelManagement
{
    public class SampleLevelBuilder : MonoBehaviour
    {
        [Header("Level Settings")]
        public int levelIndex = 1;
        public float groundSize = 30f;

        [Header("Prefabs")]
        public GameObject playerPrefab;
        public Material softBodyMaterial;
        public Material fluidMaterial;

        [Header("Spawn Points")]
        public Vector3 playerSpawn = new Vector3(0, 2, -10);

        private GameObject _ground;
        private GameObject _player;

        private void Start()
        {
            BuildLevel();
        }

        public void BuildLevel()
        {
            CreateGround();
            CreateWalls();
            CreatePlayer();
            CreateSoftBodies();
            CreateFluidSystem();
            CreatePuzzleElements();
            CreateGoalZone();

            SetupLevelManager();
        }

        private void CreateGround()
        {
            _ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _ground.name = "Ground";
            _ground.transform.localScale = Vector3.one * groundSize * 0.1f;
            _ground.transform.position = Vector3.zero;

            Renderer groundRenderer = _ground.GetComponent<Renderer>();
            groundRenderer.material = new Material(Shader.Find("Standard"));
            groundRenderer.material.color = new Color(0.3f, 0.3f, 0.35f);
        }

        private void CreateWalls()
        {
            float wallHeight = 5f;
            float wallThickness = 1f;
            float halfSize = groundSize * 0.5f;

            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "BackWall";
            backWall.transform.position = new Vector3(0, wallHeight * 0.5f, -halfSize);
            backWall.transform.localScale = new Vector3(groundSize, wallHeight, wallThickness);

            GameObject frontWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontWall.name = "FrontWall";
            frontWall.transform.position = new Vector3(0, wallHeight * 0.5f, halfSize);
            frontWall.transform.localScale = new Vector3(groundSize, wallHeight, wallThickness);

            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LeftWall";
            leftWall.transform.position = new Vector3(-halfSize, wallHeight * 0.5f, 0);
            leftWall.transform.localScale = new Vector3(wallThickness, wallHeight, groundSize);

            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "RightWall";
            rightWall.transform.position = new Vector3(halfSize, wallHeight * 0.5f, 0);
            rightWall.transform.localScale = new Vector3(wallThickness, wallHeight, groundSize);
        }

        private void CreatePlayer()
        {
            if (playerPrefab != null)
            {
                _player = Instantiate(playerPrefab, playerSpawn, Quaternion.identity);
            }
            else
            {
                _player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _player.name = "Player";
                _player.transform.position = playerSpawn;

                Rigidbody rb = _player.AddComponent<Rigidbody>();
                rb.mass = 1f;
                rb.constraints = RigidbodyConstraints.FreezeRotation;

                _player.AddComponent<PlayerInput>();
                PlayerController controller = _player.AddComponent<PlayerController>();
                controller.groundLayer = LayerMask.GetMask("Default");

                _player.AddComponent<PlayerInteraction>();
                _player.AddComponent<PlayerFluidInteraction>();

                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.transform.SetParent(_player.transform, false);
                cameraObj.transform.localPosition = new Vector3(0, 1.6f, 0);
                cameraObj.AddComponent<Camera>();
                cameraObj.AddComponent<AudioListener>();
                PlayerCamera playerCam = cameraObj.AddComponent<PlayerCamera>();
                playerCam.SetTarget(controller);
            }
        }

        private void CreateSoftBodies()
        {
            GameObject softBodyObj = new GameObject("SoftBody_Cube");
            softBodyObj.transform.position = new Vector3(-5f, 3f, 0f);

            SoftBody softBody = softBodyObj.AddComponent<SoftBody>();
            softBody.resolution = 6;
            softBody.radius = 1.5f;
            softBody.stiffness = 150f;
            softBody.damping = 3f;
            softBody.pressure = 30f;
            softBody.gravityScale = 0.8f;
            softBody.collisionLayers = LayerMask.GetMask("Default");

            MeshRenderer renderer = softBodyObj.GetComponent<MeshRenderer>();
            if (renderer != null && softBodyMaterial != null)
            {
                renderer.material = softBodyMaterial;
            }
            else if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = new Color(0.2f, 0.8f, 0.6f);
            }

            if (PhysicsSimulationManager.Instance != null)
            {
                PhysicsSimulationManager.Instance.RegisterSoftBody(softBody);
            }
        }

        private void CreateFluidSystem()
        {
            GameObject fluidObj = new GameObject("FluidSystem");
            fluidObj.transform.position = new Vector3(5f, 8f, 0f);

            FluidSystem fluidSystem = fluidObj.AddComponent<FluidSystem>();
            fluidSystem.maxParticles = 500;
            fluidSystem.particleRadius = 0.2f;
            fluidSystem.particleMass = 0.5f;
            fluidSystem.smoothingRadius = 0.6f;
            fluidSystem.collisionLayers = LayerMask.GetMask("Default");

            FluidEmitter emitter = fluidObj.AddComponent<FluidEmitter>();
            emitter.targetFluidSystem = fluidSystem;
            emitter.particlesPerSecond = 30;
            emitter.emissionForce = 3f;
            emitter.shape = FluidEmitter.EmitterShape.Cone;
            emitter.maxEmissionAngle = 20f;

            FluidRenderer renderer = fluidObj.AddComponent<FluidRenderer>();
            renderer.fluidSystem = fluidSystem;
            renderer.particleSize = 1.5f;
            renderer.particleColor = new Color(0.2f, 0.5f, 1f, 0.8f);

            if (fluidMaterial != null)
            {
                renderer.particleMaterial = fluidMaterial;
            }
            else
            {
                Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
                mat.color = new Color(0.2f, 0.5f, 1f, 0.8f);
                mat.SetFloat("_Mode", 3);
                renderer.particleMaterial = mat;
            }

            Bounds spawnBounds = new Bounds(new Vector3(5f, 5f, 0f), new Vector3(3f, 2f, 3f));
            fluidSystem.EmitParticlesInVolume(spawnBounds, 100);

            if (FluidRenderingManager.Instance != null)
            {
                FluidRenderingManager.Instance.RegisterFluidSystem(fluidSystem);
                FluidRenderingManager.Instance.RegisterFluidRenderer(renderer);
            }
        }

        private void CreatePuzzleElements()
        {
            GameObject pressurePlateObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pressurePlateObj.name = "PressurePlate";
            pressurePlateObj.transform.position = new Vector3(-8f, 0.1f, 5f);
            pressurePlateObj.transform.localScale = new Vector3(3f, 0.2f, 3f);

            PressurePlate plate = pressurePlateObj.AddComponent<PressurePlate>();
            plate.plateId = "plate_1";
            plate.activationForce = 20f;
            plate.detectionLayers = LayerMask.GetMask("Default");

            GameObject collectorObj = new GameObject("FluidCollector");
            collectorObj.transform.position = new Vector3(8f, 0.5f, -5f);

            FluidCollector collector = collectorObj.AddComponent<FluidCollector>();
            collector.collectorId = "collector_1";
            collector.targetVolume = 50f;
            collector.collectionRadius = 2f;

            GameObject collectorVis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            collectorVis.transform.SetParent(collectorObj.transform, false);
            collectorVis.transform.localScale = new Vector3(2f, 0.5f, 2f);
            collectorVis.transform.localPosition = Vector3.zero;
            collector.fillRenderer = collectorVis.GetComponent<Renderer>();
        }

        private void CreateGoalZone()
        {
            GameObject goalObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            goalObj.name = "GoalZone";
            goalObj.transform.position = new Vector3(0f, 1f, 10f);
            goalObj.transform.localScale = new Vector3(4f, 2f, 4f);

            Collider collider = goalObj.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            Renderer renderer = goalObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = new Color(1f, 1f, 0f, 0.3f);
                renderer.material.SetFloat("_Mode", 3);
            }

            GoalZone goal = goalObj.AddComponent<GoalZone>();
            goal.goalId = "goal_1";
            goal.playerLayer = LayerMask.GetMask("Default");
            goal.requireAllObjectives = false;
        }

        private void SetupLevelManager()
        {
            if (LevelManager.Instance == null)
            {
                GameObject managerObj = new GameObject("LevelManager");
                managerObj.AddComponent<LevelManager>();
            }

            LevelInitializer initializer = gameObject.GetComponent<LevelInitializer>();
            if (initializer == null)
            {
                initializer = gameObject.AddComponent<LevelInitializer>();
            }

            initializer.levelIndex = levelIndex;
        }
    }
}
