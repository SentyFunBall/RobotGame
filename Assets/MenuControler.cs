using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MenuControler : MonoBehaviour
{
    Button startButton;
    Button exitButton;

    void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();

        startButton = uiDoc.rootVisualElement.Q<Button>("StartButton");

        exitButton = uiDoc.rootVisualElement.Q<Button>("ExitButton");

        if(startButton != null)
        {
            startButton.clicked += PlayGame;
        }

        if(exitButton != null)
        {
            exitButton.clicked += ExitGame;
        }

    }

    void OnDisable()
    {
        if(startButton != null)
        {
            startButton.clicked -= PlayGame;
        }

        if(exitButton != null)
        {
            exitButton.clicked -= ExitGame;
        } 
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(1);
    }

    public void ExitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
