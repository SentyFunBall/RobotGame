using System.Collections.Generic;
using UnityEngine;

public enum PowerUpType
{
    Speed,
}

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager instance { get; private set; }

    private static Dictionary<PowerUpType, bool> activePowerUps = new();

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
        switch (powerUpType)
        {
            case PowerUpType.Speed:
                // Implement speed power-up logic here
                activePowerUps[PowerUpType.Speed] = true;
                break;
        }
    }

    public void DeactivatePowerUp(PowerUpType powerUpType)
    {
        switch (powerUpType)
        {
            case PowerUpType.Speed:
                // Implement speed power-up deactivation logic here
                activePowerUps[PowerUpType.Speed] = false;
                break;
        }
    }

    public bool IsPowerUpActive(PowerUpType powerUpType)
    {
        switch (powerUpType)
        {
            case PowerUpType.Speed:
                // Implement logic to check if speed power-up is active
                return activePowerUps.ContainsKey(PowerUpType.Speed)
                    && activePowerUps[PowerUpType.Speed];
            default:
                return false;
        }
    }

    // Update is called once per frame
    void Update() { }
}
