using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ReactionLabelController : MonoBehaviour
{
    public static ReactionLabelController Instance { get; private set; }

    [Header("Label Settings")]
    [SerializeField] private GameObject reactionLabelPrefab;
    [SerializeField] private float spawnDelaySeconds = 0.5f;
    [SerializeField] private float autoDismissSeconds = 5f;
    [SerializeField] private float labelHeightOffset = 0.15f;

    [Header("Pool")]
    [SerializeField] private int poolSize = 8;

    [SerializeField] private float reactionCooldown = 1.5f;
    private readonly Dictionary<ReactionType, float> _lastReactionTime = new Dictionary<ReactionType, float>();

    private readonly Queue<GameObject> _pool = new Queue<GameObject>();
    private readonly Dictionary<GameObject, Coroutine> _dismissCoroutines = new Dictionary<GameObject, Coroutine>();
    private readonly List<(GameObject label, Vector3 basePosition)> _activeLabels = new List<(GameObject, Vector3)>();

    private const float OverlapSeparation = 0.12f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        PrewarmPool();
    }

    private void OnEnable() => ReactionEvents.Occurred += OnReactionOccurred;
    private void OnDisable() => ReactionEvents.Occurred -= OnReactionOccurred;

    private void OnReactionOccurred(ReactionSO evt)
    {
        // Check cooldown
        if (_lastReactionTime.TryGetValue(evt.Type, out float lastTime))
        {
            if (Time.time - lastTime < reactionCooldown) return;
        }
        _lastReactionTime[evt.Type] = Time.time;

        StartCoroutine(SpawnLabelDelayed(evt));
    }

    private IEnumerator SpawnLabelDelayed(ReactionSO evt)
    {
        yield return new WaitForSeconds(spawnDelaySeconds);

        if (reactionLabelPrefab == null) yield break;

        string line1 = evt.ReactionName;
        string line2 = evt.ReactionDescription;
        Vector3 spawnPos = evt.Position + Vector3.up * labelHeightOffset;
        spawnPos = ResolveOverlap(spawnPos);

        GameObject label = RentFromPool();
        label.transform.position = spawnPos;
        label.SetActive(true);

        TextMeshProUGUI[] textFields = label.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (textFields.Length >= 1) textFields[0].text = line1;
        if (textFields.Length >= 2) textFields[1].text = line2;

        TextMeshProUGUI timerText = null;
        if (textFields.Length >= 3) timerText = textFields[2];

        ReactionLabelDismiss dismisser = label.GetComponent<ReactionLabelDismiss>();
        if (dismisser != null) dismisser.Init(this);

        _activeLabels.Add((label, spawnPos));
        _dismissCoroutines[label] = StartCoroutine(AutoDismiss(label, autoDismissSeconds, timerText));
    }

    public void DismissLabel(GameObject label)
    {
        if (_dismissCoroutines.TryGetValue(label, out Coroutine c))
        {
            StopCoroutine(c);
            _dismissCoroutines.Remove(label);
        }
        ReturnToPool(label);
    }

    private IEnumerator AutoDismiss(GameObject label, float delay, TextMeshProUGUI timerText)
    {
        float remaining = delay;
        while (remaining > 0f)
        {
            if (timerText != null)
                timerText.text = $"poke to dismiss  {Mathf.CeilToInt(remaining)}s";
            remaining -= Time.deltaTime;
            yield return null;
        }

        if (label != null && label.activeSelf)
            ReturnToPool(label);
        _dismissCoroutines.Remove(label);
    }

    private void PrewarmPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            if (reactionLabelPrefab == null) break;
            GameObject go = Instantiate(reactionLabelPrefab, transform);
            go.SetActive(false);
            _pool.Enqueue(go);
        }
    }

    private GameObject RentFromPool()
    {
        if (_pool.Count > 0) return _pool.Dequeue();
        return Instantiate(reactionLabelPrefab, transform);
    }

    private void ReturnToPool(GameObject label)
    {
        _activeLabels.RemoveAll(entry => entry.label == label);
        label.SetActive(false);
        label.transform.SetParent(transform);
        _pool.Enqueue(label);
    }

    private Vector3 ResolveOverlap(Vector3 candidate)
    {
        bool conflict = true;
        int iterations = 0;
        while (conflict && iterations < 10)
        {
            conflict = false;
            foreach (var entry in _activeLabels)
            {
                if (Vector3.Distance(entry.basePosition, candidate) < OverlapSeparation)
                {
                    candidate.y += OverlapSeparation;
                    conflict = true;
                    break;
                }
            }
            iterations++;
        }
        return candidate;
    }
}
