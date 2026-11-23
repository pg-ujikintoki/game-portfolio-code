using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerLife : MonoBehaviour
{
    [SerializeField] private int maxHp = 100;
    [SerializeField] private float restartDelay = 5f;
    [SerializeField] private GameObject gameOverText;

    [Header("SFX")]
    [Tooltip("被ダメージ時。撃破時は鳴らない。")]
    [SerializeField] private AudioClip damageSfx;
    [SerializeField, Range(0f, 1f)] private float damageSfxVolume = 0.9f;
    [Tooltip("撃破時。被ダメージ SE より優先する。")]
    [SerializeField] private AudioClip defeatSfx;
    [SerializeField, Range(0f, 1f)] private float defeatSfxVolume = 0.9f;

    public int CurrentHp { get; private set; }
    public int MaxHp => maxHp;

    public event Action<int, int> HpChanged;

    private bool isDead = false;

    void Awake()
    {
        CurrentHp = maxHp;
        ResolveGameOverText();

        if (gameOverText != null)
        {
            gameOverText.SetActive(false);
        }

        NotifyHpChanged();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        int previousHp = CurrentHp;
        CurrentHp = Mathf.Max(CurrentHp - damage, 0);
        NotifyHpChanged();

        if (CurrentHp <= 0)
        {
            Die();
            return;
        }

        if (CurrentHp < previousHp)
        {
            SfxOneShot.Play(damageSfx, damageSfxVolume);
        }
    }

    public bool Heal(int amount)
    {
        if (isDead)
        {
            return false;
        }

        if (amount <= 0)
        {
            return false;
        }

        int previousHp = CurrentHp;
        CurrentHp = Mathf.Min(CurrentHp + amount, maxHp);

        bool healed = CurrentHp > previousHp;

        NotifyHpChanged();
        return healed;
    }

    public void OnFallDeath()
    {
        if (isDead) return;

        StartCoroutine(FallRescueDelayCoroutine());
    }

    private System.Collections.IEnumerator FallRescueDelayCoroutine()
    {
        yield return new WaitForSeconds(1f);

        if (isDead)
        {
            yield break;
        }

        var controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.StartFallRescue();
        }
    }

    private void Die()
    {
        isDead = true;
        SfxOneShot.Play(defeatSfx, defeatSfxVolume);

        if (gameOverText != null)
        {
            gameOverText.SetActive(true);
        }

        StopPlayerActions();
        StartCoroutine(RestartSceneAfterDelay());
    }

    private void StopPlayerActions()
    {
        var controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.DisableControl();
        }

        var inputRouter = GetComponent<PlayerInputRouter>();
        if (inputRouter != null)
        {
            inputRouter.enabled = false;
        }

        var attackController = GetComponent<PlayerAttackController>();
        if (attackController != null)
        {
            attackController.enabled = false;
        }
    }

    private System.Collections.IEnumerator RestartSceneAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void NotifyHpChanged()
    {
        HpChanged?.Invoke(CurrentHp, maxHp);
    }

    private void ResolveGameOverText()
    {
        if (gameOverText != null)
        {
            return;
        }

        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj == null || obj.name != "GameOverText")
            {
                continue;
            }

            if (!obj.scene.IsValid())
            {
                continue;
            }

            gameOverText = obj;
            break;
        }
    }
}
