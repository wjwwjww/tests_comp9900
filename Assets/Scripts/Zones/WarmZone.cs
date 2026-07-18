using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WarmZone : Zone
{
    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }
}
