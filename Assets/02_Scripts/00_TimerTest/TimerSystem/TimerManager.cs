using System.Collections.Generic;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    [Header("Timer Variable")]
    [SerializeField] private float _timer = 60.0f;
    [SerializeField] private float _currentTimer = 0.0f;
    [SerializeField] private bool _isTimerRunning = false;

    [Header("Score")]
    [SerializeField] private int _score = 0;

    [Header("Vulnerabilities")]
    [SerializeField] private VulnerabilityObject[] _vulnerabilities;

    [Header("Appeared Count")]
    [SerializeField] private int _multiTabAppeared = 0;
    [SerializeField] private int _leverAppeared = 0;
    [SerializeField] private int _objectMoveAppeared = 0;
    [SerializeField] private int _turnOffAppeared = 0;

    [Header("Runtime Groups")]
    [SerializeField] private VulnerabilityObject[] _multiTabs;
    [SerializeField] private VulnerabilityObject[] _levers;
    [SerializeField] private VulnerabilityObject[] _objectMoves;
    [SerializeField] private VulnerabilityObject[] _turnOffs;

    [Header("Runtime")]
    [SerializeField] private int[] _randomSituationNumbers;
    [SerializeField] private VulnerabilityObject[] _activeVulnerabilities;
    [SerializeField] private string[] _resultKeys;

    public float CurrentTimer => _currentTimer;
    public bool IsTimerRunning => _isTimerRunning;
    public int Score => _score;
    public IReadOnlyList<int> RandomSituationNumbers => _randomSituationNumbers;
    public IReadOnlyList<VulnerabilityObject> ActiveVulnerabilities => _activeVulnerabilities;
    public IReadOnlyList<string> ResultKeys => _resultKeys;

    private void Awake()
    {
        RefreshVulnerabilities();
        DeactivateAll();
    }

    private void Update()
    {
        if (!_isTimerRunning)
        {
            return;
        }

        _currentTimer -= Time.deltaTime;

        if (_currentTimer > 0.0f)
        {
            return;
        }

        _currentTimer = 0.0f;
        _isTimerRunning = false;
        CalculateScore();
        CollectResultKeys();
    }

    public void GetRandomSituationNumber()
    {
        List<VulnerabilityObject> selectedVulnerabilities = new List<VulnerabilityObject>();
        List<int> selectedNumbers = new List<int>();

        AddRandomVulnerabilities(_multiTabs, _multiTabAppeared, selectedVulnerabilities, selectedNumbers);
        AddRandomVulnerabilities(_levers, _leverAppeared, selectedVulnerabilities, selectedNumbers);
        AddRandomVulnerabilities(_objectMoves, _objectMoveAppeared, selectedVulnerabilities, selectedNumbers);
        AddRandomVulnerabilities(_turnOffs, _turnOffAppeared, selectedVulnerabilities, selectedNumbers);

        _activeVulnerabilities = selectedVulnerabilities.ToArray();
        _randomSituationNumbers = selectedNumbers.ToArray();
    }

    public void RefreshVulnerabilities()
    {
        SplitVulnerabilitiesByType();
    }

    public void ActivateTimer()
    {
        if (_vulnerabilities == null || _vulnerabilities.Length == 0)
        {
            Debug.LogWarning("There are no vulnerabilities to activate.", this);
            return;
        }

        if (!HasRuntimeGroups())
        {
            RefreshVulnerabilities();
        }

        DeactivateAll();
        GetRandomSituationNumber();

        for (int i = 0; i < _activeVulnerabilities.Length; i++)
        {
            VulnerabilityObject vulnerability = _activeVulnerabilities[i];

            if (vulnerability == null)
            {
                continue;
            }

            vulnerability.gameObject.SetActive(true);
            vulnerability.RestartVulnerability();
        }

        _currentTimer = _timer;
        _isTimerRunning = _currentTimer > 0.0f;

        if (!_isTimerRunning)
        {
            CalculateScore();
            CollectResultKeys();
        }
    }

    private void CalculateScore()
    {
        if (_activeVulnerabilities == null)
        {
            return;
        }

        for (int i = 0; i < _activeVulnerabilities.Length; i++)
        {
            VulnerabilityObject vulnerability = _activeVulnerabilities[i];

            if (vulnerability == null)
            {
                continue;
            }

            if (vulnerability.IsFixed)
            {
                _score -= 1;
                continue;
            }

            if (vulnerability.IsResolved)
            {
                continue;
            }

            if (vulnerability.IsFailed)
            {
                _score -= 2;
                continue;
            }

            if (vulnerability.IsActive)
            {
                _score -= 2;
            }
        }
    }

    private void CollectResultKeys()
    {
        if (_activeVulnerabilities == null)
        {
            _resultKeys = new string[0];
            return;
        }

        List<string> resultKeys = new List<string>(_activeVulnerabilities.Length);

        for (int i = 0; i < _activeVulnerabilities.Length; i++)
        {
            VulnerabilityObject vulnerability = _activeVulnerabilities[i];

            if (vulnerability != null)
            {
                resultKeys.Add(vulnerability.GetResultKey());
            }
        }

        _resultKeys = resultKeys.ToArray();
    }

    private void SplitVulnerabilitiesByType()
    {
        List<VulnerabilityObject> multiTabs = new List<VulnerabilityObject>();
        List<VulnerabilityObject> levers = new List<VulnerabilityObject>();
        List<VulnerabilityObject> objectMoves = new List<VulnerabilityObject>();
        List<VulnerabilityObject> turnOffs = new List<VulnerabilityObject>();

        if (_vulnerabilities != null)
        {
            for (int i = 0; i < _vulnerabilities.Length; i++)
            {
                VulnerabilityObject vulnerability = _vulnerabilities[i];

                if (vulnerability == null)
                {
                    continue;
                }

                switch (vulnerability.VulnerabilityType)
                {
                    case VulnerabilityType.MultiTab:
                        multiTabs.Add(vulnerability);
                        break;
                    case VulnerabilityType.Lever:
                        levers.Add(vulnerability);
                        break;
                    case VulnerabilityType.ObjectMove:
                        objectMoves.Add(vulnerability);
                        break;
                    case VulnerabilityType.TurnOff:
                        turnOffs.Add(vulnerability);
                        break;
                }
            }
        }

        _multiTabs = multiTabs.ToArray();
        _levers = levers.ToArray();
        _objectMoves = objectMoves.ToArray();
        _turnOffs = turnOffs.ToArray();
    }

    private bool HasRuntimeGroups()
    {
        return HasAny(_multiTabs)
            || HasAny(_levers)
            || HasAny(_objectMoves)
            || HasAny(_turnOffs);
    }

    private bool HasAny(VulnerabilityObject[] vulnerabilities)
    {
        return vulnerabilities != null && vulnerabilities.Length > 0;
    }

    private void AddRandomVulnerabilities(
        VulnerabilityObject[] source,
        int appearedCount,
        List<VulnerabilityObject> selectedVulnerabilities,
        List<int> selectedNumbers)
    {
        if (source == null || source.Length == 0)
        {
            return;
        }

        int activeCount = Mathf.Clamp(appearedCount, 0, source.Length);
        List<int> availableNumbers = new List<int>(source.Length);

        for (int i = 0; i < source.Length; i++)
        {
            availableNumbers.Add(i);
        }

        for (int i = 0; i < activeCount; i++)
        {
            int randomIndex = Random.Range(0, availableNumbers.Count);
            int selectedIndex = availableNumbers[randomIndex];
            VulnerabilityObject selectedVulnerability = source[selectedIndex];

            selectedVulnerabilities.Add(selectedVulnerability);
            selectedNumbers.Add(GetOriginalIndex(selectedVulnerability));
            availableNumbers.RemoveAt(randomIndex);
        }
    }

    private int GetOriginalIndex(VulnerabilityObject vulnerability)
    {
        if (_vulnerabilities == null)
        {
            return -1;
        }

        for (int i = 0; i < _vulnerabilities.Length; i++)
        {
            if (_vulnerabilities[i] == vulnerability)
            {
                return i;
            }
        }

        return -1;
    }

    private void DeactivateAll()
    {
        if (_vulnerabilities == null)
        {
            return;
        }

        for (int i = 0; i < _vulnerabilities.Length; i++)
        {
            if (_vulnerabilities[i] != null)
            {
                _vulnerabilities[i].gameObject.SetActive(false);
            }
        }
    }
}
