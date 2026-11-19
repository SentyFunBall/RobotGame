using System.Collections.Generic;
using UnityEngine;

public class ExplosiveHazard : MonoBehaviour
{
    [Header("Trigger & Fuse")]
    [SerializeField]
    [Tooltip("Distance at which the player arms this explosive hazard.")]
    private float triggerRange = 4.5f;

    [SerializeField]
    [Range(3f, 5f)]
    [Tooltip("Seconds between arming and detonation.")]
    private float fuseDuration = 4f;

    [SerializeField]
    [Tooltip("If true, leaving triggerRange resets the fuse before detonation.")]
    private bool cancelIfPlayerLeaves = false;

    [Header("Explosion")]
    [SerializeField]
    [Tooltip("Radius of the lethal blast.")]
    private float explosionRadius = 5f;

    [SerializeField]
    [Tooltip("Damage dealt to the player in the blast radius (high enough to kill).")]
    private float playerDamage = 10000f;

    [SerializeField]
    [Tooltip("Whether the explosion also damages enemies in range.")]
    private bool damageEnemies = true;

    [SerializeField]
    private float enemyDamage = 50f;

    [SerializeField]
    private float knockbackForce = 12f;

    [SerializeField]
    private GameObject explosionVfxPrefab;

    [Header("Visual Feedback")]
    [SerializeField]
    private Color idleColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [SerializeField]
    private Color warningColor = Color.red;

    [SerializeField]
    private float minPulseSpeed = 3f;

    [SerializeField]
    private float maxPulseSpeed = 16f;

    [SerializeField]
    [Tooltip("Peak scale multiplier while armed.")]
    private float maxScalePulse = 1.2f;

    [SerializeField]
    [Tooltip("Optional child object (e.g., ground warning ring) shown while armed.")]
    private GameObject warningIndicator;

    [SerializeField]
    [Tooltip("Optional Light component that flashes faster as detonation approaches.")]
    private Light warningLight;

    [SerializeField]
    private float maxLightIntensity = 6f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private Transform player;
    private Renderer[] renderers;
    private MaterialPropertyBlock propBlock;
    private Vector3 initialScale;

    private bool isArmed;
    private bool hasExploded;
    private float fuseElapsed;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
        initialScale = transform.localScale;

        ApplyColor(idleColor, Color.black);

        if (warningIndicator != null)
            warningIndicator.SetActive(false);

        if (warningLight != null)
            warningLight.enabled = false;
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (hasExploded)
            return;

        if (player == null)
        {
            FindPlayer();
            if (player == null)
                return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (!isArmed)
        {
            if (distance <= triggerRange)
            {
                Arm();
            }
            return;
        }

        if (cancelIfPlayerLeaves && distance > triggerRange)
        {
            Disarm();
            return;
        }

        fuseElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(fuseElapsed / Mathf.Max(0.01f, fuseDuration));
        UpdateVisualFeedback(progress);

        if (fuseElapsed >= fuseDuration)
        {
            Explode();
        }
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void Arm()
    {
        isArmed = true;
        fuseElapsed = 0f;

        if (warningIndicator != null)
            warningIndicator.SetActive(true);

        if (warningLight != null)
            warningLight.enabled = true;
    }

    private void Disarm()
    {
        isArmed = false;
        fuseElapsed = 0f;
        transform.localScale = initialScale;
        ApplyColor(idleColor, Color.black);

        if (warningIndicator != null)
            warningIndicator.SetActive(false);

        if (warningLight != null)
            warningLight.enabled = false;
    }

    private void UpdateVisualFeedback(float progress)
    {
        float pulseFrequency = Mathf.Lerp(minPulseSpeed, maxPulseSpeed, progress * progress);
        float pulse = 0.5f + 0.5f * Mathf.Sin(fuseElapsed * pulseFrequency * Mathf.PI);

        Color currentColor = Color.Lerp(idleColor, warningColor, pulse);
        Color emission = warningColor * (pulse * Mathf.Lerp(1f, 3f, progress));
        ApplyColor(currentColor, emission);

        float scaleFactor = Mathf.Lerp(1f, maxScalePulse, progress * (0.4f + 0.6f * pulse));
        transform.localScale = initialScale * scaleFactor;

        if (warningLight != null)
        {
            warningLight.color = warningColor;
            warningLight.intensity = pulse * Mathf.Lerp(1.5f, maxLightIntensity, progress);
        }
    }

    private void ApplyColor(Color baseCol, Color emissionCol)
    {
        if (renderers == null || propBlock == null)
            return;

        foreach (Renderer r in renderers)
        {
            if (r == null)
                continue;
            r.GetPropertyBlock(propBlock);
            propBlock.SetColor(BaseColorId, baseCol);
            propBlock.SetColor(EmissionColorId, emissionCol);
            r.SetPropertyBlock(propBlock);
        }
    }

    private void Explode()
    {
        hasExploded = true;

        if (explosionVfxPrefab != null)
        {
            Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        HashSet<GameObject> damagedRoots = new HashSet<GameObject>();

        foreach (Collider hit in hits)
        {
            PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null && damagedRoots.Add(playerHealth.gameObject))
            {
                playerHealth.TakeDamage(playerDamage);
                ApplyKnockback(playerHealth.GetComponent<Rigidbody>(), playerHealth.transform.position);
                continue;
            }

            if (!damageEnemies)
                continue;

            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy != null && enemy.IsAlive && damagedRoots.Add(enemy.gameObject))
            {
                enemy.TakeDamage(enemyDamage);
                continue;
            }

            RobotBossAI boss = hit.GetComponentInParent<RobotBossAI>();
            if (boss != null && damagedRoots.Add(boss.gameObject))
            {
                boss.TakeDamage(enemyDamage);
            }
        }

        Destroy(gameObject);
    }

    private void ApplyKnockback(Rigidbody targetRb, Vector3 targetPos)
    {
        if (targetRb == null || knockbackForce <= 0f)
            return;

        Vector3 pushDir = targetPos - transform.position;
        pushDir.y = 0f;
        pushDir = pushDir.sqrMagnitude > 0.0001f ? pushDir.normalized : Vector3.forward;
        targetRb.AddForce(pushDir * knockbackForce, ForceMode.Impulse);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}