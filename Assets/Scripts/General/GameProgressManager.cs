using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
#endif

public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance;

    private const string StagePrefabFolder = "Assets/Prefab/Stage/Stages";

    [SerializeField] private GameObject[] stagePrefabs;
    [FormerlySerializedAs("currentStage")]
    [SerializeField] private int currentStageNumber = 1;

    [Header("Player Session")]
    [SerializeField] private int startingCurrency = 0;

    public PlayerCurrencyWallet CurrencyWallet
    {
        get
        {
            if (currencyWallet == null)
            {
                InitializeCurrencyWallet(false);
            }

            return currencyWallet;
        }
    }

    public int CurrentCurrency => CurrencyWallet != null ? CurrencyWallet.Balance : 0;

    public int CurrentStageIndex => Mathf.Max(0, currentStageNumber - 1);
    public int CurrentStageNumber => currentStageNumber;
    public int MaxStageCount => stagePrefabs != null ? stagePrefabs.Length : 0;
    public GameObject CurrentStagePrefab => HasStageSequence
        ? stagePrefabs[Mathf.Clamp(CurrentStageIndex, 0, stagePrefabs.Length - 1)]
        : null;
    public StageDefinition CurrentStage => CurrentStagePrefab != null
        ? CurrentStagePrefab.GetComponent<StageDefinition>()
        : null;
    public bool HasStageSequence => stagePrefabs != null && stagePrefabs.Length > 0;

    private PlayerCurrencyWallet currencyWallet;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
#if UNITY_EDITOR
            if (!HasStageSequence)
            {
                SyncStagesFromFolder();
            }
#endif
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            NormalizeProgress();
            InitializeCurrencyWallet(true);
            Debug.Log($"[GameProgressManager] Awake stagePrefabs={MaxStageCount} currentStageNumber={currentStageNumber} currentStagePrefab={(CurrentStagePrefab != null ? CurrentStagePrefab.name : "null")} currency={CurrentCurrency}");
        }
        else
        {
            Debug.Log("[GameProgressManager] Duplicate instance destroyed.");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SyncStagesFromFolder();
        NormalizeProgress();
        startingCurrency = Mathf.Max(0, startingCurrency);
    }

    [ContextMenu("Sync Stage Prefabs")]
    private void SyncStagesFromFolder()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { StagePrefabFolder });
        stagePrefabs = prefabGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .Where(prefab => prefab != null)
            .Where(prefab => prefab.GetComponent<StageDefinition>() != null)
            .OrderBy(prefab => prefab.GetComponent<StageDefinition>().StageId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
#endif

    public void ResetProgress()
    {
        currentStageNumber = 1;
        NormalizeProgress();
        ResetCurrencyToInitial();
        Debug.Log($"[GameProgressManager] ResetProgress currentStagePrefab={(CurrentStagePrefab != null ? CurrentStagePrefab.name : "null")} currency={CurrentCurrency}");
    }

    public string GetCurrentStageLabel()
    {
        return CurrentStage != null ? CurrentStage.DisplayName : "Debug Mode";
    }

    public bool TryAddCurrency(int amount)
    {
        return CurrencyWallet != null && CurrencyWallet.TryAdd(amount);
    }

    public bool TrySpendCurrency(int amount)
    {
        return CurrencyWallet != null && CurrencyWallet.TrySpend(amount);
    }

    public bool HasCurrency(int amount)
    {
        return CurrencyWallet != null && CurrencyWallet.HasEnough(amount);
    }

    public void ResetCurrencyToInitial()
    {
        InitializeCurrencyWallet(true);
    }

    private void NormalizeProgress()
    {
        if (!HasStageSequence)
        {
            currentStageNumber = 1;
            return;
        }

        currentStageNumber = Mathf.Clamp(currentStageNumber, 1, stagePrefabs.Length);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var stageManager = FindFirstObjectByType<StageManager>(FindObjectsInactive.Include);
        Debug.Log($"[GameProgressManager] SceneLoaded scene={scene.name} mode={mode} stageManager={(stageManager != null ? stageManager.name : "null")} rootCount={scene.rootCount}");
    }

    private void InitializeCurrencyWallet(bool forceReset)
    {
        int clampedInitial = Mathf.Max(0, startingCurrency);

        if (currencyWallet == null)
        {
            currencyWallet = new PlayerCurrencyWallet(clampedInitial);
            return;
        }

        if (forceReset)
        {
            currencyWallet.SetBalance(clampedInitial);
        }
    }
}
