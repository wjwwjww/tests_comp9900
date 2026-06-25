using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ControlPoint { public float x, y, z, w; }

[System.Serializable]
public class ProteinData
{
    public int order;
    public List<ControlPoint> control_points;
    public List<float> knot_vector;
}

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ProteinTest : MonoBehaviour
{
    [SerializeField] private TextAsset proteinJson;

    [Range(3, 16)] public int tubeSides = 8;
    [Range(0.005f, 0.1f)] public float tubeRadius = 0.03f;
    [Range(10, 300)] public int numSamples = 100;   // how many points to sample along the NURBS curve

    // ── loaded data ──
    private List<Vector3> controlPoints = new List<Vector3>();
    private List<float> weights = new List<float>();
    private List<float> knotVector = new List<float>();
    private int order = 4;

    void Start()
    {
        LoadProteinData();

        List<Vector3> curvePoints = SampleNURBS(numSamples);
        Mesh mesh = BuildTubeMesh(curvePoints);
        GetComponent<MeshFilter>().mesh = mesh;

        if (GetComponent<MeshRenderer>().sharedMaterial == null)
            GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));

        Debug.Log($"Protein built — {controlPoints.Count} control points, {curvePoints.Count} sampled curve points.");
    }

    // ─────────────────────────────────────────────
    // JSON Loading
    // ─────────────────────────────────────────────

    void LoadProteinData()
    {
        if (proteinJson == null)
        {
            Debug.LogError("ProteinBuilder: No JSON assigned.");
            return;
        }

        ProteinData data = JsonUtility.FromJson<ProteinData>(proteinJson.text);

        order = data.order;

        controlPoints = data.control_points
            .Select(p => new Vector3(p.x, p.y, p.z))
            .ToList();

        weights = data.control_points
            .Select(p => p.w)
            .ToList();

        knotVector = data.knot_vector;

        Debug.Log($"Loaded — order: {order}, points: {controlPoints.Count}, knots: {knotVector.Count}");
    }

    // ─────────────────────────────────────────────
    // NURBS Evaluation
    // ─────────────────────────────────────────────

    // Sample the NURBS curve at evenly spaced parameter values
    List<Vector3> SampleNURBS(int samples)
    {
        List<Vector3> result = new List<Vector3>();

        float tMin = knotVector[order - 1];
        float tMax = knotVector[knotVector.Count - order];

        for (int i = 0; i < samples; i++)
        {
            // Subtract tiny epsilon at the end to avoid boundary issues with basis function
            float t = Mathf.Lerp(tMin, tMax - 0.0001f, (float)i / (samples - 1));
            result.Add(EvaluateNURBS(t));
        }

        return result;
    }

    // Evaluate a point on the NURBS curve at parameter t
    // C(t) = Sum(N_i,k(t) * w_i * P_i) / Sum(N_i,k(t) * w_i)
    Vector3 EvaluateNURBS(float t)
    {
        int n = controlPoints.Count - 1;

        Vector3 numerator = Vector3.zero;
        float denominator = 0f;

        for (int i = 0; i <= n; i++)
        {
            float basis = BSplineBasis(i, order, t);
            float w = weights[i];

            numerator += basis * w * controlPoints[i];
            denominator += basis * w;
        }

        return denominator == 0f ? Vector3.zero : numerator / denominator;
    }

    // Recursive Cox-de Boor B-Spline basis function N_i,k(t)
    float BSplineBasis(int i, int k, float t)
    {
        if (k == 1)
            return (t >= knotVector[i] && t < knotVector[i + 1]) ? 1f : 0f;

        float left = 0f;
        float right = 0f;

        float dLeft = knotVector[i + k - 1] - knotVector[i];
        float dRight = knotVector[i + k] - knotVector[i + 1];

        if (dLeft != 0f) left = ((t - knotVector[i]) / dLeft) * BSplineBasis(i, k - 1, t);
        if (dRight != 0f) right = ((knotVector[i + k] - t) / dRight) * BSplineBasis(i + 1, k - 1, t);

        return left + right;
    }

    // ─────────────────────────────────────────────
    // Tube Mesh Generation
    // ─────────────────────────────────────────────

    Mesh BuildTubeMesh(List<Vector3> points)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        // Vertex rings along the curve
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 forward = i < points.Count - 1
                ? (points[i + 1] - points[i]).normalized
                : (points[i] - points[i - 1]).normalized;

            if (forward == Vector3.zero) forward = Vector3.forward;

            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
            float v = (float)i / (points.Count - 1);

            for (int s = 0; s < tubeSides; s++)
            {
                float angle = s * Mathf.PI * 2f / tubeSides;
                Vector3 offset = rot * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * tubeRadius;
                vertices.Add(points[i] + offset);
                normals.Add(offset.normalized);
                uvs.Add(new Vector2((float)s / tubeSides, v));
            }
        }

        // Triangles connecting rings
        for (int i = 0; i < points.Count - 1; i++)
        {
            for (int s = 0; s < tubeSides; s++)
            {
                int a = i * tubeSides + s;
                int b = i * tubeSides + (s + 1) % tubeSides;
                int c = (i + 1) * tubeSides + s;
                int d = (i + 1) * tubeSides + (s + 1) % tubeSides;

                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        // End caps
        AddEndCap(ref vertices, ref normals, ref uvs, ref triangles,
                  points[0], -(points[1] - points[0]).normalized, 0, true);

        AddEndCap(ref vertices, ref normals, ref uvs, ref triangles,
                  points[points.Count - 1],
                  (points[points.Count - 1] - points[points.Count - 2]).normalized,
                  (points.Count - 1) * tubeSides, false);

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    void AddEndCap(
        ref List<Vector3> vertices,
        ref List<Vector3> normals,
        ref List<Vector2> uvs,
        ref List<int> triangles,
        Vector3 center,
        Vector3 capNormal,
        int ringStartIndex,
        bool isStartCap)
    {
        int capCenter = vertices.Count;
        vertices.Add(center);
        normals.Add(capNormal);
        uvs.Add(new Vector2(0.5f, 0.5f));

        for (int s = 0; s < tubeSides; s++)
        {
            int a = ringStartIndex + s;
            int b = ringStartIndex + (s + 1) % tubeSides;

            if (isStartCap) { triangles.Add(capCenter); triangles.Add(b); triangles.Add(a); }
            else { triangles.Add(capCenter); triangles.Add(a); triangles.Add(b); }
        }
    }

    // ─────────────────────────────────────────────
    // Gizmos — shows raw control points in Scene view
    // ─────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (controlPoints == null) return;
        Gizmos.color = Color.cyan;
        foreach (var p in controlPoints)
            Gizmos.DrawSphere(p, 0.02f);
    }
}