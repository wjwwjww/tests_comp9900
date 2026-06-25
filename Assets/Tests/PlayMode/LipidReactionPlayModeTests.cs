using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class LipidReactionPlayModeTests
{

    private const string GroundLipidObjectName = "Test_GroundLipid";
    private const string FallingLipidObjectName = "Test_FallingLipid";
    private const string MergeHelperGroundLipidObjectName = "Test_MergeHelper_Ground";
    private const string MergeHelperFallingLipidObjectName = "Test_MergeHelper_Falling";
    private const string WaterObjectName = "Test_Water";
    private const string RepelForceFieldName = "repelForce";

    private const float MergeTimeoutSeconds = 4f;
    private const float MergeAnimationDurationSeconds = 0.35f;
    private const float RepulsionSampleSeconds = 1.2f;
    private const float RequiredRepulsionDelta = 0.02f;

    private const float FallingLipidInitialDownwardVelocity = 1.5f;
    private const float WaterRepelForceForTest = 20f;
    private const int ExpectedLipidCountAfterMerge = 1;

    private static readonly Vector3 WaterSpawnOffset = new Vector3(0.55f, 0f, 0f);

    private static Type _lipidType;
    private static Type _waterType;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _lipidType = TestHelper.FindType(TestHelper.LipidTypeName);
        _waterType = TestHelper.FindType(TestHelper.WaterTypeName);

        Assert.NotNull(_lipidType, "Could not locate Lipid script type in loaded assemblies.");
        Assert.NotNull(_waterType, "Could not locate Water script type in loaded assemblies.");
    }

    [SetUp]
    public void SetUp()
    {
        Physics.gravity = TestHelper.PhysicsGravity;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in UnityEngine.Object.FindObjectsOfType<GameObject>())
        {
            if (go.name.StartsWith(TestHelper.TestObjectPrefix, StringComparison.Ordinal))
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }

    [UnityTest]
    public IEnumerator LipidMerge_FallingLipid_MergesAndGrowsGroundLipid()
    {
        TestHelper.CreateVisualRig();
        TestHelper.CreateTestGround();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        GameObject groundLipid = TestHelper.SpawnLipid(GroundLipidObjectName, TestHelper.GroundSpawnPosition);
        Rigidbody groundRb = groundLipid.GetComponent<Rigidbody>();
        groundRb.isKinematic = true;
        groundRb.useGravity = false;
        float baselineScaleMagnitude = groundLipid.transform.localScale.magnitude;

        GameObject fallingLipid = TestHelper.SpawnLipid(FallingLipidObjectName, TestHelper.FallingSpawnPosition);
        Rigidbody fallingRb = fallingLipid.GetComponent<Rigidbody>();
        fallingRb.isKinematic = false;
        fallingRb.useGravity = true;
        fallingRb.AddForce(Vector3.down * FallingLipidInitialDownwardVelocity, ForceMode.VelocityChange);

        // Let Start/Awake and one fixed step run before waiting for merge completion.
        yield return new WaitForFixedUpdate();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        yield return WaitForConditionOrTimeout(
            () => CountTestObjectsWithComponent(_lipidType) == ExpectedLipidCountAfterMerge,
            MergeTimeoutSeconds,
            "Expected exactly one test lipid after merge, but merge timed out.");

        yield return new WaitForSeconds(MergeAnimationDurationSeconds);

        GameObject[] lipids = TestHelper.FindTestObjectsWithComponent(_lipidType);
        Assert.AreEqual(ExpectedLipidCountAfterMerge, lipids.Length, "Expected exactly one lipid after merge.");
        Assert.Greater(lipids[0].transform.localScale.magnitude, baselineScaleMagnitude, "Merged lipid should have larger scale than the original ground lipid.");

        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);
    }

    [UnityTest]
    public IEnumerator WaterNearMergedLipid_RepelsLipidAway()
    {
        TestHelper.CreateVisualRig();
        TestHelper.CreateTestGround();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        GameObject mergedLipid = null;
        yield return MergeTwoLipidsAndReturnRemaining(result => mergedLipid = result);

        Assert.NotNull(mergedLipid, "Merged lipid should exist before water repulsion test.");
        Rigidbody mergedRb = mergedLipid.GetComponent<Rigidbody>();
        mergedRb.isKinematic = false;
        mergedRb.useGravity = true;

        GameObject water = TestHelper.SpawnWater(WaterObjectName, mergedLipid.transform.position + WaterSpawnOffset);
        Rigidbody rb = water.GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Assert.AreEqual(TestHelper.WaterTypeName, water.tag, "Spawned water object must have Water tag.");

        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        float initialDistance = Vector3.Distance(mergedLipid.transform.position, water.transform.position);

        SetPrivateField(water.GetComponent<Water>(), RepelForceFieldName, WaterRepelForceForTest);
        SetAllCollidersTriggerState(water, true);
    
        float elapsed = 0f;
        while (elapsed < RepulsionSampleSeconds)
        {
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        float finalDistance = Vector3.Distance(mergedLipid.transform.position, water.transform.position);
        Assert.Greater(finalDistance, initialDistance + RequiredRepulsionDelta,
            $"Expected lipid to move away from water. Initial={initialDistance:F3}, Final={finalDistance:F3}");

        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);
    }

    private IEnumerator MergeTwoLipidsAndReturnRemaining(Action<GameObject> onCompleted)
    {
        GameObject groundLipid = TestHelper.SpawnLipid(MergeHelperGroundLipidObjectName, TestHelper.GroundSpawnPosition);
        Rigidbody groundRb = groundLipid.GetComponent<Rigidbody>();
        groundRb.isKinematic = true;
        groundRb.useGravity = false;

        GameObject fallingLipid = TestHelper.SpawnLipid(MergeHelperFallingLipidObjectName, TestHelper.FallingSpawnPosition);
        Rigidbody fallingRb = fallingLipid.GetComponent<Rigidbody>();
        fallingRb.isKinematic = false;
        fallingRb.useGravity = true;
        fallingRb.AddForce(Vector3.down * FallingLipidInitialDownwardVelocity, ForceMode.VelocityChange);

        // Let Start/Awake and one fixed step run before waiting for merge completion.
        yield return new WaitForFixedUpdate();

        yield return WaitForConditionOrTimeout(
            () => CountTestObjectsWithComponent(_lipidType) == ExpectedLipidCountAfterMerge,
            MergeTimeoutSeconds,
            "Merge helper timed out waiting for single lipid state.");

        yield return new WaitForSeconds(MergeAnimationDurationSeconds);

        GameObject[] remainingLipids = TestHelper.FindTestObjectsWithComponent(_lipidType);
        Assert.AreEqual(ExpectedLipidCountAfterMerge, remainingLipids.Length, "Merge helper expected one remaining lipid.");
        onCompleted?.Invoke(remainingLipids[0]);
    }

    private static int CountTestObjectsWithComponent(Type componentType)
    {
        return TestHelper.FindTestObjectsWithComponent(componentType).Length;
    }

    private static IEnumerator WaitForConditionOrTimeout(Func<bool> condition, float timeoutSeconds, string timeoutMessage)
    {
        float elapsed = 0f;
        while (!condition())
        {
            if (elapsed >= timeoutSeconds)
            {
                Assert.Fail(timeoutMessage);
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    private static void SetPrivateField(Component target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }

    private static void SetAllCollidersTriggerState(GameObject go, bool isTrigger)
    {
        Collider[] colliders = go.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.isTrigger = isTrigger;
        }
    }
}
