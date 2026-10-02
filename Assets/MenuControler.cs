using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MenuControler : MonoBehaviour
{
    Button startButton;
    Button exitButton;
    public GameObject titleScreen;
    //private Slider enemyStrengthSlider;
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
        exitButton = rootElement.Q<Button>("ExitButton");
        //change difficulty to public in DungeonGenerator.cs line 65
        //add DontDestroyOnLoad(gameObject) to line 88
        /*
        enemyStrengthSlider = rootElement.Q<Slider>("EnemyStrengthSlider");

        if(DungeonGenerator.Instance != null)
        {
            if(enemyStrengthSlider != null)
            {
                enemyStrengthSlider.value = DungeonGenerator.Instance.Difficulty.EnemyStrength; 
                enemyStrengthSlider.RegisterValueChangedCallback(OnEnemyStrengthChanged);
            }
        }
        */
        if(startButton != null)
        {
            startButton.clicked += PlayGame;
        }

        if(exitButton != null)
        {
            exitButton.clicked += ExitGame;
        }
    }
    public void PlayGame()
    {
        SceneManager.LoadScene(1);
    }
    /*
    private void OnEnemyStrengthChanged(ChangeEvent<float> evt)
    {
        DungeonGenerator.Instance.Difficulty.EnemyStrength = Mathf.RoundToInt(evt.newValue);

    }
    */
    public void ExitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
