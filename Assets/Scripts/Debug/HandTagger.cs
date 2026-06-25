using UnityEngine;

public class HandTagger : MonoBehaviour
{
    private void Start()
    {
        Invoke(nameof(TagHandColliders), 0.5f);
    }

    private void TagHandColliders()
    {
        int count = 0;
        foreach (CapsuleCollider col in GetComponentsInChildren<CapsuleCollider>(true))
        {
            col.gameObject.tag = "Hand";
            count++;
        }
        Debug.Log($"[HandTagger] Tagged {count} hand capsule colliders.");
    }
}