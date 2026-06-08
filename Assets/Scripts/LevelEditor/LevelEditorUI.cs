using UnityEngine;
using UnityEngine.UI;
using SoftFluidPuzzle.Core;

namespace SoftFluidPuzzle.LevelEditor
{
    public class LevelEditorUI : MonoBehaviour
    {
        [Header("References")]
        public LevelEditorManager editorManager;

        [Header("UI Panels")]
        public GameObject toolbarPanel;
        public GameObject objectLibraryPanel;
        public GameObject propertiesPanel;
        public GameObject saveLoadPanel;

        [Header("Buttons")]
        public Button toggleEditorBtn;
        public Button newBtn;
        public Button saveBtn;
        public Button loadBtn;
        public Button deleteBtn;
        public Button duplicateBtn;

        [Header("Tool Buttons")]
        public Button selectToolBtn;
        public Button moveToolBtn;
        public Button rotateToolBtn;
        public Button scaleToolBtn;
        public Button placeToolBtn;

        [Header("Inputs")]
        public InputField levelNameInput;
        public InputField gridSizeInput;
        public Toggle snapToGridToggle;

        [Header("Category Tabs")]
        public Button[] categoryButtons;

        [Header("Object Library")]
        public Transform objectLibraryContent;
        public GameObject objectButtonPrefab;

        [Header("Properties")]
        public Text objectNameText;
        public InputField posXInput;
        public InputField posYInput;
        public InputField posZInput;
        public InputField rotXInput;
        public InputField rotYInput;
        public InputField rotZInput;
        public InputField scaleXInput;
        public InputField scaleYInput;
        public InputField scaleZInput;

        private string _currentCategory = "Basic";

        private void Start()
        {
            InitializeUI();
            RegisterEvents();
            RefreshObjectLibrary();
        }

        private void InitializeUI()
        {
            if (editorManager == null)
            {
                editorManager = LevelEditorManager.Instance;
            }

            UpdateToolButtons();
            UpdateCategoryTabs();
        }

        private void RegisterEvents()
        {
            if (toggleEditorBtn != null)
                toggleEditorBtn.onClick.AddListener(ToggleEditorMode);

            if (newBtn != null)
                newBtn.onClick.AddListener(OnNewClicked);

            if (saveBtn != null)
                saveBtn.onClick.AddListener(OnSaveClicked);

            if (loadBtn != null)
                loadBtn.onClick.AddListener(OnLoadClicked);

            if (deleteBtn != null)
                deleteBtn.onClick.AddListener(OnDeleteClicked);

            if (duplicateBtn != null)
                duplicateBtn.onClick.AddListener(OnDuplicateClicked);

            if (selectToolBtn != null)
                selectToolBtn.onClick.AddListener(() => SetTool(EditorTool.Select));

            if (moveToolBtn != null)
                moveToolBtn.onClick.AddListener(() => SetTool(EditorTool.Move));

            if (rotateToolBtn != null)
                rotateToolBtn.onClick.AddListener(() => SetTool(EditorTool.Rotate));

            if (scaleToolBtn != null)
                scaleToolBtn.onClick.AddListener(() => SetTool(EditorTool.Scale));

            if (placeToolBtn != null)
                placeToolBtn.onClick.AddListener(() => SetTool(EditorTool.Place));

            if (snapToGridToggle != null)
                snapToGridToggle.onValueChanged.AddListener(OnSnapToggleChanged);

            if (gridSizeInput != null)
                gridSizeInput.onEndEdit.AddListener(OnGridSizeChanged);

            if (levelNameInput != null)
                levelNameInput.onEndEdit.AddListener(OnLevelNameChanged);

            EventBus.Subscribe<string>(GameEvents.OnObjectSelected, OnObjectSelected);
            EventBus.Subscribe<string>(GameEvents.OnToolChanged, OnToolChanged);
            EventBus.Subscribe<bool>(GameEvents.OnEditorModeChanged, OnEditorModeChanged);
        }

        private void ToggleEditorMode()
        {
            if (editorManager != null)
            {
                editorManager.ToggleEditorMode();
            }
        }

        private void OnNewClicked()
        {
            if (editorManager != null)
            {
                editorManager.NewLevel();
                levelNameInput.text = "New Level";
            }
        }

