using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VirtualRescue.EditorTools.DialogueCsvAuthoring
{
    public sealed class DialogueCsvAuthoringWindow : EditorWindow
    {
        [SerializeField] private TextAsset _dialogueCsv;
        [SerializeField] private TextAsset _textCsv;
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
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawCsvSelection();
            EditorGUILayout.Space(8f);
            DrawRequiredFields();
            EditorGUILayout.Space(8f);
            DrawOptionalDefaults();
            EditorGUILayout.Space(8f);
            DrawLines();
            EditorGUILayout.Space(8f);
            DrawWarningsAndSave();
            EditorGUILayout.EndScrollView();
        }

        private void DrawCsvSelection()
        {
            EditorGUILayout.LabelField("CSV Files", EditorStyles.boldLabel);
            _dialogueCsv = (TextAsset)EditorGUILayout.ObjectField(
                new GUIContent("Dialogue CSV", "Requires id. group/order are required for group mode."),
                _dialogueCsv,
                typeof(TextAsset),
                false);
            _textCsv = (TextAsset)EditorGUILayout.ObjectField(
                new GUIContent("Text CSV", "Requires id, language, text."),
                _textCsv,
                typeof(TextAsset),
                false);
        }

        private void DrawRequiredFields()
        {
            EditorGUILayout.LabelField("Required", EditorStyles.boldLabel);
            _mode = (DialogueCsvAuthoringMode)EditorGUILayout.EnumPopup("Mode", _mode);
            _id = EditorGUILayout.TextField(
                new GUIContent(
                    _mode == DialogueCsvAuthoringMode.Group ? "Base ID" : "ID",
                    _mode == DialogueCsvAuthoringMode.Group
                        ? "Rows are saved as BaseID_001, BaseID_002..."
                        : "ID used by DialogueManager.Play(id)."),
                _id);
            _language = EditorGUILayout.TextField("Language", _language);

            if (_mode == DialogueCsvAuthoringMode.Group)
            {
                _group = EditorGUILayout.TextField(
                    new GUIContent(
                        "Group",
                        "Group ID used by DialogueManager.PlayGroup(group)."),
                    _group);
            }
        }

        private void DrawOptionalDefaults()
        {
            EditorGUILayout.LabelField("Optional Defaults", EditorStyles.boldLabel);
            _speaker = EditorGUILayout.TextField("Speaker", _speaker);
            _audioPath = EditorGUILayout.TextField("Audio Path", _audioPath);
            _callbackKey = EditorGUILayout.TextField("Callback Key", _callbackKey);
            _delayAfterAudio = EditorGUILayout.TextField(
                "Delay After Audio",
                _delayAfterAudio);
        }

        private void DrawLines()
        {
            EditorGUILayout.LabelField(
                _mode == DialogueCsvAuthoringMode.Group ? "Group Lines" : "Text",
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
                                ? $"Line {index + 1}"
                                : idPreview,
                            EditorStyles.boldLabel);

                        if (_mode == DialogueCsvAuthoringMode.Group &&
                            GUILayout.Button("Remove", GUILayout.Width(76f)))
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
                        "Optional Overrides");
                    if (line.ShowOverrides)
                    {
                        line.Speaker = EditorGUILayout.TextField("Speaker", line.Speaker);
                        line.AudioPath = EditorGUILayout.TextField("Audio Path", line.AudioPath);
                        line.CallbackKey = EditorGUILayout.TextField("Callback Key", line.CallbackKey);
                        line.DelayAfterAudio = EditorGUILayout.TextField(
                            "Delay After Audio",
                            line.DelayAfterAudio);
                    }
                }
            }

            if (_mode == DialogueCsvAuthoringMode.Group &&
                GUILayout.Button("Add Group Line"))
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
                if (GUILayout.Button("Append Rows", GUILayout.Height(30f)))
                {
                    Save(dialoguePath, textPath);
                }
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
    }
}
