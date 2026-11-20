using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
	public float damage = 10;
	public float speed = 1;

	private float firstAlive = 0;

    void Start()
	{
		firstAlive = Time.time;

		if (TryGetComponent(out Rigidbody rb))
		{
			rb.linearVelocity = transform.forward * speed;
		}
	}

	void Update()
	{
		// destroy the bullet after 10 seconds
		if (firstAlive + 10 <= Time.time)
			Destroy(gameObject);
	}

    private void OnTriggerEnter(Collider other)
    {
		// If the other collider belongs to the enemy layer, just ignore it
		if (other.gameObject.layer == 8)
			return;

		// If the other collider belongs to the player layer (the player itself), attempt to cause damage
		if (other.gameObject.layer == 6)
		{
			if (other.gameObject.TryGetComponent(out PlayerHealth health))
				health.TakeDamage(damage);
		}
		// Either way, destroy the bullet
		Destroy(gameObject);
    }
}