using System.Collections;
using UnityEngine;

public class FermentedDough : Molecule
{
    [SerializeField] private float expansionMultiplier = 1.45f;
    [SerializeField] private float expansionDuration = 0.8f;
    [SerializeField] private int bubbleCount = 8;

    private bool hasAnimated;

    private void OnEnable()
    {
        if (!hasAnimated)
        {
            StartCoroutine(AnimateFermentation());
        }
    }

    private IEnumerator AnimateFermentation()
    {
        hasAnimated = true;

        Vector3 initialScale = transform.localScale;
        Vector3 expandedScale = initialScale * Mathf.Max(1f, expansionMultiplier);
        float elapsed = 0f;

        while (elapsed < expansionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / expansionDuration);
            transform.localScale = Vector3.Lerp(initialScale, expandedScale, t * t * (3f - 2f * t));
            yield return null;
        }

        transform.localScale = expandedScale;
        CreateBubbles();
    }

    private void CreateBubbles()
    {
        for (int i = 0; i < Mathf.Max(0, bubbleCount); i++)
        {
            GameObject bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bubble.name = "FermentationBubble";
            bubble.transform.SetParent(transform, false);
            bubble.transform.localScale = Vector3.one * 0.08f;
            bubble.transform.localPosition = new Vector3(
                Random.Range(-0.35f, 0.35f),
                Random.Range(0.1f, 0.45f),
                Random.Range(-0.35f, 0.35f)
            );

            Collider bubbleCollider = bubble.GetComponent<Collider>();
            if (bubbleCollider != null)
            {
                Destroy(bubbleCollider);
            }

            Renderer bubbleRenderer = bubble.GetComponent<Renderer>();
            if (bubbleRenderer != null)
            {
                bubbleRenderer.material.color = new Color(1f, 0.95f, 0.72f);
            }

            StartCoroutine(RiseBubble(bubble.transform));
        }
    }

    private IEnumerator RiseBubble(Transform bubble)
    {
        Vector3 start = bubble.localPosition;
        Vector3 end = start + Vector3.up * 0.35f;
        float elapsed = 0f;

        while (elapsed < 1f && bubble != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed);
            bubble.localPosition = Vector3.Lerp(start, end, t);
            bubble.localScale = Vector3.one * Mathf.Lerp(0.08f, 0.015f, t);
            yield return null;
        }

        if (bubble != null)
        {
            Destroy(bubble.gameObject);
        }
    }
}
