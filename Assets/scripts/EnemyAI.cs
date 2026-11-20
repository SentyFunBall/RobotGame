using UnityEngine;
using UnityEngine.AI;
using static SpawnUtility;

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

    [Header("Audio")]
    [SerializeField]
    private float reloadTime = 5;

    [SerializeField]
    private AudioSource stepAudio;

    [SerializeField]
    private AudioSource hitAudio;

    [SerializeField]
    private AudioSource attackAudio;

    [Header("Loot")]
    [SerializeField]
    private GameObject lootPrefab;

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

    [Header("Heavy Animation")]
    [SerializeField]
    [Tooltip("Exact Bool parameter name in the Heavy Animator Controller.")]
    private string heavyWalkingBoolParam = "IsWalking";

    [SerializeField]
    [Tooltip("Exact Trigger parameter name for the Heavy attack.")]
    private string heavyAttackTriggerParam = "Attack";

    [SerializeField]
    [Tooltip("Exact Trigger parameter name for the Heavy death animation.")]
    private string heavyDieTriggerParam = "Die";

    [SerializeField]
    private float heavyWalkSpeedThreshold = 0.1f;

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
    private Vector3 previousPosition;

    private CharacterAnimation characterAnimation;

    private float freezeEndTime = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyCollider = GetComponentInChildren<Collider>();
        characterAnimation = GetComponent<CharacterAnimation>();
        characterAnimation.FindAndSetAnimator();
        health = maxHealth;

        renderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        if (stepAudio != null)
            stepAudio.loop = true;
    }

    private void OnEnable()
    {
        previousPosition = transform.position;
    }

    private void OnDisable()
    {
        if (stepAudio != null)
            stepAudio.Stop();
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

        bool isFrozen = Time.time < freezeEndTime;
        if (isFrozen)
        {
            agent.isStopped = true;
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
        if (PowerUpManager.instance.IsPowerUpActive(PowerUpType.Stealth))
        {
            detectionRadius *= 0.5f; // Reduce detection radius by 50% if Stealth power-up is active
        }

        if (distance <= detectionRadius)
        {
            if (enemyType == EnemyType.Melee || enemyType == EnemyType.Heavy)
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

    private void LateUpdate()
    {
        Vector3 position = transform.position;
        if (stepAudio != null)
        {
            bool moving =
                agent.isActiveAndEnabled
                && Time.timeScale > 0f
                && (position - previousPosition).sqrMagnitude > 0f
                && !agent.isStopped;
            if (moving && !stepAudio.isPlaying)
                stepAudio.Play();
            else if (!moving && stepAudio.isPlaying)
                stepAudio.Stop();
        }
        previousPosition = position;
    }

    private void UpdateAnimatorSpeed()
    {
        if (characterAnimation != null)
        {
            float speed = meleeState == MeleeState.Lunge ? lungeSpeed : agent.velocity.magnitude;
            if (enemyType == EnemyType.Heavy)
            {
                bool isWalking = heavyState == HeavyState.None && speed > heavyWalkSpeedThreshold;
                characterAnimation.SetBool(heavyWalkingBoolParam, isWalking);
                return;
            }
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
        characterAnimation.SetBool(heavyWalkingBoolParam, false);
        characterAnimation.SetTrigger(heavyAttackTriggerParam);
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
            playerHealth.TakeDamage(
                attackDamage * DungeonGenerator.Instance.Difficulty.EnemyStrength
            );
        }
    }

    private void MeleeAttack()
    {
        if (Time.time < nextAttackTime)
            return;

        if (attackAudio != null && attackAudio.clip != null)
            attackAudio.PlayOneShot(attackAudio.clip);

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

        characterAnimation.Attack();
        nextAttackTime = Time.time + 3f; // Assuming 3 second cooldown between attacks

        // Apply damage to the player
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(
                attackDamage * DungeonGenerator.Instance.Difficulty.EnemyStrength
            );
        }
    }

    private void RangedAttack()
    {
        if (bulletPrefab == null)
            return;

        if (Time.time >= nextAttackTime)
        {
            if (attackAudio != null && attackAudio.clip != null)
                attackAudio.PlayOneShot(attackAudio.clip);

            GameObject bulletObj = Instantiate(bulletPrefab, attackOrigin.transform.position, Quaternion.identity);
            bulletObj.transform.forward = (player.position - attackOrigin.transform.position).normalized;
            if (bulletObj.TryGetComponent(out EnemyBullet bullet))
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
        Flash();
        if (GetComponent<AudioSource>() != null)
        {
            hitAudio.Play();
        }
        if (health <= 0f && !dying)
        {
            dying = true;
            Die();
        }
    }

    public void Freeze(float duration)
    {
        freezeEndTime = Time.time + duration;
    }

    private void Die()
    {
        heavyState = HeavyState.None;
        if (enemyType == EnemyType.Heavy)
        {
            characterAnimation.SetBool(heavyWalkingBoolParam, false);
            characterAnimation.SetTrigger(heavyDieTriggerParam);
        }
        else
        {
            characterAnimation.SetDead(true);
        }
        agent.enabled = false;
        if (stepAudio != null)
            stepAudio.Stop();
        if (enemyCollider != null)
            enemyCollider.enabled = false;
        SpawnLoot();
        StartCoroutine(ShrinkAndDisable());
    }

    private void SpawnLoot()
    {
        Debug.Log("Spawning loot");
        if (lootPrefab == null)
            return;

        if (TryGetRandomNavMeshPosition(transform.position, 2f, out Vector3 spawnPosition))
        {
            GameObject newLoot = Instantiate(lootPrefab);
            newLoot.transform.position = spawnPosition;
        }
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
