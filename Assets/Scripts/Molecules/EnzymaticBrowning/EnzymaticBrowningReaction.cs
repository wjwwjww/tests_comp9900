using UnityEngine;

[RequireComponent(typeof(Polyphenol))]
public class EnzymaticBrowningReaction : MonoBehaviour
{
    [Header("Reaction Data")]
    [SerializeField] private ReactionSO enzymaticBrowningSO;

    [Header("Product")]
    [SerializeField] private BrownedPolyphenol brownedPolyphenolPrefab;

    [Header("Optional Feedback")]
    [SerializeField] private ParticleSystem browningVFXPrefab;

    private bool _reactionStarted;

    private void OnTriggerEnter(Collider other)
    {
        if (_reactionStarted) return;

        Oxygen oxygen = GetOxygen(other);
        if (oxygen == null) return;

        if (enzymaticBrowningSO == null)
        {
            Debug.LogWarning(
                "EnzymaticBrowningReaction is missing its ReactionSO.",
                this);
            return;
        }

        if (brownedPolyphenolPrefab == null)
        {
            Debug.LogWarning(
                "EnzymaticBrowningReaction is missing its product prefab.",
                this);
            return;
        }

        if (!oxygen.TryConsume()) return;

        _reactionStarted = true;
        TriggerReaction(oxygen);
    }

    private void TriggerReaction(Oxygen oxygen)
    {
        Vector3 reactionPosition =
            (transform.position + oxygen.transform.position) * 0.5f;
        Quaternion productRotation = transform.rotation;

        DisableColliders(gameObject);
        DisableColliders(oxygen.gameObject);

        if (browningVFXPrefab != null)
        {
            ParticlePoolManager.TryPlayFromPool(
                browningVFXPrefab,
                reactionPosition,
                Quaternion.identity);
        }

        BrownedPolyphenol product = Instantiate(
            brownedPolyphenolPrefab,
            reactionPosition,
            productRotation);

        product.gameObject.name = brownedPolyphenolPrefab.gameObject.name;
        StateManager.Instance?.RegisterMolecule(product.gameObject);

        enzymaticBrowningSO.Position = reactionPosition;
        enzymaticBrowningSO.Source = product.gameObject;
        enzymaticBrowningSO.Participants =
            new[] { product.gameObject };
        ReactionEvents.Raise(enzymaticBrowningSO);

        Destroy(oxygen.gameObject);
        Destroy(gameObject);
    }

    private static Oxygen GetOxygen(Collider other)
    {
        if (other == null) return null;

        Oxygen oxygen = other.GetComponentInParent<Oxygen>();
        if (oxygen != null) return oxygen;

        Rigidbody attachedRigidbody = other.attachedRigidbody;
        return attachedRigidbody != null
            ? attachedRigidbody.GetComponentInParent<Oxygen>()
            : null;
    }

    private static void DisableColliders(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }
}
