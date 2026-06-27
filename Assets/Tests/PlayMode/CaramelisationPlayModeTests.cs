using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CaramelisationPlayModeTests
{
    private const string SugarName = "Test_Sugar";
    private const string HeatZoneName = "Test_HeatZone";
    private const string VfxControllerName = "Test_VFXController";
    private const float HeatingDelaySeconds = 0.75f;
    private const float HeatingDuration = 2f;

    [SetUp]
    public void SetUp()
    {
        Physics.gravity = TestHelper.PhysicsGravity;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
        {
            if (go.name.StartsWith(TestHelper.TestObjectPrefix, System.StringComparison.Ordinal))
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    [UnityTest]
    public IEnumerator SugarInHeatZone_CreatesCaramelAndRaisesReactionEvent()
    {
        bool reactionRaised = false;
        Caramel[] existingCaramels = Object.FindObjectsByType<Caramel>(FindObjectsSortMode.None);
        GameObject heatZoneObject = null;
        GameObject sugarObject = null;

        void OnReactionOccurred(ReactionSO reaction)
        {
            if (reaction != null && reaction.Type == ReactionType.Caramelisation)
            {
                reactionRaised = true;
            }
        }

        ReactionEvents.Occurred += OnReactionOccurred;

        try
        {
            TestHelper.CreateVisualRig();
            TestHelper.CreateTestGround();
            TestHelper.SpawnVfxController(VfxControllerName);
            yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

            Vector3 heatZoneSpawnPosition = TestHelper.GroundSpawnPosition + new Vector3(0f, 1.0f, 0f);
            heatZoneObject = TestHelper.SpawnHeatZone(HeatZoneName, heatZoneSpawnPosition);

            yield return new WaitForFixedUpdate();
            yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);

            Vector3 sugarSpawnPosition = TestHelper.GroundSpawnPosition + new Vector3(0f, 1.0f, 0f);
            sugarObject = TestHelper.SpawnSugar(SugarName, sugarSpawnPosition);
            SugarCaramelisation caramelisation = sugarObject.GetComponent<SugarCaramelisation>();
            SetPrivateField(caramelisation, "heatingDelaySeconds", HeatingDelaySeconds);

            yield return new WaitForFixedUpdate();
            yield return TestHelper.WaitIfVisualizing(HeatingDuration);

            Caramel createdCaramel = FindCreatedCaramel(existingCaramels);
            Assert.NotNull(createdCaramel, "A new Caramel object should be created.");
            createdCaramel.gameObject.name = TestHelper.TestObjectPrefix + createdCaramel.gameObject.name;

            Assert.IsTrue(sugarObject == null, "Sugar should be destroyed after caramelisation.");
            Assert.IsTrue(reactionRaised, "Caramelisation reaction event should be raised.");

            yield return TestHelper.WaitIfVisualizing(TestHelper.VisualStepDelaySeconds);
        }
        finally
        {
            ReactionEvents.Occurred -= OnReactionOccurred;

            if (heatZoneObject != null) Object.Destroy(heatZoneObject);
            if (sugarObject != null) Object.Destroy(sugarObject);

            Caramel createdCaramel = FindCreatedCaramel(existingCaramels);
            if (createdCaramel != null) Object.Destroy(createdCaramel.gameObject);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        Assert.IsNotNull(field, $"Field '{fieldName}' was not found.");
        field.SetValue(target, value);
    }

    private static Caramel FindCreatedCaramel(Caramel[] existingCaramels)
    {
        return Object.FindObjectsByType<Caramel>(FindObjectsSortMode.None)
            .FirstOrDefault(c => c != null && !existingCaramels.Contains(c));
    }
}
