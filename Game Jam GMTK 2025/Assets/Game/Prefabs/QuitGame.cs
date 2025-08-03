using UnityEngine;

public class QuitGame : MonoBehaviour
{
    public void DoQuitGame()
    {
        // If running in the Unity editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        // If running in a built application
#else
            Application.Quit();
#endif
    }
}


