using UnityEngine;

public class AttachCameraToPlayer : MonoBehaviour
{
    public GameObject playerObj;

    // Default offsets
    public Vector3 posOff = new Vector3(3.65f, 7.88f, -1.09f);
    public Vector3 rotOff = new Vector3(63.4f, -52.1f, -2.037f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (playerObj != null)
        {
            transform.position = playerObj.transform.position + posOff;
            transform.rotation = playerObj.transform.rotation * Quaternion.Euler(rotOff);
        }
    }
}
