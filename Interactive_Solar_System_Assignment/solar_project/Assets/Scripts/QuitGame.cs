using UnityEngine;

public class QuitGame : MonoBehaviour
{
    public void Quit()
    {
        Debug.Log("Quit Game");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}