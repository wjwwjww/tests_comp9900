using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PolysaccharidePlayModeTests
{
    private const string PolysaccharideName = "Test_Polysaccharide";
    private const string HeatZoneName = "Test_HeatZone";
    private const string FridgeName = "Test_Fridge";
    private const int ExpectedTotalWaterAfterSwelling = 0;
    private const int ExpectedTotalGel = 1;
    private const int ExpectedTotalPolysaccharide = 0;
    private const float AbsorbDuration = 2f;
    private const float GelatinizeDuration = 5f;
    private const float RetrogradationDuration = 5f;
    private static readonly Quaternion FridgeRotation = Quaternion.Euler(-90f, 0f, 0f);

    private static readonly Vector3 OtherPosition = new Vector3(3f, 0.6f, 3f);

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
            if (go.name.StartsWith(TestHelper.TestObjectPrefix, StringComparison.Ordinal) || go.name.StartsWith("AmyloseGel"))
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }

    [UnityTest]
    public IEnumerator PolysaccharideSwelling()
    {
        TestHelper.CreateVisualRig();
        TestHelper.CreateTestGround();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        GameObject polysaccharide = TestHelper.SpawnPolysaccharide(PolysaccharideName, TestHelper.FallingSpawnPosition);
        PolysaccharideSwelling polysaccharideSwelling = polysaccharide.GetComponent<PolysaccharideSwelling>();

        Assert.NotNull(polysaccharideSwelling, "Polysaccharide must have polysaccharide script attached!");
        
        yield return new WaitForFixedUpdate();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);
    
        TestHelper.SpawnWater(PolysaccharideName, TestHelper.FallingSpawnPosition);
        yield return TestHelper.WaitIfVisualizing(AbsorbDuration);

        Assert.IsTrue(TestHelper.FindTestObjectsWithComponent(TestHelper.FindType(TestHelper.WaterTypeName)).Length == ExpectedTotalWaterAfterSwelling, "Polysaccharide should absorb the water!");
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);
    }

    [UnityTest]
    public IEnumerator PolysaccharideGelatinization()
    {
        TestHelper.CreateVisualRig();
        TestHelper.CreateTestGround();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        GameObject polysaccharide = TestHelper.SpawnPolysaccharide(PolysaccharideName, TestHelper.FallingSpawnPosition);
        PolysaccharideSwelling polysaccharideSwelling = polysaccharide.GetComponent<PolysaccharideSwelling>();

        Assert.NotNull(polysaccharideSwelling, "Polysaccharide must have polysaccharide script attached!");
        
        yield return new WaitForFixedUpdate();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);
    
        TestHelper.SpawnWater(PolysaccharideName, TestHelper.FallingSpawnPosition);

        yield return new WaitForFixedUpdate();
        yield return TestHelper.WaitIfVisualizing(AbsorbDuration);
        Assert.IsTrue(TestHelper.FindTestObjectsWithComponent(TestHelper.FindType(TestHelper.WaterTypeName)).Length == ExpectedTotalWaterAfterSwelling, "Water should disappear because it's absorbed to the polysaccharide");

        GameObject heatZone = TestHelper.SpawnHeatZone(HeatZoneName, TestHelper.FallingSpawnPosition);
        yield return new WaitForFixedUpdate();
        yield return TestHelper.WaitIfVisualizing(AbsorbDuration);

        polysaccharide.transform.position = TestHelper.FallingSpawnPosition;

        yield return new WaitForFixedUpdate();
        yield return TestHelper.WaitIfVisualizing(GelatinizeDuration);

        Assert.IsTrue(FindTotalAmyloseGel() == ExpectedTotalGel && FindTotalPolysaccharide() == ExpectedTotalPolysaccharide, "Polysaccharide should be disappeared and there is an amylose gel");
    }

    [UnityTest]
    public IEnumerator PolysaccharideRetrogradation()
    {
        TestHelper.CreateVisualRig();
        TestHelper.CreateTestGround();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        GameObject polysaccharide = TestHelper.SpawnPolysaccharide(PolysaccharideName, TestHelper.FallingSpawnPosition);
        PolysaccharideSwelling polysaccharideSwelling = polysaccharide.GetComponent<PolysaccharideSwelling>();

        Assert.NotNull(polysaccharideSwelling, "Polysaccharide must have polysaccharide script attached!");
        
        yield return new WaitForFixedUpdate();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);
    
        TestHelper.SpawnWater(PolysaccharideName, TestHelper.FallingSpawnPosition);
        yield return TestHelper.WaitIfVisualizing(AbsorbDuration);

        Assert.IsTrue(TestHelper.FindTestObjectsWithComponent(TestHelper.FindType(TestHelper.WaterTypeName)).Length == ExpectedTotalWaterAfterSwelling, "Polysaccharide should absorb the water");
        polysaccharide.transform.position = OtherPosition;
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        GameObject fridge = TestHelper.SpawnFridge(FridgeName, TestHelper.FallingSpawnPosition);
        fridge.transform.rotation = FridgeRotation;

        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        polysaccharide.transform.position = TestHelper.FallingSpawnPosition;
        yield return TestHelper.WaitIfVisualizing(RetrogradationDuration);

        fridge.transform.position = OtherPosition;
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        Assert.IsTrue(!polysaccharideSwelling.IsSwelled, "Polysaccharide must not in swelled state!");
    }

    private int FindTotalPolysaccharide()
    {
        return UnityEngine.Object.FindObjectsByType<PolysaccharideSwelling>(FindObjectsSortMode.None).Length;
    }

    private int FindTotalAmyloseGel()
    {
        return UnityEngine.Object.FindObjectsByType<AmyloseGel>(FindObjectsSortMode.None).Length;
    }
}
