using UnityEngine;

public static class RunState
{
    public static int karma;

    // Where the player last rested
    public static bool hasCheckpoint;
    public static string checkpointScene;
    public static Vector3 checkpointPosition;
    public static Quaternion checkpointRotation;

    // Called when a new run starts from the selection screen
    public static void Reset()
    {
        karma = 0;
        hasCheckpoint = false;
    }
}
