using UnityEngine;

public class BillboardLabel : MonoBehaviour
{
    [SerializeField] private bool lockVerticalAxis = true;

    private static Camera _cachedCamera;

    private void LateUpdate()
    {
        if (_cachedCamera == null)
        {
            GameObject centerEye = GameObject.Find("CenterEyeAnchor");
            _cachedCamera = centerEye != null ? centerEye.GetComponent<Camera>() : Camera.main;
        }
        if (_cachedCamera == null) return;

        Vector3 directionToCamera = _cachedCamera.transform.position - transform.position;

        if (lockVerticalAxis)
            directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(-directionToCamera.normalized);
    }
}
