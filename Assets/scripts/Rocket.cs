using UnityEngine;

public class Rocket : MonoBehaviour
{
    public ShopItem parentWeapon;
    public LayerMask layerMask;

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & layerMask) != 0)
        {
            collision
                .gameObject.GetComponent<EnemyAI>()
                ?.TakeDamage(parentWeapon != null ? parentWeapon.weaponDamage : 10f);
            Destroy(gameObject);
        }
    }
}