        private void OnSaveClicked()
        {
            if (editorManager != null)
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, "Levels", levelNameInput.text + ".json");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                editorManager.SaveLevel(path);
                Debug.Log("Level saved to: " + path);
            }
        }

        private void OnLoadClicked()
        {
            string levelDir = System.IO.Path.Combine(Application.persistentDataPath, "Levels");
            if (System.IO.Directory.Exists(levelDir))
            {
                string[] files = System.IO.Directory.GetFiles(levelDir, "*.json");
                if (files.Length > 0 && editorManager != null)
                {
                    editorManager.LoadLevel(files[0]);
                }
            }
        }

        private void OnDeleteClicked()
        {
            if (editorManager != null)
            {
                editorManager.DeleteSelectedObject();
            }
        }

        private void OnDuplicateClicked()
        {
            if (editorManager != null)
            {
                editorManager.DuplicateSelected();
            }
        }

        private void SetTool(EditorTool tool)
        {
            if (editorManager != null)
            {
                editorManager.SetTool(tool);
            }
            UpdateToolButtons();
        }

        private void UpdateToolButtons()
        {
            if (editorManager == null) return;

            EditorTool current = editorManager.currentTool;

            SetButtonState(selectToolBtn, current == EditorTool.Select);
            SetButtonState(moveToolBtn, current == EditorTool.Move);
            SetButtonState(rotateToolBtn, current == EditorTool.Rotate);
            SetButtonState(scaleToolBtn, current == EditorTool.Scale);
            SetButtonState(placeToolBtn, current == EditorTool.Place);
        }

        private void SetButtonState(Button button, bool selected)
        {
            if (button == null) return;

            ColorBlock colors = button.colors;
            if (selected)
            {
                colors.normalColor = new Color(0.3f, 0.6f, 1f, 1f);
                colors.highlightedColor = new Color(0.4f, 0.7f, 1f, 1f);
            }
            else
            {
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            }
            button.colors = colors;
        }

        private void UpdateCategoryTabs()
        {
            if (PlaceableObjectLibrary.Instance == null) return;

            string[] categories = PlaceableObjectLibrary.Instance.categories;

            for (int i = 0; i < categoryButtons.Length && i < categories.Length; i++)
            {
                if (categoryButtons[i] != null)
                {
                    string cat = categories[i];
                    Text btnText = categoryButtons[i].GetComponentInChildren<Text>();
                    if (btnText != null) btnText.text = cat;
                    int index = i;
                    categoryButtons[i].onClick.AddListener(() => OnCategoryClicked(categories[index]));
                }
            }
        }

        private void OnCategoryClicked(string category)
        {
            _currentCategory = category;
            RefreshObjectLibrary();
        }

        private void RefreshObjectLibrary()
        {
            if (PlaceableObjectLibrary.Instance == null || objectLibraryContent == null) return;

            foreach (Transform child in objectLibraryContent)
            {
                Destroy(child.gameObject);
            }

            var objects = PlaceableObjectLibrary.Instance.GetObjectsByCategory(_currentCategory);

            foreach (var obj in objects)
            {
                GameObject btnObj = Instantiate(objectButtonPrefab, objectLibraryContent);
                Text btnText = btnObj.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = obj.displayName;
                }

                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    string objId = obj.id;
                    btn.onClick.AddListener(() => OnObjectLibraryItemClicked(objId));
                }
            }
        }

        private void OnObjectLibraryItemClicked(string objectId)
        {
            if (editorManager != null)
            {
                editorManager.selectedObjectId = objectId;
                editorManager.SetTool(EditorTool.Place);
            }
        }

        private void OnObjectSelected(string objectName)
        {
            UpdatePropertiesPanel();
        }

        private void OnToolChanged(string toolName)
        {
            UpdateToolButtons();
        }

        private void OnEditorModeChanged(bool inEditor)
        {
            if (toolbarPanel != null) toolbarPanel.SetActive(inEditor);
            if (objectLibraryPanel != null) objectLibraryPanel.SetActive(inEditor);
            if (propertiesPanel != null) propertiesPanel.SetActive(inEditor);

            if (toggleEditorBtn != null)
            {
                Text btnText = toggleEditorBtn.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.text = inEditor ? "退出编辑" : "关卡编辑";
                }
            }
        }

        private void UpdatePropertiesPanel()
        {
            if (editorManager == null || editorManager.SelectedObject == null)
            {
                if (objectNameText != null) objectNameText.text = "未选择对象";
                ClearPositionInputs();
                ClearRotationInputs();
                ClearScaleInputs();
                return;
            }

            GameObject obj = editorManager.SelectedObject;

            if (objectNameText != null) objectNameText.text = obj.name;

            if (posXInput != null) posXInput.text = obj.transform.position.x.ToString("F2");
            if (posYInput != null) posYInput.text = obj.transform.position.y.ToString("F2");
            if (posZInput != null) posZInput.text = obj.transform.position.z.ToString("F2");

            if (rotXInput != null) rotXInput.text = obj.transform.eulerAngles.x.ToString("F0");
            if (rotYInput != null) rotYInput.text = obj.transform.eulerAngles.y.ToString("F0");
            if (rotZInput != null) rotZInput.text = obj.transform.eulerAngles.z.ToString("F0");

            if (scaleXInput != null) scaleXInput.text = obj.transform.localScale.x.ToString("F2");
            if (scaleYInput != null) scaleYInput.text = obj.transform.localScale.y.ToString("F2");
            if (scaleZInput != null) scaleZInput.text = obj.transform.localScale.z.ToString("F2");
        }

        private void ClearPositionInputs()
        {
            if (posXInput != null) posXInput.text = "";
            if (posYInput != null) posYInput.text = "";
            if (posZInput != null) posZInput.text = "";
        }

        private void ClearRotationInputs()
        {
            if (rotXInput != null) rotXInput.text = "";
            if (rotYInput != null) rotYInput.text = "";
            if (rotZInput != null) rotZInput.text = "";
        }

        private void ClearScaleInputs()
        {
            if (scaleXInput != null) scaleXInput.text = "";
            if (scaleYInput != null) scaleYInput.text = "";
            if (scaleZInput != null) scaleZInput.text = "";
        }

        private void OnSnapToggleChanged(bool snap)
        {
            if (editorManager != null)
            {
                editorManager.snapToGrid = snap;
            }
        }

        private void OnGridSizeChanged(string value)
        {
            if (editorManager != null && float.TryParse(value, out float size))
            {
                editorManager.gridSize = Mathf.Max(0.1f, size);
            }
        }

        private void OnLevelNameChanged(string value)
        {
            if (editorManager != null && editorManager.CurrentLevelData != null)
            {
                editorManager.CurrentLevelData.levelName = value;
            }
        }

        private void Update()
        {
            if (editorManager != null && editorManager.IsEditorMode && editorManager.SelectedObject != null)
            {
                HandleTransformInputs();
            }
        }

        private void HandleTransformInputs()
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                float moveSpeed = editorManager.gridSize;

                if (Input.GetKeyDown(KeyCode.UpArrow))
                    editorManager.MoveSelected(Vector3.forward * moveSpeed);
                if (Input.GetKeyDown(KeyCode.DownArrow))
                    editorManager.MoveSelected(Vector3.back * moveSpeed);
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                    editorManager.MoveSelected(Vector3.left * moveSpeed);
                if (Input.GetKeyDown(KeyCode.RightArrow))
                    editorManager.MoveSelected(Vector3.right * moveSpeed);

                if (Input.GetKeyDown(KeyCode.PageUp))
                    editorManager.MoveSelected(Vector3.up * moveSpeed);
                if (Input.GetKeyDown(KeyCode.PageDown))
                    editorManager.MoveSelected(Vector3.down * moveSpeed);
            }

            if (Input.GetKey(KeyCode.R) && editorManager.currentTool == EditorTool.Rotate)
            {
                editorManager.RotateSelected(Vector3.up * 90f * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.S) && editorManager.currentTool == EditorTool.Scale)
            {
                editorManager.ScaleSelected(Vector3.one * Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<string>(GameEvents.OnObjectSelected, OnObjectSelected);
            EventBus.Unsubscribe<string>(GameEvents.OnToolChanged, OnToolChanged);
            EventBus.Unsubscribe<bool>(GameEvents.OnEditorModeChanged, OnEditorModeChanged);
        }
    }
}
