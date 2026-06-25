using UnityEngine;

public class MoleculeSpawner : MonoBehaviour
{
    [SerializeField] private Molecule molecule;
    [ContextMenu("Spawn Molecule")]
    public void SpawnMolecule()
    {
        Molecule obj = Instantiate(molecule, transform.position, transform.rotation);
        StateManager.Instance.RegisterMolecule(obj.gameObject);
    }
}