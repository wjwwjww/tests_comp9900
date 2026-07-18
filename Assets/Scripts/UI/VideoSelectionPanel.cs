using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Head-locked reaction video selection panel.
/// Reaction buttons are created manually in the Canvas.
/// </summary>
public class VideoSelectionPanel : MonoBehaviour
{
    [Header("Head-Locked Settings")]
    [SerializeField] private float panelDistance = 0.5f;
    [SerializeField] private float panelHeight = -0.1f;
    [SerializeField] private Vector3 panelScale = new Vector3(0.005f, 0.005f, 0.005f);

    [Header("UI")]
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Reaction Buttons")]
    [SerializeField] private List<ReactionButton> reactionButtons = new();

    [Header("References")]
    [SerializeField] private WristMenu wristMenu;
    [SerializeField] private ReactionVideo reactionVideo;

    private Transform eye;
    private CanvasGroup canvasGroup;
    private bool isOpen;
    private int selectedReactionIndex = -1;

    public int SelectedReactionIndex => selectedReactionIndex;

    [System.Serializable]
    public class ReactionButton
    {
        public Button button;
        public ReactionSO reaction;
    }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (wristMenu == null)
            wristMenu = FindFirstWristMenu();

        if (reactionVideo == null)
            reactionVideo = FindFirstReactionVideo();

        if (backButton != null)
            backButton.onClick.AddListener(GoBack);

        for (int i = 0; i < reactionButtons.Count; i++)
        {
            ReactionButton entry = reactionButtons[i];

            if (entry.button == null)
                continue;

            int reactionIndex = i;
            entry.button.onClick.AddListener(() => SelectReaction(reactionIndex));
        }

        gameObject.SetActive(false);
    }

    public void Open()
    {
        if (isOpen)
            return;

        isOpen = true;

        gameObject.SetActive(true);

        PositionInFrontOfUser();

        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (titleText != null)
            titleText.text = "Choose videos";

        StartCoroutine(AnimateIn());
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;

        StartCoroutine(AnimateOut());
    }

    public void GoBack()
    {
        Close();

        if (wristMenu != null)
        {
            wristMenu.gameObject.SetActive(true);
            wristMenu.OpenMenu();
        }
    }

    private void SelectReaction(int index)
    {
        if (index < 0 || index >= reactionButtons.Count)
            return;

        ReactionSO reaction = reactionButtons[index].reaction;

        if (reaction == null)
        {
            Debug.LogWarning("No reaction assigned to this video button.");
            return;
        }

        selectedReactionIndex = index;
        RefreshButtonHighlights();
        ReactionEvents.Raise(reaction);

        if (reaction.VideoClip != null && reactionVideo != null)
            reactionVideo.Play(reaction.VideoClip);

        Debug.Log("Selected reaction video: " + reaction.name);
    }

    private void RefreshButtonHighlights()
    {
        for (int i = 0; i < reactionButtons.Count; i++)
        {
            if (reactionButtons[i].button == null)
                continue;

            Image image = reactionButtons[i].button.GetComponent<Image>();

            if (image == null)
                continue;

            image.color = i == selectedReactionIndex
                ? new Color(0.08f, 0.48f, 0.78f, 1)
                : Color.white;
        }
    }

    private void PositionInFrontOfUser()
    {
        if (eye == null)
            eye = FindCenterEye();

        if (eye == null)
            return;

        Vector3 position =
            eye.position +
            eye.forward * panelDistance +
            Vector3.up * panelHeight;

        transform.position = position;

        Vector3 lookDir = transform.position - eye.position;
        lookDir.y = 0;

        transform.rotation = Quaternion.LookRotation(lookDir);

        transform.localScale = panelScale;
    }

    private IEnumerator AnimateIn()
    {
        float duration = 0.25f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.SmoothStep(0, 1, elapsed / duration);

            canvasGroup.alpha = t;

            yield return null;
        }

        canvasGroup.alpha = 1;

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private IEnumerator AnimateOut()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float duration = 0.15f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            canvasGroup.alpha = 1 - elapsed / duration;

            yield return null;
        }

        canvasGroup.alpha = 0;

        gameObject.SetActive(false);
    }

    private static Transform FindCenterEye()
    {
        GameObject eye = GameObject.Find("CenterEyeAnchor");

        if (eye != null)
            return eye.transform;

        if (Camera.main != null)
            return Camera.main.transform;

        Camera cam = FindFirstObjectByType<Camera>();

        return cam != null ? cam.transform : null;
    }

    private static WristMenu FindFirstWristMenu()
    {
        WristMenu[] wristMenus = FindObjectsByType<WristMenu>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        return wristMenus.Length > 0 ? wristMenus[0] : null;
    }

    private static ReactionVideo FindFirstReactionVideo()
    {
        ReactionVideo[] reactionVideos = FindObjectsByType<ReactionVideo>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        return reactionVideos.Length > 0 ? reactionVideos[0] : null;
    }
}
