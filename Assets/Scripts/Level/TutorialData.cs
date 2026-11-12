using UnityEngine;

[System.Serializable]
public class TutorialData
{
    public string title;
    [TextArea] public string tutorialText;
    public Sprite icon; // optional
    public string keybind;
    public bool oneTimeHint;
    public CharacterData onlyFor; // leave empty to show it to every character
}
