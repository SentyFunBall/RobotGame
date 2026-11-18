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

    private MeleeState meleeState = MeleeState.None;
    private float meleeStateEndTime;
    private Vector3 lockedDirection;
    private bool lungeHit;
    private bool previousUpdateRotation;

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
            playerHealth.TakeDamage(
                attackDamage * DungeonGenerator.Instance.Difficulty.EnemyStrength
            );
        }
    }

    private void Attack()
    {
        if (Time.time < nextAttackTime)
            return;

        if (attackAudio != null && attackAudio.clip != null)
            attackAudio.PlayOneShot(attackAudio.clip);

        if (enemyType == EnemyType.Melee)
        {
            StartMeleeAttack();
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

    private bool dying;
    public bool IsAlive => !dying;

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (GetComponent<AudioSource>() != null)
        {
            hitAudio.Play();
        }
        if (health <= 0f && !dying)
        {
            dying = true;
            Die();
        }
        Flash();
    }

    public void Freeze(float duration)
    {
        freezeEndTime = Time.time + duration;
    }

    private void Die()
    {
        characterAnimation.SetDead(true);
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
    }
}
