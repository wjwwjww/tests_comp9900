using UnityEngine;

public abstract class Molecule : MonoBehaviour
{
    [SerializeField] private Outline outline;
    [SerializeField] private MoleculeSO moleculeSO;

    protected virtual void Awake()
    {
        if (outline != null)
        {
            outline.enabled = false;
        } 
    }

    public string GetDescription()
    {
        return moleculeSO.description;
    }

    public void SetMoleculeSODescription(string description)
    {
        moleculeSO.description = description;
    }

    public MoleculeSO GetMoleculeSO()
    {
        return moleculeSO;
    }
    public virtual string GetState()
    {
        return string.Empty;
    }
}
