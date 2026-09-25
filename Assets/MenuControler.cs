using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MenuControler : MonoBehaviour
{
    Button startButton;
    public GameObject titleScreen;
    Button exitButton;

    private PanelRenderer panelRenderer;

    void OnEnable()
    {
        panelRenderer = titleScreen.GetComponent<PanelRenderer>();
        panelRenderer.RegisterUIReloadCallback(OnUIReload);

    }

    void OnDisable()
    {
        panelRenderer.UnregisterUIReloadCallback(OnUIReload);
    }

    void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement)
    {
        startButton = rootElement.Q<Button>("StartButton");

        if(startButton != null)
        {
            startButton.clicked += PlayGame;
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
