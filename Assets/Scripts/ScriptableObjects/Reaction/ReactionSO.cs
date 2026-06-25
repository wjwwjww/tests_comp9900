using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "ReactionSO", menuName = "Scriptable Objects/ReactionSO")]
public class ReactionSO : ScriptableObject
{
    public ReactionType Type;
    public float Intensity;
    public Vector3 Position;
    public GameObject Source;
    public GameObject[] Participants = null;
    public VideoClip VideoClip;
    public string ReactionName;
    public string ReactionDescription;
    public AudioClip AudioClip;
    public Color color;
}
