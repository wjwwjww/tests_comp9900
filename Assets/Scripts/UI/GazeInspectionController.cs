using UnityEngine;

public class GazeInspectionController : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float maxGazeDistance = 3f;
    [SerializeField] private float gazeRadius = 0.1f;
    [SerializeField] private LayerMask moleculeLayer;

    private Camera _camera;

    private void Start()
    {
        GameObject centerEye = GameObject.Find("CenterEyeAnchor");
        _camera = centerEye != null ? centerEye.GetComponent<Camera>() : Camera.main;
    }

    private void Update()
    {
        PerformanceManager pm = PerformanceManager.Instance;
        if (pm != null && !pm.ShouldRunGazeThisFrame())
        {
            return;
        }

        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null) return;
        }

        Ray gazeRay = new Ray(_camera.transform.position, _camera.transform.forward);

        if (Physics.SphereCast(gazeRay, gazeRadius, out RaycastHit hit, maxGazeDistance, moleculeLayer))
        {
            Molecule molecule = hit.collider.GetComponentInParent<Molecule>();
            if (molecule != null) 
            {
                UIInfoController.Instance?.ShowInfo(molecule, hit.point);
            }
            else
            {
                ClearTarget();
            }
        }
        else
        {
            ClearTarget();
        }
    }

    private void ClearTarget()
    {
        UIInfoController.Instance?.Hide();
    }
}
