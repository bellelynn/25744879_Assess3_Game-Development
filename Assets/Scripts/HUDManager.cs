using UnityEngine;
using UnityEngine.SceneManagement;

public class HUDManager : MonoBehaviour
{
    public void ExitToStart()
    {
        SceneManager.LoadScene("StartScene");
    }
}