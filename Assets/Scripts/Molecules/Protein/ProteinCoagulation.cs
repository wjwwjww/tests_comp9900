using UnityEngine;

[RequireComponent(typeof(Protein))]
public class ProteinCoagulation : MonoBehaviour
{
    [Header("Prefabs to Spawn")]
    [SerializeField] private GameObject solidifiedProteinPrefab;
    [SerializeField] private ParticleSystem solidificationParticlesPrefab;

    [Header("Reaction Config")]
    [SerializeField] private ReactionSO coagulationReactionSO;

    private bool _coagulated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_coagulated) return;

        if (other.TryGetComponent<Acid>(out Acid acid))
        {
            _coagulated = true;
            TriggerCoagulation(acid);
        }
    }

    private void TriggerCoagulation(Acid acid)
    {
        Vector3 spawnPos = transform.position;
        Quaternion spawnRot = transform.rotation;

        if (solidificationParticlesPrefab != null)
        {
            ParticlePoolManager.TryPlayFromPool(solidificationParticlesPrefab, spawnPos, Quaternion.identity);
        }

        GameObject solidifiedObj = null;

        if (solidifiedProteinPrefab != null)
        {
            solidifiedObj = Instantiate(solidifiedProteinPrefab, spawnPos, spawnRot);
            StateManager.Instance?.RegisterMolecule(solidifiedObj);
        }

        if (coagulationReactionSO != null)
        {
            coagulationReactionSO.Position = spawnPos;
            coagulationReactionSO.Source = solidifiedObj;
            coagulationReactionSO.Participants = solidifiedObj != null 
                ? new GameObject[] { solidifiedObj } 
                : null;
            ReactionEvents.Raise(coagulationReactionSO);
        }

        Destroy(acid.gameObject);
        Destroy(gameObject);
    }
}