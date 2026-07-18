using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class EmulsifierReactionPlayModeTests
{
    private const string WaterObjectName = "Test_Water";
    private const string EmulsifierName = "Test_Emulsifier";
    private const string LipidName = "Test_Lipid";

    private readonly Vector3 WaterSpawnOffset = new Vector3(-0.1f, 0f, -0f);
    private readonly Vector3 LipidSpawnOffset = new Vector3(0.1f, 0f, 0f);
    private const float SpawnSetUpDuration = 2f;
    private const string WaterSnapPointString = "WaterSnapPoint";
    private const string LipidSnapPointString = "LipidSnapPoint";

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {

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
    public IEnumerator EmulsifierHydrophilicAttached()
    {
        TestHelper.CreateVisualRig();
        TestHelper.CreateTestGround();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        GameObject emulsifier = TestHelper.SpawnEmulsifier(EmulsifierName, TestHelper.FallingSpawnPosition);
        Rigidbody emulsifierRb = emulsifier.GetComponent<Rigidbody>();
        if (emulsifierRb != null)
        {
            emulsifierRb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        yield return TestHelper.WaitIfVisualizing(SpawnSetUpDuration);

        GameObject water = TestHelper.SpawnWater(WaterObjectName, TestHelper.FallingSpawnPosition + WaterSpawnOffset);
        yield return TestHelper.WaitIfVisualizing(SpawnSetUpDuration);
    
        Assert.IsTrue(Equals(water.transform.parent.gameObject.name, WaterSnapPointString), "Water should be attached to the emulsifier hydrophilic");
    }

    [UnityTest]
    public IEnumerator EmulsifierHydrophobicAttached()
    {
        TestHelper.CreateVisualRig();
        TestHelper.CreateTestGround();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        GameObject emulsifier = TestHelper.SpawnEmulsifier(EmulsifierName, TestHelper.FallingSpawnPosition);
        Rigidbody emulsifierRb = emulsifier.GetComponent<Rigidbody>();
        if (emulsifierRb != null)
        {
            emulsifierRb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        yield return TestHelper.WaitIfVisualizing(SpawnSetUpDuration);

        GameObject lipid = TestHelper.SpawnLipid(LipidName, TestHelper.FallingSpawnPosition + LipidSpawnOffset);
        yield return TestHelper.WaitIfVisualizing(SpawnSetUpDuration);

        Assert.IsTrue(Equals(lipid.transform.parent.gameObject.name, LipidSnapPointString), "Lipid should be attached to the emulsifier hydrophobic");
    }

    [UnityTest]
    public IEnumerator EmulsifierHydrophobicAndHydrophobicAttached()
    {
        TestHelper.CreateVisualRig();
        TestHelper.CreateTestGround();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        GameObject emulsifier = TestHelper.SpawnEmulsifier(EmulsifierName, TestHelper.FallingSpawnPosition);
        Rigidbody emulsifierRb = emulsifier.GetComponent<Rigidbody>();
        if (emulsifierRb != null)
        {
            emulsifierRb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        yield return TestHelper.WaitIfVisualizing(SpawnSetUpDuration);

        GameObject water = TestHelper.SpawnWater(WaterObjectName, TestHelper.FallingSpawnPosition + WaterSpawnOffset);
        yield return TestHelper.WaitIfVisualizing(SpawnSetUpDuration);

        GameObject lipid = TestHelper.SpawnLipid(LipidName, TestHelper.FallingSpawnPosition + LipidSpawnOffset);
        yield return TestHelper.WaitIfVisualizing(SpawnSetUpDuration);
        
        Assert.IsTrue(Equals(water.transform.parent.gameObject.name, WaterSnapPointString), "Water should be attached to the emulsifier hydrophilic");
        Assert.IsTrue(Equals(lipid.transform.parent.gameObject.name, LipidSnapPointString), "Lipid should be attached to the emulsifier hydrophobic");
    }
}
