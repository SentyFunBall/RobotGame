using UnityEngine;

public class Rocket : MonoBehaviour
{
    public ShopItem parentWeapon;
    public LayerMask enemyMask;
    public LayerMask colliderMask;
    public GameObject explosionEffect;

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & colliderMask) != 0)
        {
            Vector3 origin = transform.position;
            float attackRadius = 5f;
            Collider[] closeTargets = Physics.OverlapSphere(
                origin,
                attackRadius,
                enemyMask,
                QueryTriggerInteraction.Ignore
            );
            foreach (Collider closeTarget in closeTargets)
            {
                EnemyAI enemy = closeTarget.GetComponentInParent<EnemyAI>();
                if (enemy != null)
                {
                    enemy.TakeDamage(parentWeapon != null ? parentWeapon.weaponDamage : 10f);
                }
            }

            if (explosionEffect != null)
                Instantiate(explosionEffect, transform.position, Quaternion.identity);
            // freeze ourselves so effect doesnt move
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
                rb.isKinematic = true;

            GetComponent<MeshRenderer>().enabled = false;
            Destroy(gameObject, 1);
        }
    }
}
