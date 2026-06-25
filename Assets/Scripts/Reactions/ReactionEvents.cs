using System;
public enum ReactionType
{
    EmulsifierSnap,
    LipidMerge,
    PolysaccharideSwell,
    Denaturation,
    PolysaccharideGelatinize,
    ProteinBond,
    Retrogradation
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