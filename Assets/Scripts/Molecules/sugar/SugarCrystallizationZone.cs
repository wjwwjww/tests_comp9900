using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SugarCrystallizationZone : MonoBehaviour
{
    [Header("Reaction Output")]
    [SerializeField] private SugarCrystal sugarCrystalPrefab;
    [SerializeField] private ReactionSO crystallizationSO;
    [SerializeField] private ParticleSystem crystalParticles;

    [Header("Timing")]
    [SerializeField] private float crystallizationDelaySeconds = 3f;

    private readonly List<Sugar> sugars = new List<Sugar>();
    private readonly List<Water> waters = new List<Water>();

    private bool isReacting;

    private void OnTriggerEnter(Collider other)
    {
        Sugar sugar = other.GetComponentInParent<Sugar>();
        if (sugar != null && !sugars.Contains(sugar))
        {
            sugars.Add(sugar);
        }

        Water water = other.GetComponentInParent<Water>();
        if (water != null && !waters.Contains(water))
        {
            waters.Add(water);
        }

        TryStartCrystallization();
    }

    private void OnTriggerStay(Collider other)
    {
    OnTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        Sugar sugar = other.GetComponentInParent<Sugar>();
        if (sugar != null)
        {
            sugars.Remove(sugar);
        }

        Water water = other.GetComponentInParent<Water>();
        if (water != null)
        {
            waters.Remove(water);
        }
    }

    private void TryStartCrystallization()
    {
        if (isReacting) return;

        if (sugars.Count > 0 && waters.Count > 0)
        {
            StartCoroutine(CrystallizeAfterDelay());
        }
    }

    private IEnumerator CrystallizeAfterDelay()
{
    isReacting = true;

    sugars.RemoveAll(s => s == null);
    waters.RemoveAll(w => w == null);

    if (sugars.Count == 0 || waters.Count == 0)
    {
        isReacting = false;
        yield break;
    }

    Sugar sugar = sugars[0];
    Water water = waters[0];

    Vector3 spawnPosition = (sugar.transform.position + water.transform.position) * 0.5f;

    yield return StartCoroutine(ShrinkObject(sugar.gameObject, 1.2f));
    yield return new WaitForSeconds(0.8f);

    GameObject seed = null;

    if (sugarCrystalPrefab != null)
    {
        seed = Instantiate(sugarCrystalPrefab.gameObject, spawnPosition, Quaternion.identity);
        seed.name = "Crystal Seed";
        seed.transform.localScale = Vector3.one * 0.03f;
    }

    yield return new WaitForSeconds(0.8f);

    if (seed != null)
    {
        yield return StartCoroutine(GrowObject(seed, Vector3.one * 0.15f, 2f));
    }

    if (crystalParticles != null)
    {
        ParticlePoolManager.TryPlayFromPool(crystalParticles, spawnPosition, Quaternion.identity);
    }

    SugarCrystal crystal = seed != null ? seed.GetComponent<SugarCrystal>() : null;

    if (crystal != null)
    {
        StateManager.Instance?.RegisterMolecule(crystal.gameObject);
    }

    RaiseCrystallizationEvent(spawnPosition, crystal, sugar, water);

    Destroy(sugar.gameObject);
    Destroy(water.gameObject);

    sugars.Clear();
    waters.Clear();

    isReacting = false;
}

private IEnumerator ShrinkObject(GameObject obj, float duration)
{
    if (obj == null) yield break;

    Vector3 startScale = obj.transform.localScale;
    Vector3 endScale = startScale * 0.2f;

    float timer = 0f;

    while (timer < duration)
    {
        timer += Time.deltaTime;
        float t = timer / duration;

        obj.transform.localScale = Vector3.Lerp(startScale, endScale, t);

        yield return null;
    }

    obj.transform.localScale = endScale;
}

private IEnumerator GrowObject(GameObject obj, Vector3 targetScale, float duration)
{
    if (obj == null) yield break;

    Vector3 startScale = obj.transform.localScale;

    float timer = 0f;

    while (timer < duration)
    {
        timer += Time.deltaTime;
        float t = timer / duration;

        obj.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

        yield return null;
    }

    obj.transform.localScale = targetScale;
}

    private void RaiseCrystallizationEvent(Vector3 position, SugarCrystal crystal, Sugar sugar, Water water)
    {
        if (crystallizationSO == null) return;

        crystallizationSO.Position = position;
        crystallizationSO.Source = crystal != null ? crystal.gameObject : null;
        crystallizationSO.Participants = new GameObject[]
        {
            sugar.gameObject,
            water.gameObject
        };

        ReactionEvents.Raise(crystallizationSO);
    }
}