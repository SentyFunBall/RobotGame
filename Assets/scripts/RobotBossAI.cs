using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RobotBossAI : MonoBehaviour
{
    [Header("Boss Stats")]
    [SerializeField]
    private float maxHealth = 300f;
    private float currentHealth;
    public float Health => currentHealth;
    public float MaxHealth => maxHealth;
    private bool isDead = false;

    [Header("Detection & Movement")]
    [SerializeField]
    private float detectionRadius = 35f;

    [SerializeField]
    private float attackRange = 18f;

    [SerializeField]
    private float slamTriggerDistance = 6f;

    [Header("Ground Slam")]
    [SerializeField]
    private float slamRadius = 7f;

    [SerializeField]
    private float slamDamage = 40f;

    [SerializeField]
    private float slamCooldown = 6f;

    [SerializeField]
    private GameObject slamVfxPrefab;

    [Header("Terrain Throw")]
    [SerializeField]
    private GameObject rockPrefab;

    [SerializeField]
    private Transform throwSocket;

    [SerializeField]
    private float throwCooldown = 8f;

    [SerializeField]
    private float throwForce = 22f;

    [SerializeField]
    private float upwardArc = 4f;

    private Transform player;
    private NavMeshAgent agent;
    private bool isAttacking = false;
    private float nextSlamTime = 0f;
    private float nextThrowTime = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void Update()
    {
        if (isDead || player == null || isAttacking)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > detectionRadius)
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.isStopped = true;
            return;
        }

        // Face player while engaging
        Vector3 targetDir = (player.position - transform.position).normalized;
        targetDir.y = 0;
        if (targetDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(targetDir),
                Time.deltaTime * 5f
            );

        // Attack selection based on proximity and cooldowns
        if (distance <= slamTriggerDistance && Time.time >= nextSlamTime)
        {
            StartCoroutine(PerformGroundSlam());
        }
        else if (distance <= attackRange && Time.time >= nextThrowTime)
        {
            StartCoroutine(PerformTerrainThrow());
        }
        else
        {
            if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
        }
    }

    private IEnumerator PerformGroundSlam()
    {
        isAttacking = true;
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            agent.isStopped = true;

        yield return new WaitForSeconds(0.8f);

        Vector3 impactPos = transform.position + transform.forward * 1.5f;

        if (slamVfxPrefab != null)
            Instantiate(slamVfxPrefab, impactPos, Quaternion.identity);

        Collider[] hits = Physics.OverlapSphere(impactPos, slamRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(slamDamage);
                }
            }
        }

        nextSlamTime = Time.time + slamCooldown;
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
    }

    private IEnumerator PerformTerrainThrow()
    {
        isAttacking = true;
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            agent.isStopped = true;

        yield return new WaitForSeconds(0.6f);

        Vector3 spawnPoint =
            throwSocket != null
                ? throwSocket.position
                : (transform.position + Vector3.up * 2f + transform.forward);
        GameObject rock = Instantiate(rockPrefab, spawnPoint, Quaternion.identity);

        Rigidbody rb = rock.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 aimDir = (player.position - spawnPoint).normalized;
            Vector3 velocity = (aimDir * throwForce) + (Vector3.up * upwardArc);
            rb.linearVelocity = velocity;
        }

        nextThrowTime = Time.time + throwCooldown;
        yield return new WaitForSeconds(0.4f);
        isAttacking = false;
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        StopAllCoroutines();

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            agent.isStopped = true;

        // Win is triggered by reaching the win zone, which requires the boss to be defeated
        if (DungeonGenerator.Instance != null)
            DungeonGenerator.Instance.BossDefeated = true;

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        StartCoroutine(ShrinkAndDisable());
    }

    private IEnumerator ShrinkAndDisable()
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * 1.5f, slamRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
