using UnityEngine;
using UnityEngine.UI;

public class HelpAvailableMark : MonoBehaviour
{
    [Tooltip("表示するマーク。Sprite が未設定のあいだは出しません。")]
    [SerializeField] Image markImage;

    void Awake()
    {
        if (!markImage)
        {
            markImage = GetComponent<Image>();
        }

        if (markImage)
        {
            markImage.preserveAspect = true;
            markImage.raycastTarget = false;
        }

        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (!markImage)
        {
            return;
        }

        markImage.enabled = visible && markImage.sprite != null;
    }
}
