using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameMenusController : MonoBehaviour
{
    [SerializeField]
    private GameObject player;
    private RobotBossAI bossAI;

    [SerializeField]
    private GameObject pauseMenu;
    private bool isPaused = false;

    [SerializeField]
    private GameObject deathMenu;

    [SerializeField]
    private GameObject winMenu;

    [SerializeField]
    private GameObject bossUI;

    private GameObject boss;

    private InputSystem_Actions controls;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pauseMenu.SetActive(false);
        deathMenu.SetActive(false);
        winMenu.SetActive(false);
        bossUI.SetActive(false);
        boss = GameObject.FindWithTag("Boss");
        if (boss != null)
            bossAI = boss.GetComponent<RobotBossAI>();
    }

    void Awake()
    {
        controls = new InputSystem_Actions();
    }

    void OnEnable()
    {
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        bool isPausePressed = controls.UI.Pause.triggered;
        if (isPausePressed)
        {
            isPaused = !isPaused;
            if (isPaused)
                Pause();
            else
                Resume();
        }

        // Update boss UI healthbar
        if (boss != null && bossUI.activeSelf)
        {
            float bossHealth = bossAI.Health;
            float bossMaxHealth = bossAI.MaxHealth;
            bossUI.transform.Find("HealthBar").GetComponent<UnityEngine.UI.Slider>().value =
                bossHealth / bossMaxHealth;
        }
    }

    public void Pause()
    {
        pauseMenu.SetActive(isPaused);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void QuitToDesktop()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ShowDeathMenu()
    {
        deathMenu.SetActive(true);
        //Time.timeScale = 0f;
    }

    public void ShowWinMenu()
    {
        winMenu.SetActive(true);
        Time.timeScale = 0f;
        GameObject lootText = winMenu.transform.Find("loottx").gameObject;
        lootText.GetComponent<TextMeshProUGUI>().text = "Loot Collected: " + PickupLoot.lootCount;
        bossUI.SetActive(false);
    }

    public void ShowBossUI()
    {
        bossUI.SetActive(true);
    }
}
