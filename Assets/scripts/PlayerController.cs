using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private InputSystem_Actions controls;
    private Rigidbody rb;

    void Awake()
    {
        controls = new InputSystem_Actions();
    }

    void OnEnable()
    {
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Disable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 moveInput = controls.Player.Move.ReadValue<Vector2>();
        Vector3 move = new Vector3(
            moveInput.x - moveInput.y,
            0.0f,
            moveInput.x + moveInput.y
        ).normalized * moveSpeed;

        // Since camera is tilted, W actually is forward + left, etc
        rb.AddForce(move);
    }
}
