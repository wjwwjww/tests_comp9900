using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class DebugUiHeadlockedBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureDebugUi()
    {


        GameObject debugUi = GameObject.Find("DebugUI");
        if (debugUi == null) return;

        Image panel = debugUi.GetComponent<Image>();
        if (panel != null)
        {
            panel.color = new Color(0f, 0f, 0f, 0.75f); // darker background
        }

        DebugUiHeadlockedFollower follower = debugUi.GetComponent<DebugUiHeadlockedFollower>();
        if (follower == null)
        {
            follower = debugUi.AddComponent<DebugUiHeadlockedFollower>();
        }

        RectTransform rootRect = debugUi.GetComponent<RectTransform>();
        if (rootRect != null)
        {
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(20f, -20f);
        }

        Canvas canvas = debugUi.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.worldCamera = null;
            canvas.planeDistance = 1f;
        }

        GraphicRaycaster raycaster = debugUi.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = false;
        }

        Rigidbody rb = debugUi.GetComponent<Rigidbody>();
        if (rb != null) Object.Destroy(rb);

        BoxCollider box = debugUi.GetComponent<BoxCollider>();
        if (box != null) Object.Destroy(box);

        Component grabbable = debugUi.GetComponent("Grabbable");
        if (grabbable != null) Object.Destroy(grabbable);

        TextMeshProUGUI text = debugUi.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(0f, 1f);
            textRect.pivot = new Vector2(0f, 1f);
            textRect.anchoredPosition = new Vector2(10f, -10f);
            textRect.sizeDelta = new Vector2(700f, 420f);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableAutoSizing = false;
            text.fontSize = 72f;
            text.overflowMode = TextOverflowModes.Overflow;
            text.color = Color.white;
        }
    }
}

public class DebugUiHeadlockedFollower : MonoBehaviour
{
    [SerializeField] private Vector3 localHudOffset = new Vector3(-2.6f, 2.3f, 2f);
    [SerializeField] private Vector3 localHudScale = new Vector3(0.0015f, 0.0015f, 0.0015f);

    private Transform _eye;

    private void LateUpdate()
    {
        if (_eye == null)
        {
            _eye = FindCenterEye();
            if (_eye == null) return;
        }

        if (transform.parent != _eye)
        {
            transform.SetParent(_eye, false);
        }

        transform.localPosition = localHudOffset;
        transform.localRotation = Quaternion.identity;
        transform.localScale = localHudScale;
    }

    private static Transform FindCenterEye()
    {
        GameObject centerEye = GameObject.Find("CenterEyeAnchor");
        if (centerEye != null) return centerEye.transform;

        if (Camera.main != null) return Camera.main.transform;

        Camera anyCam = Object.FindObjectOfType<Camera>();
        return anyCam != null ? anyCam.transform : null;
    }
}