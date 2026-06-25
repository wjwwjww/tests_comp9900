using UnityEngine;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class EmulsifierSnapPoint : MonoBehaviour
{
    private enum ZoneType { Hydrophobic, Hydrophilic }


    [SerializeField] private Emulsifier emulsifier;
    [SerializeField] private ZoneType zoneType;
    [SerializeField] private float snapRadius = 0.0125f;
    [SerializeField] float attractionForce = 3f;

    [Header("Snap VFX")]
    [SerializeField] private ParticleSystem successfulSnapSparklePrefab;

    private bool isOccupied = false;
    private HashSet<Rigidbody> attractedMolecules = new HashSet<Rigidbody>();
    private readonly Dictionary<Rigidbody, Collider> _colliderCache = new Dictionary<Rigidbody, Collider>();
    private readonly List<Rigidbody> _staleBodies = new List<Rigidbody>();
    private float _throttleAccumulator;
    private const string lipidString = "Lipid";
    private const string waterString = "Water";

    private void FixedUpdate()
    {
        if (isOccupied || attractedMolecules.Count == 0) return;

        PerformanceManager pm = PerformanceManager.Instance;
        if (pm != null && !pm.ShouldRunThrottled(ref _throttleAccumulator))
        {
            return;
        }

        _staleBodies.Clear();

        foreach (Rigidbody mol in attractedMolecules)
        {
            if (mol == null)
            {
                _staleBodies.Add(mol);
                continue;
            }

            if (!_colliderCache.TryGetValue(mol, out Collider molCollider) || molCollider == null)
            {
                molCollider = mol.GetComponent<Collider>();
                _colliderCache[mol] = molCollider;
            }

            if (molCollider == null)
            {
                _staleBodies.Add(mol);
                continue;
            }

            Vector3 direction = (transform.position - mol.transform.position).normalized;
            mol.AddForce(direction * attractionForce, ForceMode.Acceleration);

            Vector3 closestPoint = molCollider.ClosestPoint(transform.position);
            float dist = Vector3.Distance(transform.position, closestPoint);

            if (dist <= snapRadius)
            {
                Snap(mol);
                return;
            }
        }

        foreach (Rigidbody stale in _staleBodies)
        {
            attractedMolecules.Remove(stale);
            _colliderCache.Remove(stale);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOccupied || !IsCompatible(other)) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        _colliderCache[rb] = other;

        Vector3 closestPoint = other.ClosestPoint(transform.position);
        float dist = Vector3.Distance(transform.position, closestPoint);

        if (dist <= snapRadius)
        {
            Snap(rb);
        }
        else
        {
            attractedMolecules.Add(rb);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isOccupied) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            attractedMolecules.Remove(rb);
            _colliderCache.Remove(rb);
        }
    }

    private void Snap(Rigidbody rb)
    {
        EmitSuccessfulSnapSparkle();

        ReactionSO emulsifierSnapSO = emulsifier.GetEmulsifierSnapReactionSO();
        emulsifierSnapSO.Position = transform.position;
        emulsifierSnapSO.Participants = new[] { gameObject, rb.gameObject };
        ReactionEvents.Raise(emulsifierSnapSO);

        isOccupied = true;
        attractedMolecules.Clear();
        _colliderCache.Clear();

        // if phsyics is enabled during the snap process it ends up pushing it around a bit
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        rb.transform.position = transform.position;
        rb.transform.rotation = transform.rotation;
        rb.transform.SetParent(transform);

        Destroy(rb.GetComponent<HandGrabInteractable>());
        Destroy(rb.GetComponent<GrabInteractable>());
        Destroy(rb.GetComponent<Grabbable>());
        Destroy(rb.GetComponent<OneGrabFreeTransformer>());
        Destroy(rb.GetComponent<Outline>());

        if (rb.CompareTag(waterString))
            Destroy(rb.GetComponent<Water>());
        else
            Destroy(rb.GetComponent<Lipid>());

        Destroy(rb);

    }

    private void EmitSuccessfulSnapSparkle()
    {
        if (successfulSnapSparklePrefab == null) return;

        ParticlePoolManager.TryPlayFromPool(successfulSnapSparklePrefab, transform.position, transform.rotation);
    }

    private bool IsCompatible(Collider other)
    {
        return zoneType == ZoneType.Hydrophobic && other.CompareTag(lipidString) || zoneType == ZoneType.Hydrophilic && other.CompareTag(waterString);
    }
}
