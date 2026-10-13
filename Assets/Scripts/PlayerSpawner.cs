using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    public CharacterData selectedCharacter;
    public CameraController cameraController;
    void Start()
    {
        selectedCharacter = RunSelection.SelectedCharacter;

        if (selectedCharacter == null)
        {
            Debug.LogError("No character selected.");
            return;
        }

        if (selectedCharacter.playerPrefab == null)
        {
            Debug.LogError(selectedCharacter.characterName + " has no player prefab assigned.");
            return;
        }

        GameObject player = Instantiate(selectedCharacter.playerPrefab, spawnPoint.position, spawnPoint.rotation);

        if (cameraController != null)
        {
            cameraController.target = player.transform;
        }
    }
}