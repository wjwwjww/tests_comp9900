using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class PolysaccharideSwelling : MonoBehaviour
{
    private float swelledScale = 2f;
    private float swellDuration = 1.5f;

    [Range(0f, 1f)]
    [SerializeField] private float deswellTarget = 0.25f;

    [Header("Hydration VFX")]
    [SerializeField] private ParticleSystem hydrationStartParticlesPrefab;
    [SerializeField] private ReactionSO polysaccharideSwellSO;
    [SerializeField] private ReactionSO retrogradationSO;

    private bool _isSwelling = false;
    private bool _isDeswelling = false;

    public bool IsSwelled { get; private set; } = false;

    private Transform[] _children;
    private Vector3[] _originalScales;
    private const string glucoseString = "Glucose_";
    private const string waterString = "Water";
    private const string fridgeString = "Fridge";

    void Awake()
    {
        _children = GetComponentsInChildren<Transform>();
        _originalScales = new Vector3[_children.Length];

        for (int i = 0; i < _children.Length; i++)
        {
            _originalScales[i] = _children[i].localScale;
        }

        retrogradationSO.Source = polysaccharideSwellSO.Source = gameObject;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(waterString) && !_isSwelling && !IsSwelled)
        {
            Swelling(other);
        }

        if (other.CompareTag(fridgeString) && IsSwelled && !_isDeswelling)
        {
            retrogradationSO.Participants = new[] { gameObject, other.gameObject };
            retrogradationSO.Position = other.transform.position;
            ReactionEvents.Raise(retrogradationSO);
            StartCoroutine(DeswellBeads());
        }
    }

    private void EmitHydrationStartParticles(Vector3 position)
    {
        if (hydrationStartParticlesPrefab == null) return;

        ParticlePoolManager.TryPlayFromPool(hydrationStartParticlesPrefab, position, Quaternion.identity);
    }

    private void Swelling(Collider other)
    {
        Vector3 contactPoint = other.ClosestPoint(transform.position);
        EmitHydrationStartParticles(contactPoint);

        polysaccharideSwellSO.Position = other.transform.position;
        polysaccharideSwellSO.Participants = new[] { gameObject, other.gameObject };
        ReactionEvents.Raise(polysaccharideSwellSO);
        StartCoroutine(SwellBeads());
        Destroy(other.gameObject);
    }

    public void ForceSwelled()
    {
        if (IsSwelled) return;

        Transform[] children = GetComponentsInChildren<Transform>();

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name.StartsWith(glucoseString))
            {
                children[i].localScale *= swelledScale;
            }
        }

        IsSwelled = true;
    }

    IEnumerator SwellBeads()
    {
        _isSwelling = true;

        float elapsed = 0f;

        Vector3[] startScales = new Vector3[_children.Length];
        for (int i = 0; i < _children.Length; i++)
        {
            startScales[i] = _children[i].localScale;
        }

        while (elapsed < swellDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / swellDuration);

            for (int i = 0; i < _children.Length; i++)
            {
                if (_children[i].name.StartsWith(glucoseString))
                {
                    Vector3 target = _originalScales[i] * swelledScale;

                    _children[i].localScale = Vector3.Lerp(
                        startScales[i],
                        target,
                        t
                    );
                }
            }

            yield return null;
        }

        IsSwelled = true;
        _isSwelling = false;
    }

    IEnumerator DeswellBeads()
    {
        _isDeswelling = true;

        float elapsed = 0f;
        while (elapsed < swellDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / swellDuration);

            for (int i = 0; i < _children.Length; i++)
            {
                if (_children[i].name.StartsWith(glucoseString))
                {
                    Vector3 swelled = _originalScales[i] * swelledScale;
                    Vector3 partial = _originalScales[i] * Mathf.Lerp(1f, swelledScale, deswellTarget);
                    _children[i].localScale = Vector3.Lerp(swelled, partial, t);
                }
            }

            yield return null;
        }

        IsSwelled = false;
        _isDeswelling = false;
    }    
}
