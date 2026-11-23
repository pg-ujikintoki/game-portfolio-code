using System.Collections.Generic;
using UnityEngine;

namespace Preparation
{
    public enum PartType
    {
        Passive = 0,
        Equipment = 1,
        Active = 2,
    }

    [CreateAssetMenu(fileName = "PartData", menuName = "Game/Intermission/Part Data", order = 0)]
    public class PartData : ScriptableObject
    {
        [SerializeField] private string partId;
        [SerializeField] private string displayName;
        [SerializeField][TextArea] private string description;
        [SerializeField] private int price;
        [SerializeField] private PartType partType;
        [SerializeField] private Sprite icon;
        [SerializeField] private bool initialUnlocked;
        [SerializeField] private Vector2 displayPosition;
        [SerializeField] private string upPartId;
        [SerializeField] private string downPartId;
        [SerializeField] private string leftPartId;
        [SerializeField] private string rightPartId;
        [SerializeField] private List<string> prerequisitePartIds = new List<string>();
        [SerializeField] private string equipmentGroupId;

        public string PartId => partId;
        public string DisplayName => displayName;
        public string Description => description;
        public int Price => Mathf.Max(0, price);
        public PartType PartType => partType;
        public Sprite Icon => icon;
        public bool InitialUnlocked => initialUnlocked;
        public Vector2 DisplayPosition => displayPosition;
        public string UpPartId => upPartId;
        public string DownPartId => downPartId;
        public string LeftPartId => leftPartId;
        public string RightPartId => rightPartId;
        public IReadOnlyList<string> PrerequisitePartIds => prerequisitePartIds;
        public string EquipmentGroupId => equipmentGroupId;

        public bool HasValidId => !string.IsNullOrWhiteSpace(partId);

        public PartType GetEffectivePartType()
        {
            return System.Enum.IsDefined(typeof(PartType), partType)
                ? partType
                : PartType.Active;
        }

        private void OnValidate()
        {
            price = Mathf.Max(0, price);
            equipmentGroupId = equipmentGroupId != null ? equipmentGroupId.Trim() : string.Empty;
        }
    }
}
