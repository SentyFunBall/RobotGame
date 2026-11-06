using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterAnimation))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

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
    private CharacterAnimation characterAnimation;

    private bool doAttack;

    [SerializeField]
    private float attackRange = 2f;

    [SerializeField]
    private float attackDamage = 10f;

    [SerializeField]
    private LayerMask enemyMask;

    [SerializeField]
    private float attackCooldown = 2.0f;
    private float nextAttackTime;

    void Awake()
    {
        controls = new InputSystem_Actions();
        characterAnimation = GetComponent<CharacterAnimation>();
        characterAnimation.FindAndSetAnimator();
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
        characterAnimation = GetComponent<CharacterAnimation>();
        if (characterAnimation == null)
        {
            Debug.LogError("CharacterAnimation not found on player.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        isSlow = controls.Player.Crouch.ReadValue<float>() > 0.5f;
        isSprint = controls.Player.Sprint.ReadValue<float>() > 0.5f;
        isSpeedPowerUpActive = powerUpManager.IsPowerUpActive(PowerUpType.Speed);
        // Latch the press: Update can run several times between FixedUpdates, and
        // overwriting with `triggered` each frame dropped most attack inputs
        if (controls.Player.Attack.triggered)
            doAttack = true;

        bool t = controls.Player.DEBUGAddPowerUp.triggered;
        if (t)
        {
            powerUpManager.ActivatePowerUp(PowerUpType.Speed);
        }
    }

    void FixedUpdate()
    {
        // Since camera is tilted, W actually is forward + left, etc
        Vector3 desiredVelocity =
            new Vector3(-moveInput.x, 0.0f, -moveInput.y).normalized
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

        Vector3 move = Vector3.ProjectOnPlane(velocityChange, groundNormal);

        rb.AddForce(move, ForceMode.Acceleration);

        // If no moveinput, slow down the player
        if (moveInput == Vector2.zero && grounded)
        {
            rb.AddForce(-velocity * 0.1f, ForceMode.VelocityChange);
        }

        // Rotate player child to face direction of actual travel, not input direction
        if (moveInput != Vector2.zero)
        {
            Transform playerChild = transform.GetChild(0); // Assuming the first child is the visual representation of the player
            Vector3 forward = Vector3
                .ProjectOnPlane(Camera.main.transform.forward, Vector3.up)
                .normalized;
            Vector3 right = Vector3
                .ProjectOnPlane(Camera.main.transform.right, Vector3.up)
                .normalized;
            Vector3 moveDir = rb.linearVelocity;
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude < 0.001f)
            {
                moveDir = (forward * moveInput.y + right * moveInput.x).normalized;
            }
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            playerChild.rotation = Quaternion.Slerp(
                playerChild.rotation,
                targetRotation,
                Time.fixedDeltaTime * 10f
            );
        }

        // Update animator
        float speed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        if (characterAnimation != null)
        {
            if (isSprint && isSpeedPowerUpActive)
                characterAnimation.Sprint(speed);
            else
                characterAnimation.SetMovementSpeed(speed);
        }

        // Attack
        if (doAttack)
        {
            doAttack = false;
            if (Time.time < nextAttackTime)
                return;
            nextAttackTime = Time.time + attackCooldown;
            Transform playerChild = transform.GetChild(0);
            Vector3 attackDir = Vector3.ProjectOnPlane(playerChild.forward, Vector3.up).normalized;
            // Cast from body height; the child pivot is at the feet, below enemy colliders
            Vector3 origin = playerCollider.bounds.center;
            const float attackRadius = 0.5f;
            if (
                Physics.SphereCast(
                    origin,
                    attackRadius,
                    attackDir,
                    out RaycastHit hitInfo,
                    attackRange,
                    enemyMask,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                EnemyAI enemy = hitInfo.collider.GetComponentInParent<EnemyAI>();
                if (enemy != null)
                {
                    enemy.TakeDamage(attackDamage);
                }
            }
            characterAnimation.Attack();
        }
    }
}
