using UnityEngine;

public class SlowPlayerOnZone : MonoBehaviour
{
    private GameObject player;
	private float oldSpeed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindWithTag("Player");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player)
        {
			if (TryGetComponent(out PlayerController player)) {
				oldSpeed = player.moveSpeed;
				player.moveSpeed = 2;
			}
        }
    }

    private void OnTriggerExit(Collider other)
    {
		if (other.gameObject == player)
		{
			if (TryGetComponent(out PlayerController player)) {
				player.moveSpeed = oldSpeed;
			}
		}
    }
}
