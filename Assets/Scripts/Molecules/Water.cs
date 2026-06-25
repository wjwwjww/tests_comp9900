using UnityEngine;
using UnityEngine.Video;

public class Water : Molecule
{
    [SerializeField] private float repelForce = 5f;
    private readonly System.Collections.Generic.Dictionary<Collider, Rigidbody> _rbCache = new System.Collections.Generic.Dictionary<Collider, Rigidbody>();
    private float _throttleAccumulator;
    private const string lipidString = "Lipid";

    protected override void Awake()
    {
        base.Awake();
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(lipidString)) return;

        PerformanceManager pm = PerformanceManager.Instance;
        if (pm != null && !pm.ShouldRunThrottled(ref _throttleAccumulator))
        {
            return;
        }

        Repel(other);
    }

    private void Repel(Collider other)
    {
        if (!_rbCache.TryGetValue(other, out Rigidbody lipidRb) || lipidRb == null)
        {
            lipidRb = other.attachedRigidbody != null ? other.attachedRigidbody : other.GetComponent<Rigidbody>();
            _rbCache[other] = lipidRb;
        }

        if (lipidRb == null) return;

        Vector3 repelDirection = other.transform.position - transform.position;
        lipidRb.AddForce(repelDirection.normalized * repelForce);
    }

    private void OnTriggerExit(Collider other)
    {
        _rbCache.Remove(other);
    }
}