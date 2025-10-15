using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Controller_Player : Parent_Entity
{
    //[SerializeField] private Controller_Build buildController;

    [SerializeField] private Collider playerCollider;
    public Collider PlayerCollider => playerCollider;

    [SerializeField] protected LayerMask blockingBuildMask = ~0;
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private GameObject block;

    private int itemInHand = 0;

    [SerializeField] private Sword sword;

    // Attack Vars
    [SerializeField] private float closeAssistRadius = 0.18f;
    [SerializeField] private float attackCooldown = 0f;

    public Action OnHealthChange;
    public Action playerDie;

    [SerializeField] private UI_Manager uiManager;

    private int iFrames = 0; // invincibility frames counter

    private int points = 0;
    public int Points { get => points; set => points = value; }

    // Regen Vars
    [SerializeField] private int regenAmount = 1;
    [SerializeField] private float regenCooldown = 0.5f;
    [SerializeField] private float regenMaxWait = 3f;
    [SerializeField] private float regenWaitTimer = 0f;

    private int healthLost = 0;
    public int HealthLost { get => healthLost; }

    #region Movement
    [Header("Player")]
    [Tooltip("Move speed of the character in m/s")]
    public float MoveSpeed = 4.0f;
    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 6.0f;
    [Tooltip("Rotation speed of the character")]
    public float RotationSpeed = 1.0f;
    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    [Space(10)]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;
    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.1f;
    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;
    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;
    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.5f;
    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;
    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 90.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -90.0f;

    // cinemachine
    private float _cinemachineTargetPitch;

    // player
    private float _speed;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif
    private CharacterController _controller;
    private Inputs _input;
    private GameObject _mainCamera;

    private const float _threshold = 0.01f;

    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
        }
    }

    private void Awake()
    {
        // get a reference to our main camera
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void MoveStart()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<Inputs>();
#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    private void MoveUpdate()
    {
        JumpAndGravity();
        GroundedCheck();
        Move();
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }

    private void CameraRotation()
    {
        // if there is an input
        if (_input.look.sqrMagnitude >= _threshold)
        {
            //Don't multiply mouse input by Time.deltaTime
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
            _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

            // clamp our pitch rotation
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Update Cinemachine camera target pitch
            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

            // rotate the player left and right
            transform.Rotate(Vector3.up * _rotationVelocity);
        }
    }

    private void Move()
    {
        // set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

        // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

        // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is no input, set the target speed to 0
        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // creates curved result rather than a linear one giving a more organic speed change
            // note T in Lerp is clamped, so we don't need to clamp our speed
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

            // round speed to 3 decimal places
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        // normalise input direction
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is a move input rotate player when the player is moving
        if (_input.move != Vector2.zero)
        {
            // move
            inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
        }

        // move the player
        _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            // reset the fall timeout timer
            _fallTimeoutDelta = FallTimeout;

            // stop our velocity dropping infinitely when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            {
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            }

            // jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }

            // if we are not grounded, do not jump
            _input.jump = false;
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
    }
    #endregion

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MoveStart();
    }

    // Update is called once per frame
    void Update()
    {
        if (_input.pauseGame)
        {
            uiManager.TogglePauseMenu();
            _input.pauseGame = false;
        }

        if (_input.instructions)
        {
            uiManager.ToggleInstructions();
            _input.instructions = false;
        }

        if (Time.timeScale == 0f) return;

        MoveUpdate();

        //Debug.Log("attackInput: " + _input.attack);
        if (_input.attack && attackCooldown <= 0 && itemInHand == 0)
        {
            Attack();
            //_input.attack = false; // prevent multiple attack inputs

            attackCooldown = sword.SwingSpeed; //* Time.fixedDeltaTime;
            //Debug.Log("Attack initiated. Cooldown set to: " + attackCooldown);

            string[] attackSounds = new string[2] { "Sounds/Draw Weapon Metal 1-1", "Sounds/Stab 4-1" };

            AudioSource.PlayClipAtPoint(
                Resources.Load<AudioClip>(attackSounds[UnityEngine.Random.Range(0, attackSounds.Length)]),
                transform.position,
                0.5f
            );
        }

        if (_input.useItem && itemInHand == 1)
        {
            placeBlock();
            _input.useItem = false;
        }

        if (_input.attack && itemInHand == 1)
        {
            //if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hitInfo, 5f, LayerMask.NameToLayer("Blocks")))
            //{
            //    var blockComp = hitInfo.collider.gameObject;
            //    if (blockComp != null && blockComp.layer == LayerMask.NameToLayer("Blocks"))
            //    {
            //        Destroy(blockComp.gameObject);
            //    }
            //}
            BlockPlacer.TryDestroyBlockFromCamera(Camera.main, 5f, GroundLayers, LayerMask.NameToLayer("Blocks"), transform);
            _input.attack = false;
        }

        if (_input.swapSword)
        {
            itemInHand = 0;
            _input.swapSword = false;

            _input.attack = false;
            _input.useItem = false;
        }

        if (_input.swapBlock)
        {
            itemInHand = 1;
            _input.swapBlock = false;

            _input.attack = false;
            _input.useItem = false;
        }


        switch (itemInHand)
        {
            case 0:
                uiManager.SelectHotbarSlot(0);
                //buildController.enabled = true;
                sword.gameObject.GetComponent<MeshRenderer>().enabled = true;
                block.gameObject.GetComponent<MeshRenderer>().enabled = false;
                break;
            case 1:
                uiManager.SelectHotbarSlot(1);
                //buildController.enabled = false;
                sword.gameObject.GetComponent<MeshRenderer>().enabled = false;
                block.gameObject.GetComponent<MeshRenderer>().enabled = true;
                break;
            default:
                uiManager.SelectHotbarSlot(0);
                //buildController.enabled = true;
                sword.gameObject.GetComponent<MeshRenderer>().enabled = false;
                block.gameObject.GetComponent<MeshRenderer>().enabled = true;
                break;
        }
    }

    private void LateUpdate()
    {
        if (Time.timeScale == 0f) return;
        CameraRotation();
    }

    void FixedUpdate()
    {
        if (iFrames > 0)
            iFrames--;

        if (attackCooldown > 0)
        {
            attackCooldown--;
            //Debug.Log("Attack cooldown: " + attackCooldown);
        }

        if (regenWaitTimer > 0f)
        {
            regenWaitTimer -= Time.fixedDeltaTime;
        }
        else
        {
            if (health.Cur < health.Max)
            {
                Heal(regenAmount);
                regenWaitTimer = regenCooldown;
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        var enemy = collision.collider.GetComponentInParent<TestEnemyController>();

        if (enemy != null && iFrames <= 0)
        {
            // Take damage over time while in contact with enemy
            TakeDamage(10);
            iFrames = 20; // Set invincibility frames (e.g., 20 frames)

            // Set regen wait timer
            regenWaitTimer = regenMaxWait;
        }
    }

    override public void Heal(int heal)
    {
        base.Heal(heal);

        OnHealthChange?.Invoke();
    }

    override public void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        healthLost += damage;

        OnHealthChange?.Invoke();

        if (health.Cur <= 0) Die();
    }

    override protected void Die()
    {
        playerDie?.Invoke();
    }

    private void placeBlock()
    {
        BlockPlacer.TryPlaceBlockFromCamera(Camera.main, 5f, blockingBuildMask, blockPrefab);
    }

    override public void Attack()
    {
        // Build a ray from the screen center (your crosshair)
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Nudge origin slightly forward so it starts outside your head/capsule
        ray.origin += ray.direction * 0.06f;

        // 1) Precise thin ray first (long-range)
        if (TryHitScan(ray, sword.Range, 0f, out var hit))
        {
            ApplyHit(hit);
            return;
        }

        // 2) Close-range assist (small sphere) if the thin ray missed
        if (TryHitScan(ray, sword.Range, closeAssistRadius, out hit))
        {
            ApplyHit(hit);
            return;
        }

        // No hit
    }

    private bool TryHitScan(Ray ray, float range, float radius, out RaycastHit firstValid)
    {
        firstValid = default;

        RaycastHit[] hits = (radius > 0f)
            ? Physics.SphereCastAll(ray, radius, range, attackMask, QueryTriggerInteraction.Collide)
            : Physics.RaycastAll(ray, range, attackMask, QueryTriggerInteraction.Collide);

        if (hits == null || hits.Length == 0) return false;

        //Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            // Robust self-filter: ignore anything under our own root (covers weapon arms, etc.)
            if (h.collider && h.collider.transform.root == transform.root)
                continue;

            // Enemy?
            if (h.collider.GetComponentInParent<TestEnemyController>() != null)
            {
                firstValid = h;
                return true;
            }

            // Solid non-enemy blocks the shot path
            if (!h.collider.isTrigger)
                return false;
        }
        return false;
    }

    private void ApplyHit(RaycastHit hit)
    {
        var enemy = hit.collider.GetComponentInParent<Parent_Entity>();
        if (enemy != null)
            enemy.TakeDamage(sword != null ? sword.Damage : 10);
    }

    override protected IEnumerator AttackRoutine()
    {
        // (Optional) play windup animation/sound here
        yield return new WaitForSeconds(attackWindup);

        // Do the actual hit once (hitscan)
        DoAttackRaycast();

        // If you want a short "active" window (e.g., for multi-hit sweep), you could
        // loop DoAttackRaycast() during attackActive. For hitscan, once is typical.
        if (attackActive > 0f)
            yield return new WaitForSeconds(attackActive);

        // Recovery (can’t attack again during this)
        if (attackRecovery > 0f)
            yield return new WaitForSeconds(attackRecovery);

        attackCoroutine = null;
    }
    private void DoAttackRaycast()
    {
        Debug.Log("Player Attack: Swinging sword");

        //// Origin/direction from your existing setup
        //Vector3 origin = transform.position;
        //origin.y += 1.375f; // adjust to approximate player "eye" height
        //Vector3 direction = Camera.main.transform.forward;

        // Visualize shot
        Debug.DrawRay(transform.position + new Vector3(0f, 1.375f, 0f), Camera.main.transform.forward * sword.Range, Color.red, 0.1f);

        // First-hit logic that respects blockers (walls)
        var hits = Physics.RaycastAll(
            transform.position + new Vector3(0f, 1.375f, 0f),
            Camera.main.transform.forward,
            sword.Range,
            ~0, //attackMask,
            QueryTriggerInteraction.Ignore
        );

        Debug.Log("Player Attack: Hit " + hits.Length + " things.");

        if (hits == null || hits.Length == 0) return;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            // Skip self
            if (hit.collider == playerCollider || hit.collider.transform.root == transform.root) continue;

            var enemy = hit.collider.GetComponentInParent<Parent_Entity>();
            if (enemy != null)
            {
                enemy.TakeDamage(sword.Damage);
                // (Optional) VFX/SFX using hit.point, hit.normal
                return;
            }

            // Solid non-enemy blocks the ray
            if (!hit.collider.isTrigger)
                return;
        }
    }
    public void CancelAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
    }
}
