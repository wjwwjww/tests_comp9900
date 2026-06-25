using UnityEngine;

public static class ProteinBondUtils
{
    private const float BONDS_LENGTH = 0.020f;
    private const float BONDS_RADIUS = 0.004f;
    private const int NUM_SEGMENTS = 5;
    private const float HYDROGEN_BOND_DISTANCE = 0.5f;

    private const string COVALENT_BONDS = "Covalent Bonds";
    private const string HYDROGEN_BONDS = "Hydrogen Bonds";
    private const string BOND = "Bond";

    private static bool isCovalentBond = true;

    public static GameObject CreateBonds(Vector3 p1, Vector3 p2, Transform proteinParent, Material bondMaterial)
    {
        isCovalentBond = !isCovalentBond;
        return isCovalentBond ? CreateCovalentBonds(p1, p2, proteinParent, bondMaterial) : CreateHydrogenBonds(p1, p2, proteinParent, bondMaterial);
    }

    private static GameObject CreateCovalentBonds(Vector3 p1, Vector3 p2, Transform proteinParent, Material bondMaterial)
    {
        GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Capsule);

        cyl.name = COVALENT_BONDS;
        Object.Destroy(cyl.GetComponent<Collider>());

        Vector3 dir = p2 - p1;
        cyl.transform.position = (p1 + p2) * 0.5f;
        cyl.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        cyl.transform.localScale = new Vector3(BONDS_RADIUS, BONDS_LENGTH, BONDS_RADIUS);
        cyl.transform.SetParent(proteinParent, worldPositionStays: true);
        cyl.GetComponent<MeshRenderer>().material = bondMaterial;
        return cyl;
    }

    private static GameObject CreateHydrogenBonds(Vector3 p1, Vector3 p2, Transform proteinParent, Material bondMaterial)
    {
        Vector3 direction = p2 - p1;
        float totalDistance = direction.magnitude;
        Vector3 dirNorm = direction.normalized;

        GameObject bondGroup = new GameObject(HYDROGEN_BONDS);
        bondGroup.transform.SetParent(proteinParent, worldPositionStays: true);

        for (int i = 0; i < NUM_SEGMENTS; i++)
        {
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.Destroy(cyl.GetComponent<Collider>());

            float t = (i + HYDROGEN_BOND_DISTANCE) / NUM_SEGMENTS;
            Vector3 center = p1 + dirNorm * totalDistance * t;

            cyl.transform.position = center;
            cyl.transform.rotation = Quaternion.FromToRotation(Vector3.up, dirNorm);
            cyl.transform.localScale = new Vector3(BONDS_RADIUS, BONDS_LENGTH / NUM_SEGMENTS, BONDS_RADIUS);
            cyl.name = BOND;

            cyl.GetComponent<MeshRenderer>().material = bondMaterial;
            cyl.transform.SetParent(bondGroup.transform, worldPositionStays: true);
        }

        return bondGroup;
    }
}