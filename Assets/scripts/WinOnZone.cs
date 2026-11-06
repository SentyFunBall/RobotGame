using UnityEngine;

public class WinOnZone : MonoBehaviour
{
    private GameObject player;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindWithTag("Player");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player)
        {
            GameMenusController menus = FindAnyObjectByType<GameMenusController>();
            if (menus != null)
                menus.ShowWinMenu();
        }
    }
}
