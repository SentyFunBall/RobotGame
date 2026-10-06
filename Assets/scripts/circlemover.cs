using UnityEngine;

public class circlemover : MonoBehaviour
{
    public float speed = 5.0f;
    public float radius = 1.0f;
    private GameObject thisObject;
    private float x = 0.0f;
    private float y = 0.0f;
    private Vector3 initialPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        thisObject = gameObject;
        initialPosition = thisObject.transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        x += speed * Time.fixedDeltaTime;
        y += speed * Time.fixedDeltaTime;
        thisObject.transform.position =
            initialPosition + new Vector3(Mathf.Cos(x) * radius, 0, Mathf.Sin(y) * radius);
    }
}
