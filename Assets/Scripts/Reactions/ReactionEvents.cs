using System;
public enum ReactionType
{
    EmulsifierSnap,
    LipidMerge,
    PolysaccharideSwell,
    Denaturation,
    PolysaccharideGelatinize,
    ProteinBond,
    Retrogradation,
    Caramelisation,
    ProteinCoagulation,    //add reaction for protein and acid
    Maillard // add reaction for maillard
}

public static class ReactionEvents
{
    public static float initialIntensity = 1f;
    public static event Action<ReactionSO> Occurred;

    public static void Raise(ReactionSO reactionEvent)
    {
        Occurred?.Invoke(reactionEvent);
    }
}
