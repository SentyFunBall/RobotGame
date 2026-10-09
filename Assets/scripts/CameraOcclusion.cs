using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraOcclusion : MonoBehaviour
{
    [SerializeField]
    private Transform player;

    [SerializeField]
    private LayerMask occlusionMask;

    [SerializeField]
    private float fadeSpeed = 8f;

    private MaterialPropertyBlock props;

    private Dictionary<Renderer, float> fadedObjects = new();

    // Update is called once per frame
    void Update()
    {
        HashSet<Renderer> hitThisFrame = new();

        if (player == null)
            return;
        Vector3 direction = player.position - transform.position;
        float distance = direction.magnitude;

        // We do multiple raycasts, from the left, right, top, bottom, and center of the player (relative to the camera) to detect occluding objects
        Quaternion rotation = transform.rotation;
        List<RaycastHit> hits = new List<RaycastHit>(
            Physics.RaycastAll(transform.position, direction, distance, occlusionMask)
        );
        hits.AddRange(
            Physics.RaycastAll(
                transform.position + rotation * Vector3.left * 0.5f,
                direction,
                distance,
                occlusionMask
            )
        );
        hits.AddRange(
            Physics.RaycastAll(
                transform.position + rotation * Vector3.right * 0.5f,
                direction,
                distance,
                occlusionMask
            )
        );
        hits.AddRange(
            Physics.RaycastAll(
                transform.position + rotation * Vector3.up * 0.5f,
                direction,
                distance,
                occlusionMask
            )
        );
        hits.AddRange(
            Physics.RaycastAll(
                transform.position + rotation * Vector3.down * 0.5f,
                direction,
                distance,
                occlusionMask
            )
        );

        foreach (RaycastHit hit in hits)
        {
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (
                renderer == null
                || renderer.gameObject.CompareTag("Player")
                || renderer.gameObject.CompareTag("NoOcclude")
            )
                continue;

            hitThisFrame.Add(renderer);

            if (!fadedObjects.ContainsKey(renderer))
                fadedObjects.Add(renderer, 1f);

            fadedObjects[renderer] = Mathf.MoveTowards(
                fadedObjects[renderer],
                0.25f,
                fadeSpeed * Time.deltaTime
            );

            SetAlpha(renderer, fadedObjects[renderer]);
        }

        // Restore objects no longer blocking the player
        List<Renderer> restore = new();
        foreach (var pair in fadedObjects.ToList())
        {
            if (!hitThisFrame.Contains(pair.Key))
            {
                float alpha = Mathf.MoveTowards(pair.Value, 1f, fadeSpeed * Time.deltaTime);

                fadedObjects[pair.Key] = alpha;
                SetAlpha(pair.Key, alpha);

                if (alpha >= 0.99f)
                    fadedObjects.Remove(pair.Key);
            }
        }
        foreach (Renderer renderer in restore)
        {
            fadedObjects.Remove(renderer);
        }
    }

    void SetAlpha(Renderer renderer, float alpha)
    {
        props ??= new MaterialPropertyBlock();

        renderer.GetPropertyBlock(props);
        props.SetFloat("_FadeAmt", alpha);
        renderer.SetPropertyBlock(props);
    }
}
