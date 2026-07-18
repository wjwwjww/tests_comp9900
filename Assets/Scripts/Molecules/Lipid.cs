using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

public class Lipid : Molecule
{
    private Vector3 currentScale;
    private bool isMerging;
    [SerializeField] private ReactionSO lipidMergeSO;

    [Header("Oxidation")]
    [SerializeField] private OxidisedLipid oxidisedLipidPrefab;
    [SerializeField] private ReactionSO lipidOxidationSO;
    [SerializeField] private float normalOxidationDuration = 10f;
    [SerializeField] private float heatedOxidationDuration = 3f;

    [Header("Oxidation Visual")]
    [SerializeField] private MeshRenderer lipidRenderer;
    [SerializeField] private Material oxidisedLipidMaterial;

    private float oxidationProgress;
    private bool oxidationCompleted;
    private bool warnedInvalidOxidationDuration;
    private bool warnedInvalidHeatedOxidationDuration;
    private bool warnedOxidationVisual;
    private bool hasOxidationColourProperty;
    private int oxidationColourPropertyId;
    private Color freshLipidColour;
    private Color oxidisedLipidColour;
    private Material originalSharedMaterial;
    private Material runtimeLipidMaterial;
    private readonly HashSet<Collider> activeHeatZoneColliders = new HashSet<Collider>();
    private readonly List<Collider> staleHeatZoneColliders = new List<Collider>();

    private const string lipidString = "Lipid";
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    protected override void Awake()
    {
        base.Awake();
        lipidMergeSO.Source = gameObject;
        InitialiseOxidationVisual();
    }

    void Start()
    {
        currentScale = transform.localScale;
    }

