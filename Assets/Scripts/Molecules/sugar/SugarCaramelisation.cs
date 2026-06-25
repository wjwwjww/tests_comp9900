using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Sugar))]
public class SugarCaramelisation : MonoBehaviour
{
    [SerializeField] private Caramel caramelPrefab;
    [SerializeField] private ReactionSO caramelisationSO;
    [SerializeField] private ParticleSystem smokePrefab;
    [SerializeField] private float heatingDelaySeconds = 2f;

    private Coroutine _heatingRoutine;
    private HeatZone _activeHeatZone;
    private bool _reactionStarted;
    private bool _caramelised;
    private bool _warnedMissingCaramelPrefab;
    private bool _warnedMissingReactionSO;
    private bool _warnedMissingSmokePrefab;

    private void OnTriggerEnter(Collider other)
    {
        TryStartCaramelisation(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryStartCaramelisation(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_caramelised || _activeHeatZone == null) return;

        HeatZone heatZone = GetHeatZone(other);

        if (heatZone != _activeHeatZone) return;

        if (_heatingRoutine != null)
        {
            StopCoroutine(_heatingRoutine);
            _heatingRoutine = null;
        }

        Debug.Log($"Sugar caramelisation cancelled after exiting HeatZone '{_activeHeatZone.name}'.", this);

        _activeHeatZone = null;
        _reactionStarted = false;
    }

    private void TryStartCaramelisation(Collider other)
    {
        if (_reactionStarted || _caramelised) return;

        HeatZone heatZone = GetHeatZone(other);
        if (heatZone == null) return;

        Debug.Log($"Sugar detected HeatZone '{heatZone.name}'.", this);

        _reactionStarted = true;
        _activeHeatZone = heatZone;
        _heatingRoutine = StartCoroutine(CarameliseAfterDelay(heatZone));

        Debug.Log($"Sugar caramelisation started. Heating delay: {Mathf.Max(0f, heatingDelaySeconds):0.##}s.", this);
    }

    private static HeatZone GetHeatZone(Collider other)
    {
        if (other == null) return null;

        HeatZone heatZone = other.GetComponentInParent<HeatZone>();
        if (heatZone != null) return heatZone;

        Rigidbody attachedRigidbody = other.attachedRigidbody;
        return attachedRigidbody != null
            ? attachedRigidbody.GetComponentInParent<HeatZone>()
            : null;
    }

    private IEnumerator CarameliseAfterDelay(HeatZone heatZone)
    {
        float delay = Mathf.Max(0f, heatingDelaySeconds);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        _heatingRoutine = null;
        Caramelise(heatZone);
    }

    private void Caramelise(HeatZone heatZone)
    {
        if (_caramelised) return;

        _caramelised = true;
        Vector3 reactionPosition = transform.position;
        Quaternion reactionRotation = transform.rotation;

        Caramel caramel = null;
        if (caramelPrefab != null)
        {
            caramel = Instantiate(caramelPrefab, reactionPosition, reactionRotation);
            caramel.gameObject.name = caramelPrefab.name;
            StateManager.Instance?.RegisterMolecule(caramel.gameObject);
        }
        else
        {
            WarnMissingCaramelPrefab();
        }

        PlaySmoke(reactionPosition);
        RaiseCaramelisationEvent(reactionPosition, caramel);

        Debug.Log("Sugar caramelisation completed.", this);

        if (caramel != null)
        {
            Destroy(gameObject);
        }
    }

    private void PlaySmoke(Vector3 position)
    {
        if (smokePrefab == null)
        {
            WarnMissingSmokePrefab();
            return;
        }

        ParticlePoolManager.TryPlayFromPool(smokePrefab, position, Quaternion.identity);
    }

    private void RaiseCaramelisationEvent(Vector3 position, Caramel caramel)
    {
        if (caramelisationSO == null)
        {
            WarnMissingReactionSO();
            return;
        }

        caramelisationSO.Position = position;
        caramelisationSO.Source = caramel != null ? caramel.gameObject : null;
        caramelisationSO.Participants = caramel != null
            ? new[] { caramel.gameObject }
            : null;

        ReactionEvents.Raise(caramelisationSO);
    }

    private void WarnMissingCaramelPrefab()
    {
        if (_warnedMissingCaramelPrefab) return;

        _warnedMissingCaramelPrefab = true;
        Debug.LogWarning("SugarCaramelisation is missing a Caramel prefab. Sugar will not be replaced.", this);
    }

    private void WarnMissingReactionSO()
    {
        if (_warnedMissingReactionSO) return;

        _warnedMissingReactionSO = true;
        Debug.LogWarning("SugarCaramelisation is missing a Caramelisation ReactionSO. ReactionEvents will not be raised.", this);
    }

    private void WarnMissingSmokePrefab()
    {
        if (_warnedMissingSmokePrefab) return;

        _warnedMissingSmokePrefab = true;
        Debug.LogWarning("SugarCaramelisation is missing a smoke ParticleSystem prefab. Smoke will not play.", this);
    }
}
