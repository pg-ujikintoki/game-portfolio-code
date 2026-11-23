using System;
using System.Collections.Generic;
using UnityEngine;

namespace Preparation
{
    [Serializable]
    public struct PartEquipmentStateEntry
    {
        [SerializeField] private string partId;
        [SerializeField] private bool enabled;

        public string PartId => partId;
        public bool Enabled => enabled;

        public PartEquipmentStateEntry(string partId, bool enabled)
        {
            this.partId = partId != null ? partId.Trim() : string.Empty;
            this.enabled = enabled;
        }
    }

    [Serializable]
    public sealed class PartEquipmentStateSnapshot
    {
        [SerializeField] private List<PartEquipmentStateEntry> entries = new List<PartEquipmentStateEntry>();

        public IReadOnlyList<PartEquipmentStateEntry> Entries => entries;

        public PartEquipmentStateSnapshot()
        {
        }

        public PartEquipmentStateSnapshot(IEnumerable<KeyValuePair<string, bool>> states)
        {
            entries = new List<PartEquipmentStateEntry>();
            if (states == null)
            {
                return;
            }

            foreach (var state in states)
            {
                if (string.IsNullOrWhiteSpace(state.Key))
                {
                    continue;
                }

                entries.Add(new PartEquipmentStateEntry(state.Key, state.Value));
            }

            entries.Sort((left, right) => string.Compare(left.PartId, right.PartId, StringComparison.Ordinal));
        }

        public Dictionary<string, bool> ToDictionary()
        {
            var snapshot = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.PartId))
                {
                    continue;
                }

