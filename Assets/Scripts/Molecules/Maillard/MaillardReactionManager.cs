using System.Collections.Generic;
using UnityEngine;

public class MaillardReactionManager : MonoBehaviour
{
    [Header("Reaction Data")]
    [SerializeField] private ReactionSO maillardSO;
    [Header("Product")]
    [SerializeField] private GameObject maillardProductPrefab;
    [Header("VFX")]
    [SerializeField] private ParticleSystem smokePrefab;
    [Header("Reaction Behaviour")]
    [SerializeField] private bool hideReactantsAfterReaction = true;
    [Header("Detection Settings")]
#pragma warning disable 0414
    [HideInInspector] [SerializeField] private float detectionRadius = 2f;
    [SerializeField] private float scanInterval = 0.2f;
#pragma warning restore 0414
    [SerializeField] private float heatContactTolerance = 0.05f;
    [SerializeField] private float heatSurfaceHorizontalTolerance = 0.05f;
    [SerializeField] private float heatSurfaceVerticalReach = 3f;
    [SerializeField] private float placementSurfaceTolerance = 0.08f;
    [SerializeField] private float maillardPlacementDelaySeconds = 2f;
    [Header("Product Appearance")]
    [SerializeField] private float productScaleMultiplier = 0.6f;
    [SerializeField] private Color productColor = new Color(0.34f, 0.17f, 0.07f);
    [SerializeField] private float productSmoothness = 0.04f;
    [SerializeField] private bool addProductSurfaceBumps = true;
    [SerializeField] private int productSurfaceBumpCount = 18;
    [SerializeField] private float productSurfaceBumpScale = 0.08f;

    private readonly HashSet<string> reactedPairs = new HashSet<string>();
    private readonly Dictionary<HeatZone, Protein> readyProteinsByHeatZone = new Dictionary<HeatZone, Protein>();
    private readonly Dictionary<HeatZone, PendingMaillardReaction> pendingReactionsByHeatZone = new Dictionary<HeatZone, PendingMaillardReaction>();

    private class PendingMaillardReaction
    {
        public Sugar Sugar;
        public Protein Protein;
        public float ElapsedSeconds;
    }

    private void Update()
    {
        ScanForMaillardReaction();
    }

    private void ScanForMaillardReaction()
    {
        HeatZone[] heatZones = FindObjectsByType<HeatZone>(FindObjectsSortMode.None);

        foreach (HeatZone heatZone in heatZones)
        {
            if (heatZone == null || !heatZone.gameObject.activeInHierarchy)
                continue;

            TryReactInsideHeatZone(heatZone);
        }
    }

    private void TryReactInsideHeatZone(HeatZone heatZone)
    {
        Collider heatCollider = heatZone.GetComponent<Collider>();
        if (heatCollider == null || !heatCollider.enabled)
            return;

        bool hasSugarOnSurface = TryFindReactantPlacedOnHeatSurface(heatCollider, out Sugar sugar);
        bool hasProteinOnSurface = TryFindReactantPlacedOnHeatSurface(heatCollider, out Protein protein);

        bool hasReadyProtein = TryGetReadyProtein(heatZone, out Protein readyProtein);
        if (hasReadyProtein && !IsReactantPlacedOnHeatSurface(readyProtein, heatCollider))
        {
            readyProteinsByHeatZone.Remove(heatZone);
            pendingReactionsByHeatZone.Remove(heatZone);
            hasReadyProtein = false;
        }

        if (hasReadyProtein && hasSugarOnSurface)
        {
            UpdatePendingMaillardReaction(heatZone, sugar, readyProtein);
            return;
        }

        if (!hasProteinOnSurface)
        {
            readyProteinsByHeatZone.Remove(heatZone);
            pendingReactionsByHeatZone.Remove(heatZone);
            return;
        }

        if (!hasSugarOnSurface)
        {
            readyProteinsByHeatZone[heatZone] = protein;
            pendingReactionsByHeatZone.Remove(heatZone);
            return;
        }

        UpdatePendingMaillardReaction(heatZone, sugar, protein);
    }

    private void TriggerMaillard(Sugar sugar, Protein protein, HeatZone heatZone)
    {
        string pairKey = GetPairKey(sugar.gameObject, protein.gameObject);

        if (reactedPairs.Contains(pairKey))
            return;

        reactedPairs.Add(pairKey);

        Vector3 reactionPosition = (sugar.transform.position + protein.transform.position) * 0.5f;

        GameObject product = null;

        if (maillardProductPrefab != null)
        {
            product = Instantiate(maillardProductPrefab, reactionPosition, Quaternion.identity);
            ConfigureMaillardProduct(product, heatZone, reactionPosition);
        }

        if (smokePrefab != null)
        {
            ParticleSystem smoke = Instantiate(smokePrefab, reactionPosition, Quaternion.identity);
            smoke.Play();
        }

        if (maillardSO != null)
        {
            maillardSO.Position = reactionPosition;
            maillardSO.Source = product != null ? product : sugar.gameObject;
            maillardSO.Participants = new GameObject[]
            {
                sugar.gameObject,
                protein.gameObject,
                heatZone.gameObject
            };

            ReactionEvents.Raise(maillardSO);
        }

        if (hideReactantsAfterReaction)
        {
            StopSugarCaramelisation(sugar);
            sugar.gameObject.SetActive(false);
            protein.gameObject.SetActive(false);
        }
    }

