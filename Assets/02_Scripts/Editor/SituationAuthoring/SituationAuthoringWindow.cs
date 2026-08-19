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
        private const string ReadmePath = "Docs/README-SituationAuthoring.md";
        private const string LanguagePrefsKey =
            "VirtualRescue.SituationAuthoring.Language";

        private enum Tab
        {
            NewSituation,
            EditExisting,
            BuildingBlocks,
            Validate
        }

        private enum Language
        {
            English,
            Korean
        }

        private static Language _language = Language.English;

        [SerializeField] private Tab _tab;
        [SerializeField] private Vector2 _scrollPosition;

        [Header("New Situation")]
        [SerializeField] private string _displayName = "Location_Situation";
        [SerializeField] private string _situationId = "location.situation";
        [SerializeField] private string _selectedLocationId = "room";
        [SerializeField] private RoomLocation _roomLocation = RoomLocation.None;
        [SerializeField] private SituationLevel _level = SituationLevel.Level0;
        [SerializeField] private string _sceneName = "Scenario_Room_NewSituation";
        [SerializeField] private string _controllerClassName =
            "NewSituationController";
        [SerializeField] private string _controllerNamespace =
            "VirtualRescue.Situations";
        [SerializeField] private int _weight = 1;
        [SerializeField] private int _minimumDay = 1;
        [SerializeField] private string _resolvedDialogueId = string.Empty;
        [SerializeField] private string _failedDialogueId = string.Empty;
        [SerializeField] private string _beforeResolveCallingDialogueGroupId =
            string.Empty;
        [SerializeField] private string _afterResolveCallingDialogueGroupId =
            string.Empty;
        [SerializeField] private string _level2CallingDialogueGroupId =
            string.Empty;
        [SerializeField] private string _resolvedDialogueText = string.Empty;
        [SerializeField] private string _failedDialogueText = string.Empty;
        [SerializeField] private bool _registerAsCandidate;
        [SerializeField] private bool _usesTimeLimit;
        [SerializeField] private float _timeLimitSeconds = 60f;
        [SerializeField] private bool _allowElevator;
        [SerializeField] private bool _allowCellPhone;
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
        [SerializeField] private string _editResolvedDialogueText = string.Empty;
        [SerializeField] private string _editFailedDialogueText = string.Empty;

        [Header("New Location")]
        [SerializeField] private bool _showAddLocation;
        [SerializeField] private string _newLocationId = string.Empty;
        [SerializeField] private string _newLocationName = string.Empty;

        [Header("Building Blocks")]
        [SerializeField] private SituationDefinition _buildingDefinition;
        [SerializeField] private SituationController _buildingController;
        [SerializeField] private GameObject _buildingTarget;
        [SerializeField] private GameObject _buildingPrefab;
        [SerializeField] private SituationDefinition _homeModuleParentDefinition;
        [SerializeField] private string _homeModuleParentName = string.Empty;
        [SerializeField] private List<string> _selectedHomeModuleSceneNames = new();

        private readonly List<SituationValidationResult> _validationResults = new();
        private readonly List<ModuleObjectId> _availableModuleIds = new();
        private readonly List<DoorId> _availableDoorIds = new();
        private readonly HashSet<ModuleObjectId> _selectedModuleIds = new();
        private readonly HashSet<DoorId> _selectedDoorIds = new();
        private Vector2 _moduleIdScroll;
        private Vector2 _doorIdScroll;
        private Vector2 _homeModuleSceneScroll;
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
            _language = (Language)EditorPrefs.GetInt(
                LanguagePrefsKey,
                (int)Language.English);

            if (_displayName == "New Situation")
            {
                _displayName = "Location_Situation";
            }

            if (_situationId == "new.situation")
            {
                _situationId = "location.situation";
            }

            ApplyDerivedNames();

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
            titleContent = new GUIContent(Tr("Situation Authoring"));
            DrawPendingCreation();

            using (new EditorGUILayout.HorizontalScope())
            {
                _tab = (Tab)GUILayout.Toolbar(
                    (int)_tab,
                    new[]
                    {
                        Tr("New"),
                        Tr("Edit Existing"),
                        Tr("Building Blocks"),
                        Tr("Validate")
                    });
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(
                        new GUIContent(
                            _language == Language.English ? "한국어" : "English",
                            Tr("Switch Language")),
                        GUILayout.Width(70f)))
                {
                    ToggleLanguage();
                }

                if (GUILayout.Button(
                        new GUIContent(
                            "?",
                            Tr("Open Situation Authoring guide")),
                        GUILayout.Width(26f)))
                {
                    OpenReadme();
                }
            }

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

        private static void OpenReadme()
        {
            string absolutePath =
                SituationAuthoringUtility.ToAbsolutePath(ReadmePath);
            if (!System.IO.File.Exists(absolutePath))
            {
                EditorUtility.DisplayDialog(
                    "사용 설명서를 찾을 수 없음",
                    $"다음 경로에 문서가 없습니다.\n{absolutePath}",
                    "확인");
                return;
            }

            EditorUtility.OpenWithDefaultApp(absolutePath);
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
                if (GUILayout.Button(Tr("Resume")))
                {
                    SituationCreationResumeHandler.TryResume();
                }

                GUI.enabled = true;
                if (GUILayout.Button(Tr("Cancel Pending Request")))
                {
                    SituationCreationResumeHandler.Cancel();
                }
            }

            EditorGUILayout.Space(6f);
        }

        private void DrawNewSituation()
        {
            EditorGUILayout.LabelField(Tr("New Situation"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                Tr("Creates the Controller script first, then resumes scene and Definition creation after Unity compiles."),
                MessageType.Info);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(Tr("Name"), EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _displayName = EditorGUILayout.TextField(Required(
                "Situation Definition Name",
                "Hierarchy에 표시되며 Definition 파일명에 사용되는 이름입니다."),
                _displayName);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyDerivedNames();
            }

            using (new EditorGUI.DisabledScope(true))
            {
                _sceneName = EditorGUILayout.TextField(Required(
                    "Scene Name",
                    "Situation Definition Name에서 자동 생성됩니다."),
                    _sceneName);
                _controllerClassName = EditorGUILayout.TextField(Required(
                    "Controller Class Name",
                    "Situation Definition Name에서 자동 생성됩니다."),
                    _controllerClassName);
            }

            _controllerNamespace = EditorGUILayout.TextField(Required(
                "Controller Namespace",
                "생성할 Controller 클래스의 C# 네임스페이스입니다."),
                _controllerNamespace);
            using (new EditorGUI.DisabledScope(true))
            {
                _situationId = EditorGUILayout.TextField(Required(
                    "Situation ID",
                    "Situation Definition Name에서 자동 생성됩니다."),
                    _situationId);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                Tr("Level & Location"),
                EditorStyles.boldLabel);
            _level = (SituationLevel)EditorGUILayout.EnumPopup(
                Required("Level", "출구와 제한시간 규칙에 사용할 상황 단계를 선택합니다."),
                _level);
            DrawLocationSelector();
            _roomLocation = (RoomLocation)EditorGUILayout.EnumPopup(
                Required(
                    "Room Trigger",
                    "상황 입장 대사를 출력할 RoomTrigger 위치입니다."),
                _roomLocation);
            DrawRoomLocationWarning();
            DrawSelectedLocationFolders();
            DrawLocationManagement();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(Tr("Percentage"), EditorStyles.boldLabel);
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

            DrawAllowedExits();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(Tr("Optional"), EditorStyles.boldLabel);
            _resolvedDialogueId = EditorGUILayout.TextField(Optional(
                "Resolved Dialogue Id",
                "상황 해결 시 재생할 Dialogue ID입니다. 비워두면 재생하지 않습니다."),
                _resolvedDialogueId);
            _resolvedDialogueText = EditorGUILayout.TextField(Optional(
                "Resolved Dialogue Text",
                "Resolved Dialogue ID로 Loop Text CSV에 추가할 대사 본문입니다."),
                _resolvedDialogueText);
            _failedDialogueId = EditorGUILayout.TextField(Optional(
                "Failed Dialogue Id",
                "상황 실패 시 재생할 Dialogue ID입니다. 비워두면 재생하지 않습니다."),
                _failedDialogueId);
            _failedDialogueText = EditorGUILayout.TextField(Optional(
                "Failed Dialogue Text",
                "Failed Dialogue ID로 Loop Text CSV에 추가할 대사 본문입니다."),
                _failedDialogueText);
            if (ShouldShowPhoneCallFields(_level, _allowCellPhone))
            {
                DrawPhoneCallDialogueIds();
            }

            if (GUILayout.Button(Tr("Append Missing Dialogue Rows")))
            {
                AppendMissingDialogueRows(
                    _resolvedDialogueId,
                    _resolvedDialogueText,
                    _failedDialogueId,
                    _failedDialogueText);
            }

            _registerAsCandidate = EditorGUILayout.Toggle(Optional(
                "Register as Candidate",
                "활성화하면 생성한 Definition을 LoopBase Candidates에 추가합니다. " +
                "테스트용 상황 생성을 위해 기본값은 꺼져 있습니다."),
                _registerAsCandidate);

            DrawLevel2Rules();
            DrawAssetList("Initial Prefabs", _initialPrefabs, true);
            DrawAssetList("Module Object IDs", _newModuleIds, true);
            DrawAssetList("Locked Door IDs", _newLockedDoorIds, true);
            DrawAssetList("Trap Door IDs", _newTrapDoorIds, true);
            if (GUILayout.Button(Tr("View Door ID Layout")))
            {
                DoorIdReferenceWindow.Open();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(Tr("Planned Assets"), EditorStyles.boldLabel);
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
                if (GUILayout.Button(
                        Tr("Create Situation"),
                        GUILayout.Height(32f)))
                {
                    if (SituationControllerScriptGenerator.TryBegin(
                            previewRequest,
                            out string creationError))
                    {
                        SetMessage(
                            Tr("Controller script created. Unity will compile and resume the remaining work."),
                            MessageType.Info);
                    }
                    else
                    {
                        SetMessage(creationError, MessageType.Error);
                    }
                }
            }
        }

        private void DrawAllowedExits()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(Tr("Allowed Exits"), EditorStyles.boldLabel);
            _allowElevator = EditorGUILayout.ToggleLeft(
                Tr("Elevator"),
                _allowElevator);
            _allowCellPhone = EditorGUILayout.ToggleLeft(
                Tr("CellPhone"),
                _allowCellPhone);
            _allowEmergencyStairs = EditorGUILayout.ToggleLeft(
                Tr("Emergency Stairs"),
                _allowEmergencyStairs);
            _allowRefugeArea = EditorGUILayout.ToggleLeft(
                Tr("Refuge Area"),
                _allowRefugeArea);
            _allowLightweightPartition = EditorGUILayout.ToggleLeft(
                Tr("Lightweight Partition"),
                _allowLightweightPartition);
            _allowDescender = EditorGUILayout.ToggleLeft(
                Tr("Descender"),
                _allowDescender);
        }

        private void DrawPhoneCallDialogueIds()
        {
            _beforeResolveCallingDialogueGroupId =
                EditorGUILayout.TextField(Optional(
                    "Before Resolve Calling Dialogue Group ID",
                    "상황 발견 후 해결 전에 전화했을 때 재생할 Dialogue Group ID입니다. Group row는 Dialogue CSV에서 별도로 구성합니다."),
                    _beforeResolveCallingDialogueGroupId);
            _afterResolveCallingDialogueGroupId =
                EditorGUILayout.TextField(Optional(
                    "After Resolve Calling Dialogue Group ID",
                    "상황 해결 후 전화했을 때 재생할 Dialogue Group ID입니다. Group row는 Dialogue CSV에서 별도로 구성합니다."),
                    _afterResolveCallingDialogueGroupId);
            _level2CallingDialogueGroupId = EditorGUILayout.TextField(Optional(
                "Level 2 Calling Dialogue Group ID",
                "Level 2 상황 발견 후 전화했을 때 재생할 Dialogue Group ID입니다. Group row는 Dialogue CSV에서 별도로 구성합니다."),
                _level2CallingDialogueGroupId);
        }

        private void DrawPhoneCallDialogueProperties(
            SerializedObject serializedDefinition,
            SituationLevel level,
            bool hasCellPhoneExit)
        {
            if (!ShouldShowPhoneCallFields(level, hasCellPhoneExit))
            {
                return;
            }

            DrawPhoneCallDialogueProperties(serializedDefinition);
        }

        private static void DrawPhoneCallDialogueProperties(
            SerializedObject serializedDefinition)
        {
            EditorGUILayout.PropertyField(serializedDefinition.FindProperty(
                "_beforeResolveCallingDialogueGroupId"));
            EditorGUILayout.PropertyField(serializedDefinition.FindProperty(
                "_afterResolveCallingDialogueGroupId"));
            EditorGUILayout.PropertyField(serializedDefinition.FindProperty(
                "_level2CallingDialogueGroupId"));
        }

        private void DrawLevel2Rules()
        {
            if (_level != SituationLevel.Level2)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(Tr("Level 2 Rules"), EditorStyles.boldLabel);
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
        }

        private void DrawLocationSelector()
        {
            if (_locationCatalog == null || _locationCatalog.Locations.Count == 0)
            {
                EditorGUILayout.LabelField(Required(
                    "Location",
                    "씬과 Controller 저장 폴더를 결정하는 Location 항목입니다."));
                EditorGUILayout.HelpBox(
                    Tr("Situation Location Catalog is missing or empty."),
                    MessageType.Error);
                if (GUILayout.Button(Tr("Create or Reload Catalog")))
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

            int newIndex = EditorGUILayout.Popup(
                Required(
                    "Location",
                    "씬과 Controller 저장 폴더를 결정하는 Location 항목입니다."),
                selectedIndex,
                displayNames);
            if (newIndex != selectedIndex ||
                string.IsNullOrWhiteSpace(_selectedLocationId))
            {
                SelectLocation(_locationCatalog.Locations[newIndex]);
            }

        }

        private void DrawSelectedLocationFolders()
        {
            SituationLocationEntry selected = GetSelectedLocation();
            if (selected == null)
            {
                return;
            }

            EditorGUILayout.LabelField(
                Tr("Scene Folder"),
                SituationLocationPathMap.GetSceneFolder(
                    selected.SceneFolderName,
                    _level));
            EditorGUILayout.LabelField(
                Tr("Controller Folder"),
                SituationLocationPathMap.GetControllerFolder(
                    selected.ControllerFolderName,
                    _level));
        }

        private void DrawLocationManagement()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Tr("Add New Location")))
                {
                    _showAddLocation = !_showAddLocation;
                }

                if (GUILayout.Button(Tr("Select Catalog Asset")))
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
                    Tr("Add New Location"),
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
                    if (GUILayout.Button(Tr("Save Location")))
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
                                string.Format(
                                    Tr("Location '{0}' was added."),
                                    entry.DisplayName),
                                MessageType.Info);
                        }
                        else
                        {
                            SetMessage(error, MessageType.Error);
                            EditorUtility.DisplayDialog(
                                Tr("Could Not Save Location"),
                                error,
                                Tr("OK"));
                        }
                    }

                    if (GUILayout.Button(Tr("Cancel")))
                    {
                        ClearAddLocationForm();
                    }
                }
            }
        }

        private void DrawEditExisting()
        {
            EditorGUILayout.LabelField(Tr("Edit Existing"), EditorStyles.boldLabel);
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
                    Tr("Select a SituationDefinition."),
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
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_roomTrigger"));
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_resolvedDialogueId"));
            _editResolvedDialogueText = EditorGUILayout.TextField(Optional(
                "Resolved Dialogue Text",
                "Resolved Dialogue ID로 Loop Text CSV에 추가할 대사 본문입니다."),
                _editResolvedDialogueText);
            EditorGUILayout.PropertyField(
                serializedDefinition.FindProperty("_failedDialogueId"));
            _editFailedDialogueText = EditorGUILayout.TextField(Optional(
                "Failed Dialogue Text",
                "Failed Dialogue ID로 Loop Text CSV에 추가할 대사 본문입니다."),
                _editFailedDialogueText);

            SerializedProperty allowedExitsProperty =
                serializedDefinition.FindProperty("_allowedExits");
            DrawPhoneCallDialogueProperties(
                serializedDefinition,
                (SituationLevel)levelProperty.enumValueIndex,
                ContainsExit(allowedExitsProperty, ExitType.CellPhone));
            if (GUILayout.Button(Tr("Append Missing Dialogue Rows")))
            {
                AppendMissingDialogueRows(
                    serializedDefinition.FindProperty("_resolvedDialogueId")
                        .stringValue,
                    _editResolvedDialogueText,
                    serializedDefinition.FindProperty("_failedDialogueId")
                        .stringValue,
                    _editFailedDialogueText);
            }

            EditorGUILayout.PropertyField(
                allowedExitsProperty,
                true);

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
            }

            if (GUILayout.Button(Tr("Apply Definition Changes")))
            {
                Undo.RecordObject(_editDefinition, "Edit Situation Definition");
                serializedDefinition.ApplyModifiedProperties();
                EditorUtility.SetDirty(_editDefinition);
                AssetDatabase.SaveAssets();
                serializedDefinition.Update();
                SetMessage(Tr("Definition changes applied."), MessageType.Info);
            }

            EditorGUILayout.Space(8f);
            DrawExistingRegistration();

            string scenePath = SituationAuthoringUtility.FindScenePath(
                _editDefinition.SceneName);
            bool inBuild = !string.IsNullOrEmpty(scenePath) &&
                           SituationRegistrationService.IsInBuildSettings(scenePath);
            EditorGUILayout.LabelField(
                Tr("Build Settings"),
                inBuild ? Tr("Registered") : Tr("Missing or Disabled"));
            if (!inBuild && !string.IsNullOrEmpty(scenePath) &&
                GUILayout.Button(Tr("Add to Build Settings")))
            {
                SituationRegistrationService.AddToBuildSettings(scenePath);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Tr("Open Situation Scene")))
                {
                    SituationRegistrationService.OpenSituationScene(
                        _editDefinition);
                }

                if (GUILayout.Button(Tr("Open with Home Layout")))
                {
                    if (!SituationRegistrationService.OpenWithHomeLayout(
                            _editDefinition))
                    {
                        SetMessage(
                            Tr("Could not open the situation with its Home Layout."),
                            MessageType.Error);
                    }
                }
            }

            if (GUILayout.Button(Tr("Validate This Situation")))
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
                ? string.Format(Tr("Registered ({0} entry)"), _editRegistrationCount)
                : Tr("Not registered (valid for test-only use)");
            EditorGUILayout.LabelField(Tr("Candidate"), status);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (!_editRegistered && GUILayout.Button(Tr("Register Candidate")))
                {
                    SituationRegistrationService.RegisterCandidate(_editDefinition);
                    RefreshEditRegistration();
                }

                if (_editRegistered && GUILayout.Button(Tr("Unregister Candidate")))
                {
                    SituationRegistrationService.UnregisterCandidate(_editDefinition);
                    RefreshEditRegistration();
                }

                if (GUILayout.Button(Tr("Refresh Status")))
                {
                    RefreshEditRegistration();
                }
            }
        }

        private void DrawBuildingBlocks()
        {
            EditorGUILayout.LabelField(Tr("Building Blocks"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                Tr("Adds common components to the active situation scene. It does not create situation-specific success or failure logic."),
                MessageType.Info);

            DrawBuildingBlockPhoneCallDialogue();

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
                if (GUILayout.Button(Tr("Refresh Scene Controller")))
                {
                    SituationBuildingBlockService.TryGetCurrentController(
                        out _buildingController);
                    _buildingTarget = _buildingController != null
                        ? _buildingController.gameObject
                        : null;
                }

                if (GUILayout.Button(Tr("Refresh ID Assets")))
                {
                    RefreshIdAssets();
                }
            }

            DrawIdSelection(
                Tr("Module Object IDs"),
                "Assets/02_Scripts/00_Core/Loop/ModuleRegistry/ModuleObjectIds",
                _availableModuleIds,
                _selectedModuleIds,
                ref _moduleIdScroll);
            using (new EditorGUI.DisabledScope(
                       _buildingController == null || _buildingTarget == null))
            {
                if (GUILayout.Button(Tr("Add Object Override")))
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
                Tr("Door IDs"),
                "Assets/02_Scripts/00_Core/Loop/ModuleRegistry/DoorIds",
                _availableDoorIds,
                _selectedDoorIds,
                ref _doorIdScroll);
            if (GUILayout.Button(Tr("View Door ID Layout")))
            {
                DoorIdReferenceWindow.Open();
            }

            using (new EditorGUI.DisabledScope(
                       _buildingController == null || _buildingTarget == null))
            {
                if (GUILayout.Button(Tr("Add Door Lock Override")))
                {
                    RunBuildingBlockAction(() =>
                        SituationBuildingBlockService.AddComponent<
                            SituationDoorLockOverride>(
                            _buildingTarget,
                            _buildingController,
                            "_doorIds",
                            _selectedDoorIds.Cast<UnityEngine.Object>().ToList()));
                }

                if (GUILayout.Button(Tr("Add Trap Door Trigger")))
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
            EditorGUILayout.LabelField(Tr("Prefab Palette"), EditorStyles.boldLabel);
            _buildingPrefab = (GameObject)EditorGUILayout.ObjectField(
                Optional("Prefab", "대상 오브젝트 아래에 추가할 프리팹입니다."),
                _buildingPrefab,
                typeof(GameObject),
                false);
            using (new EditorGUI.DisabledScope(
                       _buildingPrefab == null || _buildingTarget == null))
            {
                if (GUILayout.Button(Tr("Add Selected Prefab")))
                {
                    RunBuildingBlockAction(() =>
                        SituationBuildingBlockService.AddPrefab(
                            _buildingPrefab,
                            _buildingTarget));
                }
            }

            DrawHomeModuleParents();
        }

        private void DrawBuildingBlockPhoneCallDialogue()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                Tr("Phone Call Dialogue"),
                EditorStyles.boldLabel);
            _buildingDefinition =
                (SituationDefinition)EditorGUILayout.ObjectField(
                    Optional(
                        "Situation Definition",
                        "통화 Dialogue Group ID를 편집할 Situation Definition입니다."),
                    _buildingDefinition,
                    typeof(SituationDefinition),
                    false);

            if (_buildingDefinition == null)
            {
                return;
            }

            SerializedObject serializedDefinition =
                new(_buildingDefinition);
            SerializedProperty levelProperty =
                serializedDefinition.FindProperty("_level");
            SerializedProperty allowedExitsProperty =
                serializedDefinition.FindProperty("_allowedExits");
            SituationLevel level =
                (SituationLevel)levelProperty.enumValueIndex;

            if (!ShouldShowPhoneCallFields(
                    level,
                    ContainsExit(allowedExitsProperty, ExitType.CellPhone)))
            {
                EditorGUILayout.HelpBox(
                    Tr("CellPhone is not an allowed exit for this definition."),
                    MessageType.Info);
                return;
            }

            DrawPhoneCallDialogueProperties(serializedDefinition);

            if (GUILayout.Button(Tr("Apply Definition Changes")))
            {
                Undo.RecordObject(
                    _buildingDefinition,
                    "Edit Situation Phone Call Dialogue");
                serializedDefinition.ApplyModifiedProperties();
                EditorUtility.SetDirty(_buildingDefinition);
                AssetDatabase.SaveAssets();
                SetMessage(Tr("Definition changes applied."), MessageType.Info);
            }
        }

        private void DrawHomeModuleParents()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                Tr("Home Module Parents"),
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                Tr("Creates empty root parent objects in selected Home Layout module scenes for situation-specific staging."),
                MessageType.Info);

            if (_homeLayout == null)
            {
                RefreshHomeLayout();
            }

            _homeModuleParentDefinition =
                (SituationDefinition)EditorGUILayout.ObjectField(
                    Optional(
                        "Situation Definition",
                        "부모 오브젝트 이름 추천에 사용할 Situation Definition입니다."),
                    _homeModuleParentDefinition,
                    typeof(SituationDefinition),
                    false);

            using (new EditorGUILayout.HorizontalScope())
            {
                _homeModuleParentName = EditorGUILayout.TextField(
                    Required(
                        "Parent Object Name",
                        "Home module 씬 루트에 생성할 빈 부모 오브젝트 이름입니다."),
                    _homeModuleParentName);
                if (GUILayout.Button(Tr("Suggest"), GUILayout.Width(90f)))
                {
                    _homeModuleParentName = SuggestHomeModuleParentName();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Tr("Refresh Home Layout")))
                {
                    RefreshHomeLayout();
                    SyncSelectedHomeModuleSceneNames();
                }

                using (new EditorGUI.DisabledScope(
                           _homeLayout == null ||
                           _homeLayout.ModuleSceneNames.Count == 0))
                {
                    if (GUILayout.Button(Tr("Select All")))
                    {
                        _selectedHomeModuleSceneNames =
                            _homeLayout.ModuleSceneNames.ToList();
                    }

                    if (GUILayout.Button(Tr("Clear Selection")))
                    {
                        _selectedHomeModuleSceneNames.Clear();
                    }
                }
            }

            if (_homeLayout == null || _homeLayout.ModuleSceneNames.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    Tr("Home Layout is missing or has no module scenes."),
                    MessageType.Warning);
                return;
            }

            SyncSelectedHomeModuleSceneNames();
            DrawHomeModuleSceneSelection();

            bool canCreate =
                !string.IsNullOrWhiteSpace(_homeModuleParentName) &&
                _selectedHomeModuleSceneNames.Count > 0;
            using (new EditorGUI.DisabledScope(!canCreate))
            {
                if (GUILayout.Button(
                        Tr("Create Parent In Selected Scenes"),
                        GUILayout.Height(28f)))
                {
                    RunHomeModuleParentCreation();
                }
            }
        }

        private void DrawValidate()
        {
            EditorGUILayout.LabelField(Tr("Validate"), EditorStyles.boldLabel);
            _validationDefinition = (SituationDefinition)EditorGUILayout.ObjectField(
                Required("Definition", "검증할 Situation Definition을 선택합니다."),
                _validationDefinition,
                typeof(SituationDefinition),
                false);

            if (GUILayout.Button(
                    Tr("Validate Current Situation"),
                    GUILayout.Height(28f)))
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
                    ? string.Format(
                        Tr("Required validation passed. Warnings: {0}."),
                        warnings)
                    : string.Format(
                        Tr("Validation failed. Errors: {0}, Warnings: {1}."),
                        errors,
                        warnings),
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
                        if (result.Context != null && GUILayout.Button(Tr("Select")))
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

        private void DrawHomeModuleSceneSelection()
        {
            EditorGUILayout.LabelField(
                Tr("Home Layout Module Scenes"),
                EditorStyles.boldLabel);
            _homeModuleSceneScroll = EditorGUILayout.BeginScrollView(
                _homeModuleSceneScroll,
                EditorStyles.helpBox,
                GUILayout.MinHeight(90f),
                GUILayout.MaxHeight(160f));
            foreach (string sceneName in _homeLayout.ModuleSceneNames)
            {
                bool wasSelected = _selectedHomeModuleSceneNames.Contains(sceneName);
                string scenePath = SituationAuthoringUtility.FindScenePath(sceneName);
                bool sceneExists = !string.IsNullOrEmpty(scenePath);
                using (new EditorGUI.DisabledScope(!sceneExists))
                {
                    bool isSelected = EditorGUILayout.ToggleLeft(
                        new GUIContent(
                            sceneName,
                            sceneExists ? scenePath : Tr("Scene asset was not found.")),
                        wasSelected);
                    if (isSelected == wasSelected)
                    {
                        continue;
                    }

                    if (isSelected)
                    {
                        _selectedHomeModuleSceneNames.Add(sceneName);
                    }
                    else
                    {
                        _selectedHomeModuleSceneNames.Remove(sceneName);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void RunHomeModuleParentCreation()
        {
            try
            {
                SituationHomeModuleParentService.Result result =
                    SituationHomeModuleParentService.CreateParents(
                        _homeModuleParentName,
                        _selectedHomeModuleSceneNames);
                SetMessage(FormatHomeModuleParentResult(result), MessageType.Info);
            }
            catch (OperationCanceledException exception)
            {
                SetMessage(Tr(exception.Message), MessageType.Warning);
            }
            catch (Exception exception)
            {
                SetMessage(Tr(exception.Message), MessageType.Error);
            }
        }

        private string FormatHomeModuleParentResult(
            SituationHomeModuleParentService.Result result)
        {
            List<string> parts = new()
            {
                string.Format(
                    Tr("Created {0} parent object(s)."),
                    result.CreatedCount)
            };

            if (result.ExistingCount > 0)
            {
                parts.Add(string.Format(
                    Tr("Skipped {0} scene(s) where the parent already exists."),
                    result.ExistingCount));
            }

            if (result.MissingSceneNames.Count > 0)
            {
                parts.Add(string.Format(
                    Tr("Missing scenes: {0}"),
                    string.Join(", ", result.MissingSceneNames)));
            }

            return string.Join(" ", parts);
        }

        private void SyncSelectedHomeModuleSceneNames()
        {
            if (_homeLayout == null)
            {
                _selectedHomeModuleSceneNames.Clear();
                return;
            }

            HashSet<string> available = new(_homeLayout.ModuleSceneNames);
            _selectedHomeModuleSceneNames.RemoveAll(sceneName =>
                !available.Contains(sceneName));
        }

        private string SuggestHomeModuleParentName()
        {
            string sceneName = _homeModuleParentDefinition != null
                ? _homeModuleParentDefinition.SceneName
                : _sceneName;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return _homeModuleParentName;
            }

            const string prefix = "Scenario_";
            string normalized = sceneName.Trim();
            if (normalized.StartsWith(prefix, StringComparison.Ordinal))
            {
                normalized = normalized.Substring(prefix.Length);
            }

            return normalized.EndsWith("_Common", StringComparison.Ordinal)
                ? normalized
                : $"{normalized}_Common";
        }

        private void ApplyDerivedNames()
        {
            string definitionName = SanitizeSituationDefinitionName(_displayName);
            _sceneName = string.IsNullOrEmpty(definitionName)
                ? string.Empty
                : $"Scenario_{definitionName}";
            _controllerClassName = string.IsNullOrEmpty(definitionName)
                ? string.Empty
                : $"{ToPascalIdentifier(definitionName)}SituationController";
            _situationId = ToSituationId(definitionName);
        }

        private static string SanitizeSituationDefinitionName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string trimmed = value.Trim();
            foreach (char invalidCharacter in
                     System.IO.Path.GetInvalidFileNameChars())
            {
                trimmed = trimmed.Replace(invalidCharacter, '_');
            }

            return trimmed.Replace(' ', '_');
        }

        private static string ToPascalIdentifier(string value)
        {
            string[] tokens = SplitSituationName(value);
            if (tokens.Length == 0)
            {
                return string.Empty;
            }

            string result = string.Concat(tokens.Select(ToIdentifierToken));
            if (string.IsNullOrEmpty(result))
            {
                return string.Empty;
            }

            return char.IsDigit(result[0])
                ? $"_{result}"
                : result;
        }

        private static string ToIdentifierToken(string token)
        {
            string cleaned = new(token
                .Where(char.IsLetterOrDigit)
                .ToArray());
            if (string.IsNullOrEmpty(cleaned))
            {
                return string.Empty;
            }

            return char.ToUpperInvariant(cleaned[0]) +
                   (cleaned.Length > 1 ? cleaned.Substring(1) : string.Empty);
        }

        private static string ToSituationId(string value)
        {
            string[] tokens = SplitSituationName(value)
                .Select(token => new string(token
                    .Where(char.IsLetterOrDigit)
                    .Select(char.ToLowerInvariant)
                    .ToArray()))
                .Where(token => !string.IsNullOrEmpty(token))
                .ToArray();
            return string.Join(".", tokens);
        }

        private static string[] SplitSituationName(string value)
        {
            return (value ?? string.Empty)
                .Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private SituationCreationRequest BuildRequest()
        {
            List<int> exits = new();
            if (_allowElevator)
            {
                exits.Add((int)ExitType.Elevator);
            }

            if (_allowCellPhone)
            {
                exits.Add((int)ExitType.CellPhone);
            }

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
                roomLocation = (int)_roomLocation,
                level = (int)_level,
                sceneName = _sceneName,
                controllerClassName = _controllerClassName,
                controllerNamespace = _controllerNamespace,
                weight = _weight,
                minimumDay = _minimumDay,
                resolvedDialogueId = _resolvedDialogueId,
                failedDialogueId = _failedDialogueId,
                beforeResolveCallingDialogueGroupId =
                    _beforeResolveCallingDialogueGroupId,
                afterResolveCallingDialogueGroupId =
                    _afterResolveCallingDialogueGroupId,
                level2CallingDialogueGroupId = _level2CallingDialogueGroupId,
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

        private static bool ShouldShowPhoneCallFields(
            SituationLevel level,
            bool hasCellPhoneExit)
        {
            return hasCellPhoneExit ||
                   level == SituationLevel.Level1 ||
                   level == SituationLevel.Level2;
        }

        private static bool ContainsExit(
            SerializedProperty exitsProperty,
            ExitType exitType)
        {
            if (exitsProperty == null || !exitsProperty.isArray)
            {
                return false;
            }

            for (int index = 0; index < exitsProperty.arraySize; index++)
            {
                if (exitsProperty.GetArrayElementAtIndex(index).enumValueIndex ==
                    (int)exitType)
                {
                    return true;
                }
            }

            return false;
        }

        private void AppendMissingDialogueRows(
            string resolvedDialogueId,
            string resolvedDialogueText,
            string failedDialogueId,
            string failedDialogueText)
        {
            SituationDialogueCsvEntry[] entries =
            {
                new(resolvedDialogueId, resolvedDialogueText),
                new(failedDialogueId, failedDialogueText)
            };

            if (SituationDialogueCsvService.TryAppendMissingRows(
                    entries,
                    out string message))
            {
                SetMessage(Tr(message), MessageType.Info);
            }
            else
            {
                SetMessage(Tr(message), MessageType.Error);
            }
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
            else if (_roomLocation == RoomLocation.None)
            {
                ApplyDefaultRoomLocation(selected, false);
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
            ApplyDefaultRoomLocation(location, true);
        }

        private void DrawRoomLocationWarning()
        {
            SituationLocationEntry location = GetSelectedLocation();
            if (location == null ||
                !TryGetDefaultRoomLocation(location, out RoomLocation expected) ||
                expected == RoomLocation.None ||
                _roomLocation == expected)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                string.Format(
                    Tr("Selected Location usually maps to RoomLocation {0}. Current RoomLocation is {1}."),
                    expected,
                    _roomLocation),
                MessageType.Warning);
        }

        private void ApplyDefaultRoomLocation(
            SituationLocationEntry location,
            bool force)
        {
            if (location == null ||
                !TryGetDefaultRoomLocation(location, out RoomLocation roomLocation) ||
                roomLocation == RoomLocation.None)
            {
                return;
            }

            if (force || _roomLocation == RoomLocation.None)
            {
                _roomLocation = roomLocation;
            }
        }

        private static bool TryGetDefaultRoomLocation(
            SituationLocationEntry location,
            out RoomLocation roomLocation)
        {
            foreach (string key in GetLocationKeys(location))
            {
                if (TryMapLocationKeyToRoomLocation(key, out roomLocation))
                {
                    return true;
                }
            }

            roomLocation = RoomLocation.None;
            return false;
        }

        private static IEnumerable<string> GetLocationKeys(
            SituationLocationEntry location)
        {
            if (location == null)
            {
                yield break;
            }

            yield return location.Id;
            yield return location.DisplayName;
            yield return location.SceneFolderName;
            yield return location.ControllerFolderName;
        }

        private static bool TryMapLocationKeyToRoomLocation(
            string key,
            out RoomLocation roomLocation)
        {
            string normalizedKey = NormalizeLocationKey(key);
            foreach (RoomLocation candidate in
                     Enum.GetValues(typeof(RoomLocation)).Cast<RoomLocation>())
            {
                if (candidate != RoomLocation.None &&
                    normalizedKey == NormalizeLocationKey(candidate.ToString()))
                {
                    roomLocation = candidate;
                    return true;
                }
            }

            roomLocation = RoomLocation.None;
            return false;
        }

        private static string NormalizeLocationKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value
                .Trim()
                .ToLowerInvariant()
                .Replace("&", "and")
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty);
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
                EditorGUILayout.HelpBox(Tr("No ID assets were found."), MessageType.Info);
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

        private static void DrawAssetList<T>(
            string label,
            List<T> assets,
            bool optional = false)
            where T : UnityEngine.Object
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                optional
                    ? $"{Tr(label)} [{Tr("Optional")}]"
                    : Tr(label),
                EditorStyles.boldLabel);
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

            if (GUILayout.Button(
                    string.Format(Tr("Add {0}"), typeof(T).Name),
                    GUILayout.Width(180f)))
            {
                assets.Add(null);
            }
        }

        private void RunBuildingBlockAction(Action action)
        {
            try
            {
                action();
                SetMessage(Tr("Building block added."), MessageType.Info);
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
            return new GUIContent($"{Tr(label)} * [{Tr("Required")}]", tooltip);
        }

        private static GUIContent Conditional(string label, string tooltip)
        {
            return new GUIContent($"{Tr(label)} [{Tr("Conditional")}]", tooltip);
        }

        private static GUIContent Optional(string label, string tooltip)
        {
            return new GUIContent($"{Tr(label)} [{Tr("Optional")}]", tooltip);
        }

        private static void ToggleLanguage()
        {
            _language = _language == Language.English
                ? Language.Korean
                : Language.English;
            EditorPrefs.SetInt(LanguagePrefsKey, (int)_language);
        }

        private static string Tr(string text)
        {
            if (_language != Language.Korean)
            {
                return text;
            }

            switch (text)
            {
                case "New": return "신규";
                case "Situation Authoring": return "상황 저작";
                case "Edit Existing": return "기존 수정";
                case "Building Blocks": return "빌딩 블록";
                case "Validate": return "검증";
                case "Switch Language": return "언어 전환";
                case "Open Situation Authoring guide": return "Situation Authoring 사용 설명서 열기";
                case "Resume": return "재개";
                case "Cancel Pending Request": return "대기 요청 취소";
                case "New Situation": return "신규 상황";
                case "Creates the Controller script first, then resumes scene and Definition creation after Unity compiles.": return "Controller 스크립트를 먼저 생성한 뒤, Unity 컴파일 후 씬과 Definition 생성을 이어서 진행합니다.";
                case "Name": return "이름";
                case "Display Name": return "표시 이름";
                case "Situation Definition Name": return "Situation Definition 이름";
                case "Situation ID": return "상황 ID";
                case "Level & Location": return "레벨 및 위치";
                case "Home Layout": return "홈 레이아웃";
                case "Level": return "레벨";
                case "Room Trigger": return "룸 트리거";
                case "Scene Name": return "씬 이름";
                case "Controller Class Name": return "Controller 클래스 이름";
                case "Controller Namespace": return "Controller 네임스페이스";
                case "Percentage": return "비율";
                case "Weight": return "가중치";
                case "Minimum Day": return "최소 날짜";
                case "Resolved Dialogue Id": return "해결 대사 ID";
                case "Resolved Dialogue Text": return "해결 대사 본문";
                case "Failed Dialogue Id": return "실패 대사 ID";
                case "Failed Dialogue Text": return "실패 대사 본문";
                case "Before Resolve Calling Dialogue Group ID": return "해결 전 통화 대사 그룹 ID";
                case "After Resolve Calling Dialogue Group ID": return "해결 후 통화 대사 그룹 ID";
                case "Level 2 Calling Dialogue Group ID": return "Level 2 통화 대사 그룹 ID";
                case "Append Missing Dialogue Rows": return "누락 대사 행 추가";
                case "Register as Candidate": return "후보로 등록";
                case "Initial Prefabs": return "초기 프리팹";
                case "Module Object IDs": return "모듈 오브젝트 ID";
                case "Locked Door IDs": return "잠긴 문 ID";
                case "Trap Door IDs": return "함정 문 ID";
                case "View Door ID Layout": return "Door ID 배치도 보기";
                case "Planned Assets": return "생성 예정 에셋";
                case "Create Situation": return "상황 생성";
                case "Controller script created. Unity will compile and resume the remaining work.": return "Controller 스크립트를 생성했습니다. Unity 컴파일 후 남은 작업을 이어서 진행합니다.";
                case "Level 2 Rules": return "Level 2 규칙";
                case "Uses Time Limit": return "제한시간 사용";
                case "Time Limit Seconds": return "제한시간(초)";
                case "Allowed Exits": return "허용 출구";
                case "Elevator": return "엘리베이터";
                case "CellPhone": return "휴대전화";
                case "Emergency Stairs": return "비상계단";
                case "Refuge Area": return "대피공간";
                case "Lightweight Partition": return "경량칸막이";
                case "Descender": return "완강기";
                case "Location": return "위치";
                case "Situation Location Catalog is missing or empty.": return "Situation Location Catalog가 없거나 비어 있습니다.";
                case "Create or Reload Catalog": return "Catalog 생성/다시 불러오기";
                case "Scene Folder": return "씬 폴더";
                case "Controller Folder": return "Controller 폴더";
                case "Add New Location": return "새 위치 추가";
                case "Select Catalog Asset": return "Catalog 에셋 선택";
                case "Location Name": return "위치 이름";
                case "Location ID": return "위치 ID";
                case "Save Location": return "위치 저장";
                case "Location '{0}' was added.": return "위치 '{0}'을(를) 추가했습니다.";
                case "Could Not Save Location": return "위치를 저장할 수 없음";
                case "OK": return "확인";
                case "Cancel": return "취소";
                case "Definition": return "Definition";
                case "Select a SituationDefinition.": return "SituationDefinition을 선택하세요.";
                case "Apply Definition Changes": return "Definition 변경 적용";
                case "Definition changes applied.": return "Definition 변경을 적용했습니다.";
                case "Build Settings": return "Build Settings";
                case "Registered": return "등록됨";
                case "Missing or Disabled": return "없음 또는 비활성화";
                case "Add to Build Settings": return "Build Settings에 추가";
                case "Open Situation Scene": return "상황 씬 열기";
                case "Open with Home Layout": return "Home Layout과 함께 열기";
                case "Could not open the situation with its Home Layout.": return "상황을 Home Layout과 함께 열 수 없습니다.";
                case "Validate This Situation": return "이 상황 검증";
                case "Registered ({0} entry)": return "등록됨 ({0}개 항목)";
                case "Not registered (valid for test-only use)": return "등록되지 않음 (테스트 전용으로 유효)";
                case "Candidate": return "후보";
                case "Register Candidate": return "후보 등록";
                case "Unregister Candidate": return "후보 등록 해제";
                case "Refresh Status": return "상태 새로고침";
                case "Adds common components to the active situation scene. It does not create situation-specific success or failure logic.": return "현재 활성 상황 씬에 공통 컴포넌트를 추가합니다. 상황별 성공/실패 로직은 생성하지 않습니다.";
                case "Situation Controller": return "상황 Controller";
                case "Target Object": return "대상 오브젝트";
                case "Refresh Scene Controller": return "씬 Controller 새로고침";
                case "Refresh ID Assets": return "ID 에셋 새로고침";
                case "Add Object Override": return "오브젝트 Override 추가";
                case "Door IDs": return "Door ID";
                case "Add Door Lock Override": return "문 잠금 Override 추가";
                case "Add Trap Door Trigger": return "함정 문 Trigger 추가";
                case "Prefab Palette": return "프리팹 팔레트";
                case "Prefab": return "프리팹";
                case "Add Selected Prefab": return "선택한 프리팹 추가";
                case "Phone Call Dialogue": return "통화 대사";
                case "CellPhone is not an allowed exit for this definition.": return "이 Definition은 CellPhone이 허용 출구가 아닙니다.";
                case "Home Module Parents": return "Home 모듈 부모";
                case "Creates empty root parent objects in selected Home Layout module scenes for situation-specific staging.": return "상황별 연출 정리를 위해 선택한 Home Layout 모듈 씬 루트에 빈 부모 오브젝트를 생성합니다.";
                case "Situation Definition": return "상황 Definition";
                case "Parent Object Name": return "부모 오브젝트 이름";
                case "Suggest": return "추천";
                case "Refresh Home Layout": return "Home Layout 새로고침";
                case "Select All": return "전체 선택";
                case "Clear Selection": return "선택 해제";
                case "Home Layout is missing or has no module scenes.": return "Home Layout이 없거나 모듈 씬이 비어 있습니다.";
                case "Create Parent In Selected Scenes": return "선택한 씬에 부모 생성";
                case "Home Layout Module Scenes": return "Home Layout 모듈 씬";
                case "Scene asset was not found.": return "씬 에셋을 찾을 수 없습니다.";
                case "Selected Location usually maps to RoomLocation {0}. Current RoomLocation is {1}.": return "선택한 Location은 보통 RoomLocation {0}에 매핑됩니다. 현재 RoomLocation은 {1}입니다.";
                case "Created {0} parent object(s).": return "부모 오브젝트 {0}개를 생성했습니다.";
                case "Skipped {0} scene(s) where the parent already exists.": return "이미 부모가 있는 씬 {0}개는 건너뛰었습니다.";
                case "Missing scenes: {0}": return "찾을 수 없는 씬: {0}";
                case "Parent object name is required.": return "부모 오브젝트 이름이 필요합니다.";
                case "Select at least one Home Layout module scene.": return "Home Layout 모듈 씬을 하나 이상 선택하세요.";
                case "Home module parent creation was cancelled.": return "Home 모듈 부모 생성이 취소되었습니다.";
                case "Validate Current Situation": return "현재 상황 검증";
                case "Required validation passed. Warnings: {0}.": return "필수 검증 통과. 경고: {0}개.";
                case "Validation failed. Errors: {0}, Warnings: {1}.": return "검증 실패. 오류: {0}개, 경고: {1}개.";
                case "Select": return "선택";
                case "No ID assets were found.": return "ID 에셋을 찾을 수 없습니다.";
                case "Add {0}": return "{0} 추가";
                case "Building block added.": return "빌딩 블록을 추가했습니다.";
                case "Required": return "필수";
                case "Conditional": return "조건부";
                case "Optional": return "선택";
                default: return text;
            }
        }
    }
}
