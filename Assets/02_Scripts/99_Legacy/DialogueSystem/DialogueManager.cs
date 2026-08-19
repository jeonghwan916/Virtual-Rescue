using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace VirtualRescue.DialogueSystem
{
    public class DialogueManager : MonoBehaviour
    {
        [Serializable]
        private class DialogueEntry
        {
            public string id;
            public string group;
            public int order;
            public string speaker;
            public string audioPath;
            public string callbackKey;
            public float? delayAfterAudio;
        }

        [Serializable]
        private class DialogueTextEntry
        {
            public string id;
            public string language;
            public string text;
        }

        [Header("CSV Data")]
        [Tooltip("로딩할 Dialogue.csv 파일 연결")]
        [SerializeField] private TextAsset _dialogueCsv;
        [Tooltip("로딩할 Dialogue_Text 파일 연결")]
        [SerializeField] private TextAsset _dialogueTextCsv;

        [Header("Audio")]
        [Tooltip("대사 음성 파일 실행할 AudioSource. XR Origin의 AudioSource 컴포넌트 연결")]
        [SerializeField] private AudioSource _audioSource;
        [Tooltip("활성화하면 런타임에 PlayerPrefabs의 XR Origin AudioSource를 사용")]
        [SerializeField] private bool _usePlayerAudioSource = true;
        [Tooltip("실행할 대사 음성 파일이 저장되어있는 경로. Resources 폴더의 하위 경로임")]
        [SerializeField] private string _audioBasePath = "Audio/Dialogue/KR";

        [Header("Subtitle UI")]
        [Tooltip("자막 파일 자체 GameObject. SubtitleFollwer 프리팹 그대로 연결하면 됨")]
        [SerializeField] private GameObject _subtitleRoot;
        [Tooltip("화자 표현 다이얼로그 텍스트 컴포넌트")]
        [SerializeField] private TMP_Text _speakerText;
        [Tooltip("대사 표현 다이얼로그 텍스트 컴포넌트")]
        [SerializeField] private TMP_Text _dialogueText;

        [Header("Typing Effect")]
        [Tooltip("자막 타이핑 효과 On/Off 결정 플래그")]
        [SerializeField] private bool _useTypewriterEffect = true;
        [Tooltip("자막이 타이핑되는 간격")]
        [SerializeField] private float _characterInterval = 0.04f;

        [Header("Language")]
        private string _currentLanguage = "kr"; // 추후 바꿀수 있음

        [Header("Callback Target")]
        [FormerlySerializedAs("dialogueMethodTest")]
        [SerializeField] private DialogueMethodTest _dialogueMethodTest;

        private readonly Dictionary<string, DialogueEntry> _dialogueById = new();
        private readonly Dictionary<string, List<DialogueEntry>> _dialoguesByGroup = new();
        private readonly Dictionary<string, string> _textById = new();
        private readonly Dictionary<string, Action> _callbackByKey = new();

        private Coroutine _currentDialogueRoutine;

        public event Action<string> GroupCompleted;

        // 컴포넌트가 초기화될 때 CSV 데이터를 읽고 검색용 Dictionary를 구성한다.
        private void Awake()
        {
            BindPlayerAudioSource();
            LoadDialogueData();
            
            if (_dialogueMethodTest != null)
            {
                RegisterCallback("LockDoor", _dialogueMethodTest.LockDoor);
                RegisterCallback("HighLight", _dialogueMethodTest.HighLight);
            }
        }

        private void BindPlayerAudioSource()
        {
            if (!_usePlayerAudioSource)
            {
                return;
            }

            if (PlayerReferenceHub.Instance == null)
            {
                Debug.LogWarning("PlayerReferenceHub를 찾을 수 없어 기존 DialogueManager AudioSource를 유지합니다.", this);
                return;
            }

            AudioSource playerAudioSource = PlayerReferenceHub.Instance.XrAudioSource;
            if (playerAudioSource == null)
            {
                Debug.LogWarning("PlayerReferenceHub에 AudioSource가 없어 기존 DialogueManager AudioSource를 유지합니다.", this);
                return;
            }

            _audioSource = playerAudioSource;
        }

        // 단일 대사 ID를 기준으로 대사를 재생한다.
        public void Play(string dialogueId)
        {
            if (string.IsNullOrWhiteSpace(dialogueId))
            {
                Debug.LogWarning("재생할 dialogueId가 비어 있습니다.");
                return;
            }

            if (!_dialogueById.TryGetValue(dialogueId, out DialogueEntry entry))
            {
                Debug.LogWarning($"Dialogue ID를 찾을 수 없습니다: {dialogueId}");
                return;
            }

            StopCurrentDialogue();
            _currentDialogueRoutine = StartCoroutine(PlayDialogueRoutine(entry));
        }

        // 그룹 ID에 속한 대사들을 order 순서대로 재생한다.
        public void PlayGroup(string groupId)
        {
            TryPlayGroup(groupId);
        }

        public bool TryPlayGroup(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
            {
                Debug.LogWarning("재생할 groupId가 비어 있습니다.");
                return false;
            }

            if (!_dialoguesByGroup.TryGetValue(groupId, out List<DialogueEntry> entries))
            {
                Debug.LogWarning($"Dialogue Group을 찾을 수 없습니다: {groupId}");
                return false;
            }

            StopCurrentDialogue();
            _currentDialogueRoutine = StartCoroutine(PlayDialogueGroupRoutine(groupId, entries));
            return true;
        }

        // 현재 재생 중인 대사와 음성을 중단하고 자막을 비활성화한다.
        public void Stop()
        {
            StopCurrentDialogue();
            HideSubtitle();
        }

        private void StopCurrentDialogue()
        {
            if (_currentDialogueRoutine != null)
            {
                StopCoroutine(_currentDialogueRoutine);
                _currentDialogueRoutine = null;
            }

            if (_audioSource != null)
            {
                _audioSource.Stop();
            }
        }

        // CSV의 callbackKey와 실제 실행할 메서드를 연결한다.
        public void RegisterCallback(string callbackKey, Action callback)
        {
            if (string.IsNullOrWhiteSpace(callbackKey) || callback == null)
            {
                return;
            }

            _callbackByKey[callbackKey] = callback;
        }

        // Dialogue.csv와 Dialogue_Text.csv의 내용을 메모리에 로드한다.
        private void LoadDialogueData()
        {
            _dialogueById.Clear();
            _dialoguesByGroup.Clear();
            _textById.Clear();

            TextAsset loadedDialogueCsv = _dialogueCsv != null
                ? _dialogueCsv
                : Resources.Load<TextAsset>("Dialogue");

            TextAsset loadedDialogueTextCsv = _dialogueTextCsv != null
                ? _dialogueTextCsv
                : Resources.Load<TextAsset>("Dialogue_Text");

            if (loadedDialogueCsv == null)
            {
                Debug.LogError("Dialogue.csv를 찾을 수 없습니다. Inspector에 할당하거나 Resources/Dialogue.csv에 배치하세요.");
                return;
            }

            if (loadedDialogueTextCsv == null)
            {
                Debug.LogError("Dialogue_Text.csv를 찾을 수 없습니다. Inspector에 할당하거나 Resources/Dialogue_Text.csv에 배치하세요.");
                return;
            }

            LoadDialogueCsv(loadedDialogueCsv.text);
            LoadDialogueTextCsv(loadedDialogueTextCsv.text);
        }

        // 단일 대사 데이터를 받아 자막, 음성, 콜백을 순서대로 처리한다.
        private IEnumerator PlayDialogueRoutine(DialogueEntry entry)
        {
            yield return PlayDialogueEntryRoutine(entry);

            if (ShouldAutoHideSubtitle(entry))
            {
                HideSubtitle();
            }
        }

        private IEnumerator PlayDialogueEntryRoutine(DialogueEntry entry)
        {
            string subtitle = GetDialogueText(entry.id);
            ShowSubtitle(entry.speaker, subtitle);
            InvokeCallback(entry.callbackKey);

            AudioClip clip = LoadAudioClip(entry.audioPath);
            float audioDuration = 0f;
            if (clip != null && _audioSource != null)
            {
                _audioSource.clip = clip;
                _audioSource.Play();
                audioDuration = clip.length;
            }
            else if (!string.IsNullOrWhiteSpace(entry.audioPath))
            {
                Debug.LogWarning($"오디오를 재생할 수 없습니다: {entry.audioPath}");
            }

            if (ShouldUseTypewriterEffect(subtitle))
            {
                yield return PlayTypewriterRoutine(audioDuration);
            }
            else if (audioDuration > 0f)
            {
                yield return new WaitForSeconds(audioDuration);
            }

            if (entry.delayAfterAudio.HasValue && entry.delayAfterAudio.Value > 0f)
            {
                yield return new WaitForSeconds(entry.delayAfterAudio.Value);
            }

        }

        // 여러 대사 데이터를 받아 순차적으로 재생한다.
        private IEnumerator PlayDialogueGroupRoutine(string groupId, List<DialogueEntry> entries)
        {
            DialogueEntry lastEntry = null;

            foreach (DialogueEntry entry in entries)
            {
                lastEntry = entry;
                yield return PlayDialogueEntryRoutine(entry);
            }

            if (lastEntry != null && ShouldAutoHideSubtitle(lastEntry))
            {
                HideSubtitle();
            }

            _currentDialogueRoutine = null;
            GroupCompleted?.Invoke(groupId);
        }

        // 대사 ID와 현재 언어 설정에 맞는 자막 문자열을 반환한다.
        private string GetDialogueText(string dialogueId)
        {
            if (_textById.TryGetValue(dialogueId, out string text))
            {
                return text;
            }

            Debug.LogWarning($"대사 텍스트를 찾을 수 없습니다: {dialogueId}, language: {_currentLanguage}");
            return string.Empty;
        }

        // audioPath 값에 기본 오디오 경로를 붙여 AudioClip을 로드한다.
        private AudioClip LoadAudioClip(string audioPath)
        {
            if (string.IsNullOrWhiteSpace(audioPath))
            {
                return null;
            }

            string path = $"{_audioBasePath}/{audioPath}";
            AudioClip clip = Resources.Load<AudioClip>(path);

            if (clip == null)
            {
                Debug.LogWarning($"AudioClip을 찾을 수 없습니다: Resources/{path}");
            }

            return clip;
        }

        // 화자와 대사 문자열을 UI에 표시하고 자막 오브젝트를 활성화한다.
        private void ShowSubtitle(string speaker, string text)
        {
            if (_subtitleRoot != null)
            {
                _subtitleRoot.SetActive(true);
            }

            if (_speakerText != null)
            {
                if (!string.IsNullOrWhiteSpace(speaker))
                {
                    if (!_speakerText.transform.parent.gameObject.activeSelf)
                    {
                        _speakerText.transform.parent.gameObject.SetActive(true);
                    }
                
                    _speakerText.text = speaker;
                }
                else
                {
                    _speakerText.transform.parent.gameObject.SetActive(false);
                }
            }

            if (_dialogueText != null)
            {
                _dialogueText.text = text;
                _dialogueText.maxVisibleCharacters = ShouldUseTypewriterEffect(text) ? 0 : int.MaxValue;
            }
        }

        // 자막 UI를 비활성화하고 표시 중인 문자열을 정리한다.
        private void HideSubtitle()
        {
            if (_speakerText != null)
            {
                _speakerText.text = string.Empty;
            }

            if (_dialogueText != null)
            {
                _dialogueText.text = string.Empty;
                _dialogueText.maxVisibleCharacters = int.MaxValue;
            }

            if (_subtitleRoot != null)
            {
                _subtitleRoot.SetActive(false);
            }
        }

        // callbackKey가 등록되어 있으면 연결된 메서드를 실행한다.
        private void InvokeCallback(string callbackKey)
        {
            if (string.IsNullOrWhiteSpace(callbackKey))
            {
                return;
            }

            if (_callbackByKey.TryGetValue(callbackKey, out Action callback))
            {
                callback.Invoke();
                return;
            }

            Debug.LogWarning($"등록되지 않은 callbackKey입니다: {callbackKey}");
        }

        private bool ShouldUseTypewriterEffect(string text)
        {
            return _useTypewriterEffect && _characterInterval > 0f && !string.IsNullOrEmpty(text) && _dialogueText != null;
        }

        private bool ShouldAutoHideSubtitle(DialogueEntry entry)
        {
            return entry.delayAfterAudio.HasValue;
        }

        private IEnumerator PlayTypewriterRoutine(float minimumDuration)
        {
            _dialogueText.maxVisibleCharacters = 0;
            _dialogueText.ForceMeshUpdate();

            int characterCount = _dialogueText.textInfo.characterCount;
            float elapsed = 0f;

            for (int i = 1; i <= characterCount; i++)
            {
                yield return new WaitForSeconds(_characterInterval);
                elapsed += _characterInterval;
                _dialogueText.maxVisibleCharacters = i;
            }

            if (minimumDuration > elapsed)
            {
                yield return new WaitForSeconds(minimumDuration - elapsed);
            }
        }

        // Dialogue.csv 문자열을 읽어 대사 데이터와 그룹 데이터를 구성한다.
        private void LoadDialogueCsv(string csvText)
        {
            List<string[]> rows = ParseCsv(csvText);
            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (row.Length == 0 || string.IsNullOrWhiteSpace(GetCell(row, 0)))
                {
                    continue;
                }

                DialogueEntry entry = new()
                {
                    id = GetCell(row, 0),
                    group = GetCell(row, 1),
                    order = ParseInt(GetCell(row, 2)),
                    speaker = GetCell(row, 3),
                    audioPath = GetCell(row, 4),
                    callbackKey = GetCell(row, 5),
                    delayAfterAudio = ParseOptionalFloat(GetCell(row, 6))
                };

                _dialogueById[entry.id] = entry;

                if (!string.IsNullOrWhiteSpace(entry.group))
                {
                    if (!_dialoguesByGroup.TryGetValue(entry.group, out List<DialogueEntry> groupEntries))
                    {
                        groupEntries = new List<DialogueEntry>();
                        _dialoguesByGroup[entry.group] = groupEntries;
                    }

                    groupEntries.Add(entry);
                }
            }

            foreach (List<DialogueEntry> groupEntries in _dialoguesByGroup.Values)
            {
                groupEntries.Sort((a, b) => a.order.CompareTo(b.order));
            }
        }

        // Dialogue_Text.csv 문자열을 읽어 현재 언어에 맞는 자막 데이터를 구성한다.
        private void LoadDialogueTextCsv(string csvText)
        {
            List<string[]> rows = ParseCsv(csvText);
            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                string id = GetCell(row, 0);
                string language = GetCell(row, 1);
                string text = GetCell(row, 2);

                if (string.IsNullOrWhiteSpace(id) || language != _currentLanguage)
                {
                    continue;
                }

                _textById[id] = text;
            }
        }

        // CSV 문자열을 쉼표, 줄바꿈, 따옴표 규칙에 맞춰 행과 열로 분리한다.
        private List<string[]> ParseCsv(string csvText)
        {
            List<string[]> rows = new();
            List<string> currentRow = new();
            string currentCell = string.Empty;
            bool isInsideQuote = false;

            for (int i = 0; i < csvText.Length; i++)
            {
                char current = csvText[i];

                if (current == '"')
                {
                    if (isInsideQuote && i + 1 < csvText.Length && csvText[i + 1] == '"')
                    {
                        currentCell += '"';
                        i++;
                    }
                    else
                    {
                        isInsideQuote = !isInsideQuote;
                    }
                }
                else if (current == ',' && !isInsideQuote)
                {
                    currentRow.Add(currentCell);
                    currentCell = string.Empty;
                }
                else if ((current == '\n' || current == '\r') && !isInsideQuote)
                {
                    if (current == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                    {
                        i++;
                    }

                    currentRow.Add(currentCell);
                    rows.Add(currentRow.ToArray());
                    currentRow.Clear();
                    currentCell = string.Empty;
                }
                else
                {
                    currentCell += current;
                }
            }

            if (currentCell.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(currentCell);
                rows.Add(currentRow.ToArray());
            }

            return rows;
        }

        // CSV 행에서 지정한 인덱스의 값을 안전하게 가져온다.
        private string GetCell(string[] row, int index)
        {
            if (index < 0 || index >= row.Length)
            {
                return string.Empty;
            }

            return row[index].Trim();
        }

        // 문자열을 int로 변환하고 실패하면 0을 반환한다.
        private int ParseInt(string value)
        {
            return int.TryParse(value, out int result) ? result : 0;
        }

        // 문자열을 float으로 변환하고 빈 값이면 null, 실패하면 0을 반환한다.
        private float? ParseOptionalFloat(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return float.TryParse(value, out float result) ? result : 0f;
        }
    }
}
