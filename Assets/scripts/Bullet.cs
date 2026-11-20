using UnityEngine;

public class Bullet : MonoBehaviour
{
    public ShopItem parentWeapon;
    public LayerMask enemyMask;

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & enemyMask) != 0)
        {
            collision
                .gameObject.GetComponent<EnemyAI>()
                ?.TakeDamage(parentWeapon != null ? parentWeapon.weaponDamage : 10f);
            Destroy(gameObject);
        }
    }
}
