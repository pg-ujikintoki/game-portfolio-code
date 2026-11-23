using System;
using System.Collections.Generic;
using UnityEngine;

namespace Preparation
{
    public class IntermissionProgressManager : MonoBehaviour
    {
        public static IntermissionProgressManager Instance { get; private set; }

        [Header("Initial Settings")]
        [SerializeField] private int startingDiamonds = 0;
        [SerializeField] private PartData[] initialOwnedParts;
        [SerializeField] private bool verboseLogging;

        private readonly HashSet<string> ownedPartIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> unlockedPartIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, PartData> partCatalog = new Dictionary<string, PartData>(StringComparer.Ordinal);

        private PlayerCurrencyWallet currencyWallet;
        private bool ownsLocalWallet;

        public event Action<int> OnDiamondsChanged;
        public event Action<int> OnCurrencyChanged;
        public event Action<PartData> OnPartPurchased;

        public int OwnedDiamonds => currencyWallet != null ? currencyWallet.Balance : 0;
        public IEnumerable<string> OwnedPartIds => ownedPartIds;
        public IEnumerable<string> UnlockedPartIds => unlockedPartIds;
        public IReadOnlyDictionary<string, PartData> PartCatalog => partCatalog;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[IntermissionProgressManager] Duplicate instance detected. Destroying this instance.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupCurrencyWallet();
            InitializeInventory();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (currencyWallet != null)
            {
                currencyWallet.OnCurrencyChanged -= HandleWalletCurrencyChanged;
            }
        }

        public void InitializeInventory()
        {
            if (currencyWallet == null)
            {
                SetupCurrencyWallet();
            }

            bool shouldNotifyManually = true;

            if (ownsLocalWallet && currencyWallet != null)
            {
                int clamped = Mathf.Max(0, startingDiamonds);
                bool balanceChanged = currencyWallet.Balance != clamped;
                currencyWallet.SetBalance(clamped);
                shouldNotifyManually = !balanceChanged;
            }

            ownedPartIds.Clear();
            unlockedPartIds.Clear();

            if (initialOwnedParts != null)
            {
                foreach (var part in initialOwnedParts)
                {
                    if (part == null || !part.HasValidId)
                    {
                        continue;
                    }

                    RegisterPartInternal(part, false);
                    ownedPartIds.Add(part.PartId);
                    unlockedPartIds.Add(part.PartId);
                }
            }

            if (shouldNotifyManually)
            {
                NotifyDiamondsChanged();
            }
        }

        public void RegisterKnownParts(IEnumerable<PartData> parts, bool clearExistingCatalog = false)
        {
            if (parts == null)
            {
                return;
            }

            if (clearExistingCatalog)
            {
                partCatalog.Clear();
            }

            var seenIdsInBatch = new HashSet<string>(StringComparer.Ordinal);

            foreach (var part in parts)
            {
                if (part == null)
                {
                    continue;
                }

                if (!part.HasValidId)
                {
                    Debug.LogWarning($"[IntermissionProgressManager] Part asset '{part.name}' has an empty partId.");
                    continue;
                }

                if (!seenIdsInBatch.Add(part.PartId))
                {
                    Debug.LogWarning($"[IntermissionProgressManager] Duplicate partId '{part.PartId}' detected within the provided collection.");
                }

                RegisterPartInternal(part, true);

                if (part.InitialUnlocked)
                {
                    unlockedPartIds.Add(part.PartId);
                }

                if (ownedPartIds.Contains(part.PartId))
                {
                    unlockedPartIds.Add(part.PartId);
                }
            }
        }

        public bool HasPart(string partId)
        {
            if (string.IsNullOrWhiteSpace(partId))
            {
                return false;
            }

            return ownedPartIds.Contains(partId);
        }

        public bool IsPartUnlocked(string partId)
        {
            if (string.IsNullOrWhiteSpace(partId))
            {
                return false;
            }

            return unlockedPartIds.Contains(partId) || ownedPartIds.Contains(partId);
        }

        public void UnlockPart(string partId)
        {
            if (string.IsNullOrWhiteSpace(partId))
            {
                return;
            }

            unlockedPartIds.Add(partId);
        }

        public void UnlockPart(PartData part)
        {
            if (part == null || !part.HasValidId)
            {
                return;
            }

            UnlockPart(part.PartId);
            RegisterPartInternal(part, false);
        }

        public void AddDiamonds(int amount)
        {
            if (currencyWallet == null)
            {
                Debug.LogWarning("[IntermissionProgressManager] 通貨ウォレットが初期化されていません。");
                return;
            }

            currencyWallet.TryAdd(amount);
        }

        public void SetDiamonds(int amount)
        {
            if (currencyWallet == null)
            {
                Debug.LogWarning("[IntermissionProgressManager] 通貨ウォレットが初期化されていません。");
                return;
            }

            currencyWallet.SetBalance(amount);
        }

        public bool CanAfford(PartData part)
        {
            if (part == null)
            {
                return false;
            }

            return OwnedDiamonds >= part.Price;
        }

