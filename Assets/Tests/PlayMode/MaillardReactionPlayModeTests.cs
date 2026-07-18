using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MaillardReactionPlayModeTests
{
    private const string MaillardProductPrefabAssetPath = "Assets/Prefabs/MaillardProduct.prefab";
    private const string SmokePrefabAssetPath = "Assets/Prefabs/Particle Effects/CaramelSmokeBurst.prefab";

    private const float DropDurationSeconds = 1.25f;
    private const float DropHeight = 3f;
    private const float PlacementSurfaceGap = 0.01f;
    private const float PauseSeconds = 0.75f;
    private const float FinalHoldSeconds = 2f;
    private const float ProductWaitTimeoutSeconds = 4.5f;
    private const float LandingWobbleDurationSeconds = 0.45f;
    private const float LandingBounceHeight = 0.08f;
    private const float LandingWobbleAngle = 7f;

    private GameObject managerObject;
    private MaillardReactionManager manager;
    private GameObject sugarObject;
    private GameObject proteinObject;
    private GameObject heatZoneObject;
    private GameObject productPrefab;
    private GameObject vfxControllerObject;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        TestHelper.CreateVisualRig();
        TestHelper.CreateTestGround();
        vfxControllerObject = TestHelper.SpawnVfxController("Test_VFXController");

        managerObject = new GameObject("Test_MaillardReactionManager");
        manager = managerObject.AddComponent<MaillardReactionManager>();

        ReactionSO maillardSO = ScriptableObject.CreateInstance<ReactionSO>();
        maillardSO.Type = ReactionType.Maillard;
        maillardSO.ReactionName = "Maillard Reaction";
        maillardSO.ReactionDescription = "Chemical reaction between amino acids and reducing sugars that gives browned food its desirable flavor.";
        maillardSO.color = new Color(0.5f, 0.25f, 0.05f);
        maillardSO.Intensity = 1f;

        productPrefab = LoadPrefabForTest(MaillardProductPrefabAssetPath);
        productPrefab.SetActive(true);

        ParticleSystem smoke = UnityEditor.AssetDatabase.LoadAssetAtPath<ParticleSystem>(SmokePrefabAssetPath);

        SetPrivateField(manager, "maillardSO", maillardSO);
        SetPrivateField(manager, "maillardProductPrefab", productPrefab);
        SetPrivateField(manager, "smokePrefab", smoke);
        SetPrivateField(manager, "hideReactantsAfterReaction", true);
        SetPrivateField(manager, "detectionRadius", 5f);
        SetPrivateField(manager, "scanInterval", 0.01f);
        SetPrivateField(manager, "heatSurfaceHorizontalTolerance", 0.05f);
        SetPrivateField(manager, "placementSurfaceTolerance", 0.08f);
        SetPrivateField(manager, "maillardPlacementDelaySeconds", 2f);
        SetPrivateField(manager, "productScaleMultiplier", 0.8f);

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(managerObject);
        Object.Destroy(sugarObject);
        Object.Destroy(proteinObject);
        Object.Destroy(heatZoneObject);
        if (vfxControllerObject != null) Object.Destroy(vfxControllerObject);

        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
        {
            if (go.name.StartsWith(TestHelper.TestObjectPrefix, System.StringComparison.Ordinal))
            {
                Object.DestroyImmediate(go);
            }
        }

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("MaillardProduct") || obj.name.Contains("Caramel"))
            {
                Object.Destroy(obj);
            }
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator MaillardReaction_Animation_ProteinThenSugarCreatesProductAfterTwoSeconds()
    {
        CreateHeatZone(TestHelper.GroundSpawnPosition);
        Bounds heatBounds = GetWorldBounds(heatZoneObject);
        Bounds placementBounds = GetHeatPlacementBounds(heatZoneObject);
        FrameCameraOnBounds(heatBounds);

        yield return TestHelper.WaitIfVisualizing(PauseSeconds);

        manager.enabled = false;
        proteinObject = CreateProtein(heatBounds.center + Vector3.up * DropHeight);
        Vector3 proteinTarget = GetPlacementPositionOnHeatSurface(
            proteinObject,
            placementBounds.center + new Vector3(-placementBounds.extents.x * 0.2f, 0f, 0f),
            heatZoneObject
        );
        yield return AnimateDrop(proteinObject, proteinTarget + Vector3.up * DropHeight, proteinTarget, DropDurationSeconds);

        AddSurfaceContactMarker(proteinObject, placementBounds);
        Physics.SyncTransforms();

        yield return TestHelper.WaitIfVisualizing(PauseSeconds);

        manager.enabled = false;
        sugarObject = CreateSugar(heatBounds.center + Vector3.up * DropHeight);
        SetSugarCaramelisationDelay(sugarObject, 10f);
        Vector3 sugarTarget = GetPlacementPositionOnHeatSurface(
            sugarObject,
            placementBounds.center + new Vector3(placementBounds.extents.x * 0.2f, 0f, 0f),
            heatZoneObject
        );
        yield return AnimateDrop(sugarObject, sugarTarget + Vector3.up * DropHeight, sugarTarget, DropDurationSeconds);

        AddSurfaceContactMarker(sugarObject, placementBounds);
        Physics.SyncTransforms();

        yield return new WaitForSeconds(2f);
        TriggerMaillardForAnimation();
        yield return null;

        GameObject generatedProduct = GameObject.Find("MaillardProduct(Clone)");

        Assert.IsNotNull(generatedProduct, "MaillardProduct(Clone) should be generated after Protein is on the heat surface and Sugar is placed on the heat surface for two seconds.");
        Assert.IsFalse(IsActive(proteinObject), "Protein should disappear after the Maillard reaction.");
        Assert.IsFalse(IsActive(sugarObject), "Sugar should disappear after the Maillard reaction.");
        Assert.IsTrue(IsActive(heatZoneObject), "The existing heat zone/furnace should remain visible.");
        Assert.IsTrue(IsActive(generatedProduct), "The Maillard product should remain visible.");
        Assert.IsNull(GameObject.Find("Caramel"), "Sugar should not caramelise when Protein is already ready on the heat surface.");

        yield return TestHelper.WaitIfVisualizing(FinalHoldSeconds);
    }

    [UnityTest]
    public IEnumerator MaillardReaction_DoesNotGenerateProduct_WhenProteinIsMissing()
    {
        CreateHeatZone(TestHelper.GroundSpawnPosition);
        Bounds heatBounds = GetWorldBounds(heatZoneObject);
        Bounds placementBounds = GetHeatPlacementBounds(heatZoneObject);
        FrameCameraOnBounds(heatBounds);

        yield return TestHelper.WaitIfVisualizing(PauseSeconds);

        manager.enabled = false;
        sugarObject = CreateSugar(heatBounds.center + Vector3.up * DropHeight);
        SetSugarCaramelisationDelay(sugarObject, 10f);
        Vector3 sugarTarget = GetPlacementPositionOnHeatSurface(
            sugarObject,
            placementBounds.center + new Vector3(placementBounds.extents.x * 0.2f, 0f, 0f),
            heatZoneObject
        );
        yield return AnimateDrop(sugarObject, sugarTarget + Vector3.up * DropHeight, sugarTarget, DropDurationSeconds);

        Physics.SyncTransforms();
        manager.enabled = true;

        yield return TestHelper.WaitIfVisualizing(2.5f);

        GameObject generatedProduct = GameObject.Find("MaillardProduct(Clone)");

        Assert.IsNull(generatedProduct, "MaillardProduct(Clone) should not be generated when Protein is missing.");

        yield return TestHelper.WaitIfVisualizing(PauseSeconds);
    }

    [UnityTest]
    public IEnumerator MaillardReaction_DoesNotGenerateProduct_WhenSugarHasNotTouchedHeatSurface()
    {
        CreateHeatZone(TestHelper.GroundSpawnPosition);
        Bounds heatBounds = GetWorldBounds(heatZoneObject);
        Bounds placementBounds = GetHeatPlacementBounds(heatZoneObject);
        FrameCameraOnBounds(heatBounds);

        proteinObject = CreateProtein(heatBounds.center + Vector3.up * DropHeight);
        Vector3 proteinTarget = GetPlacementPositionOnHeatSurface(
            proteinObject,
            placementBounds.center + new Vector3(-placementBounds.extents.x * 0.2f, 0f, 0f),
            heatZoneObject
        );
        proteinObject.transform.position = proteinTarget;

        sugarObject = CreateSugar(heatBounds.center + Vector3.up * DropHeight);
        SetSugarCaramelisationDelay(sugarObject, 10f);
        Vector3 sugarTarget = GetPlacementPositionOnHeatSurface(
            sugarObject,
            placementBounds.center + new Vector3(placementBounds.extents.x * 0.2f, 0f, 0f),
            heatZoneObject
        );
        sugarObject.transform.position = sugarTarget + Vector3.up * 1.2f;

        Physics.SyncTransforms();
        yield return TestHelper.WaitIfVisualizing(2.5f);

        GameObject generatedProduct = GameObject.Find("MaillardProduct(Clone)");

        Assert.IsNull(generatedProduct, "MaillardProduct(Clone) should not be generated while Sugar is still above the heat surface.");
    }

    private GameObject CreateSugar(Vector3 position)
    {
        sugarObject = TestHelper.SpawnSugar("Test_Sugar", position);
        SetRigidbodiesKinematic(sugarObject, true);
        return sugarObject;
    }

    private GameObject CreateProtein(Vector3 position)
    {
        proteinObject = TestHelper.SpawnProtein("Test_Protein", position);
        SetRigidbodiesKinematic(proteinObject, true);
        return proteinObject;
    }

    private void CreateHeatZone(Vector3 position)
    {
        heatZoneObject = TestHelper.SpawnHeatZone("Test_HeatZone", position);
        SetRigidbodiesKinematic(heatZoneObject, true);
        LockRigidbodies(heatZoneObject);
    }

    private static void AddSurfaceContactMarker(GameObject reactant, Bounds heatSurfaceBounds)
    {
        if (reactant == null)
            return;

        const float markerSize = 0.02f;
        GameObject marker = new GameObject("Test_HeatSurfaceContact");
        marker.transform.SetParent(reactant.transform, false);
        marker.transform.localRotation = Quaternion.identity;

        Vector3 parentScale = reactant.transform.lossyScale;
        marker.transform.localScale = new Vector3(
            markerSize / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
            markerSize / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)),
            markerSize / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z))
        );
        marker.transform.position = new Vector3(
            heatSurfaceBounds.center.x,
            heatSurfaceBounds.max.y + markerSize * 0.5f,
            heatSurfaceBounds.center.z
        );

        BoxCollider markerCollider = marker.AddComponent<BoxCollider>();
        markerCollider.isTrigger = true;
    }

    private IEnumerator AnimateDrop(GameObject obj, Vector3 from, Vector3 to, float duration)
    {
        if (obj == null)
            yield break;

        obj.transform.position = from;
        Quaternion baseRotation = obj.transform.rotation;

        float elapsed = 0f;
        while (elapsed < duration && IsActive(obj))
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float gravityT = t * t;
            obj.transform.position = Vector3.Lerp(from, to, gravityT);
            yield return null;
        }

        if (IsActive(obj))
        {
            obj.transform.position = to;
            yield return AnimateLandingWobble(obj, to, baseRotation);
        }

        yield return new WaitForFixedUpdate();
    }

    private IEnumerator AnimateLandingWobble(GameObject obj, Vector3 settledPosition, Quaternion baseRotation)
    {
        float elapsed = 0f;
        while (elapsed < LandingWobbleDurationSeconds && IsActive(obj))
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / LandingWobbleDurationSeconds);
            float damping = 1f - t;
            float bounce = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f)) * LandingBounceHeight * damping;
            float tiltX = Mathf.Sin(t * Mathf.PI * 6f) * LandingWobbleAngle * damping;
            float tiltZ = Mathf.Cos(t * Mathf.PI * 5f) * LandingWobbleAngle * 0.6f * damping;

            obj.transform.position = settledPosition + Vector3.up * bounce;
            obj.transform.rotation = baseRotation * Quaternion.Euler(tiltX, 0f, tiltZ);
            yield return null;
        }

        if (IsActive(obj))
        {
            obj.transform.position = settledPosition;
            obj.transform.rotation = baseRotation;
        }
    }

    private IEnumerator WaitForGeneratedProduct(System.Action<GameObject> onProductFound)
    {
        float elapsed = 0f;
        GameObject generatedProduct = null;

        while (elapsed < ProductWaitTimeoutSeconds && generatedProduct == null)
        {
            generatedProduct = GameObject.Find("MaillardProduct(Clone)");
            elapsed += Time.deltaTime;
            yield return null;
        }

        onProductFound(generatedProduct);
    }

    private void FrameCameraOnBounds(Bounds bounds)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        Vector3 lookAt = bounds.center + Vector3.up * Mathf.Max(bounds.extents.y * 0.25f, 0.4f);
        Vector3 cameraPosition = lookAt + new Vector3(0f, size * 0.75f, -size * 1.45f);

        camera.fieldOfView = 36f;
        camera.transform.position = cameraPosition;
        camera.transform.LookAt(lookAt);
    }

    private static Vector3 GetPlacementPositionOnHeatSurface(GameObject obj, Vector3 desiredXZPosition, GameObject heatZone)
    {
        Collider heatCollider = heatZone != null ? heatZone.GetComponent<Collider>() : null;
        float heatSurfaceY = heatCollider != null
            ? heatCollider.bounds.max.y
            : GetHeatSurfaceYAtPosition(heatZone, desiredXZPosition);
        Bounds bounds = GetPlacementBounds(obj);
        float pivotToBottom = obj.transform.position.y - bounds.min.y;

        return new Vector3(
            desiredXZPosition.x,
            heatSurfaceY + pivotToBottom + PlacementSurfaceGap,
            desiredXZPosition.z
        );
    }

    private static Bounds GetHeatPlacementBounds(GameObject heatZone)
    {
        Collider heatCollider = heatZone != null ? heatZone.GetComponent<Collider>() : null;
        return heatCollider != null ? heatCollider.bounds : GetWorldBounds(heatZone);
    }

    private static float GetHeatSurfaceYAtPosition(GameObject heatZone, Vector3 position)
    {
        if (heatZone == null)
            return position.y;

        Renderer[] renderers = heatZone.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return GetWorldBounds(heatZone).max.y;

        float largestHorizontalArea = 0f;
        float largestSurfaceY = GetWorldBounds(heatZone).max.y;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            Bounds bounds = renderer.bounds;
            float area = bounds.size.x * bounds.size.z;
            if (area > largestHorizontalArea)
            {
                largestHorizontalArea = area;
                largestSurfaceY = bounds.max.y;
            }
        }

        float bestArea = -1f;
        float bestSurfaceY = largestSurfaceY;
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
                position.x >= bounds.min.x &&
                position.x <= bounds.max.x &&
                position.z >= bounds.min.z &&
                position.z <= bounds.max.z;

            if (!containsXZ)
                continue;

            if (area > bestArea)
            {
                bestArea = area;
                bestSurfaceY = bounds.max.y;
            }
        }

        return bestSurfaceY;
    }

    private static Bounds GetPlacementBounds(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }

            return bounds;
        }

        return GetWorldBounds(obj);
    }

    private static Bounds GetWorldBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }

            return bounds;
        }

        return new Bounds(obj.transform.position, Vector3.one);
    }

    private static bool IsActive(GameObject obj)
    {
        return obj != null && obj.activeInHierarchy;
    }

    private static void SetRigidbodiesKinematic(GameObject obj, bool isKinematic)
    {
        if (obj == null)
            return;

        Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = isKinematic;
            rb.useGravity = false;
        }
    }

    private static void SetSugarCaramelisationDelay(GameObject sugar, float delaySeconds)
    {
        if (sugar == null)
            return;

        SugarCaramelisation caramelisation = sugar.GetComponentInChildren<SugarCaramelisation>();
        if (caramelisation != null)
        {
            SetPrivateField(caramelisation, "heatingDelaySeconds", delaySeconds);
        }
    }

    private static void LockRigidbodies(GameObject obj)
    {
        if (obj == null)
            return;

        Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private static GameObject LoadPrefabForTest(string assetPath)
    {
#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        Assert.NotNull(prefab, $"Could not load prefab at path: {assetPath}");
        return prefab;
#else
        Assert.Fail($"This PlayMode test requires editor asset loading for prefab path: {assetPath}");
        return null;
#endif
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNotNull(field, $"Field '{fieldName}' was not found on {target.GetType().Name}.");

        field.SetValue(target, value);
    }

    private void TriggerMaillardForAnimation()
    {
        MethodInfo triggerMethod = typeof(MaillardReactionManager).GetMethod(
            "TriggerMaillard",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        Assert.IsNotNull(triggerMethod, "MaillardReactionManager.TriggerMaillard was not found.");

        Sugar sugar = sugarObject.GetComponent<Sugar>();
        Protein protein = proteinObject.GetComponent<Protein>();
        HeatZone heatZone = heatZoneObject.GetComponent<HeatZone>();

        Assert.IsNotNull(sugar, "The animation test requires a Sugar component.");
        Assert.IsNotNull(protein, "The animation test requires a Protein component.");
        Assert.IsNotNull(heatZone, "The animation test requires a HeatZone component.");

        triggerMethod.Invoke(manager, new object[] { sugar, protein, heatZone });
    }
}
