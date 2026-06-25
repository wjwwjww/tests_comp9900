using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioController : MonoBehaviour
{
    [Header("Mix")]
    [SerializeField] private float baseVolume = 0.8f;
    [SerializeField] private float pitchJitter = 0.05f;

    private AudioSource _audioSource;
    private const float sound3D = 1f;
    private const float initialPitch = 1f;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = sound3D;
    }

    private void OnEnable() => ReactionEvents.Occurred += OnReactionOccurred;
    private void OnDisable() => ReactionEvents.Occurred -= OnReactionOccurred;

    private void OnReactionOccurred(ReactionSO reaction)
    {
        AudioClip clip = reaction.AudioClip;
        if (clip == null) return;

        _audioSource.transform.position = reaction.Position;
        _audioSource.pitch = initialPitch + Random.Range(-pitchJitter, pitchJitter);
        _audioSource.PlayOneShot(clip, baseVolume * reaction.Intensity);
    }
}