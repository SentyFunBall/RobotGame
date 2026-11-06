using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BossTerrainRock : MonoBehaviour
{
    [SerializeField] private float damage = 25f;
    [SerializeField] private GameObject impactVfx;
    private bool hasHit = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        // Deal damage if player is hit
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }

        if (impactVfx != null)
        {
            Instantiate(impactVfx, transform.position, Quaternion.identity);
        }

        Destroy(gameObject, 0.05f);
    }
}
