using UnityEngine;
using SoftFluidPuzzle.PhysicsSimulation;
using SoftFluidPuzzle.FluidRendering;
using SoftFluidPuzzle.PlayerControl;
using SoftFluidPuzzle.DestructibleObjects;
using SoftFluidPuzzle.PressureMechanisms;

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
            CreateDestructibleWalls();
            CreatePressureSensors();
            CreateMechanicalDoors();
            CreateGoalZone();

            SetupLevelManager();
            SetupManagers();
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
            softBodyObj.transform.position = new Vector3(-5f, 4f, 0f);

            SoftBody softBody = softBodyObj.AddComponent<SoftBody>();
            softBody.resolution = 6;
            softBody.size = 2.5f;
            softBody.massPerParticle = 0.3f;
            softBody.structuralStiffness = 60f;
            softBody.shearStiffness = 30f;
            softBody.bendStiffness = 15f;
            softBody.springDamping = 0.5f;
            softBody.volumeConservation = 80f;
            softBody.gravityScale = 1f;
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
            renderer.particleSize = 1.8f;
            renderer.particleColor = new Color(0.2f, 0.5f, 1f, 0.7f);
            renderer.billboard = true;
            renderer.glowIntensity = 0.3f;

            if (fluidMaterial != null)
            {
                renderer.particleMaterial = fluidMaterial;
            }
            else
            {
                Shader fluidShader = Shader.Find("Fluid/FluidParticle");
                if (fluidShader == null)
                {
                    fluidShader = Shader.Find("Particles/Alpha Blended Premultiply");
                }
                Material mat = new Material(fluidShader);
                mat.color = new Color(0.2f, 0.5f, 1f, 0.7f);
                mat.SetFloat("_GlowIntensity", 0.3f);
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

        private void SetupManagers()
        {
            if (DestructibleManager.Instance == null)
            {
                GameObject obj = new GameObject("DestructibleManager");
                obj.AddComponent<DestructibleManager>();
            }

            if (MechanismManager.Instance == null)
            {
                GameObject obj = new GameObject("MechanismManager");
                obj.AddComponent<MechanismManager>();
            }
        }

        private void CreateDestructibleWalls()
        {
            GameObject wall1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall1.name = "DestructibleWall_1";
            wall1.transform.position = new Vector3(-8f, 1.5f, 0f);
            wall1.transform.localScale = new Vector3(1f, 3f, 4f);

            DestructibleObject dest1 = wall1.AddComponent<DestructibleObject>();
            dest1.health = 150f;
            dest1.impactThreshold = 15f;
            dest1.pressureThreshold = 40f;
            dest1.destructionType = DestructibleObject.DestructionType.Shatter;
            dest1.fragmentCount = 12;
            dest1.explosionForce = 8f;
            dest1.fragmentMinSize = 0.3f;
            dest1.fragmentMaxSize = 0.7f;

            FluidPressureDestruction fpd1 = wall1.AddComponent<FluidPressureDestruction>();
            fpd1.detectionRadius = 2.5f;
            fpd1.damagePerSecond = 30f;
            fpd1.pressureMultiplier = 80f;

            Renderer r1 = wall1.GetComponent<Renderer>();
            if (r1 != null)
            {
                r1.material.color = new Color(0.6f, 0.5f, 0.4f);
            }

            if (DestructibleManager.Instance != null)
            {
                DestructibleManager.Instance.RegisterDestructible(dest1);
            }

            GameObject wall2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall2.name = "DestructibleWall_2";
            wall2.transform.position = new Vector3(8f, 1.5f, 3f);
            wall2.transform.localScale = new Vector3(1f, 2.5f, 3f);

            DestructibleObject dest2 = wall2.AddComponent<DestructibleObject>();
            dest2.health = 100f;
            dest2.destructionType = DestructibleObject.DestructionType.Explode;
            dest2.explosionForce = 15f;
            dest2.explosionRadius = 3f;
            dest2.fragmentCount = 16;

            FluidPressureDestruction fpd2 = wall2.AddComponent<FluidPressureDestruction>();
            fpd2.detectionRadius = 2f;
            fpd2.damagePerSecond = 25f;

            Renderer r2 = wall2.GetComponent<Renderer>();
            if (r2 != null)
            {
                r2.material.color = new Color(0.8f, 0.4f, 0.3f);
            }

            if (DestructibleManager.Instance != null)
            {
                DestructibleManager.Instance.RegisterDestructible(dest2);
            }
        }

        private void CreatePressureSensors()
        {
            GameObject sensor1Obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sensor1Obj.name = "PressureSensor_1";
            sensor1Obj.transform.position = new Vector3(3f, 0.5f, -5f);
            sensor1Obj.transform.localScale = Vector3.one * 0.5f;

            FluidPressureSensor sensor1 = sensor1Obj.AddComponent<FluidPressureSensor>();
            sensor1.sensorId = "sensor_1";
            sensor1.activationPressure = 30f;
            sensor1.deactivationPressure = 10f;
            sensor1.detectionRadius = 2.5f;
            sensor1.useHysteresis = true;
            sensor1.indicatorRenderer = sensor1Obj.GetComponent<Renderer>();

            if (MechanismManager.Instance != null)
            {
                MechanismManager.Instance.RegisterSensor(sensor1);
            }

            GameObject sensor2Obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sensor2Obj.name = "PressureSensor_2";
            sensor2Obj.transform.position = new Vector3(-10f, 0.5f, 5f);
            sensor2Obj.transform.localScale = Vector3.one * 0.4f;

            FluidPressureSensor sensor2 = sensor2Obj.AddComponent<FluidPressureSensor>();
            sensor2.sensorId = "sensor_2";
            sensor2.activationPressure = 50f;
            sensor2.detectionRadius = 2f;
            sensor2.sensorMode = FluidPressureSensor.SensorMode.ParticleCount;
            sensor2.indicatorRenderer = sensor2Obj.GetComponent<Renderer>();

            if (MechanismManager.Instance != null)
            {
                MechanismManager.Instance.RegisterSensor(sensor2);
            }
        }

        private void CreateMechanicalDoors()
        {
            GameObject door1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door1.name = "MechanicalDoor_1";
            door1.transform.position = new Vector3(0f, 1.5f, 8f);
            door1.transform.localScale = new Vector3(3f, 3f, 0.5f);

            MechanicalDoor mechDoor1 = door1.AddComponent<MechanicalDoor>();
            mechDoor1.mechanismId = "door_1";
            mechDoor1.doorTransform = door1.transform;
            mechDoor1.doorType = MechanicalDoor.DoorType.Sliding;
            mechDoor1.openOffset = new Vector3(0, 3.5f, 0);
            mechDoor1.animationDuration = 2f;

            Renderer dr1 = door1.GetComponent<Renderer>();
            if (dr1 != null)
            {
                dr1.material.color = new Color(0.4f, 0.4f, 0.5f);
            }

            if (MechanismManager.Instance != null)
            {
                MechanismManager.Instance.RegisterMechanism(mechDoor1);
            }

            GameObject platformObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platformObj.name = "MovingPlatform_1";
            platformObj.transform.position = new Vector3(-5f, 0.5f, 8f);
            platformObj.transform.localScale = new Vector3(3f, 0.3f, 3f);

            MovingPlatform platform = platformObj.AddComponent<MovingPlatform>();
            platform.mechanismId = "platform_1";
            platform.platformTransform = platformObj.transform;
            platform.targetOffset = new Vector3(0, 4f, 0);
            platform.animationDuration = 3f;

            Renderer pr = platformObj.GetComponent<Renderer>();
            if (pr != null)
            {
                pr.material.color = new Color(0.5f, 0.6f, 0.7f);
            }

            if (MechanismManager.Instance != null)
            {
                MechanismManager.Instance.RegisterMechanism(platform);
            }

            FluidPressureSensor sensor = FindObjectOfType<FluidPressureSensor>();
            if (sensor != null && MechanismManager.Instance != null)
            {
                MechanismManager.Instance.AddConnection(sensor, mechDoor1, false);
            }
        }
    }
}
