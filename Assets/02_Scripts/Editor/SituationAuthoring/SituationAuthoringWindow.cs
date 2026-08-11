using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VirtualRescue.GameFlow;

namespace VirtualRescue.EditorTools.SituationAuthoring
{
    public sealed class SituationAuthoringWindow : EditorWindow
    {
        private enum Tab
        {
            NewSituation,
            EditExisting,
            BuildingBlocks,
            Validate
        }

        private const string FirePrefabPath =
            "Assets/03_Prefabs/Particles/Fire/Fire_Small_Effect.prefab";
        private const string ExtinguisherPrefabPath =
            "Assets/03_Prefabs/Interaction/Fire_Extinguisher.prefab";

        [SerializeField] private Tab _tab;
        [SerializeField] private Vector2 _scrollPosition;

        [Header("New Situation")]
        [SerializeField] private string _displayName = "Location_Situation";
        [SerializeField] private string _situationId = "location.situation";
        [SerializeField] private string _selectedLocationId = "room";
        [SerializeField] private SituationLevel _level = SituationLevel.Level0;
        [SerializeField] private string _sceneName = "Scenario_Room_NewSituation";
        [SerializeField] private string _controllerClassName =
            "NewSituationController";
        [SerializeField] private string _controllerNamespace =
            "VirtualRescue.Situations";
        [SerializeField] private int _weight = 1;
        [SerializeField] private int _minimumDay = 1;
        [SerializeField] private bool _registerAsCandidate;
        [SerializeField] private bool _usesTimeLimit;
        [SerializeField] private float _timeLimitSeconds = 60f;
        [SerializeField] private bool _allowEmergencyStairs;
        [SerializeField] private bool _allowRefugeArea;
        [SerializeField] private bool _allowLightweightPartition;
        [SerializeField] private bool _allowDescender;
        [SerializeField] private List<GameObject> _initialPrefabs = new();
        [SerializeField] private List<ModuleObjectId> _newModuleIds = new();
        [SerializeField] private List<DoorId> _newLockedDoorIds = new();
        [SerializeField] private List<DoorId> _newTrapDoorIds = new();

        [Header("Existing")]
        [SerializeField] private SituationDefinition _editDefinition;
        [SerializeField] private SituationDefinition _validationDefinition;
        [SerializeField] private HomeLayoutDefinition _homeLayout;
        [SerializeField] private SituationLocationCatalog _locationCatalog;

        [Header("New Location")]
        [SerializeField] private bool _showAddLocation;
        [SerializeField] private string _newLocationId = string.Empty;
        [SerializeField] private string _newLocationName = string.Empty;

        [Header("Building Blocks")]
        [SerializeField] private SituationController _buildingController;
        [SerializeField] private GameObject _buildingTarget;
        [SerializeField] private GameObject _buildingPrefab;

        private readonly List<SituationValidationResult> _validationResults = new();
        private readonly List<ModuleObjectId> _availableModuleIds = new();
        private readonly List<DoorId> _availableDoorIds = new();
        private readonly HashSet<ModuleObjectId> _selectedModuleIds = new();
        private readonly HashSet<DoorId> _selectedDoorIds = new();
        private Vector2 _moduleIdScroll;
        private Vector2 _doorIdScroll;
        private bool _editRegistrationKnown;
        private bool _editRegistered;
        private int _editRegistrationCount;
        private SerializedObject _editSerializedDefinition;
        private string _lastMessage = string.Empty;
        private MessageType _lastMessageType = MessageType.None;

