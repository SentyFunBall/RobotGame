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
