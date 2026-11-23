using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HelpWindowController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] GameObject dialogRoot;
    [SerializeField] GameObject characterPanel;
    [SerializeField] Image characterImage;
    [SerializeField] TextMeshProUGUI dialogText;

    HelpSequence currentSequence;
    int currentIndex = -1;

    public bool IsShowing => currentSequence != null && currentIndex >= 0;

    void Awake()
    {
        Close();
    }

    public void Show(HelpSequence sequence)
    {
        if (!sequence)
        {
            Debug.LogWarning("[HelpWindow] 有効な HelpSequence が指定されていません。");
            return;
        }

        if (!sequence.IsValid)
        {
            Debug.LogWarning($"[HelpWindow] HelpSequence '{sequence.name}' にエントリがありません。");
            return;
        }

        currentSequence = sequence;
        currentIndex = 0;
        sequence.MarkShown();
        RefreshCurrentEntry();

        if (dialogRoot)
        {
            dialogRoot.SetActive(true);
        }
    }

    public bool ShowNext()
    {
        if (!IsShowing)
        {
            return false;
        }

        int nextIndex = currentIndex + 1;
        if (currentSequence.Entries.Count <= nextIndex)
        {
            return false;
        }

        currentIndex = nextIndex;
        RefreshCurrentEntry();
        return true;
    }

    public void Close()
    {
        currentSequence = null;
        currentIndex = -1;

        if (dialogRoot)
        {
            dialogRoot.SetActive(false);
        }

        if (dialogText)
        {
            dialogText.text = string.Empty;
        }

        if (characterImage)
        {
            characterImage.sprite = null;
            characterImage.enabled = false;
        }

        if (characterPanel)
        {
            characterPanel.SetActive(false);
        }
    }

    void RefreshCurrentEntry()
    {
        if (!IsShowing)
        {
            return;
        }

        var entry = currentSequence.Entries[currentIndex];

        if (dialogText)
        {
            dialogText.text = entry?.message ?? string.Empty;
        }

        bool hasImage = entry != null && entry.image != null;

        if (characterImage)
        {
            characterImage.sprite = hasImage ? entry.image : null;
            characterImage.enabled = hasImage;
        }

        if (characterPanel)
        {
            characterPanel.SetActive(hasImage);
        }
    }
}