        [MenuItem("Tools/Virtual Rescue/Situation Authoring")]
        public static void Open()
        {
            SituationAuthoringWindow window = GetWindow<SituationAuthoringWindow>();
            window.titleContent = new GUIContent("Situation Authoring");
            window.minSize = new Vector2(560f, 640f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_displayName == "New Situation")
            {
                _displayName = "Location_Situation";
            }

            if (_situationId == "new.situation")
            {
                _situationId = "location.situation";
            }

            _locationCatalog =
                AssetDatabase.LoadAssetAtPath<SituationLocationCatalog>(
                    SituationLocationCatalogService.CatalogPath);
            if (_locationCatalog == null)
            {
                EditorApplication.delayCall += InitializeLocationCatalog;
            }
            else
            {
                EnsureSelectedLocation();
            }

            RefreshIdAssets();
            RefreshHomeLayout();
            if (Selection.activeObject is SituationDefinition definition)
            {
                _editDefinition = definition;
                _validationDefinition = definition;
                _editSerializedDefinition = new SerializedObject(definition);
            }
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= InitializeLocationCatalog;
        }

        private void OnGUI()
        {
            DrawPendingCreation();

            _tab = (Tab)GUILayout.Toolbar(
                (int)_tab,
                new[] { "New", "Edit Existing", "Building Blocks", "Validate" });
            EditorGUILayout.Space(6f);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            switch (_tab)
            {
                case Tab.NewSituation:
                    DrawNewSituation();
                    break;
                case Tab.EditExisting:
                    DrawEditExisting();
                    break;
                case Tab.BuildingBlocks:
                    DrawBuildingBlocks();
                    break;
                case Tab.Validate:
                    DrawValidate();
                    break;
            }

            if (!string.IsNullOrEmpty(_lastMessage))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox(_lastMessage, _lastMessageType);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPendingCreation()
        {
            if (!SituationCreationResumeHandler.HasPending)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                SituationCreationResumeHandler.Status,
                MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = !EditorApplication.isCompiling;
                if (GUILayout.Button("Resume"))
                {
                    SituationCreationResumeHandler.TryResume();
                }

                GUI.enabled = true;
                if (GUILayout.Button("Cancel Pending Request"))
                {
                    SituationCreationResumeHandler.Cancel();
                }
            }

            EditorGUILayout.Space(6f);
        }

        private void DrawNewSituation()
        {
            EditorGUILayout.LabelField("New Situation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Creates the Controller script first, then resumes scene and " +
                "Definition creation after Unity compiles.",
                MessageType.Info);

            _displayName = EditorGUILayout.TextField(Required(
                "Display Name",
                "Hierarchy에 표시되며 Definition 파일명에 사용되는 이름입니다."),
                _displayName);
            _situationId = EditorGUILayout.TextField(Required(
                "Situation ID",
                "상황 기록과 라디오 매칭에 사용하는 고유 ID입니다. 중복될 수 없습니다."),
                _situationId);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    Optional(
                        "Home Layout",
                        "LoopBase의 DaySceneCoordinator에서 자동으로 가져옵니다."),
                    _homeLayout,
                    typeof(HomeLayoutDefinition),
                    false);
            }

            _level = (SituationLevel)EditorGUILayout.EnumPopup(
                Required("Level", "출구와 제한시간 규칙에 사용할 상황 단계를 선택합니다."),
                _level);
            DrawLocationSelector();

            _sceneName = EditorGUILayout.TextField(Required(
                "Scene Name",
                "생성할 씬 에셋 이름입니다. Scenario_ 접두사 사용을 권장합니다."),
                _sceneName);
            _controllerClassName = EditorGUILayout.TextField(Required(
                "Controller Class Name",
                "SituationController를 상속하여 생성할 C# 클래스 이름입니다."),
                _controllerClassName);
            _controllerNamespace = EditorGUILayout.TextField(Required(
                "Controller Namespace",
                "생성할 Controller 클래스의 C# 네임스페이스입니다."),
                _controllerNamespace);
            _weight = EditorGUILayout.IntField(Required(
                "Weight",
                "상황 무작위 선택에 사용하는 상대 가중치입니다. 테스트용 상황에도 저장됩니다."),
                _weight);
            _minimumDay = EditorGUILayout.IntSlider(Required(
                "Minimum Day",
                "이 상황이 처음 선택될 수 있는 날짜입니다."),
                _minimumDay,
                1,
                7);
            _registerAsCandidate = EditorGUILayout.Toggle(Optional(
                "Register as Candidate",
                "활성화하면 생성한 Definition을 LoopBase Candidates에 추가합니다. " +
                "테스트용 상황 생성을 위해 기본값은 꺼져 있습니다."),
                _registerAsCandidate);

