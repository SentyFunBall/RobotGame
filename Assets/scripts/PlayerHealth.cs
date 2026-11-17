using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField]
    private float maxHealth = 100f;

    public float Health
    {
        get { return health; }
    }

    [SerializeField]
    private float health;

    [SerializeField]
    private string prefix = "Health: ";

    [SerializeField]
    private GameObject healthTextObject;
    private CharacterAnimation characterAnimation;

    [SerializeField]
    private GameMenusController gameMenusController;

    [SerializeField]
    private Slider healthBar;

    [SerializeField]
    private Slider cooldownBar;

    [SerializeField]
    private AudioSource hitAudio;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = maxHealth;
        characterAnimation = GetComponent<CharacterAnimation>();
        if (characterAnimation == null)
        {
            Debug.LogError("CharacterAnimation component not found on player.");
        }
        if (healthBar == null)
        {
            Debug.LogError("HealthBar component not assigned.");
        }
        healthBar.value = health / maxHealth;
    }

    public void Update()
    {
        if (healthTextObject != null)
        {
            healthTextObject.GetComponent<TextMeshProUGUI>().text = prefix + health.ToString("F0");
        }
        var cooldown = GetComponent<PlayerController>().attackCooldown;
        var nextAttackTime = GetComponent<PlayerController>().nextAttackTime;
        if (cooldownBar != null)
        {
            cooldownBar.value = 1 - ((nextAttackTime - Time.time) / cooldown);
        }
    }

    public void TakeDamage(float damage)
    {
        if (PowerUpManager.instance.IsPowerUpActive(PowerUpType.Armor))
        {
            damage *= 0.5f; // Reduce damage by 50% if Armor power-up is active
        }
        health -= damage;
        if (health < 0)
            health = 0;
        if (health == 0)
        {
            if (characterAnimation != null)
            {
                characterAnimation.SetDead(true);
            }
            if (gameMenusController != null)
            {
                gameMenusController.ShowDeathMenu();
            }
        }
        if (hitAudio != null)
            hitAudio.Play();
        if (characterAnimation != null)
            characterAnimation.Hit();
        if (healthBar != null)
            healthBar.value = health / maxHealth;
    }

    public void Heal(float amount)
    {
        health += amount;
        if (health > maxHealth)
        {
            health = maxHealth;
        }
        if (healthBar != null)
        {
            healthBar.value = health / maxHealth;
        }
    }
}