                snapshot[entry.PartId] = entry.Enabled;
            }

            return snapshot;
        }
    }

    [DisallowMultipleComponent]
    public class PartRuntimeStateManager : MonoBehaviour
    {
        public static PartRuntimeStateManager Instance { get; private set; }

        private readonly Dictionary<string, bool> equipmentEnabledStates = new Dictionary<string, bool>(StringComparer.Ordinal);

        public event Action<PartData, bool> OnEquipmentStateChanged;
        public event Action<PartData> OnActivePartUseRequested;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[PartRuntimeStateManager] Duplicate instance detected. Destroying this instance.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            RefreshOwnedEquipmentStates();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RefreshOwnedEquipmentStates()
        {
            var progress = ResolveProgressManager();
            if (progress == null)
            {
                return;
            }

            foreach (var ownedPartId in progress.OwnedPartIds)
            {
                if (!progress.TryGetPart(ownedPartId, out var part) || part == null)
                {
                    continue;
                }

                if (part.GetEffectivePartType() != PartType.Equipment)
                {
                    continue;
                }

                if (equipmentEnabledStates.ContainsKey(part.PartId))
                {
                    continue;
                }

                SetEquipmentEnabledInternal(progress, part, enabled: true, silent: true);
            }
        }

        public bool IsEquipmentEnabled(string partId)
        {
            if (string.IsNullOrWhiteSpace(partId))
            {
                return false;
            }

            return equipmentEnabledStates.TryGetValue(partId, out var enabled) && enabled;
        }

        public bool TrySetEquipmentEnabled(PartData part, bool enabled, out string reason)
        {
            var progress = ResolveProgressManager();
            if (part == null)
            {
                reason = "パーツが指定されていません。";
                return false;
            }

            if (!part.HasValidId)
            {
                reason = "パーツIDが不正です。";
                return false;
            }

            if (part.GetEffectivePartType() != PartType.Equipment)
            {
                reason = "Equipment パーツではありません。";
                return false;
            }

            if (progress == null)
            {
                reason = "IntermissionProgressManager が見つかりません。";
                return false;
            }

            if (!progress.HasPart(part.PartId))
            {
                reason = "未所持のパーツです。";
                return false;
            }

            SetEquipmentEnabledInternal(progress, part, enabled, silent: false);
            reason = string.Empty;
            return true;
        }

        public bool RequestUseActivePart(PartData part, out string reason)
        {
            var progress = ResolveProgressManager();
            if (part == null)
            {
                reason = "パーツが指定されていません。";
                return false;
            }

            if (!part.HasValidId)
            {
                reason = "パーツIDが不正です。";
                return false;
            }

            if (part.GetEffectivePartType() != PartType.Active)
            {
                reason = "Active パーツではありません。";
                return false;
            }

            if (progress == null)
            {
                reason = "IntermissionProgressManager が見つかりません。";
                return false;
            }

            if (!progress.HasPart(part.PartId))
            {
                reason = "未所持のパーツです。";
                return false;
            }

            OnActivePartUseRequested?.Invoke(part);
            reason = string.Empty;
            return true;
        }

        public IReadOnlyDictionary<string, bool> GetEquipmentStateSnapshot()
        {
            return new Dictionary<string, bool>(equipmentEnabledStates, StringComparer.Ordinal);
        }

        public PartEquipmentStateSnapshot ExportEquipmentStateSnapshot()
        {
            return new PartEquipmentStateSnapshot(equipmentEnabledStates);
        }

        public bool TryImportEquipmentStateSnapshot(PartEquipmentStateSnapshot snapshot, out string reason)
        {
            var progress = ResolveProgressManager();
            if (progress == null)
            {
                reason = "IntermissionProgressManager が見つかりません。";
                return false;
            }

            ResetOwnedEquipmentStates(progress, silent: true);

            if (snapshot == null)
            {
                reason = string.Empty;
                return true;
            }

            foreach (var entry in snapshot.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.PartId))
                {
                    continue;
                }

                if (!progress.HasPart(entry.PartId))
                {
                    continue;
                }

                if (!progress.TryGetPart(entry.PartId, out var part) || part == null)
                {
                    continue;
                }

                if (part.GetEffectivePartType() != PartType.Equipment)
                {
                    continue;
                }

                SetEquipmentEnabledInternal(progress, part, entry.Enabled, silent: false);
            }

            reason = string.Empty;
            return true;
        }

        private void ResetOwnedEquipmentStates(IntermissionProgressManager progress, bool silent)
        {
            equipmentEnabledStates.Clear();

            foreach (var ownedPartId in progress.OwnedPartIds)
            {
                if (!progress.TryGetPart(ownedPartId, out var part) || part == null)
                {
                    continue;
                }

                if (part.GetEffectivePartType() != PartType.Equipment)
                {
                    continue;
                }

                SetEquipmentEnabledInternal(progress, part, enabled: true, silent: silent);
            }
        }

        private void SetEquipmentEnabledInternal(IntermissionProgressManager progress, PartData part, bool enabled, bool silent)
        {
            if (progress == null || part == null || !part.HasValidId)
            {
                return;
            }

            if (enabled)
            {
                DisableConflictingEquipment(progress, part, silent);
            }

            bool changed = !equipmentEnabledStates.TryGetValue(part.PartId, out var currentEnabled) || currentEnabled != enabled;
            equipmentEnabledStates[part.PartId] = enabled;

            if (changed && !silent)
            {
                OnEquipmentStateChanged?.Invoke(part, enabled);
            }
        }

        private void DisableConflictingEquipment(IntermissionProgressManager progress, PartData targetPart, bool silent)
        {
            string groupId = targetPart.EquipmentGroupId;
            if (string.IsNullOrWhiteSpace(groupId))
            {
                return;
            }

            foreach (var ownedPartId in progress.OwnedPartIds)
            {
                if (string.Equals(ownedPartId, targetPart.PartId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!progress.TryGetPart(ownedPartId, out var ownedPart) || ownedPart == null)
                {
                    continue;
                }

                if (ownedPart.GetEffectivePartType() != PartType.Equipment)
                {
                    continue;
                }

                if (!string.Equals(ownedPart.EquipmentGroupId, groupId, StringComparison.Ordinal))
                {
                    continue;
                }

                SetEquipmentEnabledInternal(progress, ownedPart, enabled: false, silent: silent);
            }
        }

        private static IntermissionProgressManager ResolveProgressManager()
        {
            if (IntermissionProgressManager.Instance != null)
            {
                return IntermissionProgressManager.Instance;
            }

            return FindFirstObjectByType<IntermissionProgressManager>(FindObjectsInactive.Include);
        }
    }
}
