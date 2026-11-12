using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    public CharacterData selectedCharacter;
    public CameraController cameraController;

    // Awake so the player exists before anything's Start looks for it (UIManager, enemies)
    void Awake()
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

        Vector3 position = spawnPoint.position;
        Quaternion rotation = spawnPoint.rotation;

        // Wake up where we last rested, if that was in this scene
        if (RunState.hasCheckpoint && RunState.checkpointScene == gameObject.scene.name)
        {
            position = RunState.checkpointPosition;
            rotation = RunState.checkpointRotation;
        }

        GameObject player = Instantiate(selectedCharacter.playerPrefab, position, rotation);

        if (cameraController != null)
        {
            cameraController.target = player.transform;
        }
    }
}