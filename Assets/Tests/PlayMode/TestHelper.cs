using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public static class TestHelper
{
	public const string TestObjectPrefix = "Test_";
    private const string EmulsifierPrefabAssetPath = "Assets/Prefabs/Molecules/Emulsifier.prefab";
	private const string LipidPrefabAssetPath = "Assets/Prefabs/Molecules/Lipid.prefab";
    private const string WaterPrefabAssetPath = "Assets/Prefabs/Molecules/Water.prefab";
    private const string ProteinPrefabAssetPath = "Assets/Prefabs/Molecules/Protein.prefab";
    private const string HeatZonePrefabAssetPath = "Assets/Prefabs/Zone/HeatZone.prefab";
    private const string FridgePrefabAssetPath = "Assets/Prefabs/Zone/Fridge.prefab";
    private const string PolysaccharidePrefabAssetPath = "Assets/Prefabs/Molecules/Polysaccharide.prefab";
	private const float GravityY = -9.81f;
	public static readonly Vector3 PhysicsGravity = new Vector3(0f, GravityY, 0f);
	private static readonly Vector3 GroundScale = new Vector3(6f, 1f, 6f);
    private static readonly Vector3 GroundPosition = new Vector3(0f, -0.5f, 0f);
    private static readonly Vector3 TestCameraPosition = new Vector3(0f, 1.65f, -1f);
    private static readonly Vector3 TestCameraLookAt = new Vector3(0f, 0.9f, 0f);
    private static readonly Color TestCameraBackgroundColor = new Color(0.10f, 0.10f, 0.12f);
    private static readonly Color KeyLightColor = new Color(1f, 0.98f, 0.95f);
    private static readonly Vector3 KeyLightEulerAngles = new Vector3(45f, -30f, 0f);
    public static readonly Vector3 GroundSpawnPosition = new Vector3(0f, 0.6f, 0f);
    public static readonly Vector3 FallingSpawnPosition = new Vector3(0f, 2.4f, 0f);
	private const float PrefabVisualScaleMultiplier = 4f;
	private const float CameraFieldOfView = 55f;
    private const float CameraNearClipPlane = 0.01f;
    private const float CameraFarClipPlane = 100f;
    private const float KeyLightIntensity = 1.2f;
	private const string GroundObjectName = "Test_Ground";
    private const float MergeTriggerBaseRadius = 0.5f;
    private const string CameraObjectName = "Test_Camera";
    private const string KeyLightObjectName = "Test_KeyLight";
    private const string MainCameraTag = "MainCamera";
	public const float VisualStepDelaySeconds = 0.75f;
    public const string HeatZoneTypeName = "HeatZone";
    public const string PolysaccharideTypeName = "Polysaccharide";
	public const string ProteinTypeName = "Protein";
	public const string LipidTypeName = "Lipid";
    public const string WaterTypeName = "Water";
    public const string AmyloseGelTypeName = "AmyloseGel";
	public static Type FindType(string typeName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly assembly in assemblies)
        {
            Type directMatch = assembly.GetType(typeName);
            if (directMatch != null)
            {
                return directMatch;
            }

            Type[] candidates;
            try
            {
                candidates = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                candidates = ex.Types;
            }

            if (candidates == null)
            {
                continue;
            }

            foreach (Type candidate in candidates)
            {
                if (candidate != null && candidate.Name == typeName)
                {
                    return candidate;
                }
            }
        }

        return null;
    }

	public static void CreateTestGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = GroundObjectName;
        ground.transform.localScale = GroundScale;
        ground.transform.position = GroundPosition;
    }

    public static void CreateVisualRig()
    {
        Camera existingCamera = Camera.main;
        if (existingCamera == null)
        {
            GameObject cameraObject = new GameObject(CameraObjectName);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = MainCameraTag;
            camera.transform.position = TestCameraPosition;
            camera.transform.LookAt(TestCameraLookAt);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = TestCameraBackgroundColor;
            camera.fieldOfView = CameraFieldOfView;
            camera.nearClipPlane = CameraNearClipPlane;
            camera.farClipPlane = CameraFarClipPlane;
        }

        Light existingLight = UnityEngine.Object.FindObjectOfType<Light>();
        if (existingLight == null)
        {
            GameObject lightObject = new GameObject(KeyLightObjectName);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = KeyLightIntensity;
            light.color = KeyLightColor;
            light.transform.rotation = Quaternion.Euler(KeyLightEulerAngles);
        }
    }

	public static GameObject SpawnLipid(string objectName, Vector3 position)
    {
        GameObject lipid = InstantiatePrefabForTest(objectName, LipidPrefabAssetPath, position);
        lipid.tag = LipidTypeName;
        Assert.NotNull(lipid.GetComponent<Lipid>(), "Failed to ensure Lipid component on test object.");
        ConfigureLipidCollidersForMerge(lipid);
        return lipid;
    }

	public static GameObject SpawnWater(string objectName, Vector3 position)
    {
        GameObject water = InstantiatePrefabForTest(objectName, WaterPrefabAssetPath, position);
        water.tag = WaterTypeName;


        Assert.NotNull(water.GetComponent<Water>(), "Failed to ensure Water component on test object.");
        return water;
    }

    public static GameObject SpawnProtein(string objectName, Vector3 position)
    {
        GameObject protein = InstantiatePrefabForTest(objectName, ProteinPrefabAssetPath, position);
        Assert.NotNull(protein.GetComponent<Protein>(), "Failed to ensure Protein component on test object.");
        return protein;
    }

    public static GameObject SpawnEmulsifier(string objectName, Vector3 position)
    {
        GameObject emulsifier = InstantiatePrefabForTest(objectName, EmulsifierPrefabAssetPath, position);
        Assert.NotNull(emulsifier.GetComponent<Emulsifier>(), "Failed to ensure Emulsifier component on test object.");
        return emulsifier;
    }

    public static GameObject SpawnPolysaccharide(string objectName, Vector3 position)
    {
        GameObject polysaccharide = InstantiatePrefabForTest(objectName, PolysaccharidePrefabAssetPath, position);
        Assert.NotNull(polysaccharide.GetComponent<PolysaccharideSwelling>(), "Failed to ensure Polysaccharide component on test object.");
        return polysaccharide;
    }

    public static GameObject SpawnHeatZone(string objectName, Vector3 position)
    {
        GameObject heatZone = InstantiatePrefabForTest(objectName, HeatZonePrefabAssetPath, position);
        Assert.NotNull(heatZone.GetComponent<HeatZone>(), "Failed to ensure Heat Zone component on test object.");
        return heatZone;
    }

    public static GameObject SpawnFridge(string objectName, Vector3 position)
    {
        GameObject fridge = InstantiatePrefabForTest(objectName, FridgePrefabAssetPath, position);
        Assert.NotNull(fridge.GetComponent<FridgeZone>(), "Failed to ensure Fridge Zone component on test object.");
        return fridge;
    }

	private static GameObject InstantiatePrefabForTest(string objectName, string assetPath, Vector3 position)
    {
#if UNITY_EDITOR
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        Assert.NotNull(prefab, $"Could not load prefab at path: {assetPath}");

        GameObject instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
        instance.name = objectName;
        instance.transform.localScale *= PrefabVisualScaleMultiplier;
        return instance;
#else
        Assert.Fail($"This PlayMode test requires editor asset loading for prefab path: {assetPath}");
        return null;
#endif
    }

	private static void ConfigureLipidCollidersForMerge(GameObject lipid)
    {
        Collider[] colliders = lipid.GetComponents<Collider>();
        if (colliders.Length == 0)
        {
            lipid.AddComponent<SphereCollider>();
            colliders = lipid.GetComponents<Collider>();
        }

        foreach (Collider collider in colliders)
        {
            collider.isTrigger = false;
        }

        SphereCollider mergeTrigger = lipid.AddComponent<SphereCollider>();
        mergeTrigger.isTrigger = true;
        mergeTrigger.radius = MergeTriggerBaseRadius * PrefabVisualScaleMultiplier;
    }


	public static IEnumerator WaitIfVisualizing(float seconds)
    {
        if (seconds <= 0f)
        {
            yield break;
        }

        yield return new WaitForSeconds(seconds);
    }

	public static GameObject[] FindTestObjectsWithComponent(Type componentType)
    {
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        List<GameObject> matches = new List<GameObject>();

        foreach (GameObject go in allObjects)
        {
            if (!go.name.StartsWith(TestHelper.TestObjectPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (go.GetComponent(componentType) != null)
            {
                matches.Add(go);
            }
        }

        return matches.ToArray();
    }
}