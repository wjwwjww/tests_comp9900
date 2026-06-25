using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class Lipid : Molecule
{
    private Vector3 currentScale;
    private bool isMerging;
    [SerializeField] private ReactionSO lipidMergeSO;

    private const string lipidString = "Lipid";

    protected override void Awake()
    {
        base.Awake();
        lipidMergeSO.Source = gameObject;
    }

    void Start()
    {
        currentScale = transform.localScale;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isMerging) return;

        if (!other.CompareTag(lipidString)) return;

        Lipid otherLipid = other.GetComponent<Lipid>();
        if (otherLipid == null) return;
        if (otherLipid == this || otherLipid.isMerging) return;

        if (GetInstanceID() < otherLipid.GetInstanceID())
        {
            Merge(otherLipid);
        }
    }

    private void Merge(Lipid other)
    {
        if (other == null || other == this) return;
        if (isMerging || other.isMerging) return;

        isMerging = true;
        other.isMerging = true;

        StartCoroutine(MergeAnimation(other));
    }

    private IEnumerator MergeAnimation(Lipid other)
    {
        if (other == null)
        {
            isMerging = false;
            yield break;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        Rigidbody otherRb = other.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        if (otherRb != null) otherRb.isKinematic = true;

        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale + other.transform.localScale;

        Vector3 otherStartPos = other.transform.position;
        Vector3 otherStartScale = other.transform.localScale;
        Vector3 targetPos = (transform.position + other.transform.position) * 0.5f;

        float duration = 0.3f;
        float t = 0f;

        while (t < duration)
        {
            if (other == null)
            {
                if (rb != null) rb.isKinematic = false;
                isMerging = false;
                yield break;
            }

            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - k, 3f); // ease-out

            transform.position = Vector3.Lerp(transform.position, targetPos, eased);
            transform.localScale = Vector3.Lerp(startScale, targetScale, eased);

            other.transform.position = Vector3.Lerp(otherStartPos, transform.position, eased);
            other.transform.localScale = Vector3.Lerp(otherStartScale, Vector3.zero, eased);

            yield return null;
        }

        currentScale = targetScale;
        transform.localScale = currentScale;
        if (rb != null) rb.isKinematic = false;

        if (other == null)
        {
            isMerging = false;
            yield break;
        }

        lipidMergeSO.Participants = new[] { gameObject, other.gameObject };
        lipidMergeSO.Position = (transform.position + other.transform.position) * 0.5f;
        ReactionEvents.Raise(lipidMergeSO);

        Destroy(other.gameObject);
        isMerging = false;
    }
}