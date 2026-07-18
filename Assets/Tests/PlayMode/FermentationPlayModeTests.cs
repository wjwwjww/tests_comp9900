using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class FermentationPlayModeTests
{
    private const float DropDuration = 0.7f;
    private const float FermentationDelay = 3f;

    private GameObject managerObject;
    private GameObject warmZoneObject;
    private GameObject sugarObject;
    private GameObject yeastObject;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        TestHelper.CreateVisualRig();
        TestHelper.CreateTestGround();

        managerObject = new GameObject("Test_FermentationManager");
        FermentationReactionManager manager = managerObject.AddComponent<FermentationReactionManager>();

        ReactionSO fermentationSO = ScriptableObject.CreateInstance<ReactionSO>();
        fermentationSO.Type = ReactionType.Fermentation;
        fermentationSO.ReactionName = "Yeast Fermentation";
        fermentationSO.ReactionDescription = "Yeast consumes sugar in a warm environment and produces gas that makes dough rise.";
        fermentationSO.color = new Color(0.9f, 0.7f, 0.3f);

        SetPrivateField(manager, "fermentationSO", fermentationSO);
        SetPrivateField(manager, "fermentationDelaySeconds", FermentationDelay);

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject obj = allObjects[i];
            if (obj.name.StartsWith(TestHelper.TestObjectPrefix, System.StringComparison.Ordinal) ||
                obj.name == "FermentedDough")
            {
                Object.Destroy(obj);
            }
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator Fermentation_Animation_SugarThenYeastCreatesRisenDough()
    {
        CreateWarmZone();
        FrameFermentationCamera();

        sugarObject = TestHelper.SpawnSugar("Test_FermentationSugar", new Vector3(-0.45f, 3f, 0f));
        SetRigidbodiesKinematic(sugarObject);
        yield return AnimateDrop(sugarObject, new Vector3(-0.45f, 0.3f, 0f));

        yeastObject = CreateYeast(new Vector3(0.45f, 3f, 0f));
        yield return AnimateDrop(yeastObject, new Vector3(0.45f, 0.3f, 0f));

        yield return new WaitForSeconds(FermentationDelay + 0.5f);

        FermentedDough dough = Object.FindFirstObjectByType<FermentedDough>();

        Assert.IsNotNull(dough, "Sugar and Yeast inside WarmZone should create FermentedDough after three seconds.");
        Assert.IsFalse(sugarObject.activeInHierarchy, "Sugar should be consumed by fermentation.");
        Assert.IsFalse(yeastObject.activeInHierarchy, "Yeast should be consumed by fermentation.");
        Assert.IsTrue(warmZoneObject.activeInHierarchy, "WarmZone should remain visible after fermentation.");

        yield return new WaitForSeconds(1f);
    }

    [UnityTest]
    public IEnumerator Fermentation_DoesNotStart_WhenYeastIsMissing()
    {
        CreateWarmZone();
        sugarObject = TestHelper.SpawnSugar("Test_FermentationSugar", new Vector3(0f, 0.3f, 0f));
        SetRigidbodiesKinematic(sugarObject);

        yield return new WaitForSeconds(FermentationDelay + 0.5f);

        Assert.IsNull(Object.FindFirstObjectByType<FermentedDough>(), "Fermentation should not start without Yeast.");
    }

    private void CreateWarmZone()
    {
        warmZoneObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        warmZoneObject.name = "Test_WarmZone";
        warmZoneObject.transform.position = new Vector3(0f, 0.5f, 0f);
        warmZoneObject.transform.localScale = new Vector3(3f, 0.8f, 3f);

        Renderer zoneRenderer = warmZoneObject.GetComponent<Renderer>();
        zoneRenderer.enabled = false;

        warmZoneObject.AddComponent<WarmZone>();

        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platform.name = "Test_WarmPlatform";
        platform.transform.position = new Vector3(0f, 0.06f, 0f);
        platform.transform.localScale = new Vector3(1.5f, 0.06f, 1.5f);

        Renderer platformRenderer = platform.GetComponent<Renderer>();
        platformRenderer.material.color = new Color(0.95f, 0.55f, 0.18f);
    }

    private GameObject CreateYeast(Vector3 position)
    {
        yeastObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        yeastObject.name = "Test_Yeast";
        yeastObject.transform.position = position;
        yeastObject.transform.localScale = Vector3.one * 0.28f;
        yeastObject.AddComponent<Yeast>();

        Renderer renderer = yeastObject.GetComponent<Renderer>();
        renderer.material.color = new Color(0.94f, 0.82f, 0.55f);
        return yeastObject;
    }

    private static void FrameFermentationCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        camera.fieldOfView = 43f;
        camera.transform.position = new Vector3(0f, 2.25f, -6.2f);
        camera.transform.LookAt(new Vector3(0f, 1.05f, 0f));
    }

    private static IEnumerator AnimateDrop(GameObject obj, Vector3 target)
    {
        Vector3 start = obj.transform.position;
        float elapsed = 0f;

        while (elapsed < DropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / DropDuration);
            obj.transform.position = Vector3.Lerp(start, target, t * t);
            yield return null;
        }

        obj.transform.position = target;
    }

    private static void SetRigidbodiesKinematic(GameObject obj)
    {
        Rigidbody[] bodies = obj.GetComponentsInChildren<Rigidbody>();
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].isKinematic = true;
            bodies[i].useGravity = false;
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"Field '{fieldName}' was not found.");
        field.SetValue(target, value);
    }
}
