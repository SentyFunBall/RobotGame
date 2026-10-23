using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float stepSize = 1.5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private InputSystem_Actions controls;
    private Rigidbody rb;
    private Collider playerCollider;
    private Vector2 moveInput;

    private bool isSlow;
    private bool isSprint;

    [SerializeField]
    private float slowMod = 0.5f;
    private bool isSpeedPowerUpActive;

    [SerializeField]
    private float sprintMod = 1.5f;

    [SerializeField]
    private LayerMask groundMask;

    [SerializeField]
    private float groundCheckDistance = 0.05f;

    private PowerUpManager powerUpManager;

    private bool TryStep(Vector3 moveDirection)
    {
        Vector3 origin = transform.position;
        // is there an obstacle at foot level
        if (!Physics.Raycast(origin, moveDirection, out RaycastHit lowerHit, 0.6f, groundMask))
            return false;

        // is there space above the obstacle to step up
        Vector3 upperOrigin = origin + Vector3.up * stepSize;
        if (Physics.Raycast(upperOrigin, moveDirection, 0.1f, groundMask))
            return false;

        // we can step. get the actual step height so we dont fuckin jump
        Vector3 checkPos = lowerHit.point + moveDirection * 0.05f + Vector3.up * stepSize;

        if (Physics.Raycast(checkPos, Vector3.down, out RaycastHit topHit, stepSize, groundMask))
        {
            float step = topHit.point.y - rb.position.y;
            if (step > 0f && step <= stepSize)
            {
                rb.position += Vector3.up * step + moveDirection * 0.05f + Vector3.up * 1.1f;
                return true;
            }
        }

        return false;
    }

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
        playerCollider = GetComponent<Collider>();
        powerUpManager = PowerUpManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        isSlow = controls.Player.Crouch.ReadValue<float>() > 0.5f;
        isSprint = controls.Player.Sprint.ReadValue<float>() > 0.5f;
        isSpeedPowerUpActive = powerUpManager.IsPowerUpActive(PowerUpType.Speed);

        bool t = controls.Player.DEBUGAddPowerUp.ReadValue<float>() > 0.5f;
        if (t)
        {
            powerUpManager.ActivatePowerUp(PowerUpType.Speed);
        }
    }

    void FixedUpdate()
    {
        // Since camera is tilted, W actually is forward + left, etc
        Vector3 desiredVelocity =
            new Vector3(moveInput.x, 0.0f, moveInput.y).normalized
            * moveSpeed
            * (
                isSlow ? slowMod
                : (isSprint && isSpeedPowerUpActive) ? sprintMod
                : 1f
            );

        Vector3 groundNormal = Vector3.up;
        Vector3 velocity = rb.linearVelocity;
        Vector3 velocityChange = desiredVelocity - velocity;

        Bounds colliderBounds = playerCollider.bounds;
        bool grounded = Physics.Raycast(
            colliderBounds.center,
            Vector3.down,
            out RaycastHit hit,
            colliderBounds.extents.y + groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (grounded)
        {
            groundNormal = hit.normal;
        }

        TryStep(desiredVelocity.normalized);

        Vector3 move = Vector3.ProjectOnPlane(velocityChange, groundNormal);

        rb.AddForce(move, ForceMode.Acceleration);

        // If no moveinput, slow down the player
        if (moveInput == Vector2.zero && grounded)
        {
            rb.AddForce(-velocity * 0.1f, ForceMode.VelocityChange);
        }
    }
}
