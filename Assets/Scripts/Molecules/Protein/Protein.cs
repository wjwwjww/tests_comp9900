using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;
using Oculus.Interaction;

[RequireComponent(typeof(AudioSource))]
public class Protein : Molecule
{

    [SerializeField] private SkinnedMeshRenderer[] meshes;

    [Header("Denaturation Feedback")]
    [SerializeField] private ParticleSystem denatureSteamPrefab;
    [SerializeField] private ReactionSO proteinDenaturationSO;
    [SerializeField] private ReactionSO proteinBondSO;
    [SerializeField] private float denatureVolume = 0.8f;
    [SerializeField] private float denaturePitchJitter = 0.05f;

    [SerializeField] private Material bondMaterial;

    private State state;
    private bool _denatureFeedbackPlayed;
    private float _denatureWeight;
    private const float INFINITY = Mathf.Infinity;
    private const float threshold = 100f;
    private const string denaturedDesc = "Native protein structure disrupted by thermal energy. Peptide bonds remain intact but tertiary structure is lost.";
    private const string bondedDesc = "Stabilizing intramolecular bonds form between amino acid residues, reinforcing the protein structure. Key interactions are locked in place, representing a folding intermediate or stabilized conformation.";

    private enum State
    {
        Native,
        Denatured,
        Bonding
    }



    protected override void Awake()
    {
        base.Awake();
        state = State.Native;
        proteinDenaturationSO.Source = proteinBondSO.Source = gameObject;
    }
    public void SetDenatureWeight(float weight)
    {
        _denatureWeight = weight;

        for (int i = 0; i < meshes.Length; i++)
        {
            meshes[i].SetBlendShapeWeight(0, weight);
        }

        if (weight >= threshold && !_denatureFeedbackPlayed)
        {
            Denature();
            _denatureFeedbackPlayed = true;
        }
        else if (weight < threshold)
        {
            _denatureFeedbackPlayed = false;
        }
    }

    public float GetDenatureWeight()
    {
        return _denatureWeight;
    }

    private void EmitDenatureSteam()
    {
        if (denatureSteamPrefab == null) return;

        ParticlePoolManager.TryPlayFromPool(denatureSteamPrefab, GetVisualCenter(), Quaternion.identity);
    }


    private Vector3 GetVisualCenter()
    {
        if (meshes == null || meshes.Length == 0)
        {
            return transform.position;
        }

        Bounds bounds = meshes[0].bounds;
        for (int i = 1; i < meshes.Length; i++)
        {
            if (meshes[i] != null)
            {
                bounds.Encapsulate(meshes[i].bounds);
            }
        }

        return bounds.center;
    }

    public void Denature()
    {
        if (state == State.Denatured) return;
        SetDenatured();
        proteinDenaturationSO.Position = transform.position;
        ReactionEvents.Raise(proteinDenaturationSO);

        EmitDenatureSteam();
    }

    public bool IsDenatured()
    {
        return state == State.Denatured;
    }

    public bool IsBonded()
    {
        return state == State.Bonding;
    }

    private void SetDenatured()
    {
        SetMoleculeSODescription(denaturedDesc);
        state = State.Denatured;
    }

    public void SetBonded()
    {
        SetMoleculeSODescription(bondedDesc);
        state = State.Bonding;
    }

    public bool IsNative()
    {
        return state == State.Native;
    }
    public override string GetState()
    {
        return state.ToString();   
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsDenatured() || IsBonded()) return;
        if (other.TryGetComponent<Protein>(out Protein otherProtein) && otherProtein.IsDenatured())
        {
            if (GetInstanceID() < otherProtein.GetInstanceID())
                Bonding(otherProtein);
        }
    }


    private void Bonding(Protein other)
    {
        SetBonded();
        other.SetBonded();

        Vector3 center = GetComponent<SphereCollider>().bounds.center;
        Vector3 otherCenter = other.GetComponent<SphereCollider>().bounds.center;

        proteinBondSO.Position = (center + otherCenter) * 0.5f;
        proteinBondSO.Participants =  new[] { gameObject, other.gameObject };
        ReactionEvents.Raise(proteinBondSO);

        GameObject cyl = ProteinBondUtils.CreateBonds(center, otherCenter, this.transform, bondMaterial);

        Rigidbody rbOther = other.GetComponent<Rigidbody>();

        FixedJoint bondJoint = gameObject.AddComponent<FixedJoint>();
        bondJoint.connectedBody = rbOther;
        bondJoint.breakForce = INFINITY;
        bondJoint.breakTorque = INFINITY;

        Collider myCol = GetComponent<Collider>();
        Collider otherCol = other.GetComponent<Collider>();
        Physics.IgnoreCollision(myCol, otherCol, true);
    }
    [ContextMenu("Test Denature")]
    private void TestDenature() => Denature();
}
