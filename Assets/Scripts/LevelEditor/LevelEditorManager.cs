using UnityEngine;
using System.Collections.Generic;
using SoftFluidPuzzle.Core;
using SoftFluidPuzzle.PhysicsSimulation;
using SoftFluidPuzzle.FluidRendering;
using SoftFluidPuzzle.DestructibleObjects;
using SoftFluidPuzzle.PressureMechanisms;

namespace SoftFluidPuzzle.LevelEditor
{
    public class LevelEditorManager : Singleton<LevelEditorManager>
    {
        [Header("Editor Settings")]
        public bool editorMode = false;
        public EditorTool currentTool = EditorTool.Select;
        public string selectedObjectId;
        public float gridSize = 1f;
        public bool snapToGrid = true;
        public bool showGrid = true;

        [Header("References")]
        public Camera editorCamera;
        public Transform editorObjectsParent;

        [Header("Physics")]
        public bool simulatePhysicsInEditor = false;

        private GameObject _selectedObject;
        private List<GameObject> _placedObjects = new List<GameObject>();
        private EditorLevelData _currentLevelData;
        private bool _isDragging = false;
        private Vector3 _dragStartPosition;
        private Vector3 _objectStartPosition;
        private Plane _groundPlane;

        public GameObject SelectedObject => _selectedObject;
        public List<GameObject> PlacedObjects => _placedObjects;
        public EditorLevelData CurrentLevelData => _currentLevelData;
        public bool IsEditorMode => editorMode;

        protected override void Awake()
        {
            base.Awake();
            _groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (editorObjectsParent == null)
            {
                GameObject parentObj = new GameObject("EditorObjects");
                editorObjectsParent = parentObj.transform;
            }
        }

        public void ToggleEditorMode()
        {
            SetEditorMode(!editorMode);
        }

        public void SetEditorMode(bool enable)
        {
            editorMode = enable;

            if (enable)
            {
                EnterEditMode();
            }
            else
            {
                ExitEditMode();
            }

            EventBus.Publish(GameEvents.OnEditorModeChanged, editorMode);
        }

        private void EnterEditMode()
        {
            Time.timeScale = simulatePhysicsInEditor ? 1f : 0f;

            if (editorCamera != null)
            {
                editorCamera.enabled = true;
            }

            DisableGameplaySystems();

            if (_currentLevelData == null)
            {
                NewLevel();
            }
        }

        private void ExitEditMode()
        {
            Time.timeScale = 1f;

            if (editorCamera != null)
            {
                editorCamera.enabled = false;
            }

            DeselectObject();
        }

        private void DisableGameplaySystems()
        {
            if (PhysicsSimulationManager.Instance != null)
            {
                if (!simulatePhysicsInEditor)
                {
                    PhysicsSimulationManager.Instance.SetPaused(true);
                }
            }
        }

        public void NewLevel()
        {
            ClearAllObjects();

            _currentLevelData = new EditorLevelData();
            _currentLevelData.levelId = System.Guid.NewGuid().ToString();
            _currentLevelData.levelName = "New Level";

            CreateDefaultLevel();

            EventBus.Publish(GameEvents.OnNewLevelCreated);
        }

        private void CreateDefaultLevel()
        {
            CreateGroundPlane();

            _currentLevelData.playerSpawnPoint = new Vector3(0, 2, -10);
            _currentLevelData.goalPosition = new Vector3(0, 1, 10);
        }

