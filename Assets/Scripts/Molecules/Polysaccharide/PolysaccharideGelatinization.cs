using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(PolysaccharideSwelling))]
public class PolysaccharideGelatinization : MonoBehaviour
{
    [SerializeField] private Molecule amyloseGelPrefab;
    [SerializeField] private float burstToGelDelay = 0.35f;
    [SerializeField] private float gelExpandDuration = 3f;
    [SerializeField] private ReactionSO polysaccharideGelatinizeSO;

    private PolysaccharideSwelling _swelling;
    private bool _gelatinized = false;
    private const string meshesString = "Meshes";
    private const string amyloseGelString = "AmyloseGel";

    void Awake()
    {
        _swelling = GetComponent<PolysaccharideSwelling>();
        polysaccharideGelatinizeSO.Source = gameObject;
    }

    public void OnHeatZoneEnter()
    {
        if (_gelatinized) return;
        if (!_swelling.IsSwelled) return;

        _gelatinized = true;
        StartCoroutine(GelatinizeSequence());
    }

    private IEnumerator GelatinizeSequence()
    {
        Transform[] children = GetComponentsInChildren<Transform>();

        yield return new WaitForSeconds(burstToGelDelay);

        Vector3 spawnPosition = GetComponentInChildren<Renderer>().bounds.center;

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {    
            r.enabled = false;
        } 

        Molecule gel = null;
        if (amyloseGelPrefab != null)
        {
            gel = Instantiate(amyloseGelPrefab, spawnPosition, Quaternion.identity);
            StateManager.Instance?.RegisterMolecule(gel.gameObject);
            gel.gameObject.name = amyloseGelString;

            Transform meshesTransform = gel.gameObject.transform.Find(meshesString);
            Vector3 targetScale = meshesTransform.localScale;
            meshesTransform.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < gelExpandDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / gelExpandDuration);
                meshesTransform.localScale = targetScale * t;
                yield return null;
            }
            meshesTransform.localScale = targetScale;
        }

        polysaccharideGelatinizeSO.Position = transform.position;
        polysaccharideGelatinizeSO.Participants = new[] { gameObject };
        ReactionEvents.Raise(polysaccharideGelatinizeSO);

        Destroy(gameObject);
    }
}
