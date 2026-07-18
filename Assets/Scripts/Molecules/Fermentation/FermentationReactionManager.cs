using System.Collections.Generic;
using UnityEngine;

public class FermentationReactionManager : MonoBehaviour
{
    [Header("Reaction Data")]
    [SerializeField] private ReactionSO fermentationSO;
    [Header("Product")]
    [SerializeField] private GameObject fermentedDoughPrefab;
    [Header("Fermentation Settings")]
    [SerializeField] private float fermentationDelaySeconds = 3f;
    [SerializeField] private bool hideReactantsAfterFermentation = true;

    private readonly Dictionary<WarmZone, PendingFermentation> pendingFermentations = new Dictionary<WarmZone, PendingFermentation>();
    private readonly HashSet<string> fermentedPairs = new HashSet<string>();

    private class PendingFermentation
    {
        public Sugar Sugar;
        public Yeast Yeast;
        public float ElapsedSeconds;
    }

    private void Update()
    {
        WarmZone[] warmZones = FindObjectsByType<WarmZone>(FindObjectsSortMode.None);

        for (int i = 0; i < warmZones.Length; i++)
        {
            WarmZone warmZone = warmZones[i];
            if (warmZone == null || !warmZone.gameObject.activeInHierarchy)
                continue;

            UpdateFermentationForZone(warmZone);
        }
    }

    private void UpdateFermentationForZone(WarmZone warmZone)
    {
        Collider zoneCollider = warmZone.GetComponent<Collider>();
        if (zoneCollider == null || !zoneCollider.enabled)
            return;

        Sugar sugar = FindReactantInsideZone<Sugar>(zoneCollider);
        Yeast yeast = FindReactantInsideZone<Yeast>(zoneCollider);

        if (sugar == null || yeast == null)
        {
            pendingFermentations.Remove(warmZone);
            return;
        }

        string pairKey = GetPairKey(sugar.gameObject, yeast.gameObject);
        if (fermentedPairs.Contains(pairKey))
        {
            pendingFermentations.Remove(warmZone);
            return;
        }

        if (!pendingFermentations.TryGetValue(warmZone, out PendingFermentation pending) ||
            pending.Sugar != sugar ||
            pending.Yeast != yeast)
        {
            pending = new PendingFermentation
            {
                Sugar = sugar,
                Yeast = yeast,
                ElapsedSeconds = 0f
            };
            pendingFermentations[warmZone] = pending;
        }

        pending.ElapsedSeconds += Time.deltaTime;
        if (pending.ElapsedSeconds < Mathf.Max(0f, fermentationDelaySeconds))
            return;

        TriggerFermentation(sugar, yeast, warmZone);
        pendingFermentations.Remove(warmZone);
    }

    private T FindReactantInsideZone<T>(Collider zoneCollider) where T : Component
    {
        T[] candidates = FindObjectsByType<T>(FindObjectsSortMode.None);

        for (int i = 0; i < candidates.Length; i++)
        {
            T candidate = candidates[i];
            if (candidate != null && candidate.gameObject.activeInHierarchy &&
                zoneCollider.bounds.Contains(candidate.transform.position))
            {
                return candidate;
            }
        }

        return null;
    }

    private void TriggerFermentation(Sugar sugar, Yeast yeast, WarmZone warmZone)
    {
        string pairKey = GetPairKey(sugar.gameObject, yeast.gameObject);
        if (fermentedPairs.Contains(pairKey))
            return;

        fermentedPairs.Add(pairKey);

        Vector3 reactionPosition = (sugar.transform.position + yeast.transform.position) * 0.5f;
        GameObject dough = fermentedDoughPrefab != null
            ? Instantiate(fermentedDoughPrefab, reactionPosition, Quaternion.identity)
            : CreateFallbackDough(reactionPosition);

        dough.SetActive(true);
        dough.name = "FermentedDough";

        if (fermentationSO != null)
        {
            fermentationSO.Position = reactionPosition;
            fermentationSO.Source = dough;
            fermentationSO.Participants = new[] { sugar.gameObject, yeast.gameObject, warmZone.gameObject };
            ReactionEvents.Raise(fermentationSO);
        }

        if (hideReactantsAfterFermentation)
        {
            sugar.gameObject.SetActive(false);
            yeast.gameObject.SetActive(false);
        }
    }

    private static GameObject CreateFallbackDough(Vector3 position)
    {
        GameObject dough = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dough.transform.position = position;
        dough.transform.localScale = new Vector3(0.65f, 0.42f, 0.65f);

        Renderer renderer = dough.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.86f, 0.69f, 0.39f);
        }

        dough.AddComponent<FermentedDough>();
        return dough;
    }

    private static string GetPairKey(GameObject a, GameObject b)
    {
        int idA = a.GetInstanceID();
        int idB = b.GetInstanceID();
        return idA < idB ? idA + "_" + idB : idB + "_" + idA;
    }
}