        private void CreateGroundPlane()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = Vector3.one * 5f;
            ground.transform.position = Vector3.zero;
            ground.transform.SetParent(editorObjectsParent, true);

            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.3f, 0.3f, 0.35f);
            }

            _placedObjects.Add(ground);
        }

        public void PlaceObject(string objectId, Vector3 position)
        {
            PlaceableObject objData = PlaceableObjectLibrary.Instance.GetObjectById(objectId);
            if (objData == null) return;

            GameObject placedObj = CreateObjectFromType(objectId, position);
            if (placedObj == null) return;

            placedObj.transform.SetParent(editorObjectsParent, true);
            placedObj.name = objData.displayName + "_" + _placedObjects.Count;

            _placedObjects.Add(placedObj);

            PlacedObjectData data = new PlacedObjectData
            {
                objectId = objectId,
                objectType = objData.category,
                position = placedObj.transform.position,
                rotation = placedObj.transform.eulerAngles,
                scale = placedObj.transform.localScale
            };
            _currentLevelData.placedObjects.Add(data);

            SelectObject(placedObj);

            EventBus.Publish(GameEvents.OnObjectPlaced, objectId);
        }

        private GameObject CreateObjectFromType(string objectId, Vector3 position)
        {
            GameObject obj = null;

            switch (objectId)
            {
                case "basic_cube":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    break;
                case "basic_plane":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    break;
                case "basic_sphere":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    break;
                case "basic_cylinder":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    break;

                case "physics_box":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Rigidbody rb = obj.AddComponent<Rigidbody>();
                    rb.mass = 1f;
                    break;

                case "softbody_cube":
                    obj = new GameObject("SoftBody");
                    SoftBody softBody = obj.AddComponent<SoftBody>();
                    softBody.resolution = 6;
                    softBody.size = 2f;
                    MeshFilter mf = obj.AddComponent<MeshFilter>();
                    MeshRenderer mr = obj.AddComponent<MeshRenderer>();
                    mr.material = new Material(Shader.Find("Standard"));
                    mr.material.color = new Color(0.2f, 0.8f, 0.6f);
                    break;

                case "fluid_emitter":
                    obj = new GameObject("FluidEmitter");
                    obj.AddComponent<FluidSystem>();
                    FluidEmitter emitter = obj.AddComponent<FluidEmitter>();
                    emitter.particlesPerSecond = 30;
                    break;

                case "pressure_sensor":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    obj.transform.localScale = Vector3.one * 0.5f;
                    FluidPressureSensor sensor = obj.AddComponent<FluidPressureSensor>();
                    sensor.activationPressure = 50f;
                    break;

                case "mechanical_door":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.transform.localScale = new Vector3(2, 3, 0.3f);
                    MechanicalDoor door = obj.AddComponent<MechanicalDoor>();
                    door.doorTransform = obj.transform;
                    door.openOffset = new Vector3(0, 3f, 0);
                    break;

                case "destructible_wall":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.transform.localScale = new Vector3(2, 3, 0.5f);
                    DestructibleObject destObj = obj.AddComponent<DestructibleObject>();
                    destObj.health = 100f;
                    destObj.destructionType = DestructibleObject.DestructionType.Shatter;
                    FluidPressureDestruction fpd = obj.AddComponent<FluidPressureDestruction>();
                    fpd.detectionRadius = 2f;
                    break;

                case "pressure_plate":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.transform.localScale = new Vector3(2, 0.2f, 2);
                    LevelManagement.PressurePlate plate = obj.AddComponent<LevelManagement.PressurePlate>();
                    plate.activationForce = 20f;
                    break;

                case "goal_zone":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.GetComponent<Collider>().isTrigger = true;
                    obj.transform.localScale = new Vector3(4, 2, 4);
                    LevelManagement.GoalZone goal = obj.AddComponent<LevelManagement.GoalZone>();
                    goal.requireAllObjectives = false;
                    break;

                default:
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    break;
            }

            if (obj != null)
            {
                obj.transform.position = position;
            }

            return obj;
        }

        public void SelectObject(GameObject obj)
        {
            if (_selectedObject == obj) return;

            DeselectObject();
            _selectedObject = obj;

            HighlightObject(obj, true);

            EventBus.Publish(GameEvents.OnObjectSelected, obj != null ? obj.name : "");
        }

        public void DeselectObject()
        {
            if (_selectedObject != null)
            {
                HighlightObject(_selectedObject, false);
                _selectedObject = null;
            }
        }

        private void HighlightObject(GameObject obj, bool highlight)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (highlight)
                {
                    renderer.material.SetFloat("_OutlineWidth", 0.05f);
                    renderer.material.SetColor("_OutlineColor", Color.yellow);
                }
                else
                {
                    renderer.material.SetFloat("_OutlineWidth", 0f);
                }
            }
        }

        public void DeleteSelectedObject()
        {
            if (_selectedObject == null) return;

            string objName = _selectedObject.name;
            _placedObjects.Remove(_selectedObject);
            Destroy(_selectedObject);
            _selectedObject = null;

            EventBus.Publish(GameEvents.OnObjectDeleted, objName);
        }

        public void ClearAllObjects()
        {
            for (int i = _placedObjects.Count - 1; i >= 0; i--)
            {
                if (_placedObjects[i] != null)
                {
                    Destroy(_placedObjects[i]);
                }
            }
            _placedObjects.Clear();
            _selectedObject = null;
        }

        public void SetTool(EditorTool tool)
        {
            currentTool = tool;
            EventBus.Publish(GameEvents.OnToolChanged, tool.ToString());
        }

        public void DuplicateSelected()
        {
            if (_selectedObject == null) return;

            GameObject newObj = Instantiate(_selectedObject, editorObjectsParent);
            newObj.transform.position += Vector3.right * gridSize;
            _placedObjects.Add(newObj);
            SelectObject(newObj);
        }

        public void SaveLevel(string filePath)
        {
            if (_currentLevelData == null) return;

            UpdateLevelDataFromScene();

            string json = JsonUtility.ToJson(_currentLevelData, true);
            System.IO.File.WriteAllText(filePath, json);

            EventBus.Publish(GameEvents.OnLevelSaved, filePath);
        }

        public void LoadLevel(string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return;

            string json = System.IO.File.ReadAllText(filePath);
            _currentLevelData = JsonUtility.FromJson<EditorLevelData>(json);

            ClearAllObjects();
            LoadObjectsFromData();

            EventBus.Publish(GameEvents.OnLevelLoaded, filePath);
        }

        private void UpdateLevelDataFromScene()
        {
            _currentLevelData.placedObjects.Clear();

            foreach (GameObject obj in _placedObjects)
            {
                if (obj == null) continue;

                PlacedObjectData data = new PlacedObjectData
                {
                    position = obj.transform.position,
                    rotation = obj.transform.eulerAngles,
                    scale = obj.transform.localScale
                };
                _currentLevelData.placedObjects.Add(data);
            }
        }

        private void LoadObjectsFromData()
        {
            foreach (PlacedObjectData data in _currentLevelData.placedObjects)
            {
                if (!string.IsNullOrEmpty(data.objectId))
                {
                    GameObject obj = CreateObjectFromType(data.objectId, data.position);
                    if (obj != null)
                    {
                        obj.transform.eulerAngles = data.rotation;
                        obj.transform.localScale = data.scale;
                        obj.transform.SetParent(editorObjectsParent, true);
                        _placedObjects.Add(obj);
                    }
                }
            }
        }

        public void MoveSelected(Vector3 delta)
        {
            if (_selectedObject == null) return;

            _selectedObject.transform.position += delta;

            if (snapToGrid)
            {
                Vector3 pos = _selectedObject.transform.position;
                pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
                pos.y = Mathf.Round(pos.y / gridSize) * gridSize;
                pos.z = Mathf.Round(pos.z / gridSize) * gridSize;
                _selectedObject.transform.position = pos;
            }
        }

        public void RotateSelected(Vector3 eulerAngles)
        {
            if (_selectedObject == null) return;
            _selectedObject.transform.Rotate(eulerAngles, Space.World);
        }

        public void ScaleSelected(Vector3 scaleDelta)
        {
            if (_selectedObject == null) return;
            _selectedObject.transform.localScale += scaleDelta;

            Vector3 scale = _selectedObject.transform.localScale;
            scale.x = Mathf.Max(0.1f, scale.x);
            scale.y = Mathf.Max(0.1f, scale.y);
            scale.z = Mathf.Max(0.1f, scale.z);
            _selectedObject.transform.localScale = scale;
        }

        private void Update()
        {
            if (!editorMode) return;

            HandleEditorInput();
        }

        private void HandleEditorInput()
        {
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
            {
                DeleteSelectedObject();
            }

            if (Input.GetKeyDown(KeyCode.D) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                DuplicateSelected();
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) SetTool(EditorTool.Select);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetTool(EditorTool.Move);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetTool(EditorTool.Rotate);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetTool(EditorTool.Scale);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SetTool(EditorTool.Delete);

            if (Input.GetMouseButtonDown(0))
            {
                HandleLeftClick();
            }

            if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
            }

            if (_isDragging && currentTool == EditorTool.Move && _selectedObject != null)
            {
                HandleDragMove();
            }
        }

        private void HandleLeftClick()
        {
            Ray ray = editorCamera != null ? editorCamera.ScreenPointToRay(Input.mousePosition) : Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                switch (currentTool)
                {
                    case EditorTool.Select:
                    case EditorTool.Move:
                    case EditorTool.Rotate:
                    case EditorTool.Scale:
                        SelectObject(hit.collider.gameObject);
                        if (currentTool == EditorTool.Move)
                        {
                            _isDragging = true;
                            _dragStartPosition = hit.point;
                            _objectStartPosition = _selectedObject.transform.position;
                        }
                        break;

                    case EditorTool.Delete:
                        SelectObject(hit.collider.gameObject);
                        DeleteSelectedObject();
                        break;

                    case EditorTool.Place:
                        if (!string.IsNullOrEmpty(selectedObjectId))
                        {
                            PlaceObject(selectedObjectId, hit.point);
                        }
                        break;
                }
            }
            else
            {
                if (currentTool == EditorTool.Select)
                {
                    DeselectObject();
                }
                else if (currentTool == EditorTool.Place && !string.IsNullOrEmpty(selectedObjectId))
                {
                    float enter = 0f;
                    if (_groundPlane.Raycast(ray, out enter))
                    {
                        Vector3 worldPos = ray.GetPoint(enter);
                        PlaceObject(selectedObjectId, worldPos);
                    }
                }
            }
        }

        private void HandleDragMove()
        {
            Ray ray = editorCamera != null ? editorCamera.ScreenPointToRay(Input.mousePosition) : Camera.main.ScreenPointToRay(Input.mousePosition);
            float enter = 0f;

            if (_groundPlane.Raycast(ray, out enter))
            {
                Vector3 worldPos = ray.GetPoint(enter);
                Vector3 delta = worldPos - _dragStartPosition;

                _selectedObject.transform.position = _objectStartPosition + new Vector3(delta.x, 0, delta.z);

                if (snapToGrid)
                {
                    Vector3 pos = _selectedObject.transform.position;
                    pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
                    pos.z = Mathf.Round(pos.z / gridSize) * gridSize;
                    _selectedObject.transform.position = pos;
                }
            }
        }
    }
}
