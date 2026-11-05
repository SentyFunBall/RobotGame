using System;
using TMPro;
using UnityEngine;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = maxHealth;
        characterAnimation = GetComponent<CharacterAnimation>();
        if (characterAnimation == null)
        {
            Debug.LogError("CharacterAnimation component not found on player.");
        }
    }

    public void Update()
    {
        if (healthTextObject != null)
        {
            healthTextObject.GetComponent<TextMeshProUGUI>().text = prefix + health.ToString("F0");
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health < 0)
        {
            health = 0;
        }
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
        if (characterAnimation != null)
        {
            characterAnimation.Hit();
        }
    }

    public void Heal(float amount)
    {
        health += amount;
        if (health > maxHealth)
        {
            health = maxHealth;
        }
    }
}
