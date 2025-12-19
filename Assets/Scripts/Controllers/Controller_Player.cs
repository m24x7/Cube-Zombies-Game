using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Resource_Health))]
[RequireComponent(typeof(Controller_Build))]
[RequireComponent(typeof(Inputs))]
[RequireComponent(typeof(Inventory))]
public class Controller_Player : Parent_Entity
{
    //[SerializeField] private Controller_Build buildController;

    [SerializeField] private Collider playerCollider;
    public Collider PlayerCollider => playerCollider;

    [SerializeField] protected LayerMask blockingBuildMask = ~0;
    [SerializeField] private GameObject blockPrefab;
    //[SerializeField] private GameObject block;

    private int itemInHand = 0;

    [SerializeField] private Inventory inventory;
    public Inventory Inventory { get => inventory; }

    //[SerializeField] private Item_Weapon_Melee sword;

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


    //public GameObject Map;

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
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius - 0.1f, GroundLayers, QueryTriggerInteraction.Ignore);
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

            //// Jump
            //if (_input.jump && _jumpTimeoutDelta <= 0.0f)
            //{
            //    // the square root of H * -2 * G = how much velocity needed to reach desired height
            //    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            //}

            //// jump timeout
            //if (_jumpTimeoutDelta >= 0.0f)
            //{
            //    _jumpTimeoutDelta -= Time.deltaTime;
            //}
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
        
        inventory = GetComponent<Inventory>();

        InitializeBasicInventory();
    }

    // Update is called once per frame
    void Update()
    {
        if (_input.pauseGame)
        {
            if (uiManager != null)
            {
                uiManager.TogglePauseMenu();
                _input.pauseGame = false;
            }
        }

        if (_input.instructions)
        {
            uiManager.ToggleInstructions();
            _input.instructions = false;
        }

        if (Time.timeScale == 0f) return;

        MoveUpdate();

        // Handle Inputs
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

        if (_input.hotBarInput3)
        {
            Debug.Log("Hotbar 3 pressed");
            itemInHand = 2;
            _input.hotBarInput3 = false;

            _input.attack = false;
            _input.useItem = false;
        }
        if (_input.hotBarInput4)
        {
            Debug.Log("Hotbar 4 pressed");
            itemInHand = 3;
            _input.hotBarInput4 = false;

            _input.attack = false;
            _input.useItem = false;
        }

        if (_input.useItem)
        {
            // Check what is hit by raycast from camera
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            //Debug.DrawRay(ray.origin, ray.direction * 10);
            Physics.Raycast(ray, out RaycastHit hit, 5f);

            //if (hit.collider == null) Debug.Log("No hit");
            //else Debug.Log("Hit: " + hit.transform.name);

            if (hit.collider != null)
            {
                GameObject hitObject = hit.transform.root.gameObject;
                if (hitObject.transform.gameObject.GetComponent<WallBuy>())
                {
                    WallBuy wallBuy = hitObject.transform.gameObject.GetComponent<WallBuy>();

                    bool canBuy = wallBuy.CanBuy(points);

                    if (canBuy && wallBuy.Item != null && inventory.StillRoomInInventory())
                    {
                        // Deduct points
                        points -= wallBuy.Cost;

                        // instantiate item
                        var item = Instantiate(wallBuy.Item, transform.Find("MainCamera/Armature/AttatchPoint"));

                        // Add item to inventory
                        inventory.AddItem(item);
                        //Debug.Log("Bought item: " + wallBuy.Item.name);

                        if (uiManager != null) uiManager.UpdateScore();
                        if (uiManager != null) uiManager.UpdateHotbarSlots();

                        _input.useItem = false;
                    }
                }
                else if ( itemInHand < inventory.InventoryItems.Count)
                {
                    if (inventory.InventoryItems[itemInHand].GetComponent<Item_Block>() != null) placeBlock();
                }
            }

            _input.useItem = false;
        }

        //Debug.Log("attackInput: " + _input.attack);
        if (itemInHand < inventory.InventoryItems.Count)
        {
            if (_input.attack && attackCooldown <= 0 && inventory.InventoryItems[itemInHand].GetComponent<Item_Weapon_Melee>() != null)
            {
                Attack();
                //_input.attack = false; // prevent multiple attack inputs

                attackCooldown = inventory.InventoryItems[itemInHand].GetComponent<Item_Weapon_Melee>().SwingSpeed; //* Time.fixedDeltaTime;
                                                                                                                    //Debug.Log("Attack initiated. Cooldown set to: " + attackCooldown);

                string[] attackSounds = new string[2] { "Sounds/Draw Weapon Metal 1-1", "Sounds/Stab 4-1" };

                AudioSource.PlayClipAtPoint(
                    Resources.Load<AudioClip>(attackSounds[UnityEngine.Random.Range(0, attackSounds.Length)]),
                    transform.position,
                    0.5f
                );
            }


            if (_input.attack && inventory.InventoryItems[itemInHand].GetComponent<Item_Block>() != null)
            {
                var blockDestroyed = BlockPlacer.TryDestroyBlockFromCamera(Camera.main, 5f, GroundLayers, LayerMask.NameToLayer("Blocks"), transform);
                _input.attack = false;
            }
        }

        switch (itemInHand)
        {
            case 0:
                if (uiManager != null) uiManager.SelectHotbarSlot(0);

                //buildController.enabled = true;
                inventory.InventoryItems[0].transform.Find("Mesh").gameObject.SetActive(true);
                inventory.InventoryItems[1].GetComponent<Item_Block>().gameObject.GetComponent<MeshRenderer>().enabled = false;

                for (int i = 2; i < inventory.InventoryItems.Count; i++)
                {
                    if (inventory.InventoryItems[i].GetComponent<I_Item>() != null && inventory.InventoryItems[i].transform.Find("Mesh").gameObject != null)
                    {
                        inventory.InventoryItems[i].transform.Find("Mesh").gameObject.SetActive(false);
                    }
                }
                break;
            case 1:
                if (uiManager != null) uiManager.SelectHotbarSlot(1);

                //buildController.enabled = false;
                inventory.InventoryItems[0].transform.Find("Mesh").gameObject.SetActive(false);
                inventory.InventoryItems[1].GetComponent<Item_Block>().gameObject.GetComponent<MeshRenderer>().enabled = true;

                for (int i = 2; i < inventory.InventoryItems.Count; i++)
                {
                    if (inventory.InventoryItems[i].GetComponent<I_Item>() != null && inventory.InventoryItems[i].transform.Find("Mesh").gameObject != null)
                    {
                        inventory.InventoryItems[i].transform.Find("Mesh").gameObject.SetActive(false);
                    }
                }
                break;
            case 2:
                if (uiManager != null) uiManager.SelectHotbarSlot(2);

                inventory.InventoryItems[1].GetComponent<Item_Block>().gameObject.GetComponent<MeshRenderer>().enabled = false;
                for (int i = 0; i < inventory.InventoryItems.Count; i++)
                {
                    if (inventory.InventoryItems[i].GetComponent<I_Item>() != null && inventory.InventoryItems[i].transform.Find("Mesh") != null)
                    {
                        inventory.InventoryItems[i].transform.Find("Mesh").gameObject.SetActive(false);
                    }
                }

                if (2 < inventory.InventoryItems.Count) inventory.InventoryItems[2].transform.Find("Mesh").gameObject.SetActive(true);
                break;
            case 3:
                if (uiManager != null) uiManager.SelectHotbarSlot(3);

                inventory.InventoryItems[1].GetComponent<Item_Block>().gameObject.GetComponent<MeshRenderer>().enabled = false;
                for (int i = 0; i < inventory.InventoryItems.Count; i++)
                {
                    if (inventory.InventoryItems[i].GetComponent<I_Item>() != null && inventory.InventoryItems[i].transform.Find("Mesh") != null)
                    {
                        inventory.InventoryItems[i].transform.Find("Mesh").gameObject.SetActive(false);
                    }
                }

                if (3 < inventory.InventoryItems.Count) inventory.InventoryItems[3].transform.Find("Mesh").gameObject.SetActive(true);
                break;
            default:
                if (uiManager != null) uiManager.SelectHotbarSlot(0);
                //buildController.enabled = true;
                inventory.InventoryItems[0].transform.Find("Mesh").gameObject.SetActive(false);
                inventory.InventoryItems[1].GetComponent<Item_Block>().gameObject.GetComponent<MeshRenderer>().enabled = true;

                for (int i = 2; i < inventory.InventoryItems.Count; i++)
                {
                    if (inventory.InventoryItems[i].GetComponent<I_Item>() != null && inventory.InventoryItems[i].transform.Find("Mesh").gameObject != null)
                    {
                        inventory.InventoryItems[i].transform.Find("Mesh").gameObject.SetActive(false);
                    }
                }
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
        var enemy = collision.collider.GetComponentInParent<EnemyController>();

        if (enemy != null && iFrames <= 0)
        {
            // Take damage over time while in contact with enemy
            TakeDamage(10);
        }
    }

    override public void Heal(int heal)
    {
        base.Heal(heal);

        OnHealthChange?.Invoke();
    }

    override public void TakeDamage(int damage, bool ignoreInvincibility = false)
    {
        // if currently in invincibility frames, ignore damage
        if (iFrames > 0 && !ignoreInvincibility) return;

        // Apply damage
        health.UpdateVal(-damage);

        // Set invincibility frames
        iFrames = 20; // Set invincibility frames (e.g., 20 frames)

        // Set regen wait timer
        regenWaitTimer = regenMaxWait;

        // Track total health lost
        healthLost += damage;

        // Notify health change
        OnHealthChange?.Invoke();

        // Check for death
        if (health.Cur <= 0) Die();
    }

    override protected void Die()
    {
        playerDie?.Invoke();
    }

    private void placeBlock()
    {
        var placedBlock = BlockPlacer.TryPlaceBlockFromCamera(Camera.main, 5f, blockingBuildMask, blockPrefab);
    }

    override public void Attack()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

        // 1) Precise thin ray first (long-range)
        if (TryHitScan(ray, inventory.InventoryItems[itemInHand].GetComponent<Item_Weapon_Melee>().Range, 0f, out var hit))
        {
            ApplyHit(hit);
            return;
        }

        // 2) Close-range assist (small sphere) if the thin ray missed
        if (TryHitScan(ray, inventory.InventoryItems[itemInHand].GetComponent<Item_Weapon_Melee>().Range, closeAssistRadius, out hit))
        {
            ApplyHit(hit);
            return;
        }

        // No hit
    }

    private bool TryHitScan(Ray ray, float range, float radius, out RaycastHit firstValid)
    {
        firstValid = default;

        Physics.Raycast(ray, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore);

        if (hit.collider != null)
        {
            var objectHit = hit.transform.root.gameObject;
            if (objectHit.layer == LayerMask.NameToLayer("Enemies"))
            {
                firstValid = hit;
                return true;
            }
        }

        return false;
    }

    private void ApplyHit(RaycastHit hit)
    {
        var enemy = hit.collider.GetComponentInParent<Parent_Entity>();
        if (enemy != null)
            enemy.TakeDamage(inventory.InventoryItems[itemInHand].GetComponent<Item_Weapon_Melee>() != null ? inventory.InventoryItems[itemInHand].GetComponent<Item_Weapon_Melee>().Damage : 10);
    }

    private void InitializeBasicInventory()
    {
        if (inventory.InventoryItems.Count == 0)
        {
            // Instantiate a default sword and block if none exist in inventory
            var basicSword = Resources.Load<ItemDefinition_Weapon_Melee>("Items/Defs/Weapons/Melee/Item_Weapon_Melee_CommonSword");
            var swordObject = Instantiate(basicSword.prefab, transform.Find("MainCamera/Armature/AttatchPoint"));
            inventory.AddItem(swordObject);

            var basicBlock = Resources.Load<GameObject>("Blocks/Block");
            var blockObject = Instantiate(basicBlock, transform.Find("MainCamera/Armature/AttatchPoint"));
            blockObject.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            blockObject.transform.localPosition = new Vector3(blockObject.transform.localPosition.x, 0.126f, blockObject.transform.localPosition.z);
            inventory.AddItem(blockObject);
        }
    }
}
