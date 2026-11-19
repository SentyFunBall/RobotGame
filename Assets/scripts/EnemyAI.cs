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

    [SerializeField]
    private float damageFlashDuration = 0.1f;
    private Renderer[] renderers;
    private MaterialPropertyBlock propBlock;
    private Coroutine damageFlashCoroutine;

    private static readonly int baseColor = Shader.PropertyToID("_BaseColor");
    private static readonly Color defaultTintColor = new Color(
        0xE9 / 255f,
        0xDF / 255f,
        0xA6 / 255f
    );

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

    [SerializeField]
    private float meleeWinddownDelay = 0.653f;

    private enum MeleeState
    {
        None,
        Windup,
        Lunge,
        Winddown,
    }

    private MeleeState meleeState = MeleeState.None;
    private float meleeStateEndTime;
    private Vector3 lockedDirection;
    private bool lungeHit;
    private bool previousUpdateRotation;

    [Header("Heavy Slam")]
    [SerializeField]
    private float heavyWindupDelay = 1.25f;

    [SerializeField]
    private float heavyRecoveryDelay = 1f;

    [SerializeField]
    private float heavyAttackCooldown = 4.5f;

    [SerializeField]
    private float heavySlamRadius = 4f;

    [SerializeField]
    private float heavySlamForwardOffset = 1f;

    [SerializeField]
    private float heavyKnockbackForce = 8f;

    [SerializeField]
    private GameObject heavySlamVfxPrefab;

    [SerializeField]
    private Color heavyTelegraphColor = new Color(1f, 0.45f, 0f);

    private enum HeavyState
    {
        None,
        Windup,
        Recovery,
    }

    private HeavyState heavyState = HeavyState.None;
    private float heavyStateEndTime;
    private float heavyWindupStartTime;

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

        renderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
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

        if (heavyState != HeavyState.None)
        {
            UpdateHeavyAttack();
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
        agent.velocity = Vector3.zero;
        previousUpdateRotation = agent.updateRotation;
        agent.updateRotation = false;
        meleeState = MeleeState.Windup;
        meleeStateEndTime = Time.time + meleeWindupDelay;
    }

    private void UpdateMeleeAttack(float distance)
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        transform.rotation = Quaternion.LookRotation(lockedDirection, Vector3.up);

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

        if (meleeState == MeleeState.Lunge)
        {
            // Lunge: direction stays locked, no steering toward the player
            agent.Move(lockedDirection * lungeSpeed * Time.deltaTime);

            if (!lungeHit && IsTouchingPlayer())
            {
                lungeHit = true;
                DamagePlayer();
            }

            if (Time.time >= meleeStateEndTime)
            {
                meleeState = MeleeState.Winddown;
                meleeStateEndTime = Time.time + meleeWinddownDelay;
            }
            return;
        }

        if (Time.time >= meleeStateEndTime)
        {
            meleeState = MeleeState.None;
            agent.velocity = Vector3.zero;
            agent.updateRotation = previousUpdateRotation;
        }
    }

    private void StartHeavyAttack()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        lockedDirection = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : transform.forward;
        transform.rotation = Quaternion.LookRotation(lockedDirection, Vector3.up);

        nextAttackTime = Time.time + heavyAttackCooldown;
        characterAnimation.Attack();
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
        previousUpdateRotation = agent.updateRotation;
        agent.updateRotation = false;
        heavyState = HeavyState.Windup;
        heavyWindupStartTime = Time.time;
        heavyStateEndTime = Time.time + heavyWindupDelay;
    }

    private void UpdateHeavyAttack()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        transform.rotation = Quaternion.LookRotation(lockedDirection, Vector3.up);

        if (heavyState == HeavyState.Windup)
        {
            if (damageFlashCoroutine == null)
            {
                float progress =
                    heavyWindupDelay > 0f
                        ? Mathf.Clamp01((Time.time - heavyWindupStartTime) / heavyWindupDelay)
                        : 1f;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Lerp(6f, 18f, progress));
                SetRenderersColor(Color.Lerp(defaultTintColor, heavyTelegraphColor, pulse));
            }

            if (Time.time >= heavyStateEndTime)
            {
                if (damageFlashCoroutine == null)
                    SetRenderersColor(defaultTintColor);
                ExecuteHeavySlam();
                heavyState = HeavyState.Recovery;
                heavyStateEndTime = Time.time + heavyRecoveryDelay;
            }
            return;
        }

        if (Time.time >= heavyStateEndTime)
        {
            heavyState = HeavyState.None;
            agent.velocity = Vector3.zero;
            agent.updateRotation = previousUpdateRotation;
        }
    }

    private void ExecuteHeavySlam()
    {
        Vector3 impactPos = transform.position + lockedDirection * heavySlamForwardOffset;

        if (heavySlamVfxPrefab != null)
        {
            Instantiate(heavySlamVfxPrefab, impactPos, Quaternion.identity);
        }

        Collider[] hits = Physics.OverlapSphere(impactPos, heavySlamRadius);
        bool hitPlayer = false;
        foreach (Collider hit in hits)
        {
            if (hitPlayer)
                break;

            PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                hitPlayer = true;
                playerHealth.TakeDamage(attackDamage);

                if (heavyKnockbackForce > 0f)
                {
                    Rigidbody playerRb = playerHealth.GetComponent<Rigidbody>();
                    if (playerRb != null)
                    {
                        Vector3 pushDir = playerHealth.transform.position - impactPos;
                        pushDir.y = 0f;
                        pushDir =
                            pushDir.sqrMagnitude > 0.0001f ? pushDir.normalized : lockedDirection;
                        playerRb.AddForce(pushDir * heavyKnockbackForce, ForceMode.Impulse);
                    }
                }
            }
        }
    }

    private void SetRenderersColor(Color color)
    {
        if (renderers == null || propBlock == null)
            return;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;
            renderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(baseColor, color);
            renderer.SetPropertyBlock(propBlock);
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

        if (enemyType == EnemyType.Heavy)
        {
            if (Time.time >= nextAttackTime)
            {
                StartHeavyAttack();
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
    public bool IsAlive => !dying;

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0f && !dying)
        {
            dying = true;
            Die();
        }
        Flash();
    }

    private void Die()
    {
        heavyState = HeavyState.None;
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

    private void Flash()
    {
        if (damageFlashCoroutine != null)
            StopCoroutine(damageFlashCoroutine);
        damageFlashCoroutine = StartCoroutine(_FlashRoutine());
    }

    private System.Collections.IEnumerator _FlashRoutine()
    {
        foreach (Renderer renderer in renderers)
        {
            renderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(baseColor, Color.red);
            renderer.SetPropertyBlock(propBlock);
        }

        yield return new WaitForSeconds(damageFlashDuration);

        foreach (Renderer renderer in renderers)
        {
            renderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(baseColor, new Color(0xE9 / 255f, 0xDF / 255f, 0xA6 / 255f));
            renderer.SetPropertyBlock(propBlock);
        }

        damageFlashCoroutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (enemyType == EnemyType.Heavy)
        {
            Gizmos.color = new Color(1f, 0.45f, 0f, 0.85f);
            Gizmos.DrawWireSphere(
                transform.position + transform.forward * heavySlamForwardOffset,
                heavySlamRadius
            );
        }
    }
}