using UnityEngine;

public class ReactionLabelDismiss : MonoBehaviour
{
    [SerializeField] private string handTag = "Hand";

    [SerializeField] private float gracePeriod = 0.3f;

    private ReactionLabelController _controller;
    private float _birthTime;

    public void Init(ReactionLabelController controller)
    {
        _controller = controller;
        _birthTime = Time.time;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(handTag) && !IsPokeInteraction(other)) return;
        if (Time.time - _birthTime < gracePeriod) return;
        _controller?.DismissLabel(gameObject);
    }

    private static bool IsPokeInteraction(Collider other)
    {
        Transform t = other.transform;
        while (t != null)
        {
            if (t.name.Contains("Poke") || t.name.Contains("ISDK"))
                return true;
            t = t.parent;
        }
        return false;
    }
}
