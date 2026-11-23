using UnityEngine;

public class StageDefinition : MonoBehaviour
{
    [SerializeField] private string stageId;
    [SerializeField] private string displayName;
    [SerializeField] private Vector2 playerStartPosition;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private AudioClip bgm;

    public string StageId => stageId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public Vector2 PlayerStartPosition => playerStartPosition;
    public GameObject BossPrefab => bossPrefab;
    public AudioClip Bgm => bgm;
    public GameObject StagePrefab => gameObject;
}
