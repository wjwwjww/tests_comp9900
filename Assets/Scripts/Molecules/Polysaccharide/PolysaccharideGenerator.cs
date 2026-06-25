using UnityEngine;

public class PolysaccharideGenerator : Molecule
{
    private float beadRadius = 0.01f;
    private float bondLength = 0.025f;
    [SerializeField] private int beadCount = 3;
    [SerializeField] private Material glucoseMaterial;
    private const float multiplier = 2f;
    private const string bondString = "Bond";
    private const string glucoseString = "Glucose_";

    protected override void Awake()
    {
        base.Awake();
        BuildChain();
    }

    private void BuildChain()
    {
        GameObject previous = null;

        for (int i = 0; i < beadCount; i++)
        {
            GameObject bead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bead.name = glucoseString + i;
            bead.transform.parent = transform;
            bead.transform.localPosition = new Vector3(i * bondLength, 0, 0);
            bead.transform.localScale = Vector3.one * beadRadius * multiplier;

            if (glucoseMaterial != null)
                bead.GetComponent<Renderer>().material = glucoseMaterial;

            if (previous != null)
            {
                CreateBond(previous.transform.position, bead.transform.position);
            }

            previous = bead;
        }
    }

    private void CreateBond(Vector3 from, Vector3 to)
    {
        GameObject bond = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bond.name = bondString;
        bond.transform.parent = transform;

        Vector3 midpoint = (from + to) / multiplier;
        bond.transform.position = midpoint;

        bond.transform.up = (to - from).normalized;

        float length = Vector3.Distance(from, to);
        bond.transform.localScale = new Vector3(beadRadius / multiplier, length / multiplier, beadRadius / multiplier);
    }
}
