using System.Collections.Generic;
using UnityEngine;

public class MaillardReactionManager : MonoBehaviour
{
    [Header("Reaction Data")]
    [SerializeField] private ReactionSO maillardSO;
    [Header("Product")]
    [SerializeField] private GameObject maillardProductPrefab;
    [Header("VFX")]
    [SerializeField] private ParticleSystem smokePrefab;
    [Header("Reaction Behaviour")]
    [SerializeField] private bool hideReactantsAfterReaction = true;
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 2f;
    [SerializeField] private float scanInterval = 0.2f;

    private readonly HashSet<string> reactedPairs = new HashSet<string>();
    private float nextScanTime;

    private void Update()
    {
        if (Time.time < nextScanTime)
            return;

        nextScanTime = Time.time + scanInterval;

        ScanForMaillardReaction();
    }

    private void ScanForMaillardReaction()
    {
        HeatZone[] heatZones = FindObjectsByType<HeatZone>(FindObjectsSortMode.None);

        foreach (HeatZone heatZone in heatZones)
        {
            if (heatZone == null || !heatZone.gameObject.activeInHierarchy)
                continue;

            TryReactInsideHeatZone(heatZone);
        }
    }

    private void TryReactInsideHeatZone(HeatZone heatZone)
    {
        Collider[] colliders = Physics.OverlapSphere(
            heatZone.transform.position,
            detectionRadius,
            ~0,
            QueryTriggerInteraction.Collide
        );

        Sugar sugar = null;
        Protein protein = null;

        foreach (Collider collider in colliders)
        {
            if (collider == null)
                continue;

            if (sugar == null)
            {
                sugar = collider.GetComponentInParent<Sugar>();
            }

            if (protein == null)
            {
                protein = collider.GetComponentInParent<Protein>();
            }

            if (sugar != null && protein != null)
                break;
        }

        if (sugar == null || protein == null)
            return;

        TriggerMaillard(sugar, protein, heatZone);
    }

    private void TriggerMaillard(Sugar sugar, Protein protein, HeatZone heatZone)
    {
        string pairKey = GetPairKey(sugar.gameObject, protein.gameObject);

        if (reactedPairs.Contains(pairKey))
            return;

        reactedPairs.Add(pairKey);

        Vector3 reactionPosition = (sugar.transform.position + protein.transform.position) * 0.5f;

        GameObject product = null;

        if (maillardProductPrefab != null)
        {
            product = Instantiate(maillardProductPrefab, reactionPosition, Quaternion.identity);
        }

        if (smokePrefab != null)
        {
            ParticleSystem smoke = Instantiate(smokePrefab, reactionPosition, Quaternion.identity);
            smoke.Play();
        }

        if (maillardSO != null)
        {
            maillardSO.Position = reactionPosition;
            maillardSO.Source = product != null ? product : sugar.gameObject;
            maillardSO.Participants = new GameObject[]
            {
                sugar.gameObject,
                protein.gameObject,
                heatZone.gameObject
            };

            ReactionEvents.Raise(maillardSO);
        }
        if (hideReactantsAfterReaction)
        {
            sugar.gameObject.SetActive(false);
            protein.gameObject.SetActive(false);
        }
    }

    private string GetPairKey(GameObject a, GameObject b)
    {
        int idA = a.GetInstanceID();
        int idB = b.GetInstanceID();

        if (idA < idB)
            return idA + "_" + idB;

        return idB + "_" + idA;
    }
}