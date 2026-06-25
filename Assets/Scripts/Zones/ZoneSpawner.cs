using UnityEngine;

public class ZoneSpawner : MonoBehaviour
{
    [SerializeField] private Zone zone;
    [ContextMenu("Spawn Zone")]
    public void SpawnZone()
    {
        Zone obj = Instantiate(zone, transform.position, transform.rotation);
        StateManager.Instance.RegisterMolecule(obj.gameObject);
    }
}
