using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HeatZone : Zone
{
    [SerializeField] private float denatureDuration = 2f;

    private readonly Dictionary<Protein, float> _proteinsInZone = new Dictionary<Protein, float>();
    private readonly List<Protein> _toRemove = new List<Protein>();
    private readonly List<Protein> _proteinIterationBuffer = new List<Protein>();
    private float _throttleAccumulator;
    private const float threshold = 100f;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Update()
    {
        if (_proteinsInZone.Count == 0) return;

        PerformanceManager pm = PerformanceManager.Instance;
        float elapsed = Time.deltaTime;
        if (pm != null)
        {
            _throttleAccumulator += Time.deltaTime;
            float interval = Mathf.Max(0f, pm.ActiveTriggerInterval);
            if (interval > 0f && _throttleAccumulator < interval)
            {
                return;
            }
            elapsed = _throttleAccumulator;
            _throttleAccumulator = 0f;
        }

        float weightIncreasePerSecond = threshold / denatureDuration;

        _toRemove.Clear();
        _proteinIterationBuffer.Clear();
        foreach (Protein p in _proteinsInZone.Keys)
        {
            _proteinIterationBuffer.Add(p);
        }

        for (int i = 0; i < _proteinIterationBuffer.Count; i++)
        {
            Protein protein = _proteinIterationBuffer[i];
            float currentWeight = _proteinsInZone[protein];

            if (currentWeight >= threshold)
            {
                _toRemove.Add(protein);
                continue;
            }

            float newWeight = Mathf.Min(currentWeight + weightIncreasePerSecond * elapsed, threshold);
            _proteinsInZone[protein] = newWeight;
            protein.SetDenatureWeight(newWeight);

            if (newWeight >= threshold)
            {
                protein.Denature();
                _toRemove.Add(protein);
            }
        }

        for (int i = 0; i < _toRemove.Count; i++)
        {
            _proteinsInZone.Remove(_toRemove[i]);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Protein protein = other.GetComponentInParent<Protein>();
        if (protein == null) protein = other.GetComponent<Protein>();

        if (protein != null && !protein.IsDenatured() && !_proteinsInZone.ContainsKey(protein))
        {
            _proteinsInZone.Add(protein, protein.GetDenatureWeight());
        }

        PolysaccharideGelatinization gel = other.GetComponentInParent<PolysaccharideGelatinization>();
        if (gel == null) gel = other.GetComponent<PolysaccharideGelatinization>();

        if (gel != null)
            gel.OnHeatZoneEnter();
    }

    void OnTriggerExit(Collider other)
    {
        Protein protein = other.GetComponentInParent<Protein>();
        if (protein == null) protein = other.GetComponent<Protein>();

        if (protein != null)
        {
            _proteinsInZone.Remove(protein);
        }
    }
}
