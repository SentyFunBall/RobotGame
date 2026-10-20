using UnityEngine;

public class AttachPlayerToPlatform : MonoBehaviour
{
    private bool isAttached;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isAttached = true;
            other.transform.parent = transform;
            other.transform.position = new Vector3(
                other.transform.position.x,
                0.1f,
                other.transform.position.z
            );
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && isAttached)
        {
            isAttached = false;
            other.transform.parent = null;
        }
    }
}
