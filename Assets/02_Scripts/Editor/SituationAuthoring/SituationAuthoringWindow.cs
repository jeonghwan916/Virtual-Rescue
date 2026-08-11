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
        [SerializeField] private string _displayName = "New Situation";
        [SerializeField] private string _situationId = "new.situation";
        [SerializeField] private SituationLocation _location =
            SituationLocation.Room;
        [SerializeField] private SituationLevel _level = SituationLevel.Level0;
        [SerializeField] private string _sceneName = "Scenario_Room_NewSituation";
        [SerializeField] private string _controllerClassName =
            "NewSituationController";
        [SerializeField] private string _controllerNamespace =
            "VirtualRescue.Situations";
        [SerializeField] private string _controllerScriptFolder =
            "Assets/02_Scripts/10_Situations/Room";
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
            RefreshIdAssets();
            RefreshHomeLayout();
            if (Selection.activeObject is SituationDefinition definition)
            {
                _editDefinition = definition;
                _validationDefinition = definition;
                _editSerializedDefinition = new SerializedObject(definition);
            }
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
                "Name shown in the Hierarchy and used for the Definition file."),
                _displayName);
            _situationId = EditorGUILayout.TextField(Required(
                "Situation ID",
                "Stable ID used by history and radio matching. It must be unique."),
                _situationId);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    Optional("Home Layout", "Read from LoopBase DaySceneCoordinator."),
                    _homeLayout,
                    typeof(HomeLayoutDefinition),
                    false);
            }

            EditorGUI.BeginChangeCheck();
            _location = (SituationLocation)EditorGUILayout.EnumPopup(
                Required("Location", "Controls the situation scene folder."),
                _location);
            if (EditorGUI.EndChangeCheck())
            {
                _controllerScriptFolder =
                    SituationLocationPathMap.GetDefaultControllerFolder(_location);
            }

            _level = (SituationLevel)EditorGUILayout.EnumPopup(
                Required("Level", "Controls exit and time-limit rules."),
                _level);
            _sceneName = EditorGUILayout.TextField(Required(
                "Scene Name",
                "Generated scene asset name; use the Scenario_ prefix."),
                _sceneName);
            _controllerClassName = EditorGUILayout.TextField(Required(
                "Controller Class Name",
                "C# class generated as a SituationController subclass."),
                _controllerClassName);
            _controllerNamespace = EditorGUILayout.TextField(Required(
                "Controller Namespace",
                "C# namespace for the generated Controller."),
                _controllerNamespace);
            _controllerScriptFolder = EditorGUILayout.TextField(Required(
                "Controller Script Path",
                "Project-relative folder below Assets."),
                _controllerScriptFolder);

            _weight = EditorGUILayout.IntField(Required(
                "Weight",
                "Relative random selection weight; stored even for test-only situations."),
                _weight);
            _minimumDay = EditorGUILayout.IntSlider(Required(
                "Minimum Day",
                "First day on which the situation may be selected."),
                _minimumDay,
                1,
                7);
            _registerAsCandidate = EditorGUILayout.Toggle(Optional(
                "Register as Candidate",
                "When enabled, adds the Definition to LoopBase Candidates. " +
                "Disabled by default for safe test-only creation."),
                _registerAsCandidate);

            DrawLevel2Rules();
            DrawAssetList("Initial Prefabs [Optional]", _initialPrefabs);
            DrawAssetList("Module Object IDs [Optional]", _newModuleIds);
            DrawAssetList("Locked Door IDs [Optional]", _newLockedDoorIds);
            DrawAssetList("Trap Door IDs [Optional]", _newTrapDoorIds);

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
                "Starts the shared Level 2 countdown."),
                _usesTimeLimit);
            if (_usesTimeLimit)
            {
                _timeLimitSeconds = EditorGUILayout.FloatField(Conditional(
                    "Time Limit Seconds",
                    "Must be greater than zero when the countdown is enabled."),
                    _timeLimitSeconds);
            }

            EditorGUILayout.LabelField(Conditional(
                "Allowed Exits",
                "At least one non-elevator exit is required."));
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

        private void DrawEditExisting()
        {
            EditorGUILayout.LabelField("Edit Existing", EditorStyles.boldLabel);
            SituationDefinition previous = _editDefinition;
            _editDefinition = (SituationDefinition)EditorGUILayout.ObjectField(
                Required("Definition", "Existing situation to edit."),
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
                Required("Situation Controller", "Controller in the active scene."),
                _buildingController,
                typeof(SituationController),
                true);
            if (_buildingTarget == null && _buildingController != null)
            {
                _buildingTarget = _buildingController.gameObject;
            }

            _buildingTarget = (GameObject)EditorGUILayout.ObjectField(
                Required("Target Object", "Object that receives the component."),
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
                _availableModuleIds,
                _selectedModuleIds,
                ref _moduleIdScroll);
            DrawIdSelection(
                "Door IDs",
                _availableDoorIds,
                _selectedDoorIds,
                ref _doorIdScroll);

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
                Optional("Prefab", "Prefab to add below the target object."),
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
                Required("Definition", "Situation to validate."),
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

            return new SituationCreationRequest
            {
                displayName = _displayName,
                situationId = _situationId,
                location = (int)_location,
                level = (int)_level,
                sceneName = _sceneName,
                controllerClassName = _controllerClassName,
                controllerNamespace = _controllerNamespace,
                controllerScriptFolder = _controllerScriptFolder,
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
            IReadOnlyList<T> available,
            ISet<T> selected,
            ref Vector2 scroll)
            where T : UnityEngine.Object
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
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
