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

    [SerializeField]
    private GameObject shopMenu;

    [SerializeField]
    private GameObject playerSprintSlider;

    private InputSystem_Actions controls;
    private bool shownBossUI = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pauseMenu.SetActive(false);
        deathMenu.SetActive(false);
        winMenu.SetActive(false);
        bossUI.SetActive(false);
        shopMenu.SetActive(false);
        playerSprintSlider.SetActive(false);
        
        // Wire sprint slider to player
        if (player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null && playerSprintSlider != null)
            {
                UnityEngine.UI.Slider slider = playerSprintSlider.GetComponent<UnityEngine.UI.Slider>();
                playerController.SetSprintSlider(slider);
            }
        }
    }

    void FindBoss()
    {
        if (boss != null)
            return;
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
        if (isPausePressed && !shopMenu.activeSelf)
        {
            isPaused = !isPaused;
            if (isPaused)
                Pause();
            else
                Resume();
        }
        playerSprintSlider.SetActive(PowerUpManager.instance.IsPowerUpActive(PowerUpType.Speed));

        // Update boss UI healthbar
        if (boss != null)
        {
            float bossHealth = bossAI.Health;
            float bossMaxHealth = bossAI.MaxHealth;
            bossUI.transform.Find("HealthBar").GetComponent<UnityEngine.UI.Slider>().value =
                bossHealth / bossMaxHealth;

            if (bossHealth <= 0f)
            {
                bossUI.SetActive(false);
            }
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
        shopMenu.SetActive(false);
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
        lootText.GetComponent<TextMeshProUGUI>().text = "Loot Remaining: " + PickupLoot.lootCount;
        bossUI.SetActive(false);
    }

    public void ShowBossUI()
    {
        FindBoss();
        bossUI.SetActive(true);
        shownBossUI = true;
    }

    public void ShowShopMenu()
    {
        Time.timeScale = 0f;
        shopMenu.SetActive(true);
        Shop shop = shopMenu.GetComponent<Shop>();
        if (shop != null)
            shop.Open(player);
        else
            Debug.LogError("Shop component not found on the shop menu.", shopMenu);
    }

    public bool IsBossUIActive()
    {
        return shownBossUI;
    }
}
