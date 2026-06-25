using UnityEngine;
using TMPro;

public class UIInfoController : MonoBehaviour
{
    public static UIInfoController Instance { get; private set; }

    [Header("Panel References")]
    [SerializeField] private GameObject inspectionPanel;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI stateText;

    [Header("Positioning")]
    [SerializeField] private float panelDistanceFromCamera = 0.6f;
    [SerializeField] private float panelVerticalOffset = 0.05f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (inspectionPanel == null || !inspectionPanel.scene.IsValid())
            inspectionPanel = GameObject.Find("InspectionPanel");

        if (inspectionPanel != null)
            inspectionPanel.SetActive(false);
    }

    public void ShowInfo(Molecule molecule, Vector3 hitPoint)
    {
        MoleculeSO moleculeSO = molecule.GetMoleculeSO();
        typeText.text  = moleculeSO.type;
        descriptionText.text = moleculeSO.description;     
        stateText.text = molecule.GetState() == string.Empty ? string.Empty : molecule.GetState();

        PositionPanel(hitPoint);

        if (inspectionPanel != null && !inspectionPanel.activeSelf)
            inspectionPanel.SetActive(true);
    }

    public void Hide()
    {
        if (inspectionPanel != null && inspectionPanel.activeSelf)
            inspectionPanel.SetActive(false);
    }

    [ContextMenu("Test Show Panel")]
    private void TestShowPanel()
    {
        if (inspectionPanel == null || !inspectionPanel.scene.IsValid())
            inspectionPanel = GameObject.Find("InspectionPanel");

        if (typeText  != null) typeText.text  = "Protein";
        if (descriptionText != null) descriptionText.text = "A folded chain of amino acids in its native conformation. Functional and stable at room temperature. Denatures under heat.";
        if (stateText != null) stateText.text = "State: Native";

        Camera cam = GetCamera();
        if (inspectionPanel != null)
        {
            if (cam != null)
                inspectionPanel.transform.position = cam.transform.position + cam.transform.forward * 0.6f;
            inspectionPanel.SetActive(true);
        }
    }

    private void PositionPanel(Vector3 hitPoint)
    {
        Camera cam = GetCamera();
        if (cam == null || inspectionPanel == null) return;

        Vector3 forward = (hitPoint - cam.transform.position).normalized;
        Vector3 panelPos = cam.transform.position
                         + forward * panelDistanceFromCamera
                         + Vector3.up * panelVerticalOffset;

        inspectionPanel.transform.position = panelPos;
    }

    private static Camera GetCamera()
    {
        GameObject centerEye = GameObject.Find("CenterEyeAnchor");
        return centerEye != null ? centerEye.GetComponent<Camera>() : Camera.main;
    }
}
