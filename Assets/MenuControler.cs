using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MenuControler : MonoBehaviour
{
    private Button startButton;
    private Button exitButton;
    private DropdownField difficultyDropdown;

    public GameObject titleScreen;
    private PanelRenderer panelRenderer;

    // Available difficulty choices
    private static readonly List<string> DifficultyOptions = new List<string> { "Easy", "Medium", "Hard" };

   
    public static string SelectedDifficulty { get; private set; } = "Medium";

    void OnEnable()
    {
        if (titleScreen != null)
        {
            panelRenderer = titleScreen.GetComponent<PanelRenderer>();
            if (panelRenderer != null)
            {
                panelRenderer.RegisterUIReloadCallback(OnUIReload);
            }
            else
            {
                Debug.LogWarning("[MenuControler] No PanelRenderer found on titleScreen GameObject.");
            }
        }
    }

    void OnDisable()
    {
        if (panelRenderer != null)
        {
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }

        // Unsubscribe callbacks to prevent memory leaks and duplicate triggers
        if (startButton != null)
        {
            startButton.clicked -= PlayGame;
        }

        if (exitButton != null)
        {
            exitButton.clicked -= ExitGame;
        }

        if (difficultyDropdown != null)
        {
            difficultyDropdown.UnregisterValueChangedCallback(OnDifficultyChanged);
        }
    }

    void OnUIReload(PanelRenderer renderer, VisualElement rootElement)
    {
        startButton = rootElement.Q<Button>("StartButton");
        exitButton = rootElement.Q<Button>("ExitButton");
        
        difficultyDropdown = rootElement.Q<DropdownField>("DifficultyDropdown");

        if (difficultyDropdown == null)
        {
            difficultyDropdown = new DropdownField("Difficulty", DifficultyOptions, 1);
            difficultyDropdown.name = "DifficultyDropdown";

            VisualElement container = rootElement.Q<VisualElement>("MenuContainer") ?? rootElement;
            container.Add(difficultyDropdown);
        }
        else
        {
            difficultyDropdown.choices = DifficultyOptions;
            difficultyDropdown.value = SelectedDifficulty;
        }

        // Register value change callback (unregister first to avoid duplicates on UI reload)
        difficultyDropdown.UnregisterValueChangedCallback(OnDifficultyChanged);
        difficultyDropdown.RegisterValueChangedCallback(OnDifficultyChanged);

        // 3. Bind Button click events
        if (startButton != null)
        {
            startButton.clicked -= PlayGame;
            startButton.clicked += PlayGame;
        }
        else
        {
            Debug.LogWarning("[MenuControler] 'StartButton' not found in VisualElement hierarchy.");
        }

        if (exitButton != null)
        {
            exitButton.clicked -= ExitGame;
            exitButton.clicked += ExitGame;
        }
        else
        {
            Debug.LogWarning("[MenuControler] 'ExitButton' not found in VisualElement hierarchy.");
        }
    }

    private void OnDifficultyChanged(ChangeEvent<string> evt)
    {
        SelectedDifficulty = evt.newValue;
        PlayerPrefs.SetString("GameDifficulty",SelectedDifficulty);
        PlayerPrefs.Save();
        Debug.Log($"[MenuControler] Difficulty changed to: {SelectedDifficulty}");
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