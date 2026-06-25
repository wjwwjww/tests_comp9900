using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReactionGlowController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private float emissionMultiplier = 2f;

    private const string emission = "_EMISSION";
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private readonly Dictionary<Renderer, Coroutine> activeFades = new Dictionary<Renderer, Coroutine>();

    private void OnEnable() => ReactionEvents.Occurred += OnReactionOccurred;
    private void OnDisable() => ReactionEvents.Occurred -= OnReactionOccurred;

    private void OnReactionOccurred(ReactionSO evt)
    {
        Color baseColor = evt.color;
        float peak = Mathf.Max(0.05f, evt.Intensity) * emissionMultiplier;

        if (evt.Participants != null && evt.Participants.Length > 0)
        {
            for (int i = 0; i < evt.Participants.Length; i++)
            {
                if (evt.Participants[i] != null)
                {
                    TriggerGlowOnObject(evt.Participants[i], baseColor * peak);
                }
            }
        }
        else if (evt.Source != null)
        {
            TriggerGlowOnObject(evt.Source, baseColor * peak);
        }
    }

    private void TriggerGlowOnObject(GameObject go, Color peakEmission)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            EnableEmissionKeyword(r);

            if (activeFades.TryGetValue(r, out Coroutine running))
            {
                StopCoroutine(running);
            }

            activeFades[r] = StartCoroutine(FadeEmission(r, peakEmission));
        }
    }

    private IEnumerator FadeEmission(Renderer renderer, Color peakEmission)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        float t = 0f;

        while (t < fadeDuration)
        {
            float k = 1f - (t / fadeDuration);
            renderer.GetPropertyBlock(block);
            block.SetColor(EmissionColorId, peakEmission * k);
            renderer.SetPropertyBlock(block);

            t += Time.deltaTime;
            yield return null;
        }

        renderer.GetPropertyBlock(block);
        block.SetColor(EmissionColorId, Color.black);
        renderer.SetPropertyBlock(block);

        activeFades.Remove(renderer);
    }

    private static void EnableEmissionKeyword(Renderer renderer)
    {
        Material[] mats = renderer.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
        {
            Material m = mats[i];
            if (m != null && m.HasProperty(EmissionColorId))
            {
                m.EnableKeyword(emission);
            }
        }
    }
}