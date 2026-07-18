using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ReactionVideo : MonoBehaviour
{
    [SerializeField] private Transform vrCamera;
    [SerializeField] private VideoPlayer videoPlayer;

    [Tooltip("Meters in front of user")]
    [SerializeField] private float distance;
    [SerializeField] private float yOffset = 0.1f;
    [SerializeField] private Button closeButton;

    [Tooltip("Negative = below eye level (more comfortable)")]

    private bool isStreaming;

    void Start()
    {
        isStreaming = false;

        if (videoPlayer != null)
            videoPlayer.loopPointReached += HandleVideoEnd;

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        gameObject.SetActive(false);
    }

    private void OnEnable() => ReactionEvents.Occurred += OnReactionOccurred;
    private void OnDisable() => ReactionEvents.Occurred -= OnReactionOccurred;

    private void OnReactionOccurred(ReactionSO evt)
    {
        Play(evt.VideoClip);
    }

    void OnDestroy() 
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= HandleVideoEnd;

        ReactionEvents.Occurred -= OnReactionOccurred;
    }

    private void HandleVideoEnd(VideoPlayer videoPlayer)
    {
        Hide();
    }

    private void SetPosition()
    {
        if (vrCamera == null)
            return;

        Vector3 pos = vrCamera.position + vrCamera.forward * distance;
        pos.y += yOffset;
        transform.position = pos;
        transform.rotation = vrCamera.rotation;
    }

    public void Play(VideoClip clip)
    {
        if (isStreaming || clip == null || videoPlayer == null)
            return;

        videoPlayer.clip = clip;
        Show();
    }

    private void Show()
    {
        isStreaming = true;
        SetPosition();
        gameObject.SetActive(true);
        videoPlayer.Play();
    }

    public void Hide()
    {
        isStreaming = false;

        if (videoPlayer != null)
            videoPlayer.Stop();

        gameObject.SetActive(false);
    }
}