    private void Update()
    {
        if (oxidationCompleted || isMerging) return;

        float duration = IsInHeatZone()
            ? GetHeatedOxidationDuration()
            : GetNormalOxidationDuration();
        oxidationProgress = Mathf.Clamp01(oxidationProgress + Time.deltaTime / duration);
        UpdateOxidationVisual();

        if (oxidationProgress >= 1f)
        {
            CompleteOxidation();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        TrackHeatZone(other);

        if (isMerging) return;

        if (!other.CompareTag(lipidString)) return;

        Lipid otherLipid = other.GetComponent<Lipid>();
        if (otherLipid == null) return;
        if (otherLipid == this || otherLipid.isMerging) return;

        if (GetInstanceID() < otherLipid.GetInstanceID())
        {
            Merge(otherLipid);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        TrackHeatZone(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != null)
        {
            activeHeatZoneColliders.Remove(other);
        }
    }

    private void OnDisable()
    {
        activeHeatZoneColliders.Clear();
        staleHeatZoneColliders.Clear();
    }

    private void OnDestroy()
    {
        if (runtimeLipidMaterial == null) return;

        if (lipidRenderer != null && lipidRenderer.sharedMaterial == runtimeLipidMaterial)
        {
            lipidRenderer.sharedMaterial = originalSharedMaterial;
        }

        if (Application.isPlaying)
        {
            Destroy(runtimeLipidMaterial);
        }
        else
        {
            DestroyImmediate(runtimeLipidMaterial);
        }

        runtimeLipidMaterial = null;
    }

    private void Merge(Lipid other)
    {
        if (other == null || other == this) return;
        if (isMerging || other.isMerging) return;

        isMerging = true;
        other.isMerging = true;

        StartCoroutine(MergeAnimation(other));
    }

    private void InitialiseOxidationVisual()
    {
        if (lipidRenderer == null)
        {
            lipidRenderer = GetComponentInChildren<MeshRenderer>();
        }

        if (lipidRenderer == null || lipidRenderer.sharedMaterial == null)
        {
            WarnOxidationVisual("Lipid oxidation visual is missing a renderer or fresh material.");
            return;
        }

        originalSharedMaterial = lipidRenderer.sharedMaterial;
        runtimeLipidMaterial = new Material(originalSharedMaterial);
        lipidRenderer.sharedMaterial = runtimeLipidMaterial;

        if (runtimeLipidMaterial.HasProperty(BaseColorId))
        {
            oxidationColourPropertyId = BaseColorId;
        }
        else if (runtimeLipidMaterial.HasProperty(ColorId))
        {
            oxidationColourPropertyId = ColorId;
        }
        else
        {
            WarnOxidationVisual("Lipid oxidation visual material has no supported colour property.");
            return;
        }

        hasOxidationColourProperty = true;
        freshLipidColour = runtimeLipidMaterial.GetColor(oxidationColourPropertyId);
        oxidisedLipidColour = GetOxidisedLipidColour();
    }

    private Color GetOxidisedLipidColour()
    {
        if (oxidisedLipidMaterial != null && oxidisedLipidMaterial.HasProperty(oxidationColourPropertyId))
        {
            return oxidisedLipidMaterial.GetColor(oxidationColourPropertyId);
        }

        WarnOxidationVisual("Lipid oxidation visual is missing an oxidised target material with a supported colour property.");
        return freshLipidColour;
    }

    private void UpdateOxidationVisual()
    {
        if (runtimeLipidMaterial == null || !hasOxidationColourProperty) return;

        Color oxidationColour = Color.Lerp(freshLipidColour, oxidisedLipidColour, oxidationProgress);
        runtimeLipidMaterial.SetColor(oxidationColourPropertyId, oxidationColour);
    }

    private void WarnOxidationVisual(string message)
    {
        if (warnedOxidationVisual) return;

        warnedOxidationVisual = true;
        Debug.LogWarning(message, this);
    }

    private float GetNormalOxidationDuration()
    {
        const float minimumDuration = 0.01f;

        if (normalOxidationDuration >= minimumDuration)
        {
            return normalOxidationDuration;
        }

        if (!warnedInvalidOxidationDuration)
        {
            warnedInvalidOxidationDuration = true;
            Debug.LogWarning("Lipid normal oxidation duration must be greater than zero. Using 0.01 seconds.", this);
        }

        return minimumDuration;
    }

    private float GetHeatedOxidationDuration()
    {
        const float minimumDuration = 0.01f;

        if (heatedOxidationDuration >= minimumDuration)
        {
            return heatedOxidationDuration;
        }

        if (!warnedInvalidHeatedOxidationDuration)
        {
            warnedInvalidHeatedOxidationDuration = true;
            Debug.LogWarning("Lipid heated oxidation duration must be greater than zero. Using 0.01 seconds.", this);
        }

        return minimumDuration;
    }

    private void TrackHeatZone(Collider other)
    {
        HeatZone heatZone = GetHeatZone(other);
        if (heatZone != null && heatZone.isActiveAndEnabled)
        {
            activeHeatZoneColliders.Add(other);
        }
    }

    private bool IsInHeatZone()
    {
        if (activeHeatZoneColliders.Count == 0)
        {
            return false;
        }

        staleHeatZoneColliders.Clear();

        foreach (Collider collider in activeHeatZoneColliders)
        {
            if (!IsActiveHeatZoneCollider(collider))
            {
                staleHeatZoneColliders.Add(collider);
            }
        }

        for (int i = 0; i < staleHeatZoneColliders.Count; i++)
        {
            activeHeatZoneColliders.Remove(staleHeatZoneColliders[i]);
        }

        return activeHeatZoneColliders.Count > 0;
    }

    private static bool IsActiveHeatZoneCollider(Collider collider)
    {
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
        {
            return false;
        }

        HeatZone heatZone = GetHeatZone(collider);
        return heatZone != null && heatZone.isActiveAndEnabled;
    }

    private static HeatZone GetHeatZone(Collider other)
    {
        if (other == null) return null;

        HeatZone heatZone = other.GetComponentInParent<HeatZone>();
        if (heatZone != null) return heatZone;

        Rigidbody attachedRigidbody = other.attachedRigidbody;
        return attachedRigidbody != null
            ? attachedRigidbody.GetComponentInParent<HeatZone>()
            : null;
    }

    private void CompleteOxidation()
    {
        if (oxidationCompleted) return;

        oxidationCompleted = true;

        if (oxidisedLipidPrefab == null)
        {
            Debug.LogError("Lipid is missing an Oxidised Lipid prefab. Oxidation cannot complete.", this);
            return;
        }

        if (lipidOxidationSO == null)
        {
            Debug.LogError("Lipid is missing a Lipid Oxidation ReactionSO. Oxidation cannot complete.", this);
            return;
        }

        Vector3 productPosition = transform.position;
        Quaternion productRotation = transform.rotation;
        Vector3 productScale = transform.lossyScale;

        OxidisedLipid product = Instantiate(oxidisedLipidPrefab, productPosition, productRotation);
        if (product == null)
        {
            Debug.LogError("Lipid could not instantiate the Oxidised Lipid product.", this);
            return;
        }

        product.transform.localScale = productScale;
        StateManager stateManager = StateManager.Instance;
        stateManager?.RegisterMolecule(product.gameObject);

        lipidOxidationSO.Position = product.transform.position;
        lipidOxidationSO.Source = product.gameObject;
        lipidOxidationSO.Participants = new[] { product.gameObject };
        ReactionEvents.Raise(lipidOxidationSO);

        stateManager?.UnregisterMolecule(gameObject);
        Destroy(gameObject);
    }

    private IEnumerator MergeAnimation(Lipid other)
    {
        if (other == null)
        {
            isMerging = false;
            yield break;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        Rigidbody otherRb = other.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        if (otherRb != null) otherRb.isKinematic = true;

        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale + other.transform.localScale;

        Vector3 otherStartPos = other.transform.position;
        Vector3 otherStartScale = other.transform.localScale;
        Vector3 targetPos = (transform.position + other.transform.position) * 0.5f;

        float duration = 0.3f;
        float t = 0f;

        while (t < duration)
        {
            if (other == null)
            {
                if (rb != null) rb.isKinematic = false;
                isMerging = false;
                yield break;
            }

            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - k, 3f); // ease-out

            transform.position = Vector3.Lerp(transform.position, targetPos, eased);
            transform.localScale = Vector3.Lerp(startScale, targetScale, eased);

            other.transform.position = Vector3.Lerp(otherStartPos, transform.position, eased);
            other.transform.localScale = Vector3.Lerp(otherStartScale, Vector3.zero, eased);

            yield return null;
        }

        currentScale = targetScale;
        transform.localScale = currentScale;
        if (rb != null) rb.isKinematic = false;

        if (other == null)
        {
            isMerging = false;
            yield break;
        }

        lipidMergeSO.Participants = new[] { gameObject, other.gameObject };
        lipidMergeSO.Position = (transform.position + other.transform.position) * 0.5f;
        ReactionEvents.Raise(lipidMergeSO);

        Destroy(other.gameObject);
        isMerging = false;
    }
}
