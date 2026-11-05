using UnityEngine;
using UnityEngine.AI;

public enum DetectionState
{
    Normal,
    Alert,
    Sneak,
}

public enum EnemyType
{
    Melee,
    Ranged,
    Guard,
    Heavy,
    Scout,
    Energy,
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CharacterAnimation))]
public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField]
    private float detectionRadiusNormal;

    [SerializeField]
    private float detectionRadiusAlert;

    [SerializeField]
    private float detectionRadiusSneak;

    [SerializeField]
    private LayerMask detectionLayer;
    public DetectionState currentDetectionState = DetectionState.Normal;

    [Header("Combat")]
    [SerializeField]
    private float attackRange;

    [SerializeField]
    private float attackDamage;

    private float health;

    [SerializeField]
    private float maxHealth = 10f;

    [SerializeField]
    private EnemyType enemyType = EnemyType.Melee;

    [Header("Melee Lunge")]
    [SerializeField]
    private float meleeWindupDelay = 1f; // wait for the attack animation

    [SerializeField]
    private float lungeSpeed = 20f;

    [SerializeField]
    private float lungeDuration = 0.25f;

    private enum MeleeState
    {
        None,
        Windup,
        Lunge,
    }

    private MeleeState meleeState = MeleeState.None;
    private float meleeStateEndTime;
    private Vector3 lockedDirection;
    private bool lungeHit;

    private NavMeshAgent agent;
    private Transform player;
    private Collider enemyCollider;
    private Collider playerCollider;
    private float nextAttackTime;

    private CharacterAnimation characterAnimation;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyCollider = GetComponentInChildren<Collider>();
        characterAnimation = GetComponent<CharacterAnimation>();
        characterAnimation.FindAndSetAnimator();
        health = maxHealth;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (player != null)
        {
            playerCollider = player.GetComponentInChildren<Collider>();
        }
        if (player == null)
        {
            Debug.LogError(
                "Player not found in the scene. Make sure the player has the 'Player' tag."
            );
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!agent.isActiveAndEnabled)
            return;
        if (player == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (meleeState != MeleeState.None)
        {
            UpdateMeleeAttack(distance);
            UpdateAnimatorSpeed();
            return;
        }

        // Detect and chase player
        float detectionRadius = detectionRadiusNormal;
        switch (currentDetectionState)
        {
            case DetectionState.Normal:
                detectionRadius = detectionRadiusNormal;
                break;
            case DetectionState.Alert:
                detectionRadius = detectionRadiusAlert;
                break;
            case DetectionState.Sneak:
                detectionRadius = detectionRadiusSneak;
                break;
        }

        if (distance <= detectionRadius)
        {
            if (distance > attackRange)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            else
            {
                agent.isStopped = true;
                Attack();
            }
        }
        else
        {
            agent.isStopped = true;
        }

        UpdateAnimatorSpeed();
    }

    private void UpdateAnimatorSpeed()
    {
        if (characterAnimation != null)
        {
            float speed = meleeState == MeleeState.Lunge ? lungeSpeed : agent.velocity.magnitude;
            characterAnimation.SetMovementSpeed(speed);
        }
    }

    private void StartMeleeAttack()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        lockedDirection = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : transform.forward;
        transform.rotation = Quaternion.LookRotation(lockedDirection, Vector3.up);

        nextAttackTime = Time.time + 3f;
        characterAnimation.Attack();
        agent.isStopped = true;
        agent.ResetPath();
        meleeState = MeleeState.Windup;
        meleeStateEndTime = Time.time + meleeWindupDelay;
    }

    private void UpdateMeleeAttack(float distance)
    {
        if (meleeState == MeleeState.Windup)
        {
            if (Time.time >= meleeStateEndTime)
            {
                meleeState = MeleeState.Lunge;
                meleeStateEndTime = Time.time + lungeDuration;
                lungeHit = false;
            }
            return;
        }

        // Lunge: direction stays locked, no steering toward the player
        agent.Move(lockedDirection * lungeSpeed * Time.deltaTime);

        if (!lungeHit && IsTouchingPlayer())
        {
            lungeHit = true;
            DamagePlayer();
        }

        if (Time.time >= meleeStateEndTime)
        {
            meleeState = MeleeState.None;
        }
    }

    private bool IsTouchingPlayer()
    {
        if (enemyCollider != null && playerCollider != null)
        {
            return enemyCollider.bounds.Intersects(playerCollider.bounds);
        }

        // No colliders to test against: fall back to a body-radius distance check
        Vector3 offset = player.position - transform.position;
        offset.y = 0f;
        return offset.magnitude <= agent.radius + 0.5f;
    }

    private void DamagePlayer()
    {
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
    }

    private void Attack()
    {
        if (enemyType == EnemyType.Melee)
        {
            if (Time.time >= nextAttackTime)
            {
                StartMeleeAttack();
            }
            return;
        }

        if (Time.time >= nextAttackTime)
        {
            characterAnimation.Attack();
            nextAttackTime = Time.time + 3f; // Assuming 3 second cooldown between attacks

            // Apply damage to the player
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }

    private bool dying;

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0f && !dying)
        {
            dying = true;
            Die();
        }
    }

    private void Die()
    {
        characterAnimation.SetDead(true);
        agent.enabled = false;
        if (enemyCollider != null)
            enemyCollider.enabled = false;
        StartCoroutine(ShrinkAndDisable());
    }

    private System.Collections.IEnumerator ShrinkAndDisable()
    {
        yield return new WaitForSeconds(1f);

        const float shrinkDuration = 0.5f;
        Vector3 startScale = transform.localScale;
        for (float t = 0f; t < shrinkDuration; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / shrinkDuration);
            yield return null;
        }

        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }
}
