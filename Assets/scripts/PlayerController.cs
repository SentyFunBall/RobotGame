using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterAnimation))]
[RequireComponent(typeof(WeaponManager))]
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
    private float sprintTime = 1.0f;

    [SerializeField]
    private float sprintCooldown = 4.0f;

    private float sprintTimeRemaining;
    private float sprintCooldownRemaining;
    private bool sprintCooldownActive;

    [SerializeField]
    private Slider sprintBar;

    [SerializeField]
    private LayerMask groundMask;

    [SerializeField]
    private float groundCheckDistance = 0.05f;

    [Header("Audio")]
    [SerializeField]
    private AudioSource attackAudio;

    [SerializeField]
    private AudioSource rollingAudio;

    private PowerUpManager powerUpManager;
    private WeaponManager weaponManager;
    private CharacterAnimation characterAnimation;
    private bool grounded;

    private bool doAttack;
    private bool doMeleeAttack;

    [SerializeField]
    private float attackRange = 2f;

    [SerializeField]
    private float attackDamage = 10f;

    [SerializeField]
    private LayerMask enemyMask;

    [SerializeField]
    public float attackCooldown = 2.0f;

    public float nextAttackTime;

    [SerializeField]
    private ShopItem debugItem;

    void Awake()
    {
        controls = new InputSystem_Actions();
        characterAnimation = GetComponent<CharacterAnimation>();
        characterAnimation.FindAndSetAnimator();
        weaponManager = GetComponent<WeaponManager>();
    }

    void OnEnable()
    {
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Disable();
        if (rollingAudio != null)
            rollingAudio.Stop();
        grounded = false;
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
        if (attackAudio == null || attackAudio.clip == null)
            Debug.LogError("Player attack audio source or clip is not assigned.", this);
        if (rollingAudio == null || rollingAudio.clip == null)
            Debug.LogError("Player rolling audio source or clip is not assigned.", this);

        sprintTimeRemaining = sprintTime;
        sprintCooldownRemaining = 0f;
        sprintCooldownActive = false;

        if (sprintBar == null)
        {
            Debug.LogError("SprintBar slider not assigned.", this);
        }
        else
        {
            sprintBar.value = 1f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (powerUpManager.IsPowerUpActive(PowerUpType.Stealth))
        {
            rollingAudio.volume = 0.0585f; // Reduce volume by 50% if Stealth power-up is active
        }
        else
        {
            rollingAudio.volume = 0.117f;
        }
        if (GetComponent<PlayerHealth>().Health <= 0)
        {
            moveInput = Vector2.zero;
            if (rollingAudio != null && rollingAudio.isPlaying)
                rollingAudio.Stop();
            return;
        }

        if (Time.timeScale == 0f)
        {
            moveInput = Vector2.zero;
            doAttack = false;
            if (rollingAudio != null && rollingAudio.isPlaying)
                rollingAudio.Stop();
            return;
        }

        if (rollingAudio != null)
        {
            Vector3 velocity = rb.linearVelocity;
            bool shouldRoll =
                Time.timeScale > 0f
                && grounded
                && new Vector2(velocity.x, velocity.z).sqrMagnitude > 0.01f;
            if (shouldRoll && !rollingAudio.isPlaying)
                rollingAudio.Play();
            else if (!shouldRoll && rollingAudio.isPlaying)
                rollingAudio.Stop();
        }

        moveInput = controls.Player.Move.ReadValue<Vector2>();
        isSlow = controls.Player.Crouch.ReadValue<float>() > 0.5f;
        isSpeedPowerUpActive = powerUpManager.IsPowerUpActive(PowerUpType.Speed);

        // Handle sprint cooldown
        if (isSpeedPowerUpActive)
        {
            if (sprintCooldownActive)
            {
                sprintCooldownRemaining -= Time.deltaTime;
                if (sprintCooldownRemaining <= 0f)
                {
                    sprintCooldownActive = false;
                    sprintTimeRemaining = sprintTime;
                    sprintCooldownRemaining = 0f;
                }
            }

            isSprint = controls.Player.Sprint.ReadValue<float>() > 0.5f;

            // Only allow sprinting if not in cooldown and have time remaining
            if (isSprint && !sprintCooldownActive)
            {
                sprintTimeRemaining -= Time.deltaTime;
                if (sprintTimeRemaining <= 0f)
                {
                    sprintTimeRemaining = 0f;
                    sprintCooldownActive = true;
                    sprintCooldownRemaining = sprintCooldown;
                    isSprint = false;
                }
            }
            else
            {
                isSprint = false;
            }

            // Update sprint bar
            if (sprintBar != null)
            {
                if (sprintCooldownActive)
                {
                    // Show elapsed cooldown time (empty to full as cooldown completes)
                    sprintBar.value = 1.0f - (sprintCooldownRemaining / sprintCooldown);
                }
                else
                {
                    sprintBar.value = sprintTimeRemaining / sprintTime;
                }
            }
        }
        else
        {
            isSprint = false;
        }

        // Latch the press: Update can run several times between FixedUpdates, and
        // overwriting with `triggered` each frame dropped most attack inputs
        if (controls.Player.Attack.triggered)
            doAttack = true;

        if (
            (controls.Player.Attack.triggered && !GetComponent<WeaponManager>().HasAnyWeapon())
            || controls.Player.SecondaryAttack.triggered
        )
            doMeleeAttack = true;

#if UNITY_EDITOR
        bool t = controls.Player.DEBUGAddPowerUp.triggered;
        if (t)
        {
            powerUpManager.ActivatePowerUp(PowerUpType.Speed);
            sprintTimeRemaining = sprintTime;
            sprintCooldownRemaining = 0f;
            sprintCooldownActive = false;

            if (debugItem != null)
            {
                GetComponent<WeaponManager>().AddWeapon(debugItem);
                GetComponent<WeaponManager>().EquipWeapon(debugItem);
            }
        }
#endif
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
        grounded = Physics.Raycast(
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
            {
                characterAnimation.Sprint(speed);
            }
            else
            {
                characterAnimation.Sprint(0);
                characterAnimation.SetMovementSpeed(speed);
            }
        }

        // Attack
        if (doAttack || doMeleeAttack)
        {
            if (Time.time < nextAttackTime)
                return;

            float cooldown = attackCooldown;
            if (weaponManager.EquippedWeapon != null)
                cooldown = weaponManager.EquippedWeapon.weaponCooldown;

            nextAttackTime = Time.time + cooldown;

            if (doMeleeAttack || weaponManager.EquippedWeapon == null)
                DoMeleeAttack();
            else
                DoWeaponAttack();

            doAttack = false;
            doMeleeAttack = false;
        }
    }

    private bool TryDamageTarget(Collider targetCollider, bool melee)
    {
        float damage = attackDamage;
        if (!melee)
        {
            damage = Mathf.Max(
                0f,
                weaponManager.EquippedWeapon != null
                    ? weaponManager.EquippedWeapon.weaponDamage
                    : 0f
            );
        }
        EnemyAI enemy = targetCollider.GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Debug.Log("Hit");

            if (attackAudio != null)
                attackAudio.Play();
            return true;
        }

        RobotBossAI boss = targetCollider.GetComponentInParent<RobotBossAI>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
            Debug.Log("Hit Boss");

            if (attackAudio != null)
                attackAudio.Play();

            return true;
        }

        return false;
    }

    public void SetSprintSlider(Slider slider)
    {
        sprintBar = slider;
        if (sprintBar != null)
        {
            sprintBar.value = 1f;
        }
    }

    private void DoMeleeAttack()
    {
        Transform playerChild = transform.GetChild(0);
        Vector3 attackDir = Vector3.ProjectOnPlane(playerChild.forward, Vector3.up).normalized;
        // Cast from body height; the child pivot is at the feet, below enemy colliders
        Vector3 origin = playerCollider.bounds.center;
        const float attackRadius = 1.0f;
        bool hitTarget = false;
        Collider[] closeTargets = Physics.OverlapSphere(
            origin,
            attackRadius,
            enemyMask,
            QueryTriggerInteraction.Ignore
        );
        foreach (Collider closeTarget in closeTargets)
        {
            Vector3 targetOffset = closeTarget.ClosestPoint(origin) - origin;
            if (targetOffset.sqrMagnitude < Mathf.Epsilon)
                targetOffset = closeTarget.bounds.center - origin;
            targetOffset.y = 0f;
            if (Vector3.Dot(attackDir, targetOffset) >= 0f && TryDamageTarget(closeTarget, true))
            {
                hitTarget = true;
                break;
            }
        }

        if (
            !hitTarget
            && Physics.SphereCast(
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
            TryDamageTarget(hitInfo.collider, false);
        }
        characterAnimation.Attack();
    }

    private void DoWeaponAttack()
    {
        ShopItem weapon = weaponManager.EquippedWeapon;
        if (weapon == null)
            return;

        switch (weapon.weaponType)
        {
            case WeaponType.Ranged:
                DoRangedAttack(weapon);
                break;
            case WeaponType.EMP:
                DoEMPAttack(weapon);
                break;
            case WeaponType.Bazooka:
                DoBazookaAttack(weapon);
                break;
            case WeaponType.Rail:
                DoRailAttack(weapon);
                break;
        }
    }

    private void DoRangedAttack(ShopItem weapon)
    {
        GameObject bulletPrefabToUse = weapon.bulletPrefab;
        Vector3 forward = transform.GetChild(0).forward;
        if (bulletPrefabToUse == null)
            return;

        GameObject bullet = Instantiate(
            bulletPrefabToUse,
            transform.position + forward,
            Quaternion.identity
        );
        bullet.GetComponent<Rigidbody>().linearVelocity = forward * 20f;
        bullet.transform.forward = forward;
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
            bulletComponent.parentWeapon = weapon;
        despawnBulletAfterDelay(bullet, weapon.weaponRange / 10f);
    }

    private void DoEMPAttack(ShopItem weapon)
    {
        Transform playerChild = transform.GetChild(0);
        Vector3 attackDir = Vector3.ProjectOnPlane(playerChild.forward, Vector3.up).normalized;
        Vector3 origin = playerCollider.bounds.center;
        const float attackRadius = 1.0f;

        float range = Mathf.Max(attackRange, weapon.weaponRange);

        bool hitTarget = false;
        Collider[] closeTargets = Physics.OverlapSphere(
            origin,
            attackRadius,
            enemyMask,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider closeTarget in closeTargets)
        {
            Vector3 targetOffset = closeTarget.ClosestPoint(origin) - origin;
            if (targetOffset.sqrMagnitude < Mathf.Epsilon)
                targetOffset = closeTarget.bounds.center - origin;
            targetOffset.y = 0f;
            if (Vector3.Dot(attackDir, targetOffset) >= 0f)
            {
                ApplyEMPFreeze(closeTarget, weapon.weaponDamage);
                hitTarget = true;
                break;
            }
        }

        if (!hitTarget)
        {
            if (
                Physics.SphereCast(
                    origin,
                    attackRadius,
                    attackDir,
                    out RaycastHit hitInfo,
                    range,
                    enemyMask,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                ApplyEMPFreeze(hitInfo.collider, weapon.weaponDamage);
            }
        }
    }

    private void ApplyEMPFreeze(Collider targetCollider, float duration)
    {
        EnemyAI enemy = targetCollider.GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            enemy.Freeze(duration);
            if (attackAudio != null)
                attackAudio.Play();
        }
    }

    private void DoBazookaAttack(ShopItem weapon)
    {
        GameObject bulletPrefabToUse = weapon.bulletPrefab;
        if (bulletPrefabToUse == null)
            return;

        GameObject rocket = Instantiate(
            bulletPrefabToUse,
            transform.position + transform.forward,
            Quaternion.identity
        );
        Rigidbody rb = rocket.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = transform.forward * 15f;

        despawnBulletAfterDelay(rocket, 10f);
    }

    private void DoRailAttack(ShopItem weapon)
    {
        Transform playerChild = transform.GetChild(0);
        Vector3 attackDir = Vector3.ProjectOnPlane(playerChild.forward, Vector3.up).normalized;
        Vector3 origin = playerCollider.bounds.center;

        float range = Mathf.Max(attackRange, weapon.weaponRange);

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            attackDir,
            range,
            enemyMask,
            QueryTriggerInteraction.Ignore
        );

        float damage = Mathf.Max(0f, attackDamage + weapon.weaponDamage);

        foreach (RaycastHit hit in hits)
        {
            EnemyAI enemy = hit.collider.GetComponentInParent<EnemyAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                if (attackAudio != null)
                    attackAudio.Play();
                break;
            }

            RobotBossAI boss = hit.collider.GetComponentInParent<RobotBossAI>();
            if (boss != null)
            {
                boss.TakeDamage(damage);
                if (attackAudio != null)
                    attackAudio.Play();
                break;
            }
        }
    }

    private void despawnBulletAfterDelay(GameObject bullet, float delay)
    {
        Destroy(bullet, delay);
    }
}