            DrawLevel2Rules();
            DrawAssetList("Initial Prefabs [Optional]", _initialPrefabs);
            DrawAssetList("Module Object IDs [Optional]", _newModuleIds);
            DrawAssetList("Locked Door IDs [Optional]", _newLockedDoorIds);
            DrawAssetList("Trap Door IDs [Optional]", _newTrapDoorIds);
            if (GUILayout.Button("Door ID 배치도 보기"))
            {
                DoorIdReferenceWindow.Open();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Planned Assets", EditorStyles.boldLabel);
            SituationCreationRequest previewRequest = BuildRequest();
            EditorGUILayout.SelectableLabel(
                previewRequest.ControllerScriptPath,
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.SelectableLabel(
                previewRequest.ScenePath,
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.SelectableLabel(
                previewRequest.DefinitionPath,
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));

            bool valid = SituationControllerScriptGenerator.ValidateRequest(
                previewRequest,
                out string error,
                false);
            if (!valid)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            using (new EditorGUI.DisabledScope(
                       !valid || SituationCreationResumeHandler.HasPending ||
                       EditorApplication.isCompiling))
            {
                if (GUILayout.Button("Create Situation", GUILayout.Height(32f)))
                {
                    if (SituationControllerScriptGenerator.TryBegin(
                            previewRequest,
                            out string creationError))
                    {
                        SetMessage(
                            "Controller script created. Unity will compile and " +
                            "resume the remaining work.",
                            MessageType.Info);
                    }
                    else
                    {
                        SetMessage(creationError, MessageType.Error);
                    }
                }
            }
        }

        private void DrawLevel2Rules()
        {
            if (_level != SituationLevel.Level2)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Level 2 Rules", EditorStyles.boldLabel);
            _usesTimeLimit = EditorGUILayout.Toggle(Conditional(
                "Uses Time Limit",
                "공용 Level 2 제한시간 카운트다운을 시작합니다."),
                _usesTimeLimit);
            if (_usesTimeLimit)
            {
                _timeLimitSeconds = EditorGUILayout.FloatField(Conditional(
                    "Time Limit Seconds",
                    "제한시간을 사용할 때는 0보다 큰 값을 입력해야 합니다."),
                    _timeLimitSeconds);
            }

            EditorGUILayout.LabelField(Conditional(
                "Allowed Exits",
                "엘리베이터가 아닌 출구를 하나 이상 선택해야 합니다."));
            _allowEmergencyStairs = EditorGUILayout.ToggleLeft(
                "Emergency Stairs",
                _allowEmergencyStairs);
            _allowRefugeArea = EditorGUILayout.ToggleLeft(
                "Refuge Area",
                _allowRefugeArea);
            _allowLightweightPartition = EditorGUILayout.ToggleLeft(
                "Lightweight Partition",
                _allowLightweightPartition);
            _allowDescender = EditorGUILayout.ToggleLeft(
                "Descender",
                _allowDescender);
        }

        private void DrawLocationSelector()
        {
            EditorGUILayout.LabelField(Required(
                "Location",
                "씬과 Controller 저장 폴더를 결정하는 Location 항목입니다."));

            if (_locationCatalog == null || _locationCatalog.Locations.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Situation Location Catalog is missing or empty.",
                    MessageType.Error);
                if (GUILayout.Button("Create or Reload Catalog"))
                {
                    _locationCatalog =
                        SituationLocationCatalogService.GetOrCreate();
                    EnsureSelectedLocation();
                }

                return;
            }

            string[] displayNames = _locationCatalog.Locations
                .Select(location => location.DisplayName)
                .ToArray();
            int selectedIndex = 0;
            for (int index = 0; index < _locationCatalog.Locations.Count; index++)
            {
                if (string.Equals(
                        _locationCatalog.Locations[index].Id,
                        _selectedLocationId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = index;
                    break;
                }
            }

            int newIndex = EditorGUILayout.Popup(selectedIndex, displayNames);
            if (newIndex != selectedIndex ||
                string.IsNullOrWhiteSpace(_selectedLocationId))
            {
                SelectLocation(_locationCatalog.Locations[newIndex]);
            }

            SituationLocationEntry selected = GetSelectedLocation();
            if (selected != null)
            {
                EditorGUILayout.LabelField(
                    "Scene Folder",
                    SituationLocationPathMap.GetSceneFolder(
                        selected.SceneFolderName,
                        _level));
                EditorGUILayout.LabelField(
                    "Controller Folder",
                    SituationLocationPathMap.GetControllerFolder(
                        selected.ControllerFolderName,
                        _level));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add New Location"))
                {
                    _showAddLocation = !_showAddLocation;
                }

                if (GUILayout.Button("Select Catalog Asset"))
                {
                    Selection.activeObject = _locationCatalog;
                    EditorGUIUtility.PingObject(_locationCatalog);
                }
            }

            if (_showAddLocation)
            {
                DrawAddLocationForm();
            }
        }

        private void DrawAddLocationForm()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "Add New Location",
                    EditorStyles.boldLabel);
                _newLocationName = EditorGUILayout.TextField(Required(
                    "Location Name",
                    "드롭다운 표시와 Scene/Controller 폴더에 공통으로 사용할 이름입니다."),
                    _newLocationName);
                _newLocationId = EditorGUILayout.TextField(Required(
                    "Location ID",
                    "Location을 구분하는 소문자 고유 ID입니다. 예: bathroom"),
                    _newLocationId);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Save Location"))
                    {
                        if (SituationLocationCatalogService.TryAdd(
                                _locationCatalog,
                                _newLocationId,
                                _newLocationName,
                                _newLocationName,
                                _newLocationName,
                                out SituationLocationEntry entry,
                                out string error))
                        {
                            SelectLocation(entry);
                            ClearAddLocationForm();
                            SetMessage(
                                $"Location '{entry.DisplayName}' was added.",
                                MessageType.Info);
                        }
                        else
                        {
                            SetMessage(error, MessageType.Error);
                            EditorUtility.DisplayDialog(
                                "Could Not Save Location",
                                error,
                                "OK");
                        }
                    }

