using TMPro;
using UnityEngine;

public class PickupLoot : MonoBehaviour
{
    public GameObject textMesh;
    public static int lootCount = 0;

    [SerializeField]
    private string lootPrefix = "Loot: ";

    void Update()
    {
        if (textMesh != null && textMesh.TryGetComponent(out TextMeshProUGUI gui))
            gui.text = lootPrefix + lootCount.ToString();
    }

    void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Loot"))
        {
            Destroy(collision.gameObject);
            lootCount++;
        }
    }
}
