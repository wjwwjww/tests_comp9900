using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class StateManager : MonoBehaviour
{
    public static StateManager Instance { get; private set; }

    private List<GameObject> spawnedObjects = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterMolecule(GameObject obj)
    {
        spawnedObjects.Add(obj);
    }

    public void UnregisterMolecule(GameObject obj)
    {
        if (obj == null) return;

        spawnedObjects.Remove(obj);
    }

    public void DestroyAll()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        spawnedObjects.Clear();
    }
}
