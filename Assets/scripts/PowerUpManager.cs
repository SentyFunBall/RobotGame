using System.Collections.Generic;
using UnityEngine;

public enum PowerUpType
{
    Speed,
    Armor,
    Stealth,
}

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager instance { get; private set; }

    private Dictionary<PowerUpType, bool> activePowerUps = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        instance = this;
    }

    private void Awake()
    {
        // Prevent duplicate managers
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    public void ActivatePowerUp(PowerUpType powerUpType)
    {
        activePowerUps[powerUpType] = true;
    }

    public void DeactivatePowerUp(PowerUpType powerUpType)
    {
        activePowerUps[powerUpType] = false;
    }

    public bool IsPowerUpActive(PowerUpType powerUpType)
    {
        return activePowerUps.ContainsKey(powerUpType) && activePowerUps[powerUpType];
    }

    // Update is called once per frame
    void Update() { }
}
