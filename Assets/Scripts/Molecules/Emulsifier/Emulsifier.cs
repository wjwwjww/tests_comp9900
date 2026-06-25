using UnityEngine;
using UnityEngine.Video;

public class Emulsifier : Molecule
{
    [SerializeField] private ReactionSO emulsifierSnapSO;


    protected override void Awake()
    {
        base.Awake();
        emulsifierSnapSO.Source = gameObject;
    }

    public ReactionSO GetEmulsifierSnapReactionSO()
    {
        return emulsifierSnapSO;
    }
}
