using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Head-locked molecule selection panel.
/// Molecule buttons are created manually in the Canvas.
/// </summary>
public class MoleculeSelectionPanel : MonoBehaviour
{
    [Header("Head-Locked Settings")]
    [SerializeField] private float panelDistance = 0.5f;
    [SerializeField] private float panelHeight = -0.1f;
    [SerializeField] private Vector3 panelScale = new Vector3(0.005f, 0.005f, 0.005f);

    [Header("UI")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button trashButton;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Molecule Buttons")]
    [SerializeField] private List<MoleculeButton> moleculeButtons = new();

    [Header("Zone Buttons")]
    [SerializeField] private Button iceButton;
    [SerializeField] private GameObject icePrefab;
    [SerializeField] private Button heatButton;
    [SerializeField] private GameObject heatPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnDistance = 0.8f;
    [SerializeField] private float spawnVerticalOffset = 0.2f;

    [Header("References")]
    [SerializeField] private WristMenu wristMenu;

    private Transform eye;
    private CanvasGroup canvasGroup;
    private bool isOpen;

    [System.Serializable]
    public class MoleculeButton
    {
        public Button button;
        public Molecule moleculePrefab;
    }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        //-----------------------
        // Back Button
        //-----------------------
        if (backButton != null)
            backButton.onClick.AddListener(GoBack);

        //-----------------------
        // Trash Button
        //-----------------------
        if (trashButton != null)
            trashButton.onClick.AddListener(ClearAllMolecules);

        //-----------------------
        // Molecule Buttons
        //-----------------------
        foreach (var entry in moleculeButtons)
        {
            if (entry.button == null || entry.moleculePrefab == null)
                continue;

            Molecule prefab = entry.moleculePrefab;

            entry.button.onClick.AddListener(() =>
            {
                SpawnMolecule(prefab);
            });
        }

        //-----------------------
        // Ice and Heat Buttons
        //-----------------------
        if (iceButton != null && icePrefab != null)
        {
            iceButton.onClick.AddListener(() => SpawnGameObject(icePrefab));
        }

        if (heatButton != null && heatPrefab != null)
        {
            heatButton.onClick.AddListener(() => SpawnGameObject(heatPrefab));
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
            titleText.text = "Select a Molecule";

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

    public void ClearAllMolecules()
    {
        StateManager.Instance?.DestroyAll();
    }

    private void SpawnMolecule(Molecule prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("No molecule prefab assigned.");
            return;
        }

        Camera cam = GetEyeCamera();

        if (cam == null)
        {
            Debug.LogWarning("No camera found.");
            return;
        }

        Vector3 spawnPos =
            cam.transform.position +
            cam.transform.forward * spawnDistance;

        spawnPos.y += spawnVerticalOffset;

        Molecule spawned =
            Instantiate(prefab, spawnPos, Quaternion.identity);

        StateManager.Instance?.RegisterMolecule(spawned.gameObject);

        Debug.Log("Spawned " + prefab.name);
    }

    private void SpawnGameObject(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("No prefab assigned.");
            return;
        }

        Camera cam = GetEyeCamera();

        if (cam == null)
        {
            Debug.LogWarning("No camera found.");
            return;
        }

        Vector3 spawnPos =
            cam.transform.position +
            cam.transform.forward * spawnDistance;

        spawnPos.y += spawnVerticalOffset;

        GameObject spawned =
            Instantiate(prefab, spawnPos, Quaternion.identity);

        StateManager.Instance?.RegisterMolecule(spawned);

        Debug.Log("Spawned " + prefab.name);
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

    private static Camera GetEyeCamera()
    {
        GameObject eye = GameObject.Find("CenterEyeAnchor");

        if (eye != null)
            return eye.GetComponent<Camera>();

        return Camera.main;
    }
}