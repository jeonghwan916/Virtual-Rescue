using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VirtualRescue.EditorTools.DialogueCsvAuthoring
{
    public sealed class DialogueCsvAuthoringWindow : EditorWindow
    {
        private const string LanguagePrefsKey =
            "VirtualRescue.DialogueCsvAuthoring.Language";

        private enum Tab
        {
            AppendNew,
            EditExisting
        }

        private enum Language
        {
            English,
            Korean
        }

        private static Language _uiLanguage = Language.English;

        [SerializeField] private TextAsset _dialogueCsv;
        [SerializeField] private TextAsset _textCsv;
        [SerializeField] private Tab _tab;
        [SerializeField] private DialogueCsvAuthoringMode _mode;
        [SerializeField] private string _id = string.Empty;
        [SerializeField] private string _group = string.Empty;
        [SerializeField] private string _language = DialogueCsvAuthoringService.DefaultLanguage;
        [SerializeField] private string _speaker = string.Empty;
        [SerializeField] private string _audioPath = string.Empty;
        [SerializeField] private string _callbackKey = string.Empty;
        [SerializeField] private string _delayAfterAudio =
            DialogueCsvAuthoringService.DefaultDelayAfterAudio;
        [SerializeField] private List<DialogueLineDraft> _lines = new();
        [SerializeField] private string _searchId = string.Empty;
        [SerializeField] private string _searchGroup = string.Empty;
        [SerializeField] private List<DialogueEditDraft> _editEntries = new();
        [SerializeField] private Vector2 _scrollPosition;

        private string _lastMessage = string.Empty;
        private MessageType _lastMessageType = MessageType.None;

        [MenuItem("Tools/Virtual Rescue/Dialogue CSV Authoring")]
        public static void Open()
        {
            DialogueCsvAuthoringWindow window =
                GetWindow<DialogueCsvAuthoringWindow>();
            window.titleContent = new GUIContent("Dialogue CSV Authoring");
            window.minSize = new Vector2(560f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            _uiLanguage = (Language)EditorPrefs.GetInt(
                LanguagePrefsKey,
                (int)Language.English);

            if (_dialogueCsv == null)
            {
                _dialogueCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    DialogueCsvAuthoringService.DefaultDialogueAssetPath);
            }

            if (_textCsv == null)
            {
                _textCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    DialogueCsvAuthoringService.DefaultTextAssetPath);
            }

            if (_lines.Count == 0)
            {
                _lines.Add(new DialogueLineDraft());
            }
        }

        private void OnGUI()
        {
            titleContent = new GUIContent(Tr("Dialogue CSV Authoring"));
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawHeader();
            DrawCsvSelection();
            EditorGUILayout.Space(8f);
            _tab = (Tab)GUILayout.Toolbar(
                (int)_tab,
                new[] { Tr("Append New"), Tr("Edit Existing") });
            EditorGUILayout.Space(8f);

            if (_tab == Tab.AppendNew)
            {
                DrawRequiredFields();
                EditorGUILayout.Space(8f);
                DrawOptionalDefaults();
                EditorGUILayout.Space(8f);
                DrawLines();
                EditorGUILayout.Space(8f);
                DrawWarningsAndSave();
            }
            else
            {
                DrawEditExisting();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    Tr("Dialogue CSV Authoring"),
                    EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(
                        new GUIContent(
                            _uiLanguage == Language.English ? "한국어" : "English",
                            Tr("Switch Language")),
                        GUILayout.Width(70f)))
                {
                    ToggleLanguage();
                }
            }

            EditorGUILayout.Space(6f);
        }

        private void DrawCsvSelection()
        {
            EditorGUILayout.LabelField(Tr("CSV Files"), EditorStyles.boldLabel);
            _dialogueCsv = (TextAsset)EditorGUILayout.ObjectField(
                Required(
                    "Dialogue CSV",
                    "CSV containing id, group, order, speaker, audioPath, callbackKey, and delayAfterAudio fields."),
                _dialogueCsv,
                typeof(TextAsset),
                false);
            _textCsv = (TextAsset)EditorGUILayout.ObjectField(
                Required(
                    "Text CSV",
                    "CSV containing id, language, and text fields."),
                _textCsv,
                typeof(TextAsset),
                false);
        }

        private void DrawRequiredFields()
        {
            EditorGUILayout.LabelField(Tr("Required"), EditorStyles.boldLabel);
            _mode = (DialogueCsvAuthoringMode)EditorGUILayout.Popup(
                Required("Mode", "Choose whether to add one row or a grouped sequence."),
                (int)_mode,
                new[] { Tr("Single"), Tr("Group") });
            _id = EditorGUILayout.TextField(
                Required(
                    _mode == DialogueCsvAuthoringMode.Group ? "Base ID" : "ID",
                    _mode == DialogueCsvAuthoringMode.Group
                        ? "Rows are saved as BaseID_001, BaseID_002..."
                        : "ID used by DialogueManager.Play(id)."),
                _id);
            _language = EditorGUILayout.TextField(
                Required("Language", "Language key used by DialogueManager."),
                _language);

            if (_mode == DialogueCsvAuthoringMode.Group)
            {
                _group = EditorGUILayout.TextField(
                    Required(
                        "Group",
                        "Group ID used by DialogueManager.PlayGroup(group)."),
                    _group);
            }
        }

        private void DrawOptionalDefaults()
        {
            EditorGUILayout.LabelField(Tr("Optional Defaults"), EditorStyles.boldLabel);
            _speaker = EditorGUILayout.TextField(
                Optional("Speaker", "Speaker name shown in the subtitle UI."),
                _speaker);
            _audioPath = EditorGUILayout.TextField(
                Optional("Audio Path", "Audio file path under the DialogueManager audio base path."),
                _audioPath);
            _callbackKey = EditorGUILayout.TextField(
                Optional("Callback Key", "Callback key registered in DialogueManager."),
                _callbackKey);
            _delayAfterAudio = EditorGUILayout.TextField(
                Optional("Delay After Audio", "Seconds to wait after audio or subtitle playback."),
                _delayAfterAudio);
        }

        private void DrawLines()
        {
            EditorGUILayout.LabelField(
                Tr(_mode == DialogueCsvAuthoringMode.Group ? "Group Lines" : "Text"),
                EditorStyles.boldLabel);

            if (_mode == DialogueCsvAuthoringMode.Single && _lines.Count > 1)
            {
                _lines.RemoveRange(1, _lines.Count - 1);
            }

            for (int index = 0; index < _lines.Count; index++)
            {
                DialogueLineDraft line = _lines[index];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string idPreview = _mode == DialogueCsvAuthoringMode.Group
                            ? $"{_id}_{index + 1:000}"
                            : _id;
                        EditorGUILayout.LabelField(
                            string.IsNullOrWhiteSpace(idPreview)
                                ? $"{Tr("Line")} {index + 1}"
                                : idPreview,
                            EditorStyles.boldLabel);

                        if (_mode == DialogueCsvAuthoringMode.Group &&
                            GUILayout.Button(Tr("Remove"), GUILayout.Width(76f)))
                        {
                            _lines.RemoveAt(index);
                            index--;
                            continue;
                        }
                    }

                    line.Text = EditorGUILayout.TextArea(
                        line.Text,
                        GUILayout.MinHeight(48f));
                    line.ShowOverrides = EditorGUILayout.Foldout(
                        line.ShowOverrides,
                        Tr("Optional Overrides"));
                    if (line.ShowOverrides)
                    {
                        line.Speaker = EditorGUILayout.TextField(
                            Optional("Speaker", "Overrides the default speaker for this row."),
                            line.Speaker);
                        line.AudioPath = EditorGUILayout.TextField(
                            Optional("Audio Path", "Overrides the default audio path for this row."),
                            line.AudioPath);
                        line.CallbackKey = EditorGUILayout.TextField(
                            Optional("Callback Key", "Overrides the default callback key for this row."),
                            line.CallbackKey);
                        line.DelayAfterAudio = EditorGUILayout.TextField(
                            Optional("Delay After Audio", "Overrides the default delay for this row."),
                            line.DelayAfterAudio);
                    }
                }
            }

            if (_mode == DialogueCsvAuthoringMode.Group &&
                GUILayout.Button(Tr("Add Group Line")))
            {
                _lines.Add(new DialogueLineDraft());
            }
        }

        private void DrawWarningsAndSave()
        {
            string dialoguePath = GetAssetPath(_dialogueCsv);
            string textPath = GetAssetPath(_textCsv);
            DialogueCsvValidationResult validation =
                DialogueCsvAuthoringService.ValidateFiles(
                    dialoguePath,
                    textPath,
                    _mode == DialogueCsvAuthoringMode.Group);

            foreach (string warning in validation.Warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            if (!string.IsNullOrEmpty(_lastMessage))
            {
                EditorGUILayout.HelpBox(_lastMessage, _lastMessageType);
            }

            using (new EditorGUI.DisabledScope(!validation.CanSave))
            {
                if (GUILayout.Button(Tr("Append Rows"), GUILayout.Height(30f)))
                {
                    Save(dialoguePath, textPath);
                }
            }
        }

        private void DrawEditExisting()
        {
            string dialoguePath = GetAssetPath(_dialogueCsv);
            string textPath = GetAssetPath(_textCsv);
            DialogueCsvValidationResult validation =
                DialogueCsvAuthoringService.ValidateFiles(
                    dialoguePath,
                    textPath,
                    false);

            EditorGUILayout.LabelField(Tr("Search"), EditorStyles.boldLabel);
            _searchId = EditorGUILayout.TextField(
                Optional("ID", "Exact id match."),
                _searchId);
            _searchGroup = EditorGUILayout.TextField(
                Optional(
                    "Group",
                    "Exact group match. Returns all rows in the group."),
                _searchGroup);
            _language = EditorGUILayout.TextField(
                Required("Language", "Language key used to find text rows."),
                _language);

            foreach (string warning in validation.Warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!validation.CanSave))
            {
                if (GUILayout.Button(Tr("Search")))
                {
                    SearchExisting(dialoguePath, textPath);
                }
            }

            if (_editEntries.Count > 0)
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField(Tr("Results"), EditorStyles.boldLabel);
                foreach (DialogueEditDraft entry in _editEntries)
                {
                    DrawEditEntry(entry);
                }

                bool hasDeleteSelection = HasDeleteSelection();
                using (new EditorGUI.DisabledScope(!validation.CanSave))
                {
                    if (GUILayout.Button(Tr("Update Found Rows"), GUILayout.Height(30f)))
                    {
                        UpdateExisting(dialoguePath, textPath);
                    }
                }

                using (new EditorGUI.DisabledScope(!validation.CanSave || !hasDeleteSelection))
                {
                    if (GUILayout.Button(Tr("Delete Selected Rows"), GUILayout.Height(30f)))
                    {
                        DeleteSelected(dialoguePath, textPath);
                    }
                }
            }

            if (!string.IsNullOrEmpty(_lastMessage))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox(_lastMessage, _lastMessageType);
            }
        }

        private static void DrawEditEntry(DialogueEditDraft entry)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(entry.Id, EditorStyles.boldLabel);
                    entry.Delete = EditorGUILayout.ToggleLeft(
                        Tr("Delete"),
                        entry.Delete,
                        GUILayout.Width(72f));
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        Required("ID", "ID cannot be edited because runtime references use it."),
                        entry.Id);
                    EditorGUILayout.TextField(
                        Required("Language", "Language key for this text row."),
                        entry.Language);
                }

                entry.Group = EditorGUILayout.TextField(
                    Optional("Group", "Group ID used by DialogueManager.PlayGroup(group)."),
                    entry.Group);
                entry.Order = EditorGUILayout.TextField(
                    Optional("Order", "Playback order inside a group."),
                    entry.Order);
                entry.Speaker = EditorGUILayout.TextField(
                    Optional("Speaker", "Speaker name shown in the subtitle UI."),
                    entry.Speaker);
                entry.AudioPath = EditorGUILayout.TextField(
                    Optional("Audio Path", "Audio file path under the DialogueManager audio base path."),
                    entry.AudioPath);
                entry.CallbackKey = EditorGUILayout.TextField(
                    Optional("Callback Key", "Callback key registered in DialogueManager."),
                    entry.CallbackKey);
                entry.DelayAfterAudio = EditorGUILayout.TextField(
                    Optional("Delay After Audio", "Seconds to wait after audio or subtitle playback."),
                    entry.DelayAfterAudio);
                EditorGUILayout.LabelField(Optional("Text", "Subtitle text for the selected language."));
                entry.Text = EditorGUILayout.TextArea(
                    entry.Text,
                    GUILayout.MinHeight(54f));
            }
        }

        private void Save(string dialoguePath, string textPath)
        {
            DialogueCsvSaveRequest request = new()
            {
                DialogueAssetPath = dialoguePath,
                TextAssetPath = textPath,
                Mode = _mode,
                Id = _id,
                Group = _group,
                Language = _language,
                Speaker = _speaker,
                AudioPath = _audioPath,
                CallbackKey = _callbackKey,
                DelayAfterAudio = _delayAfterAudio
            };

            foreach (DialogueLineDraft line in _lines)
            {
                request.Lines.Add(new DialogueCsvLine(
                    line.Text,
                    line.Speaker,
                    line.AudioPath,
                    line.CallbackKey,
                    line.DelayAfterAudio));
            }

            if (DialogueCsvAuthoringService.TryAppendRows(
                    request,
                    out DialogueCsvSaveResult result))
            {
                _lastMessage = result.Message;
                _lastMessageType = MessageType.Info;
                GUI.FocusControl(null);
                return;
            }

            _lastMessage = result.Message;
            _lastMessageType = MessageType.Error;
        }

        private void SearchExisting(string dialoguePath, string textPath)
        {
            if (DialogueCsvAuthoringService.TryFindEntries(
                    dialoguePath,
                    textPath,
                    _searchId,
                    _searchGroup,
                    _language,
                    out List<DialogueCsvEditEntry> entries,
                    out string message))
            {
                _editEntries.Clear();
                foreach (DialogueCsvEditEntry entry in entries)
                {
                    _editEntries.Add(DialogueEditDraft.FromEntry(entry));
                }

                _lastMessage = message;
                _lastMessageType = MessageType.Info;
                GUI.FocusControl(null);
                return;
            }

            _editEntries.Clear();
            _lastMessage = message;
            _lastMessageType = MessageType.Warning;
        }

        private void UpdateExisting(string dialoguePath, string textPath)
        {
            List<DialogueCsvEditEntry> entries = new();
            foreach (DialogueEditDraft draft in _editEntries)
            {
                entries.Add(draft.ToEntry());
            }

            if (DialogueCsvAuthoringService.TryUpdateEntries(
                    dialoguePath,
                    textPath,
                    entries,
                    out string message))
            {
                _lastMessage = message;
                _lastMessageType = MessageType.Info;
                GUI.FocusControl(null);
                return;
            }

            _lastMessage = message;
            _lastMessageType = MessageType.Error;
        }

        private void DeleteSelected(string dialoguePath, string textPath)
        {
            List<string> ids = new();
            foreach (DialogueEditDraft draft in _editEntries)
            {
                if (draft.Delete)
                {
                    ids.Add(draft.Id);
                }
            }

            if (!EditorUtility.DisplayDialog(
                    Tr("Delete Dialogue Rows"),
                    string.Format(
                        Tr("Delete {0} selected row(s) from both CSV files?"),
                        ids.Count),
                    Tr("Delete"),
                    Tr("Cancel")))
            {
                return;
            }

            if (DialogueCsvAuthoringService.TryDeleteEntries(
                    dialoguePath,
                    textPath,
                    ids,
                    out string message))
            {
                _editEntries.RemoveAll(entry => entry.Delete);
                _lastMessage = message;
                _lastMessageType = MessageType.Info;
                GUI.FocusControl(null);
                return;
            }

            _lastMessage = message;
            _lastMessageType = MessageType.Error;
        }

        private bool HasDeleteSelection()
        {
            foreach (DialogueEditDraft draft in _editEntries)
            {
                if (draft.Delete)
                {
                    return true;
                }
            }

            return false;
        }

        private static GUIContent Required(string label, string tooltip)
        {
            return new GUIContent(
                $"{Tr(label)} * [{Tr("Required")}]",
                Tr(tooltip));
        }

        private static GUIContent Optional(string label, string tooltip)
        {
            return new GUIContent(
                $"{Tr(label)} [{Tr("Optional")}]",
                Tr(tooltip));
        }

        private static void ToggleLanguage()
        {
            _uiLanguage = _uiLanguage == Language.English
                ? Language.Korean
                : Language.English;
            EditorPrefs.SetInt(LanguagePrefsKey, (int)_uiLanguage);
        }

        private static string Tr(string text)
        {
            if (_uiLanguage != Language.Korean)
            {
                return text;
            }

            switch (text)
            {
                case "Dialogue CSV Authoring": return "대사 CSV 저작";
                case "Switch Language": return "언어 전환";
                case "Append New": return "새 대사 추가";
                case "Edit Existing": return "기존 대사 수정";
                case "CSV Files": return "CSV 파일";
                case "Dialogue CSV": return "Dialogue CSV";
                case "Text CSV": return "Text CSV";
                case "Required": return "필수";
                case "Optional": return "선택";
                case "Mode": return "모드";
                case "Single": return "단일";
                case "Base ID": return "기준 ID";
                case "ID": return "ID";
                case "Language": return "언어";
                case "Group": return "그룹";
                case "Optional Defaults": return "선택 기본값";
                case "Speaker": return "화자";
                case "Audio Path": return "오디오 경로";
                case "Callback Key": return "콜백 키";
                case "Delay After Audio": return "오디오 후 지연";
                case "Group Lines": return "그룹 대사";
                case "Text": return "본문";
                case "Line": return "행";
                case "Remove": return "제거";
                case "Optional Overrides": return "선택 개별값";
                case "Add Group Line": return "그룹 행 추가";
                case "Append Rows": return "행 추가";
                case "Search": return "검색";
                case "Results": return "검색 결과";
                case "Order": return "순서";
                case "Update Found Rows": return "검색 행 수정";
                case "Delete Selected Rows": return "선택 행 삭제";
                case "Delete": return "삭제";
                case "Cancel": return "취소";
                case "Delete Dialogue Rows": return "대사 행 삭제";
                case "Delete {0} selected row(s) from both CSV files?": return "선택한 {0}개 행을 두 CSV 파일에서 삭제할까요?";
                case "CSV containing id, group, order, speaker, audioPath, callbackKey, and delayAfterAudio fields.": return "id, group, order, speaker, audioPath, callbackKey, delayAfterAudio 필드를 가진 CSV입니다.";
                case "CSV containing id, language, and text fields.": return "id, language, text 필드를 가진 CSV입니다.";
                case "Choose whether to add one row or a grouped sequence.": return "단일 행을 추가할지, 그룹 대사 묶음을 추가할지 선택합니다.";
                case "Rows are saved as BaseID_001, BaseID_002...": return "행은 BaseID_001, BaseID_002... 형식으로 저장됩니다.";
                case "ID used by DialogueManager.Play(id).": return "DialogueManager.Play(id)에서 사용하는 ID입니다.";
                case "Language key used by DialogueManager.": return "DialogueManager가 사용하는 언어 키입니다.";
                case "Group ID used by DialogueManager.PlayGroup(group).": return "DialogueManager.PlayGroup(group)에서 사용하는 그룹 ID입니다.";
                case "Speaker name shown in the subtitle UI.": return "자막 UI에 표시할 화자 이름입니다.";
                case "Audio file path under the DialogueManager audio base path.": return "DialogueManager 오디오 기본 경로 아래의 오디오 파일 경로입니다.";
                case "Callback key registered in DialogueManager.": return "DialogueManager에 등록된 콜백 키입니다.";
                case "Seconds to wait after audio or subtitle playback.": return "오디오 또는 자막 재생 후 기다릴 시간(초)입니다.";
                case "Overrides the default speaker for this row.": return "이 행에서 기본 화자 값을 덮어씁니다.";
                case "Overrides the default audio path for this row.": return "이 행에서 기본 오디오 경로를 덮어씁니다.";
                case "Overrides the default callback key for this row.": return "이 행에서 기본 콜백 키를 덮어씁니다.";
                case "Overrides the default delay for this row.": return "이 행에서 기본 지연 시간을 덮어씁니다.";
                case "Exact id match.": return "ID가 정확히 일치하는 행을 찾습니다.";
                case "Exact group match. Returns all rows in the group.": return "그룹이 정확히 일치하는 모든 행을 찾습니다.";
                case "Language key used to find text rows.": return "Text CSV 행을 찾는 데 사용할 언어 키입니다.";
                case "ID cannot be edited because runtime references use it.": return "런타임 참조가 ID를 사용하므로 ID는 수정할 수 없습니다.";
                case "Language key for this text row.": return "이 텍스트 행의 언어 키입니다.";
                case "Playback order inside a group.": return "그룹 안에서 재생되는 순서입니다.";
                case "Subtitle text for the selected language.": return "선택한 언어의 자막 본문입니다.";
                default: return text;
            }
        }

        private static string GetAssetPath(Object asset)
        {
            return asset == null ? string.Empty : AssetDatabase.GetAssetPath(asset);
        }

        [System.Serializable]
        private sealed class DialogueLineDraft
        {
            public string Text = string.Empty;
            public bool ShowOverrides;
            public string Speaker = string.Empty;
            public string AudioPath = string.Empty;
            public string CallbackKey = string.Empty;
            public string DelayAfterAudio = string.Empty;
        }

        [System.Serializable]
        private sealed class DialogueEditDraft
        {
            public string Id = string.Empty;
            public string Group = string.Empty;
            public string Order = string.Empty;
            public string Language = string.Empty;
            public string Text = string.Empty;
            public string Speaker = string.Empty;
            public string AudioPath = string.Empty;
            public string CallbackKey = string.Empty;
            public string DelayAfterAudio = string.Empty;
            public bool Delete;

            public static DialogueEditDraft FromEntry(DialogueCsvEditEntry entry)
            {
                return new DialogueEditDraft
                {
                    Id = entry.Id,
                    Group = entry.Group,
                    Order = entry.Order,
                    Language = entry.Language,
                    Text = entry.Text,
                    Speaker = entry.Speaker,
                    AudioPath = entry.AudioPath,
                    CallbackKey = entry.CallbackKey,
                    DelayAfterAudio = entry.DelayAfterAudio
                };
            }

            public DialogueCsvEditEntry ToEntry()
            {
                return new DialogueCsvEditEntry
                {
                    Id = Id,
                    Group = Group,
                    Order = Order,
                    Language = Language,
                    Text = Text,
                    Speaker = Speaker,
                    AudioPath = AudioPath,
                    CallbackKey = CallbackKey,
                    DelayAfterAudio = DelayAfterAudio
                };
            }
        }
    }
}
