using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public static class RunSelection
{
    public static CharacterData SelectedCharacter;
}

public class SelectionScene : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public GameObject playSection;

    public Button rogueButton;
    public Button swordmanButton;
    public Button witchButton;

    [Header("Characters")]
    public CharacterData rogue;
    public CharacterData swordman;
    public CharacterData witch;

    [Header("Scene")]
    public string mainSceneName = "Main";

    void Start()
    {
        RunSelection.SelectedCharacter = null;
        playSection.SetActive(false);

        rogueButton.onClick.AddListener(() => SelectCharacter(rogue));
        swordmanButton.onClick.AddListener(() => SelectCharacter(swordman));
        witchButton.onClick.AddListener(() => SelectCharacter(witch));

        playButton.onClick.AddListener(Play);
    }

    void SelectCharacter(CharacterData character)
    {
        RunSelection.SelectedCharacter = character;
        playSection.SetActive(true);

        Debug.Log("Selected character: " + character.name);
    }

    public void DeselectCharacter()
    {
        RunSelection.SelectedCharacter = null;
        playSection.SetActive(false);

        Debug.Log("Character deselected.");
    }

    void Play()
    {
        if (RunSelection.SelectedCharacter == null)
        {
            playSection.SetActive(false);
            return;
        }

        SceneManager.LoadScene(mainSceneName);
    }
}