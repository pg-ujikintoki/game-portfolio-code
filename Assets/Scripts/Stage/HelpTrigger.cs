using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HelpTrigger : MonoBehaviour
{
    [Header("Help Settings")]
    [SerializeField] HelpSequence sequence;
    [SerializeField] bool autoShowOnEnter = false;

    [Header("References")]
    [SerializeField] PauseManager pauseManager;
    [SerializeField] GameObject indicator;

    int playerOverlapCount;
    bool isPlayerInside;

    void Reset()
    {
        var collider = GetComponent<Collider2D>();
        if (collider)
        {
            collider.isTrigger = true;
        }
    }

    void Awake()
    {
        if (!pauseManager)
        {
            pauseManager = FindFirstObjectByType<PauseManager>();
        }
    }

    void OnEnable()
    {
        UpdateIndicatorState();
    }

    void OnDisable()
    {
        if (!isPlayerInside)
        {
            return;
        }

        isPlayerInside = false;
        playerOverlapCount = 0;
        UpdateIndicatorState();
        NotifySignboard(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerOverlapCount++;
        isPlayerInside = true;
        UpdateIndicatorState();

        if (playerOverlapCount == 1)
        {
            NotifySignboard(true);
        }

        if (playerOverlapCount > 1)
        {
            return;
        }

        if (!sequence)
        {
            Debug.LogWarning($"[HelpTrigger] HelpSequence が未割り当てです: {name}");
            return;
        }

        if (!pauseManager)
        {
            Debug.LogWarning($"[HelpTrigger] PauseManager が見つかりません: {name}");
            return;
        }

        pauseManager.SetActiveHelp(sequence, this, autoShowOnEnter);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerOverlapCount = Mathf.Max(0, playerOverlapCount - 1);
        if (playerOverlapCount > 0)
        {
            return;
        }

        isPlayerInside = false;
        UpdateIndicatorState();
        NotifySignboard(false);

        if (!pauseManager)
        {
            return;
        }

        pauseManager.ClearActiveHelp(this);
    }

    bool IsPlayer(Collider2D colliderToCheck)
    {
        if (!colliderToCheck)
        {
            return false;
        }

        return colliderToCheck.GetComponentInParent<PlayerController>() != null;
    }

    void UpdateIndicatorState()
    {
        if (!indicator)
        {
            return;
        }

        indicator.SetActive(!isPlayerInside);
    }

    void NotifySignboard(bool inside)
    {
        if (!sequence)
        {
            return;
        }

        HelpSignboard.SetTriggerInside(this, sequence, inside);
    }
}