        public bool ArePurchaseConditionsMet(PartData part, out string reason)
        {
            if (part == null)
            {
                reason = "パーツが指定されていません。";
                return false;
            }

            foreach (var prerequisiteId in part.PrerequisitePartIds)
            {
                if (string.IsNullOrWhiteSpace(prerequisiteId))
                {
                    continue;
                }

                if (!HasPart(prerequisiteId))
                {
                    reason = $"前提パーツ『{prerequisiteId}』が未購入です。";
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        public bool PurchasePart(PartData part, out string message)
        {
            if (part == null)
            {
                message = "パーツが指定されていません。";
                return false;
            }

            if (!part.HasValidId)
            {
                message = "パーツのIDが正しく設定されていません。";
                return false;
            }

            if (HasPart(part.PartId))
            {
                message = "すでに購入済みです。";
                return false;
            }

            if (!ArePurchaseConditionsMet(part, out var conditionReason))
            {
                message = conditionReason;
                return false;
            }

            if (currencyWallet == null)
            {
                message = "通貨ウォレットが初期化されていません。";
                return false;
            }

            if (!CanAfford(part))
            {
                message = "ダイヤが足りません。";
                return false;
            }

            if (!currencyWallet.TrySpend(part.Price))
            {
                message = "ダイヤが足りません。";
                return false;
            }

            ownedPartIds.Add(part.PartId);
            unlockedPartIds.Add(part.PartId);
            RegisterPartInternal(part, false);

            Debug.Log($"[IntermissionProgressManager] Purchased part: Name='{part.DisplayName}', PartId='{part.PartId}', Price={part.Price}, RemainingDiamonds={OwnedDiamonds}", this);

            bool isOwnedAfterPurchase = HasPart(part.PartId);
            bool isUnlockedAfterPurchase = IsPartUnlocked(part.PartId);
            Debug.Log($"[IntermissionProgressManager] Purchase reflection check: PartId='{part.PartId}', IsOwned={isOwnedAfterPurchase}, IsUnlocked={isUnlockedAfterPurchase}", this);

            OnPartPurchased?.Invoke(part);

            message = $"{part.DisplayName} を購入しました。";
            return true;
        }

        public bool TryGetPart(string partId, out PartData part)
        {
            if (string.IsNullOrWhiteSpace(partId))
            {
                part = null;
                return false;
            }

            return partCatalog.TryGetValue(partId, out part);
        }

        public void ResetOwnedParts()
        {
            ownedPartIds.Clear();
            unlockedPartIds.Clear();

            if (initialOwnedParts != null)
            {
                foreach (var part in initialOwnedParts)
                {
                    if (part == null || !part.HasValidId)
                    {
                        continue;
                    }

                    RegisterPartInternal(part, false);
                    ownedPartIds.Add(part.PartId);
                    unlockedPartIds.Add(part.PartId);
                }
            }
        }

        public List<string> CreateOwnedPartIdSnapshot()
        {
            return CreateSortedIdList(ownedPartIds);
        }

        public List<string> CreateUnlockedPartIdSnapshot()
        {
            return CreateSortedIdList(unlockedPartIds);
        }

        public bool TryRestorePartState(IEnumerable<string> ownedPartIdsSnapshot, IEnumerable<string> unlockedPartIdsSnapshot, out string reason)
        {
            if (currencyWallet == null)
            {
                SetupCurrencyWallet();
            }

            var restoredOwnedPartIds = SanitizePartIds(ownedPartIdsSnapshot);
            var restoredUnlockedPartIds = SanitizePartIds(unlockedPartIdsSnapshot);

            ownedPartIds.Clear();
            unlockedPartIds.Clear();

            foreach (var partId in restoredOwnedPartIds)
            {
                ownedPartIds.Add(partId);
                unlockedPartIds.Add(partId);
            }

            foreach (var partId in restoredUnlockedPartIds)
            {
                unlockedPartIds.Add(partId);
            }

            reason = string.Empty;
            return true;
        }

        private void NotifyDiamondsChanged()
        {
            NotifyCurrencyListeners(OwnedDiamonds);
        }

        private void NotifyCurrencyListeners(int amount)
        {
            OnDiamondsChanged?.Invoke(amount);
            OnCurrencyChanged?.Invoke(amount);

            if (verboseLogging)
            {
                Debug.Log($"[IntermissionProgressManager] Diamonds updated: {amount}");
            }
        }

        private void SetupCurrencyWallet()
        {
            if (currencyWallet != null)
            {
                currencyWallet.OnCurrencyChanged -= HandleWalletCurrencyChanged;
                currencyWallet = null;
            }

            var progress = GameProgressManager.Instance;
            if (progress != null)
            {
                currencyWallet = progress.CurrencyWallet;
                ownsLocalWallet = false;
            }
            else
            {
                currencyWallet = new PlayerCurrencyWallet(Mathf.Max(0, startingDiamonds));
                ownsLocalWallet = true;
            }

            if (currencyWallet != null)
            {
                currencyWallet.OnCurrencyChanged += HandleWalletCurrencyChanged;
            }
        }

        private void HandleWalletCurrencyChanged(int amount)
        {
            NotifyCurrencyListeners(amount);
        }

        private void RegisterPartInternal(PartData part, bool warnOnDuplicate)
        {
            if (part == null || !part.HasValidId)
            {
                return;
            }

            if (partCatalog.TryGetValue(part.PartId, out var existingPart) && existingPart != part && warnOnDuplicate)
            {
                Debug.LogWarning($"[IntermissionProgressManager] Duplicate partId '{part.PartId}' detected between assets '{existingPart.name}' and '{part.name}'. Array order will determine which is referenced.");
            }

            partCatalog[part.PartId] = part;
        }

        private static List<string> CreateSortedIdList(IEnumerable<string> source)
        {
            var ids = new List<string>();
            if (source == null)
            {
                return ids;
            }

            foreach (var id in source)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                ids.Add(id.Trim());
            }

            ids.Sort(StringComparer.Ordinal);
            return ids;
        }

        private static HashSet<string> SanitizePartIds(IEnumerable<string> source)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (source == null)
            {
                return ids;
            }

            foreach (var id in source)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                ids.Add(id.Trim());
            }

            return ids;
        }
    }
}
