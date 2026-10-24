using UnityEngine;
using UnityEngine.AI;

public enum DetectionState
{
    Normal,
    Alert,
    Sneak,
}

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

    private NavMeshAgent agent;
    private Transform player;
    private float nextAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
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
        if (player == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

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
    }

    private void Attack()
    {
        if (Time.time >= nextAttackTime)
        {
            // Implement attack logic here, e.g., reduce player health
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
            nextAttackTime = Time.time + 1f; // Example attack cooldown of 1 second
            Debug.Log("Enemy attacked the player for " + attackDamage + " damage.");
        }
    }
}
