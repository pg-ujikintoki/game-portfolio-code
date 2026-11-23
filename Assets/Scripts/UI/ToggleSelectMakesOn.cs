using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToggleSelectMakesOn : MonoBehaviour, ISelectHandler
{
    Toggle t;
    void Awake() => t = GetComponent<Toggle>();

    public void OnSelect(BaseEventData eventData)
    {
        if (t != null)
            t.isOn = true;   // 選ばれたら即このトグルをONにする
    }
}
