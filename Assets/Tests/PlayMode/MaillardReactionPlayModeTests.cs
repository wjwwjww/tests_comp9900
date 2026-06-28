using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MaillardReactionPlayModeTests
{
    private GameObject managerObject;
    private GameObject sugarObject;
    private GameObject proteinObject;
    private GameObject heatZoneObject;
    private GameObject productPrefab;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        managerObject = new GameObject("Test_MaillardReactionManager");
        MaillardReactionManager manager = managerObject.AddComponent<MaillardReactionManager>();

        ReactionSO maillardSO = ScriptableObject.CreateInstance<ReactionSO>();

        productPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        productPrefab.name = "MaillardProduct";
        productPrefab.SetActive(true);

        SetPrivateField(manager, "maillardSO", maillardSO);
        SetPrivateField(manager, "maillardProductPrefab", productPrefab);
        SetPrivateField(manager, "detectionRadius", 5f);
        SetPrivateField(manager, "scanInterval", 0.01f);

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(managerObject);
        Object.Destroy(sugarObject);
        Object.Destroy(proteinObject);
        Object.Destroy(heatZoneObject);
        Object.Destroy(productPrefab);

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("MaillardProduct"))
            {
                Object.Destroy(obj);
            }
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator MaillardReaction_GeneratesProduct_WhenSugarProteinAndHeatZoneAreNearby()
    {
        CreateSugar(new Vector3(0f, 1f, 0f));
        CreateProtein(new Vector3(0.2f, 1f, 0f));
        CreateHeatZone(new Vector3(0f, 1f, 0f));

        yield return new WaitForSeconds(0.1f);

        GameObject generatedProduct = GameObject.Find("MaillardProduct(Clone)");

        Assert.IsNotNull(generatedProduct, "MaillardProduct(Clone) should be generated when Sugar, Protein, and HeatZone are nearby.");
    }

    [UnityTest]
    public IEnumerator MaillardReaction_DoesNotGenerateProduct_WhenProteinIsMissing()
    {
        CreateSugar(new Vector3(0f, 1f, 0f));
        CreateHeatZone(new Vector3(0f, 1f, 0f));

        yield return new WaitForSeconds(0.1f);

        GameObject generatedProduct = GameObject.Find("MaillardProduct(Clone)");

        Assert.IsNull(generatedProduct, "MaillardProduct(Clone) should not be generated when Protein is missing.");
    }

    private void CreateSugar(Vector3 position)
    {
        sugarObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sugarObject.name = "Test_Sugar";
        sugarObject.transform.position = position;
        sugarObject.AddComponent<Sugar>();
    }

    private void CreateProtein(Vector3 position)
    {
        proteinObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        proteinObject.name = "Test_Protein";
        proteinObject.transform.position = position;
        proteinObject.AddComponent<Protein>();
    }

    private void CreateHeatZone(Vector3 position)
    {
        heatZoneObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        heatZoneObject.name = "Test_HeatZone";
        heatZoneObject.transform.position = position;
        heatZoneObject.AddComponent<HeatZone>();
    }

    private void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNotNull(field, $"Field '{fieldName}' was not found on {target.GetType().Name}.");

        field.SetValue(target, value);
    }
}