    private string GetPairKey(GameObject a, GameObject b)
    {
        int idA = a.GetInstanceID();
        int idB = b.GetInstanceID();

        if (idA < idB)
            return idA + "_" + idB;

        return idB + "_" + idA;
    }

    private bool TryFindReactantOnHeatZone<T>(Collider heatCollider, out T reactant) where T : Component
    {
        reactant = null;

        T[] candidates = FindObjectsByType<T>(FindObjectsSortMode.None);
        for (int i = 0; i < candidates.Length; i++)
        {
            T candidate = candidates[i];
            if (candidate == null || !candidate.gameObject.activeInHierarchy)
                continue;

            if (IsReactantOnHeatZone(candidate, heatCollider))
            {
                reactant = candidate;
                return true;
            }
        }

        return false;
    }

    private bool TryFindReactantPlacedOnHeatSurface<T>(Collider heatCollider, out T reactant) where T : Component
    {
        reactant = null;

        T[] candidates = FindObjectsByType<T>(FindObjectsSortMode.None);
        for (int i = 0; i < candidates.Length; i++)
        {
            T candidate = candidates[i];
            if (candidate == null || !candidate.gameObject.activeInHierarchy)
                continue;

            if (IsReactantPlacedOnHeatSurface(candidate, heatCollider))
            {
                reactant = candidate;
                return true;
            }
        }

        return false;
    }

    private bool IsReactantOnHeatZone(Component reactant, Collider heatCollider)
    {
        Collider[] colliders = reactant.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider reactantCollider = colliders[i];
            if (reactantCollider == null || !reactantCollider.enabled || !reactantCollider.gameObject.activeInHierarchy)
                continue;

            if (IsColliderTouchingHeatZone(reactantCollider, heatCollider) || IsColliderOnHeatedSurface(reactantCollider, heatCollider))
                return true;
        }