                    if (GUILayout.Button("Cancel"))
                    {
                        ClearAddLocationForm();
                    }
                }
            }
        }

        private void DrawEditExisting()
        {
            EditorGUILayout.LabelField("Edit Existing", EditorStyles.boldLabel);
            SituationDefinition previous = _editDefinition;
            _editDefinition = (SituationDefinition)EditorGUILayout.ObjectField(
                Required("Definition", "수정할 기존 Situation Definition을 선택합니다."),
                _editDefinition,
                typeof(SituationDefinition),
                false);
            if (_editDefinition != previous)
            {
                _editRegistrationKnown = false;
                _validationDefinition = _editDefinition;
                _editSerializedDefinition = _editDefinition != null
                    ? new SerializedObject(_editDefinition)
                    : null;
            }

            if (_editDefinition == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a SituationDefinition.",
                    MessageType.Info);
                return;
            }

            if (_editSerializedDefinition == null ||
                _editSerializedDefinition.targetObject != _editDefinition)
            {
                _editSerializedDefinition = new SerializedObject(_editDefinition);
            }

            SerializedObject serializedDefinition = _editSerializedDefinition;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    serializedDefinition.FindProperty("_id"));
                EditorGUILayout.PropertyField(
                    serializedDefinition.FindProperty("_sceneName"));
            }

            SerializedProperty levelProperty =
                serializedDefinition.FindProperty("_level");
            EditorGUILayout.PropertyField(levelProperty);
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_weight"));
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_minimumDay"));

            if ((SituationLevel)levelProperty.enumValueIndex ==
                SituationLevel.Level2)
            {
                EditorGUILayout.PropertyField(
                    serializedDefinition.FindProperty("_usesTimeLimit"));
                if (serializedDefinition.FindProperty("_usesTimeLimit").boolValue)
                {
                    EditorGUILayout.PropertyField(
                        serializedDefinition.FindProperty("_timeLimitSeconds"));
                }

                EditorGUILayout.PropertyField(
                    serializedDefinition.FindProperty("_level2AllowedExits"),
                    true);
            }

            if (GUILayout.Button("Apply Definition Changes"))
            {
                Undo.RecordObject(_editDefinition, "Edit Situation Definition");
                serializedDefinition.ApplyModifiedProperties();
                EditorUtility.SetDirty(_editDefinition);
                AssetDatabase.SaveAssets();
                serializedDefinition.Update();
                SetMessage("Definition changes applied.", MessageType.Info);
            }

            EditorGUILayout.Space(8f);
            DrawExistingRegistration();

            string scenePath = SituationAuthoringUtility.FindScenePath(
                _editDefinition.SceneName);
            bool inBuild = !string.IsNullOrEmpty(scenePath) &&
                           SituationRegistrationService.IsInBuildSettings(scenePath);
            EditorGUILayout.LabelField(
                "Build Settings",
                inBuild ? "Registered" : "Missing or Disabled");
            if (!inBuild && !string.IsNullOrEmpty(scenePath) &&
                GUILayout.Button("Add to Build Settings"))
            {
                SituationRegistrationService.AddToBuildSettings(scenePath);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Situation Scene"))
                {
                    SituationRegistrationService.OpenSituationScene(
                        _editDefinition);
                }

                if (GUILayout.Button("Open with Home Layout"))
                {
                    if (!SituationRegistrationService.OpenWithHomeLayout(
                            _editDefinition))
                    {
                        SetMessage(
                            "Could not open the situation with its Home Layout.",
                            MessageType.Error);
                    }
                }
            }

            if (GUILayout.Button("Validate This Situation"))
            {
                _validationDefinition = _editDefinition;
                RunValidation();
                _tab = Tab.Validate;
            }
        }

        private void DrawExistingRegistration()
        {
            if (!_editRegistrationKnown)
            {
                RefreshEditRegistration();
            }

            string status = _editRegistered
                ? $"Registered ({_editRegistrationCount} entry)"
                : "Not registered (valid for test-only use)";
            EditorGUILayout.LabelField("Candidate", status);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (!_editRegistered && GUILayout.Button("Register Candidate"))
                {
                    SituationRegistrationService.RegisterCandidate(_editDefinition);
                    RefreshEditRegistration();
                }

                if (_editRegistered && GUILayout.Button("Unregister Candidate"))
                {
                    SituationRegistrationService.UnregisterCandidate(_editDefinition);
                    RefreshEditRegistration();
                }

                if (GUILayout.Button("Refresh Status"))
                {
                    RefreshEditRegistration();
                }
            }
        }

        private void DrawBuildingBlocks()
        {
            EditorGUILayout.LabelField("Building Blocks", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Adds common components to the active situation scene. It does " +
                "not create situation-specific success or failure logic.",
                MessageType.Info);

            if (_buildingController == null ||
                _buildingController.gameObject.scene !=
                UnityEngine.SceneManagement.SceneManager.GetActiveScene())
            {
                SituationBuildingBlockService.TryGetCurrentController(
                    out _buildingController);
            }

            _buildingController = (SituationController)EditorGUILayout.ObjectField(
                Required(
                    "Situation Controller",
                    "현재 씬에 있는 Situation Controller를 선택합니다."),
                _buildingController,
                typeof(SituationController),
                true);
            if (_buildingTarget == null && _buildingController != null)
            {
                _buildingTarget = _buildingController.gameObject;
            }

            _buildingTarget = (GameObject)EditorGUILayout.ObjectField(
                Required("Target Object", "컴포넌트를 추가할 대상 오브젝트입니다."),
                _buildingTarget,
                typeof(GameObject),
                true);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Scene Controller"))
                {
                    SituationBuildingBlockService.TryGetCurrentController(
                        out _buildingController);
                    _buildingTarget = _buildingController != null
                        ? _buildingController.gameObject
                        : null;
                }

                if (GUILayout.Button("Refresh ID Assets"))
                {
                    RefreshIdAssets();
                }
            }

            DrawIdSelection(
                "Module Object IDs",
                "Assets/02_Scripts/00_Core/Loop/ModuleRegistry/ModuleObjectIds",
                _availableModuleIds,
                _selectedModuleIds,
                ref _moduleIdScroll);
            using (new EditorGUI.DisabledScope(
                       _buildingController == null || _buildingTarget == null))
            {
                if (GUILayout.Button("Add Object Override"))
                {
                    RunBuildingBlockAction(() =>
                        SituationBuildingBlockService.AddComponent<
                            SituationObjectOverride>(
                            _buildingTarget,
                            _buildingController,
                            "_moduleObjectIds",
                            _selectedModuleIds.Cast<UnityEngine.Object>().ToList()));
                }
            }

            DrawIdSelection(
                "Door IDs",
                "Assets/02_Scripts/00_Core/Loop/ModuleRegistry/DoorIds",
                _availableDoorIds,
                _selectedDoorIds,
                ref _doorIdScroll);
            if (GUILayout.Button("Door ID 배치도 보기"))
            {
                DoorIdReferenceWindow.Open();
            }

            using (new EditorGUI.DisabledScope(
                       _buildingController == null || _buildingTarget == null))
            {
                if (GUILayout.Button("Add Door Lock Override"))
                {
                    RunBuildingBlockAction(() =>
                        SituationBuildingBlockService.AddComponent<
                            SituationDoorLockOverride>(
                            _buildingTarget,
                            _buildingController,
                            "_doorIds",
                            _selectedDoorIds.Cast<UnityEngine.Object>().ToList()));
                }

                if (GUILayout.Button("Add Trap Door Trigger"))
                {
                    RunBuildingBlockAction(() =>
                        SituationBuildingBlockService.AddComponent<
                            SituationTrapDoorTrigger>(
                            _buildingTarget,
                            _buildingController,
                            "_doorIds",
                            _selectedDoorIds.Cast<UnityEngine.Object>().ToList()));
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Prefab Palette", EditorStyles.boldLabel);
            _buildingPrefab = (GameObject)EditorGUILayout.ObjectField(
                Optional("Prefab", "대상 오브젝트 아래에 추가할 프리팹입니다."),
                _buildingPrefab,
                typeof(GameObject),
                false);
            using (new EditorGUI.DisabledScope(
                       _buildingPrefab == null || _buildingTarget == null))
            {
                if (GUILayout.Button("Add Selected Prefab"))
                {
                    RunBuildingBlockAction(() =>
                        SituationBuildingBlockService.AddPrefab(
                            _buildingPrefab,
                            _buildingTarget));
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawKnownPrefabButton("Add Small Fire", FirePrefabPath);
                DrawKnownPrefabButton("Add Extinguisher", ExtinguisherPrefabPath);
            }
        }

        private void DrawValidate()
        {
            EditorGUILayout.LabelField("Validate", EditorStyles.boldLabel);
            _validationDefinition = (SituationDefinition)EditorGUILayout.ObjectField(
                Required("Definition", "검증할 Situation Definition을 선택합니다."),
                _validationDefinition,
                typeof(SituationDefinition),
                false);

            if (GUILayout.Button("Validate Current Situation", GUILayout.Height(28f)))
            {
                RunValidation();
            }

            if (_validationResults.Count == 0)
            {
                return;
            }

            int errors = _validationResults.Count(result =>
                result.Severity == SituationValidationSeverity.Error);
            int warnings = _validationResults.Count(result =>
                result.Severity == SituationValidationSeverity.Warning);
            EditorGUILayout.HelpBox(
                errors == 0
                    ? $"Required validation passed. Warnings: {warnings}."
                    : $"Validation failed. Errors: {errors}, Warnings: {warnings}.",
                errors == 0 ? MessageType.Info : MessageType.Error);

            foreach (SituationValidationResult result in _validationResults)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        $"[{result.Severity}] {result.Message}",
                        EditorStyles.wordWrappedLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (result.Context != null && GUILayout.Button("Select"))
                        {
                            Selection.activeObject = result.Context;
                            EditorGUIUtility.PingObject(result.Context);
                        }

                        if (result.Fix != null && GUILayout.Button(result.FixLabel))
                        {
                            try
                            {
                                result.Fix();
                                RunValidation();
                                GUIUtility.ExitGUI();
                            }
                            catch (Exception exception)
                            {
                                SetMessage(exception.Message, MessageType.Error);
                            }
                        }
                    }
                }
            }
        }

        private SituationCreationRequest BuildRequest()
        {
            List<int> exits = new();
            if (_allowEmergencyStairs)
            {
                exits.Add((int)ExitType.EmergencyStairs);
            }

            if (_allowRefugeArea)
            {
                exits.Add((int)ExitType.RefugeArea);
            }

            if (_allowLightweightPartition)
            {
                exits.Add((int)ExitType.LightweightPartition);
            }

            if (_allowDescender)
            {
                exits.Add((int)ExitType.Descender);
            }

            SituationLocationEntry location = GetSelectedLocation();
            return new SituationCreationRequest
            {
                displayName = _displayName,
                situationId = _situationId,
                locationId = location?.Id ?? string.Empty,
                locationSceneFolder = location?.SceneFolderName ?? string.Empty,
                locationControllerFolder =
                    location?.ControllerFolderName ?? string.Empty,
                level = (int)_level,
                sceneName = _sceneName,
                controllerClassName = _controllerClassName,
                controllerNamespace = _controllerNamespace,
                weight = _weight,
                minimumDay = _minimumDay,
                registerAsCandidate = _registerAsCandidate,
                usesTimeLimit = _usesTimeLimit,
                timeLimitSeconds = _timeLimitSeconds,
                allowedExits = exits.ToArray(),
                initialPrefabPaths = ToAssetPaths(_initialPrefabs),
                moduleObjectIdPaths = ToAssetPaths(_newModuleIds),
                lockedDoorIdPaths = ToAssetPaths(_newLockedDoorIds),
                trapDoorIdPaths = ToAssetPaths(_newTrapDoorIds)
            };
        }

        private void RefreshEditRegistration()
        {
            _editRegistered = SituationRegistrationService.IsCandidateRegistered(
                _editDefinition,
                out _editRegistrationCount);
            _editRegistrationKnown = true;
        }

        private void RunValidation()
        {
            _validationResults.Clear();
            _validationResults.AddRange(
                SituationValidationService.Validate(_validationDefinition));
        }

        private void RefreshIdAssets()
        {
            LoadAssets(_availableModuleIds, "t:ModuleObjectId");
            LoadAssets(_availableDoorIds, "t:DoorId");
            _selectedModuleIds.RemoveWhere(id => !_availableModuleIds.Contains(id));
            _selectedDoorIds.RemoveWhere(id => !_availableDoorIds.Contains(id));
        }

        private void RefreshHomeLayout()
        {
            SituationRegistrationService.TryGetHomeLayout(out _homeLayout);
        }

        private void InitializeLocationCatalog()
        {
            if (this == null)
            {
                return;
            }

            _locationCatalog = SituationLocationCatalogService.GetOrCreate();
            EnsureSelectedLocation();
            Repaint();
        }

        private void EnsureSelectedLocation()
        {
            if (_locationCatalog == null || _locationCatalog.Locations.Count == 0)
            {
                _selectedLocationId = string.Empty;
                return;
            }

            SituationLocationEntry selected = GetSelectedLocation();
            if (selected == null)
            {
                selected = _locationCatalog.Locations[0];
                SelectLocation(selected);
            }
        }

        private SituationLocationEntry GetSelectedLocation()
        {
            return SituationLocationCatalogService.FindById(
                _locationCatalog,
                _selectedLocationId);
        }

        private void SelectLocation(SituationLocationEntry location)
        {
            if (location == null)
            {
                return;
            }

            _selectedLocationId = location.Id;
        }

        private void ClearAddLocationForm()
        {
            _showAddLocation = false;
            _newLocationId = string.Empty;
            _newLocationName = string.Empty;
        }

        private static void LoadAssets<T>(List<T> destination, string filter)
            where T : UnityEngine.Object
        {
            destination.Clear();
            foreach (string guid in AssetDatabase.FindAssets(
                         filter,
                         new[] { "Assets" }))
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    destination.Add(asset);
                }
            }

            destination.Sort((left, right) => string.Compare(
                left.name,
                right.name,
                StringComparison.OrdinalIgnoreCase));
        }

        private static void DrawIdSelection<T>(
            string label,
            string assetFolderPath,
            IReadOnlyList<T> available,
            ISet<T> selected,
            ref Vector2 scroll)
            where T : UnityEngine.Object
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(
                assetFolderPath,
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (available.Count == 0)
            {
                EditorGUILayout.HelpBox("No ID assets were found.", MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(
                scroll,
                EditorStyles.helpBox,
                GUILayout.MinHeight(70f),
                GUILayout.MaxHeight(130f));
            foreach (T asset in available)
            {
                bool wasSelected = selected.Contains(asset);
                bool isSelected = EditorGUILayout.ToggleLeft(
                    new GUIContent(asset.name, AssetDatabase.GetAssetPath(asset)),
                    wasSelected);
                if (isSelected == wasSelected)
                {
                    continue;
                }

                if (isSelected)
                {
                    selected.Add(asset);
                }
                else
                {
                    selected.Remove(asset);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawAssetList<T>(string label, List<T> assets)
            where T : UnityEngine.Object
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            for (int index = 0; index < assets.Count; index++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    assets[index] = (T)EditorGUILayout.ObjectField(
                        assets[index],
                        typeof(T),
                        false);
                    if (GUILayout.Button("-", GUILayout.Width(24f)))
                    {
                        assets.RemoveAt(index);
                        index--;
                    }
                }
            }

            if (GUILayout.Button($"Add {typeof(T).Name}", GUILayout.Width(180f)))
            {
                assets.Add(null);
            }
        }

        private void DrawKnownPrefabButton(string label, string assetPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            using (new EditorGUI.DisabledScope(
                       prefab == null || _buildingTarget == null))
            {
                if (GUILayout.Button(label))
                {
                    RunBuildingBlockAction(() =>
                        SituationBuildingBlockService.AddPrefab(
                            prefab,
                            _buildingTarget));
                }
            }
        }

        private void RunBuildingBlockAction(Action action)
        {
            try
            {
                action();
                SetMessage("Building block added.", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetMessage(exception.Message, MessageType.Error);
            }
        }

        private void SetMessage(string message, MessageType messageType)
        {
            _lastMessage = message;
            _lastMessageType = messageType;
            Repaint();
        }

        private static string[] ToAssetPaths<T>(IEnumerable<T> assets)
            where T : UnityEngine.Object
        {
            return assets
                .Where(asset => asset != null)
                .Select(AssetDatabase.GetAssetPath)
                .Distinct()
                .ToArray();
        }

        private static GUIContent Required(string label, string tooltip)
        {
            return new GUIContent($"{label} * [Required]", tooltip);
        }

        private static GUIContent Conditional(string label, string tooltip)
        {
            return new GUIContent($"{label} [Conditional]", tooltip);
        }

        private static GUIContent Optional(string label, string tooltip)
        {
            return new GUIContent($"{label} [Optional]", tooltip);
        }
    }
}
