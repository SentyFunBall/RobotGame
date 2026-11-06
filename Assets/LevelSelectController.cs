using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class LevelSelectController : MonoBehaviour
{
    private Button level1Button;
    private Button level2Button;
    public GameObject levelScreen;
    private PanelRenderer panelRenderer;

    void OnEnable()
    {
        if (levelScreen == null)
        {
            levelScreen = gameObject;
        }

        panelRenderer = levelScreen.GetComponent<PanelRenderer>();

        if (panelRenderer != null)
        {
            panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }
        else
        {
            Debug.LogWarning(
                "[LevelSelectController] No PanelRenderer found on levelScreen GameObject."
            );
        }
    }

    void OnDisable()
    {
        if (panelRenderer != null)
        {
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }

        if (level1Button != null)
        {
            level1Button.clicked -= LoadLevel1;
        }

        if (level2Button != null)
        {
            level2Button.clicked -= LoadLevel2;
        }
    }

    // Update is called once per frame
    void OnUIReload(PanelRenderer renderer, VisualElement rootElement)
    {
        level1Button = rootElement.Q<Button>("Lvl1Button");
        level2Button = rootElement.Q<Button>("Lvl2Button");

        if (level1Button != null)
        {
            level1Button.clicked -= LoadLevel1;
            level1Button.clicked += LoadLevel1;
        }
        else
        {
            Debug.LogWarning(
                "[LevelSelectController] 'Level1Button' not found in VisualElement hierarchy."
            );
        }

        if (level2Button != null)
        {
            level2Button.clicked -= LoadLevel2;
            level2Button.clicked += LoadLevel2;
        }
        else
        {
            Debug.LogWarning(
                "[LevelSelectController] 'Level2Button' not found in VisualElement hierarchy."
            );
        }
    }

    public void LoadLevel1()
    {
        SceneManager.LoadScene(2);
    }

    public void LoadLevel2()
    {
        //this exits the program because we don't have level 2 implemented yet
        /*#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif*/
    }
}