        return false;
    }

    private bool IsReactantPlacedOnHeatSurface(Component reactant, Collider heatCollider)
    {
        Collider[] colliders = reactant.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider reactantCollider = colliders[i];
            if (reactantCollider == null || !reactantCollider.enabled || !reactantCollider.gameObject.activeInHierarchy)
                continue;

            if (IsColliderPlacedOnHeatSurface(reactantCollider, heatCollider))
                return true;
        }

        return false;
    }

    private bool IsColliderTouchingHeatZone(Collider reactantCollider, Collider heatCollider)
    {
        float tolerance = Mathf.Max(0f, heatContactTolerance);

        Bounds heatBounds = heatCollider.bounds;
        heatBounds.Expand(tolerance * 2f);

        if (!heatBounds.Intersects(reactantCollider.bounds))
            return false;

        Vector3 reactantPoint = reactantCollider.ClosestPoint(heatCollider.bounds.center);
        Vector3 heatPoint = heatCollider.ClosestPoint(reactantPoint);
        reactantPoint = reactantCollider.ClosestPoint(heatPoint);

        return (heatPoint - reactantPoint).sqrMagnitude <= tolerance * tolerance;
    }

    private bool IsColliderOnHeatedSurface(Collider reactantCollider, Collider heatCollider)
    {
        float horizontalTolerance = Mathf.Max(0f, heatSurfaceHorizontalTolerance);
        float verticalReach = Mathf.Max(0f, heatSurfaceVerticalReach);
        float contactTolerance = Mathf.Max(0f, heatContactTolerance);

        Bounds heatBounds = GetHeatSurfaceBounds(heatCollider);
        Bounds reactantBounds = reactantCollider.bounds;

        bool overlapsX =
            reactantBounds.max.x >= heatBounds.min.x - horizontalTolerance &&
            reactantBounds.min.x <= heatBounds.max.x + horizontalTolerance;

        bool overlapsZ =
            reactantBounds.max.z >= heatBounds.min.z - horizontalTolerance &&
            reactantBounds.min.z <= heatBounds.max.z + horizontalTolerance;

        if (!overlapsX || !overlapsZ)
            return false;

        float minHeatedY = heatBounds.min.y - contactTolerance;
        float maxHeatedY = heatBounds.max.y + verticalReach;

        return reactantBounds.max.y >= minHeatedY && reactantBounds.min.y <= maxHeatedY;
    }

    private bool IsColliderPlacedOnHeatSurface(Collider reactantCollider, Collider heatCollider)
    {
        float horizontalTolerance = Mathf.Max(0f, heatSurfaceHorizontalTolerance);
        float surfaceTolerance = Mathf.Max(0f, placementSurfaceTolerance);

        Bounds reactantBounds = reactantCollider.bounds;
        Bounds heatBounds = heatCollider.bounds;

        bool overlapsX =
            reactantBounds.max.x >= heatBounds.min.x - horizontalTolerance &&
            reactantBounds.min.x <= heatBounds.max.x + horizontalTolerance;

        bool overlapsZ =
            reactantBounds.max.z >= heatBounds.min.z - horizontalTolerance &&
            reactantBounds.min.z <= heatBounds.max.z + horizontalTolerance;

        if (!overlapsX || !overlapsZ)
            return false;

        float reactantBottomY = reactantBounds.min.y;
        if (Mathf.Abs(reactantBottomY - heatBounds.max.y) <= surfaceTolerance)
            return true;

        if (!TryGetHeatSurfaceYAtPosition(heatCollider, reactantBounds.center, horizontalTolerance, out float surfaceY))
            return false;

        return Mathf.Abs(reactantBottomY - surfaceY) <= surfaceTolerance;
    }

    private void ConfigureMaillardProduct(GameObject product, HeatZone heatZone, Vector3 reactionPosition)
    {
        if (product == null)
            return;

        product.transform.localScale *= Mathf.Max(0.01f, productScaleMultiplier);
        ApplyRoughProductMaterial(product);
        PlaceProductOnHeatSurface(product, heatZone, reactionPosition);

        if (addProductSurfaceBumps)
        {
            AddProductSurfaceBumps(product);
        }
    }

    private void PlaceProductOnHeatSurface(GameObject product, HeatZone heatZone, Vector3 reactionPosition)
    {
        Collider heatCollider = heatZone != null ? heatZone.GetComponent<Collider>() : null;
        if (heatCollider == null)
            return;

        Bounds productBounds = GetWorldBounds(product);
        float pivotToBottom = product.transform.position.y - productBounds.min.y;
        if (!TryGetHeatSurfaceYAtPosition(heatCollider, reactionPosition, 0f, out float surfaceY))
        {
            surfaceY = heatCollider.bounds.max.y;
        }

        product.transform.position = new Vector3(
            reactionPosition.x,
            surfaceY + pivotToBottom + 0.02f,
            reactionPosition.z
        );
    }

    private void ApplyRoughProductMaterial(GameObject product)
    {
        Renderer[] renderers = product.GetComponentsInChildren<Renderer>();
        Material roughMaterial = null;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (roughMaterial == null)
            {
                Material source = renderer.sharedMaterial != null
                    ? renderer.sharedMaterial
                    : renderer.material;
                Shader fallbackShader = Shader.Find("Universal Render Pipeline/Lit");
                if (fallbackShader == null)
                {
                    fallbackShader = Shader.Find("Standard");
                }

                roughMaterial = source != null
                    ? new Material(source)
                    : fallbackShader != null
                        ? new Material(fallbackShader)
                        : null;

                if (roughMaterial == null)
                    continue;

                SetMaterialColor(roughMaterial, productColor);
                SetMaterialFloatIfPresent(roughMaterial, "_Smoothness", productSmoothness);
                SetMaterialFloatIfPresent(roughMaterial, "_Glossiness", productSmoothness);
                SetMaterialFloatIfPresent(roughMaterial, "_Metallic", 0f);
            }

            renderer.material = roughMaterial;
        }
    }

    private void AddProductSurfaceBumps(GameObject product)
    {
        Bounds bounds = GetWorldBounds(product);
        int bumpCount = Mathf.Max(0, productSurfaceBumpCount);
        if (bumpCount == 0)
            return;

        Material bumpMaterial = null;
        Renderer productRenderer = product.GetComponentInChildren<Renderer>();
        if (productRenderer != null)
        {
            bumpMaterial = productRenderer.material;
        }

        float bumpDiameter = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) *
                             Mathf.Max(0.01f, productSurfaceBumpScale);

        for (int i = 0; i < bumpCount; i++)
        {
            Vector3 direction = GetFibonacciSphereDirection(i, bumpCount);
            Vector3 surfaceOffset = Vector3.Scale(direction, bounds.extents * 0.85f);
            GameObject bump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bump.name = "MaillardProduct_RoughBump";
            bump.transform.SetParent(product.transform, true);
            bump.transform.position = bounds.center + surfaceOffset;
            bump.transform.localScale = Vector3.one * bumpDiameter;

            Collider bumpCollider = bump.GetComponent<Collider>();
            if (bumpCollider != null)
            {
                Destroy(bumpCollider);
            }

            Renderer bumpRenderer = bump.GetComponent<Renderer>();
            if (bumpRenderer != null && bumpMaterial != null)
            {
                bumpRenderer.material = bumpMaterial;
            }
        }
    }

    private static Vector3 GetFibonacciSphereDirection(int index, int count)
    {
        if (count <= 1)
            return Vector3.up;

        float y = 1f - (index / (float)(count - 1)) * 2f;
        float radius = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
        float theta = index * 2.399963f;

        return new Vector3(
            Mathf.Cos(theta) * radius,
            y,
            Mathf.Sin(theta) * radius
        );
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private static void SetMaterialFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static Bounds GetHeatSurfaceBounds(Collider heatCollider)
    {
        Renderer[] renderers = heatCollider.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return bounds;
        }

        return heatCollider.bounds;
    }

    private static bool TryGetHeatSurfaceYAtPosition(
        Collider heatCollider,
        Vector3 position,
        float horizontalTolerance,
        out float surfaceY
    )
    {
        surfaceY = 0f;
        Renderer[] renderers = heatCollider.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Bounds colliderBounds = heatCollider.bounds;
            bool insideColliderXZ =
                position.x >= colliderBounds.min.x - horizontalTolerance &&
                position.x <= colliderBounds.max.x + horizontalTolerance &&
                position.z >= colliderBounds.min.z - horizontalTolerance &&
                position.z <= colliderBounds.max.z + horizontalTolerance;

            if (!insideColliderXZ)
                return false;

            surfaceY = colliderBounds.max.y;
            return true;
        }

        float largestHorizontalArea = 0f;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            Bounds bounds = renderer.bounds;
            largestHorizontalArea = Mathf.Max(largestHorizontalArea, bounds.size.x * bounds.size.z);
        }

        float bestArea = -1f;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            Bounds bounds = renderer.bounds;
            float area = bounds.size.x * bounds.size.z;
            if (largestHorizontalArea > 0f && area < largestHorizontalArea * 0.35f)
                continue;

            bool containsXZ =
                position.x >= bounds.min.x - horizontalTolerance &&
                position.x <= bounds.max.x + horizontalTolerance &&
                position.z >= bounds.min.z - horizontalTolerance &&
                position.z <= bounds.max.z + horizontalTolerance;

            if (!containsXZ)
                continue;

            if (area > bestArea)
            {
                bestArea = area;
                surfaceY = bounds.max.y;
            }
        }

        return bestArea >= 0f;
    }

    private static Bounds GetWorldBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return bounds;
        }

        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    bounds.Encapsulate(colliders[i].bounds);
                }
            }

            return bounds;
        }

        return new Bounds(obj.transform.position, Vector3.one);
    }

    private void UpdatePendingMaillardReaction(HeatZone heatZone, Sugar sugar, Protein protein)
    {
        string pairKey = GetPairKey(sugar.gameObject, protein.gameObject);

        if (reactedPairs.Contains(pairKey))
        {
            pendingReactionsByHeatZone.Remove(heatZone);
            return;
        }

        StopSugarCaramelisation(sugar);

        if (!pendingReactionsByHeatZone.TryGetValue(heatZone, out PendingMaillardReaction pending) ||
            pending.Sugar != sugar ||
            pending.Protein != protein)
        {
            pending = new PendingMaillardReaction
            {
                Sugar = sugar,
                Protein = protein,
                ElapsedSeconds = 0f
            };
            pendingReactionsByHeatZone[heatZone] = pending;
        }

        pending.ElapsedSeconds += Time.deltaTime;

        if (pending.ElapsedSeconds < Mathf.Max(0f, maillardPlacementDelaySeconds))
            return;

        TriggerMaillard(sugar, protein, heatZone);
        readyProteinsByHeatZone.Remove(heatZone);
        pendingReactionsByHeatZone.Remove(heatZone);
    }

    private bool TryGetReadyProtein(HeatZone heatZone, out Protein protein)
    {
        if (readyProteinsByHeatZone.TryGetValue(heatZone, out protein) &&
            protein != null &&
            protein.gameObject.activeInHierarchy)
        {
            return true;
        }

        readyProteinsByHeatZone.Remove(heatZone);
        protein = null;
        return false;
    }

    private void StopSugarCaramelisation(Sugar sugar)
    {
        SugarCaramelisation caramelisation = sugar.GetComponentInChildren<SugarCaramelisation>();
        if (caramelisation == null)
            return;

        caramelisation.StopAllCoroutines();
        caramelisation.enabled = false;
    }
}
