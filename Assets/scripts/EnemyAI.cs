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

    [Header("Ranged Attacks")]
    [SerializeField]
    private GameObject attackOrigin;

    [SerializeField]
    private GameObject bulletPrefab;

    [SerializeField]
    private float shotSpeed = 1;

    [SerializeField]
    private int shotCount = 10;

    [SerializeField]
    private float fireInterval = 0.3f;

    [SerializeField]
    private float reloadTime = 5;

    private enum MeleeState
    {
        None,
        Windup,
        Lunge,
        Winddown,
    }

    private enum RangedState
    {
        None,
        Reloading,
    }

    private MeleeState meleeState = MeleeState.None;
    private float meleeStateEndTime;
    private Vector3 lockedDirection;
    private bool lungeHit;
    private bool previousUpdateRotation;

    private RangedState rangedState = RangedState.None;
    private int currentShotCount;

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
        if (attackOrigin == null)
            attackOrigin = gameObject;

        if (enemyType == EnemyType.Ranged)
        {
            if (bulletPrefab == null)
                UnityEngine.Debug.LogError($"bullet prefab for ranged enemy {gameObject.name} is missing.");
            currentShotCount = shotCount;
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
            if (enemyType == EnemyType.Melee)
                HandleMeleeAI(distance);
            else if (enemyType == EnemyType.Ranged)
                HandleRangedAI(distance);
        }
        else
        {
            agent.isStopped = true;
        }

        UpdateAnimatorSpeed();
    }

    private void HandleMeleeAI(float distance)
    {
        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;
            MeleeAttack();
        }
    }

    private void HandleRangedAI(float distance)
    {
        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        } else
        {
            agent.isStopped = true;

            if (rangedState == RangedState.Reloading && Time.time >= nextAttackTime)
            {
                rangedState = RangedState.None;
                currentShotCount = shotCount;
            }

            if (rangedState == RangedState.None)
            {
                Vector3 lookDir = player.position - transform.position; lookDir.y = 0;
                transform.forward = lookDir.normalized;
                RangedAttack();
            }
            
        }
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

    private void MeleeAttack()
    {
        if (enemyType == EnemyType.Melee)
        {
            if (Time.time >= nextAttackTime)
            {
                StartMeleeAttack();
            }
            return;
        }

        // merian: tbh dont know what this stuff is actually doing, but gonna keep it anyway
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

    private void RangedAttack()
    {
        if (bulletPrefab == null)
            return;

        if (Time.time >= nextAttackTime)
        {
            GameObject bulletObj = Instantiate(bulletPrefab, attackOrigin.transform.position, Quaternion.identity);
            bulletObj.transform.forward = (player.position - attackOrigin.transform.position).normalized;
            if (bulletObj.TryGetComponent(out Bullet bullet))
            {
                bullet.damage = attackDamage;
                bullet.speed = shotSpeed;
            }
            currentShotCount--;
            if (currentShotCount == 0) {
                rangedState = RangedState.Reloading;
                nextAttackTime = Time.time + reloadTime;
            } else
                nextAttackTime = Time.time + fireInterval;
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
    }
}
