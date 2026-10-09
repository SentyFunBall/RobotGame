using TMPro;
using UnityEngine;

public class PickupLoot : MonoBehaviour
{
    public GameObject textMesh;
    public int lootCount = 0;

    [SerializeField]
    private string lootPrefix = "Loot: ";

    void Update()
    {
        textMesh.GetComponent<TextMeshProUGUI>().text =
            lootPrefix + lootCount.ToString() + "/" + LootGenerator.totalLootCount.ToString();
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
