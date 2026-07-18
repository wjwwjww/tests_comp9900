using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ProteinReactionPlayModeTests
{
    private static Type _proteinType;
    private static readonly Vector3 FallingProteinSpawnPosition = new Vector3(0.1f, 2.4f, 0f);
    private static readonly Vector3 FallingProteinSpawnPosition2 = new Vector3(0.1f, 2.4f, -0.1f);
    private const float HeatingDuration = 2f;
    private const float BondDuration = 2f;
    private const string ProteinName = "Test_Protein";
    private const string HeatZoneName = "Test_HeatZone";
    private static readonly Vector3 AfterDenaturedSpawnPosition = new Vector3(0.5f, 2.4f, 0.5f);
    private static readonly Vector3 OtherPosition = new Vector3(2f, 2.4f, 2f);


    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _proteinType = TestHelper.FindType(TestHelper.ProteinTypeName);
        Assert.NotNull(_proteinType, "Could not locate Protein script type in loaded assemblies.");
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
            if (go.name.StartsWith(TestHelper.TestObjectPrefix, StringComparison.Ordinal) || go.name.Contains("SolidifiedProtein"))
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }

    [UnityTest]
    public IEnumerator ProteinDenatureByItself()
    {
        TestHelper.CreateVisualRig();
        TestHelper.CreateTestGround();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        GameObject protein = TestHelper.SpawnProtein(ProteinName, TestHelper.GroundSpawnPosition);
        Protein proteinComponent = protein.GetComponent<Protein>();

        Assert.NotNull(proteinComponent, "Protein must have protein script attached!");
        Assert.IsTrue(proteinComponent.IsNative(), "Protein must be native when spawned");
        
        yield return new WaitForFixedUpdate();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        proteinComponent.Denature();        

        Assert.IsTrue(proteinComponent.IsDenatured(), "Protein must be native when spawned");
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);
    }

    [UnityTest]
    public IEnumerator ProteinDenatureByHeatZone()
    {
        TestHelper.CreateVisualRig();
        TestHelper.CreateTestGround();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        GameObject heatZone = TestHelper.SpawnHeatZone(HeatZoneName, TestHelper.FallingSpawnPosition);
        ApplyConstraints(heatZone);

        yield return new WaitForFixedUpdate();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        GameObject protein = TestHelper.SpawnProtein(ProteinName, FallingProteinSpawnPosition);
        ApplyConstraints(protein);
        Protein proteinComponent = protein.GetComponent<Protein>();
        
        yield return new WaitForFixedUpdate();
        yield return TestHelper.WaitIfVisualizing(HeatingDuration);

        Assert.IsTrue(proteinComponent.IsDenatured(), "Protein must be denatured after heated");
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);
    }

    [UnityTest]
    public IEnumerator ProteinBonding()
    {
        TestHelper.CreateVisualRig();
        TestHelper.CreateTestGround();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        GameObject protein = TestHelper.SpawnProtein(ProteinName, TestHelper.GroundSpawnPosition);
        Protein proteinComponent = protein.GetComponent<Protein>();
        proteinComponent.Denature();   
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds); 

        Vector3 offsetPosition = TestHelper.GroundSpawnPosition + new Vector3(1.0f, 0f, 0f);
        GameObject protein2 = TestHelper.SpawnProtein(ProteinName, offsetPosition);
        Protein proteinComponent2 = protein2.GetComponent<Protein>();
        
        yield return new WaitForFixedUpdate();
        proteinComponent2.Denature();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        protein2.transform.position = TestHelper.GroundSpawnPosition;
        yield return new WaitForFixedUpdate();
        yield return TestHelper.WaitIfVisualizing(BondDuration);

        Assert.IsTrue(proteinComponent2.IsBonded() && proteinComponent.IsBonded(), "Both Protein must be bonded as they collided each other while on denatured state");
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);
    }

    [UnityTest]
    public IEnumerator TwoProteinDenaturedAtTheSameTimeThenBonded()
    {
        TestHelper.CreateVisualRig();
        TestHelper.CreateTestGround();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        GameObject heatZone = TestHelper.SpawnHeatZone(HeatZoneName, TestHelper.FallingSpawnPosition);
        ApplyConstraints(heatZone);

        yield return new WaitForFixedUpdate();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        GameObject protein = TestHelper.SpawnProtein(ProteinName, FallingProteinSpawnPosition);
        ApplyConstraints(protein);
        Protein proteinComponent = protein.GetComponent<Protein>();

        GameObject protein2 = TestHelper.SpawnProtein(ProteinName, FallingProteinSpawnPosition2);
        ApplyConstraints(protein2);
        Protein proteinComponent2 = protein2.GetComponent<Protein>();
        
        yield return new WaitForFixedUpdate();
        yield return TestHelper.WaitIfVisualizing(HeatingDuration);

        Rigidbody rb1 = protein.GetComponent<Rigidbody>();
        if (rb1 != null) rb1.constraints = RigidbodyConstraints.None;
        Rigidbody rb2 = protein2.GetComponent<Rigidbody>();
        if (rb2 != null) rb2.constraints = RigidbodyConstraints.None;

        heatZone.transform.position = OtherPosition;
        protein.transform.position = AfterDenaturedSpawnPosition;
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        protein2.transform.position = AfterDenaturedSpawnPosition;
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);
        yield return TestHelper.WaitIfVisualizing(BondDuration);

        Assert.IsTrue(proteinComponent.IsBonded() && proteinComponent2.IsBonded(), "Protein must be bonded as both of them are denatured and collided each other");
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);
    }

    [UnityTest]
    public IEnumerator ProteinCoagulatesWhenAcidCollides()
    {
        TestHelper.CreateVisualRig();
        TestHelper.CreateTestGround();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        GameObject protein = TestHelper.SpawnProtein(ProteinName, TestHelper.GroundSpawnPosition);
        Protein proteinComponent = protein.GetComponent<Protein>();

        Assert.IsTrue(proteinComponent.IsNative(), "Protein must be native when spawned");
        
        yield return new WaitForFixedUpdate();
        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

        Vector3 acidSpawnPos = TestHelper.GroundSpawnPosition + new Vector3(0f, 1.0f, 0f);
        GameObject acid = TestHelper.SpawnAcid("Test_Acid", acidSpawnPos);

        yield return new WaitForFixedUpdate();
        yield return TestHelper.WaitIfVisualizing(2.0f);

        Assert.IsTrue(protein == null, "The original protein GameObject must be destroyed after coagulation");

        SolidifiedProteinBlock solidifiedBlock = UnityEngine.Object.FindObjectOfType<SolidifiedProteinBlock>();
        Assert.NotNull(solidifiedBlock, "A solidified protein block should be spawned after coagulation");

        yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);
    }

    private void ApplyConstraints(GameObject go)
    {
        if (go == null) return;
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }
    }
}

