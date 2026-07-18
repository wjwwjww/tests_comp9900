using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class LipidOxidationPlayModeTests
{
    private const float NormalCompletionTimeoutSeconds = 15f;
    private const float HeatedCompletionTimeoutSeconds = 8f;
    private const float EmptyPlatformSeconds = 0.75f;
    private const float HeatZonePauseSeconds = 0.5f;
    private const float ProductDisplaySeconds = 1.5f;
    private const float LandingTimeoutSeconds = 3f;
    private const float ContactTimeoutSeconds = 2f;
    private const float LipidDropHeight = 0.75f;
    private const float SettledVelocitySqrMagnitude = 0.0025f;
    private const int RequiredSettledFrames = 3;

    private HashSet<int> existingProductIds;
    private GameObject lipidObject;
    private GameObject heatZoneObject;

    [SetUp]
    public void SetUp()
    {
        Physics.gravity = TestHelper.PhysicsGravity;
        TestHelper.CreateVisualRig();
        TestHelper.CreateTestGround();

        existingProductIds = new HashSet<int>();

        foreach (OxidisedLipid product in Object.FindObjectsByType<OxidisedLipid>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            existingProductIds.Add(product.GetInstanceID());
        }
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (lipidObject != null)
        {
            Object.Destroy(lipidObject);
        }

        if (heatZoneObject != null)
        {
            Object.Destroy(heatZoneObject);
        }

        foreach (OxidisedLipid product in FindNewProducts())
        {
            Object.Destroy(product.gameObject);
        }

        yield return null;

        foreach (GameObject testObject in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (testObject.name.StartsWith(TestHelper.TestObjectPrefix, StringComparison.Ordinal))
            {
                Object.DestroyImmediate(testObject);
            }
        }

        foreach (OxidisedLipid product in FindNewProducts())
        {
            Object.DestroyImmediate(product.gameObject);
        }
    }

    [UnityTest]
    public IEnumerator NormalOxidation()
    {
        yield return WaitVisibly(EmptyPlatformSeconds);

        lipidObject = TestHelper.SpawnLipid(
            "Test_NormalOxidation_Lipid",
            TestHelper.GroundSpawnPosition + Vector3.up);
        Rigidbody lipidRigidbody = GetRigidbody(lipidObject);

        bool lipidSettled = false;
        yield return WaitForRigidbodyToSettle(lipidRigidbody, LandingTimeoutSeconds, result => lipidSettled = result);

        Assert.IsTrue(lipidSettled, "NormalOxidation Lipid should visibly land on the test platform.");
        Assert.IsNotNull(lipidObject, "The original Lipid should still exist after landing.");
        Assert.AreEqual(0, FindNewProducts().Length, "No OxidisedLipid should exist before normal oxidation completes.");

        FreezeRigidbody(lipidObject);
        OxidisedLipid product = null;
        yield return WaitForProductOrTimeout(
            "NormalOxidation",
            NormalCompletionTimeoutSeconds,
            false,
            foundProduct => product = foundProduct);

        Assert.IsNotNull(product, "NormalOxidation should receive the newly created OxidisedLipid product.");
        OxidisedLipid[] products = FindNewProducts();
        Assert.AreEqual(1, products.Length, "Normal oxidation should create exactly one OxidisedLipid.");

        FreezeForDisplay(product);
        yield return WaitVisibly(ProductDisplaySeconds);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator HeatedOxidation()
    {
        yield return WaitVisibly(EmptyPlatformSeconds);

        heatZoneObject = TestHelper.SpawnHeatZone(
            "Test_HeatedOxidation_HeatZone",
            TestHelper.GroundSpawnPosition + Vector3.up);
        Rigidbody heatZoneRigidbody = GetRigidbody(heatZoneObject);

        bool heatZoneSettled = false;
        yield return WaitForRigidbodyToSettle(heatZoneRigidbody, LandingTimeoutSeconds, result => heatZoneSettled = result);

        Assert.IsTrue(heatZoneSettled, "HeatZone should visibly land on the test platform before Lipid spawns.");
        Assert.IsNotNull(heatZoneObject, "HeatZone should remain present after landing.");
        FreezeRigidbody(heatZoneObject);

        yield return WaitVisibly(HeatZonePauseSeconds);

        BoxCollider heatTrigger = heatZoneObject.GetComponent<BoxCollider>();
        Assert.NotNull(heatTrigger, "HeatZone prefab must provide a root BoxCollider trigger.");
        Assert.IsTrue(heatTrigger.isTrigger, "HeatZone root BoxCollider must be a trigger.");

        Vector3 lipidSpawnPosition = heatTrigger.bounds.center + Vector3.up * LipidDropHeight;
        lipidObject = TestHelper.SpawnLipid("Test_HeatedOxidation_Lipid", lipidSpawnPosition);
        Rigidbody lipidRigidbody = GetRigidbody(lipidObject);

        bool heatContactObserved = false;
        float contactElapsed = 0f;
        while (contactElapsed < ContactTimeoutSeconds && lipidObject != null)
        {
            if (IsOverlappingHeatTrigger(heatTrigger, lipidObject))
            {
                heatContactObserved = true;
                break;
            }

            contactElapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        Assert.IsTrue(heatContactObserved, "Falling Lipid should reach the HeatZone root trigger.");
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        FreezeRigidbody(lipidObject);

        OxidisedLipid product = null;
        yield return WaitForProductOrTimeout(
            "HeatedOxidation",
            HeatedCompletionTimeoutSeconds,
            heatContactObserved,
            foundProduct => product = foundProduct);

        Assert.IsNotNull(product, "HeatedOxidation should receive the newly created OxidisedLipid product.");
        OxidisedLipid[] products = FindNewProducts();
        Assert.AreEqual(1, products.Length, "Heated oxidation should create exactly one OxidisedLipid.");

        FreezeForDisplay(product);
        yield return WaitVisibly(ProductDisplaySeconds);
        LogAssert.NoUnexpectedReceived();
    }

    private static Rigidbody GetRigidbody(GameObject gameObject)
    {
        Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
        Assert.NotNull(rigidbody, $"{gameObject.name} must provide a Rigidbody for the staged visual test.");
        return rigidbody;
    }

    private static void FreezeForDisplay(OxidisedLipid product)
    {
        product.gameObject.name = "Test_OxidisedLipid_Product";
        FreezeRigidbody(product.gameObject);
    }

    private static void FreezeRigidbody(GameObject gameObject)
    {
        Rigidbody rigidbody = GetRigidbody(gameObject);
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
    }

    private IEnumerator WaitForRigidbodyToSettle(Rigidbody rigidbody, float timeout, Action<bool> onCompleted)
    {
        int settledFrames = 0;
        float elapsed = 0f;

        while (elapsed < timeout && rigidbody != null)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;

            if (rigidbody.linearVelocity.sqrMagnitude <= SettledVelocitySqrMagnitude &&
                rigidbody.angularVelocity.sqrMagnitude <= SettledVelocitySqrMagnitude)
            {
                settledFrames++;
                if (settledFrames >= RequiredSettledFrames)
                {
                    onCompleted?.Invoke(true);
                    yield break;
                }
            }
            else
            {
                settledFrames = 0;
            }
        }

        onCompleted?.Invoke(false);
    }

    private static bool IsOverlappingHeatTrigger(BoxCollider heatTrigger, GameObject lipid)
    {
        foreach (Collider lipidCollider in lipid.GetComponents<Collider>())
        {
            if (lipidCollider.enabled && heatTrigger.bounds.Intersects(lipidCollider.bounds))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator WaitVisibly(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitForProductOrTimeout(
        string testName,
        float timeout,
        bool heatZoneOverlapConfirmed,
        Action<OxidisedLipid> onProductFound)
    {
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            OxidisedLipid[] products = FindNewProducts();
            if (products.Length > 0)
            {
                OxidisedLipid foundProduct = products[0];
                foundProduct.gameObject.name = "Test_OxidisedLipid_Product";

                yield return null;

                if (foundProduct != null)
                {
                    onProductFound?.Invoke(foundProduct);
                    yield break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        bool sourceExists = lipidObject != null;
        bool sourceActive = sourceExists && lipidObject.activeInHierarchy;
        int newProductCount = FindNewProducts().Length;
        int totalProductCount = Object.FindObjectsByType<OxidisedLipid>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

        Assert.Fail(
            $"{testName} did not create a new OxidisedLipid within {timeout:F1} scaled seconds. " +
            $"Elapsed: {elapsed:F2}s; source exists: {sourceExists}; source active: {sourceActive}; " +
            $"new products: {newProductCount}; total products: {totalProductCount}; " +
            $"Time.timeScale: {Time.timeScale:F2}; HeatZone overlap confirmed: {heatZoneOverlapConfirmed}.");
    }

    private OxidisedLipid[] FindNewProducts()
    {
        List<OxidisedLipid> products = new List<OxidisedLipid>();

        foreach (OxidisedLipid product in Object.FindObjectsByType<OxidisedLipid>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!existingProductIds.Contains(product.GetInstanceID()))
            {
                products.Add(product);
            }
        }

        return products.ToArray();
    }
}
