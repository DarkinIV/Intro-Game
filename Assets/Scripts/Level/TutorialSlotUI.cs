using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialSlotUI : MonoBehaviour
{
    [SerializeField] TMP_Text title;
    [SerializeField] Image icon;
    [SerializeField] TMP_Text keyBinding;
    [SerializeField] TMP_Text tutorialText;

    public void Setup(TutorialData tutorialSlot)
    {
        title.text = tutorialSlot.title;
        keyBinding.text = tutorialSlot.keybind;
        tutorialText.text = tutorialSlot.tutorialText;

        // Only show the icon if this tutorial has one
        icon.sprite = tutorialSlot.icon;
        icon.SetNativeSize();
        icon.gameObject.SetActive(tutorialSlot.icon != null);
    }
}
