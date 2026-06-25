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
    [SerializeField] private Button circleButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button trashButton;

    [Header("Settings")]
    [SerializeField] private Vector3 wristOffset = new Vector3(0, 0.05f, -0.05f);
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private float expandAnimDuration = 0.2f;

    private OVRBone wristBone;

    void Start()
    {
        circleButton.onClick.AddListener(OpenMenu);
        closeButton.onClick.AddListener(CloseMenu);
        trashButton.onClick.AddListener(DeleteAll);
        menuPanel.SetActive(false);
        collapsePanel.SetActive(true);
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

    void OpenMenu()
    {
        collapsePanel.SetActive(false);
        menuPanel.SetActive(true);
        StartCoroutine(AnimateScale(menuPanel, Vector3.zero, Vector3.one));
    }

    void CloseMenu()
    {
        StartCoroutine(AnimateScale(menuPanel, Vector3.one, Vector3.zero, () =>
        {
            menuPanel.SetActive(false);
            collapsePanel.SetActive(true);
        }));
    }

    void DeleteAll()
    {
        StateManager.Instance.DestroyAll();
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
}