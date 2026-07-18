using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WristMenu : MonoBehaviour
{
    [Header("Hand Tracking")]
    [SerializeField] private OVRSkeleton leftHandSkeleton;

    [Header("UI References")]
    [SerializeField] private GameObject collapsePanel;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button tryButton;
    [SerializeField] private Button videoButton;

    [Header("Panels")]
    [SerializeField] private MoleculeSelectionPanel moleculeSelectionPanel;
    [SerializeField] private VideoSelectionPanel videoSelectionPanel;

    [Header("Settings")]
    [SerializeField] private Vector3 wristOffset = new Vector3(0, 0.05f, -0.05f);
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private float expandAnimDuration = 0.2f;

    private OVRBone wristBone;

    void Start()
    {
        closeButton.onClick.AddListener(QuitApplication);
        tryButton.onClick.AddListener(OnTryClicked);
        videoButton.onClick.AddListener(OnVideoClicked);
        menuPanel.SetActive(false);
        collapsePanel.SetActive(true);

        if (collapsePanel != null)
        {
            Button collapseButton = collapsePanel.GetComponentInChildren<Button>();
            if (collapseButton != null)
            {
                collapseButton.onClick.AddListener(OpenMenu);
            }
            else
            {
                Debug.LogWarning("[WristMenu] No Button component found in collapsePanel's hierarchy!");
            }
        }

        if (moleculeSelectionPanel != null)
            moleculeSelectionPanel.gameObject.SetActive(false);

        if (videoSelectionPanel == null)
            videoSelectionPanel = FindVideoSelectionPanel();

        if (videoSelectionPanel != null)
            videoSelectionPanel.gameObject.SetActive(false);

        StartCoroutine(InitWristBone());
    }

    IEnumerator InitWristBone()
    {
        while (leftHandSkeleton == null || !leftHandSkeleton.IsInitialized)
        {
            yield return null;
        }

        foreach (var bone in leftHandSkeleton.Bones)
        {
            if (bone.Id == OVRSkeleton.BoneId.Hand_WristRoot)
            {
                wristBone = bone;
                break;
            }
        }
    }

    void Update()
    {
        AttachToWrist();
    }

    void AttachToWrist()
    {
        if (wristBone == null) return;

        Vector3 targetPos = wristBone.Transform.position + wristBone.Transform.TransformDirection(wristOffset);

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, wristBone.Transform.rotation, Time.deltaTime * smoothSpeed);
    }

    void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnTryClicked()
    {
        if (moleculeSelectionPanel != null)
        {
            moleculeSelectionPanel.gameObject.SetActive(true);
            moleculeSelectionPanel.Open();

            // The molecule panel is a separate, head-locked surface. Hide the
            // wrist UI until the user returns so both poke surfaces cannot overlap.
            menuPanel.SetActive(false);
            collapsePanel.SetActive(false);
            gameObject.SetActive(false);
        }
    }

    void OnVideoClicked()
    {
        if (videoSelectionPanel == null)
            videoSelectionPanel = FindVideoSelectionPanel();

        if (videoSelectionPanel != null)
        {
            videoSelectionPanel.gameObject.SetActive(true);
            videoSelectionPanel.Open();

            menuPanel.SetActive(false);
            collapsePanel.SetActive(false);
            gameObject.SetActive(false);
        }
    }

    public void OpenMenu()
    {
        collapsePanel.SetActive(false);
        menuPanel.SetActive(true);
        StartCoroutine(AnimateScale(menuPanel, Vector3.zero, Vector3.one));
    }

    public void CloseMenu()
    {
        if (!menuPanel.activeSelf)
        {
            collapsePanel.SetActive(true);
            return;
        }

        StartCoroutine(AnimateScale(menuPanel, Vector3.one, Vector3.zero, () =>
        {
            menuPanel.SetActive(false);
            collapsePanel.SetActive(true);
        }));
    }

    IEnumerator AnimateScale(GameObject target, Vector3 from, Vector3 to, System.Action onComplete = null)
    {
        float elapsed = 0f;
        target.transform.localScale = from;

        while (elapsed < expandAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / expandAnimDuration);
            target.transform.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        target.transform.localScale = to;
        onComplete?.Invoke();
    }

    private static VideoSelectionPanel FindVideoSelectionPanel()
    {
        VideoSelectionPanel[] panels = FindObjectsByType<VideoSelectionPanel>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (VideoSelectionPanel panel in panels)
        {
            if (panel.gameObject.name == "VideoSelectionPanel")
                return panel;
        }

        return panels.Length > 0 ? panels[0] : null;
    }
}
