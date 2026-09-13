using UnityEngine;

public class AttachCameraToPlayer : MonoBehaviour
{
    public GameObject playerObj;

    // Default offsets
    [SerializeField]
    [Tooltip("Position offset of the camera relative to the player")]
    private Vector3 posOff = new Vector3(3.65f, 7.88f, -1.09f);

    [SerializeField]
    [Tooltip("Rotation offset of the camera relative to the player")]
    private Vector3 rotOff = new Vector3(63.4f, -52.1f, -2.037f);

    [SerializeField]
    [Tooltip("Smooth factor for camera movement and rotation")]
    private float smoothFactor = 0.125f;

    // LateUpdate is called once per frame, after all Update functions have been called
    void FixedUpdate()
    {
        // Just in case the player object is not assigned, do nothing
        if (playerObj != null)
        {
            // Smoothly move and rotate the camera to follow the player with the specified offsets and smooth factor
            transform.position = Vector3.Lerp(
                transform.position,
                playerObj.transform.position + posOff,
                smoothFactor
            );
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                playerObj.transform.rotation * Quaternion.Euler(rotOff),
                smoothFactor
            );
        }
    }
}
