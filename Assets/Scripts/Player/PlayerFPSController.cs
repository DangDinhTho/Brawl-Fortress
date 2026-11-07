using System;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerFPSController : MonoBehaviour
{
    //public PlayerInput playerInput;
    public CharacterController m_CharacterController;

    [HideInInspector]
    public Vector3 Velocity;
    [HideInInspector]
    public PlayerState State;
    private PlayerState lastState;
    [HideInInspector] public PlayerFPState fpsState;
    [HideInInspector]
    public bool jumpInput;
    private bool m_Jump;
    private bool m_Jumping;
    private Vector3 moveDirection;
    private Vector3 targetDirection;
    private Vector3 desiredMove;
    private Vector3 momentum;
    public float acceleration;
    public float jumpSpeed;
    public float jumpMomentumBooster = 2;
    public float dropSpeed;
    private Vector2 m_Input;
    public LerpControlledBob m_JumpBob;
    private Transform m_Transform;
    public CollisionFlags m_CollisionFlags { get; set; }
    [Range(0, 2)] public float AirControlMultiplier = 0.8f;
    public float m_GravityMultiplier;
    private float speed;
    private float rotateSpeed = 30;
    public float runSpeed;
    public float JumpMinRate;
    public float m_StickToGroundForce;
    public float momentunDecaySpeed = 5;
    public float forceDamage;
    private bool m_PreviouslyGrounded;
    private bool isFalling;
    private bool hasTouchGround;
    private float PostGroundVerticalPos;
    private float fallingTime;
    private float HigherPointOnJump;
    private float HorizontalInput;
    private float VerticalInput;
    private float VelocityMagnitude;
    private float lastJumpTime;
    private float capsuleRadious;
    private int JumpDirection;
    private bool hasPlatformJump = false;
    private float PlatformJumpForce = 0;
    private bool JumpInmune = false;
    private RaycastHit[] SurfaceRay = new RaycastHit[1];
    private Vector3 surfaceNormal = Vector3.zero;
    private Vector3 surfacePoint = Vector3.zero;
    public float slideSpeed = 10;
    public TF2Player player;
    //public TouchPadUI touchPadUI;
    public float sensitivity = 3;
    private float horizontalRotation;
    private float verticalRotation;
    private float CurrentSensitivity;
    private float rotAverageX;
    private float rotAverageY;
    private int framesOfSmoothing = 5;
    private float teamRotate = 1;

    //[HideInInspector]
    //public FPSReferences fpsRef;
    
    [HideInInspector]
    public WeaponType weaponType = WeaponType.Primary;

    [HideInInspector]
    public Weapon currWeapon;

    //private Camera fpsCam;
    //private Animator animator;

    [HideInInspector]
    public Dictionary<WeaponType, Weapon> weaponSlots = new Dictionary<WeaponType, Weapon>();

    public float damageForce = 10;
    private Vector3 damageDirection;
    public Action<Weapon> OnSwitchWeapon;
    public Action<bool> OnSwitchCam;
    private Vector3 forwardCam;
    private Vector3 move;
    private Transform camera;


    private void Awake()
    {
        m_Transform = transform;
        capsuleRadious = m_CharacterController.radius * 0.1f;
        CurrentSensitivity = sensitivity;
        
        //touchPadUI = TouchPadUI.instan;
    }

    // Start is called before the first frame update
    void Start()
    {
        //if (!player.playerNetwork.photonView.IsMine) return;
        //for (int i = 0; i < player.weapons.Count; i++)
        //{
        //    Weapon weapon = Instantiate(Resources.Load<GameObject>($"Weapons/{player.weapons[i]}"), player.fPSReferences.fpsWeaponContainer).GetComponent<Weapon>();
        //    weapon.InitData(player, player.fPSReferences.fpsWeaponBone, 0);
        //    if (!weaponSlots.ContainsKey(weapon.weaponConfig.weaponType))
        //    {
        //        weaponSlots.Add(weapon.weaponConfig.weaponType, weapon);
        //    }
        //    SetWeapon(weapon);
        //    weapon.gameObject.SetActive(false);
        //}

        //fpsCam = player.fPSReferences.fpsCam;

        //player.healthManager.OnDeath += OnDeath;
        //player.healthManager.OnRespawn += OnRespawn;
        //player.healthManager.OnTakeDamage += OnTakeDamage;

        //SwitchWeapon(player.characterConfig.defaultWeapon);
        //animator = player.fPSReferences.animator;
    }

    public void Init()
    {
        weaponType = player.characterConfig.defaultWeapon;

        if (!player.playerNetwork.photonView.IsMine) return;
        foreach (var weapon in player.remotePlayer.weaponSlots)
        {
            if (weapon.Value.weaponId == WeaponId.None) continue;
            //Weapon weapon = Instantiate(Resources.Load<GameObject>($"Weapons/{weaponId.Value}"), player.fPSReferences.fpsWeaponContainer).GetComponent<Weapon>();
            //weapon.transform.localPosition = Vector3.zero;
            //weapon.InitData(player, player.fPSReferences.fpsWeaponBone);
            if (!weaponSlots.ContainsKey(weapon.Key))
            {
                weaponSlots.Add(weapon.Key, weapon.Value);
            }
            SetWeapon(weapon.Value);
            //weapon.gameObject.SetActive(false);
        }

        //fpsRef = player.fPSReferences;
        //fpsCam = player.fPSReferences.fpsCam;
       // animator = player.fPSReferences.animator;

        player.healthManager.OnDeath += OnDeath;
        player.healthManager.OnRespawn += OnRespawn;
        player.healthManager.OnTakeDamage += OnTakeDamage;

        SwitchWeapon(player.characterConfig.defaultWeapon);

        runSpeed = player.characterConfig.characterStats.speed;
        jumpSpeed = player.characterConfig.characterStats.jumpForce;
        //player.remotePlayer.tPSLookAt.enabled = false;
        //this.enabled = true;
        camera = CameraController.instan.freeCam.transform;
        SwitchTPS();
        CameraController.instan.InitCam(player.team);
        if (player.team == Team.blue) teamRotate = -1;
    }

    // Update is called once per frame
    void Update()
    {
        Velocity = m_CharacterController.velocity;
        VelocityMagnitude = Velocity.magnitude;
        //GroundDetection();
        UpdateFire();


        //MovementInput();
        //CheckStates();
        //RotateLook();
        //if (Input.GetMouseButtonDown(0))
        //if(Input.GetKeyDown(KeyCode.Z))
        //{
        //    currWeapon.BeginFire();
        //}
        //else if (Input.GetKeyDown(KeyCode.R))
        //{
        //    ReloadClick();

        //}
        //else if (Input.GetKeyUp(KeyCode.Z))
        //{
        //    //fpsState = PlayerFPState.Idle;
        //    currWeapon.FinishFire();
        //}
        //else if (Input.GetKeyUp(KeyCode.Alpha3))
        //{
        //    SwitchWeapon(WeaponType.Mele);
        //}
        //else if (Input.GetKeyUp(KeyCode.Alpha2))
        //{
        //    SwitchWeapon(WeaponType.Secondary);
        //}
        //else if (Input.GetKeyUp(KeyCode.Alpha1))
        //{
        //    SwitchWeapon(WeaponType.Primary);
        //}
        //else if (Input.GetKeyUp(KeyCode.K))
        //{
        //    player.healthManager.Die(player.viewID, Vector3.up - Vector3.forward);
        //}
        //else if (Input.GetKeyUp(KeyCode.L))
        //{
        //    player.healthManager.Respawn();

        //}
        //else if (Input.GetKey(KeyCode.B))
        //{
        //    SwitchWeapon(WeaponType.PDA);
        //}
        //else if (Input.GetKeyUp(KeyCode.Alpha5))
        //{
        //    SwitchWeapon(WeaponType.Building);
        //    player.fPSReferences.GetComponent<EngineerBlueprint>().SelectBuild(BuildableType.Sentry);
        //}
        //else if (Input.GetKeyUp(KeyCode.Alpha6))
        //{
        //    SwitchWeapon(WeaponType.Building);
        //    player.fPSReferences.GetComponent<EngineerBlueprint>().SelectBuild(BuildableType.Dispenser);
        //}
        //else if (Input.GetKeyUp(KeyCode.Alpha7))
        //{
        //    SwitchWeapon(WeaponType.Building);
        //    player.fPSReferences.GetComponent<EngineerBlueprint>().SelectBuild(BuildableType.TeleporterEnter);
        //}
        //else if (Input.GetKeyDown(KeyCode.P))
        //{
        //    if (sentry != null && sentry.state == BuildableState.Moving) return;
        //    SwitchWeapon(WeaponType.Mele);
        //    sentry = player.fPSReferences.GetComponent<EngineerBlueprint>().Build();
        //    //sentry.team = player.team;

        //}
        //else if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.O))
        //{
        //    SwitchWeapon(WeaponType.Primary);
        //    player.fPSReferences.GetComponent<EngineerBlueprint>().Cancel();

        //}
        //else if ( Input.GetKeyDown(KeyCode.M))
        //{
        //    if (sentry == null) return;
        //    if(!sentry.OnMove()) return;
        //    SwitchWeapon(WeaponType.Building);
        //    player.fPSReferences.GetComponent<EngineerBlueprint>().SelectMove(sentry.buildableType);
        //}
        //else if (Input.GetKeyDown(KeyCode.L))
        //{
        //    if (sentry == null) return;
        //    PosRot posRot = player.fPSReferences.GetComponent<EngineerBlueprint>().Drop(sentry.buildableType);
        //    if (posRot == null) return;
        //    SwitchWeapon(WeaponType.Mele);
        //    sentry.OnDrop(posRot.positon, posRot.rotation);
        //}
        //else if (Input.GetKeyDown(KeyCode.J))
        //{
        //    if(currWeapon is Sniperrifle)
        //    {
        //        Sniperrifle sp = (Sniperrifle)currWeapon;
        //        if (!sp.deployed)
        //        {
        //            player.playerNetwork.SyncFireDeployedStart();
        //            sp.deployed = true;
        //        }
        //        else
        //        {
        //            player.playerNetwork.SyncFireDeployedFinish();
        //            sp.deployed = false;
        //        }

        //    }
        //}
        //else if (Input.GetKeyDown(KeyCode.I) && !animator.GetBool("Invis"))
        //{
        //    player.fPSReferences.GetComponent<SpyController>().Invisibility();
        //    animator.SetBool("Invis", true);
        //}

        //else if (Input.GetKeyDown(KeyCode.I) && animator.GetBool("Invis"))
        //{
        //    player.fPSReferences.GetComponent<SpyController>().ShowAvailble();
        //    animator.SetBool("Invis", false);
        //}

        //else if (Input.GetKeyDown(KeyCode.T))
        //{
        //    GUIManager.instan.CreatePopup<PopupTaunts>();
        //    //PlayTaunt(Taunt.heavy_taunt_bare_knuckle_beatdown);
        //}

        // player.playerAnimator.Velocity = Velocity;
    }

    private BuildableEntry sentry;


    public void PlayTaunt(Taunt taunt)
    {
        //SwitchTPS();
        player.remotePlayer.PlayTaunt(taunt);
    }

    public void EndTaunt()
    {
        player.remotePlayer.EndTaunt();
        
    }

    public void OnDeath()
    {
        m_CharacterController.enabled = false;
        //SwitchTPS();
    }

    [HideInInspector]
    public bool isTPS;
    public void SwitchTPS()
    {
        if(isTPS) return;
        OnSwitchCam?.Invoke(true);
        isTPS = true;
        //fpsRef.gameObject.SetActive(false);
        player.remotePlayer.gameObject.SetActive(true);
        //CameraController.instan.SetFreeLook(true, transform, player.fPSReferences.fpsRotate.transform);
        //player.remotePlayer.SwitchWeapon(weaponType);
        //CameraController.instan.fpsDragController.gameObject.SetActive(false);
        //CameraController.instan.dragController.gameObject.SetActive(true);
    }

    public void SwitchFPS()
    {
        //StartCoroutine(WaitSwitchFPS());
    }

    public void OnRespawn()
    {
        StartCoroutine(WaitSwitchFPS());
        //SwitchTPS();
    }

    IEnumerator WaitSwitchFPS()
    {
        isTPS = false;
        //OnSwitchCam?.Invoke(false);
        //CameraController.instan.dragController.gameObject.SetActive(false);
        //CameraController.instan.fpsDragController.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.2f);
        CameraController.instan.SetFreeLook(true);
        m_CharacterController.enabled = true;
        //fpsRef.gameObject.SetActive(true);
        player.remotePlayer.gameObject.SetActive(true);
        SwitchWeapon(weaponType);
    }

    #region WEAPON
    public void SwitchWeapon(WeaponType weaponType)
    {
        if (!weaponSlots.ContainsKey(weaponType) || weaponSlots[weaponType] == null) return;
        this.weaponType = weaponType;
        if (currWeapon != null)
        {
            currWeapon.OnHide();
        }
        currWeapon = weaponSlots[weaponType];
        currWeapon.gameObject.SetActive(true);

        if (fpsState == PlayerFPState.Reloading)
        {
            fpsState = PlayerFPState.Idle;
            //animator.SetBool("Reload", false);
        }

        //animator.SetInteger("Weapon", (int)weaponType);
        //animator.SetTrigger("SwitchWeapon");
        OnSwitchWeapon?.Invoke(currWeapon);
        player.remotePlayer.SwitchWeapon(weaponType);
        player.playerNetwork.SyncSwitchWeapon(weaponType);
        if (player.isMainPlayer && player.tF2Class == TF2Class.engineer)
        {
            UICharacterInputController.instan.SetWeaponTypeWithoutNotify(weaponType);
        }
        
    }

    public void SetWeapon(Weapon weapon)
    {
        weapon.OnBeginFire += OnWeaponStartFire;
        weapon.OnFire += OnWeaponFireLoop;
        weapon.OnFinishFire += OnWeaponFinishFire;
        if(weapon is Gun)
        {
            Gun gun = (Gun)weapon;
            gun.OnTouchFire += OnWeaponTouchFire;
            gun.OnStartReload += OnWeaponStartReload;
            gun.OnReloadLoop += OnWeaponReloadLoop;
            gun.OnReloadDone += OnWeaponReloadDone;
            gun.OnFinishReload += OnWeaponFinishReload;
            gun.OnClipEmpty += OnFireEmpty;
        }
    }


    //public void OnWeaponStart()
    //{
    //    fpsState = PlayerFPState.Firing;
    //}

    public void OnWeaponTouchFire()
    {
        fpsState = PlayerFPState.Deployed;
        //animator.SetBool("Deployed", true);
    }

    public void OnWeaponStartFire()
    {
        //fpsState = PlayerFPState.Deployed;
        //animator.SetBool("Deployed", true);
    }

    public void OnFireEmpty()
    {
        //animator.SetBool("FireLoop", false);
    }

    public void OnWeaponFireLoop()
    {
        //animator.Play("Fire", 0, 0);
        if (weaponType == WeaponType.Mele)
            //animator.SetInteger("RandomInt", UnityEngine.Random.Range(0, 3));
        //animator.SetTrigger("Fire");
        //animator.SetBool("FireLoop", true);
        fpsState = PlayerFPState.Firing;
    }

    public void OnWeaponFinishFire()
    {
        if (fpsState == PlayerFPState.Firing || fpsState == PlayerFPState.Deployed)
            fpsState = PlayerFPState.Idle;
        //animator.SetBool("FireLoop", false);
        //animator.SetBool("Deployed", false);
        //fpsState = nextState;
    }


    public void ReloadClick()
    {
        if(currWeapon is Gun)
        {
            Gun g = (Gun)currWeapon;
            g.StartReload();
        }
    }


    public void OnWeaponStartReload()
    {
        fpsState = PlayerFPState.Reloading;
        //animator.SetTrigger("Reload");
        //animator.SetBool("Reload", true);
        //animator.Play("ReloadStart", 0, 0);
    }
    
    public void OnWeaponReloadLoop()
    {
        //animator.Play("ReloadLoop", 0, 0);
        //animator.SetTrigger("ReloadLoop");
    }

    public void OnWeaponReloadDone()
    {
        //animator.SetBool("Reload", false);
        if (fpsState == PlayerFPState.Reloading)
            fpsState = PlayerFPState.Idle;
    }

    public void OnWeaponFinishReload()
    {
        //if (fpsState == PlayerFPState.Reloading)
        //    fpsState = PlayerFPState.Idle;
    }

    public void BuffWeapon(float rance, bool fromDispense = false)
    {
        foreach(Weapon wp in weaponSlots.Values)
        {
            if(wp as Gun)
            {
                ((Gun)wp).Buff(rance);
            }
            else if(!fromDispense && wp as Wrench)
            {
                ((Wrench)wp).OnBuff(rance);
            }
        }
        
        if(player.tF2Class == TF2Class.spy)
        {
             UISpyHUD.instance.BuffWatch(rance);
        }
    }

    #endregion

    private void CheckStates()
    {
        if (lastState == State) return;
        OnStateChanged(lastState, State);
        lastState = State;
    }

    private void OnStateChanged(PlayerState from, PlayerState to)
    {
        //animator.SetInteger("");
    }

    void MovementInput()
    {
        //if (State == PlayerState.Sliding)
        //{
        //    if (playerInput.Jump())
        //    {
        //        State = PlayerState.Jumping;
        //        m_Jump = true;
        //    }
        //    else return;
        //}

        //if (bl_UtilityHelper.isMobile) return;

        if (!m_Jump && State != PlayerState.Crouching && (Time.time - lastJumpTime) > JumpMinRate)
        {
            m_Jump = UICharacterInputController.instan.jump; //Input.GetKeyDown(KeyCode.Space);
            lastJumpTime = Time.time;
        }
    }

    

    public void FixedUpdate()
    {
        if (State == PlayerState.Teleport) return;
        ////if (Finish)
        ////    return;
        //if (m_CharacterController == null || !m_CharacterController.enabled)
        //    return;

        GetInput(out speed);
        Move();
        UpdateRotation();
        

        //if player focus is in game
        //if (bl_RoomMenu.Instance.isCursorLocked && !bl_GameData.Instance.isChating)
        //{
        //    //determine the player speed
        //float s;
        //GetInput(out s);
        //speed = s;


        //Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); //Just a ray through the middle of your current view
        //RaycastHit hit;

        //check if ray hits something
        // Vector3 targetPoint;
        //if (Physics.Raycast(ray, out hit))
        //    player.targetLook = hit.point;
        //else
        //    player.targetLook = ray.GetPoint(75);
        //}
        //else
        //if (State != PlayerState.Sliding)//if player is not focus in game
        //{
        //    m_Input = Vector2.zero;
        //}

        //if (isClimbing && m_Ladder != null)
        //{
        //    //climbing control
        //    OnClimbing();
        //}
        //else
        //{
        //player movement
        //Move();
        //}
    }

    private bool isTouchFire;
    public void UpdateFire()
    {
        if(!isTouchFire && UICharacterInputController.instan.aimingJoystick.Direction.magnitude >= 0.8f)
        {
            isTouchFire = true;
            currWeapon.BeginFire();
        }else if (isTouchFire && UICharacterInputController.instan.aimingJoystick.Direction.magnitude < 0.8f)
        {
            currWeapon.FinishFire();
            isTouchFire = false;
        }
    }

    public void OnTakeDamage(Vector3 direction)
    {
        damageDirection = transform.InverseTransformVector(direction);
        State = PlayerState.TakeDamage;
        //PlatformJump(damageForce);
        //DoJump();
        //moveDirection = direction;
        //targetDirection = direction;
        // transform.GetComponent<Rigidbody>()
        //     .AddForceAtPosition(m_CharacterController.velocity * 0.1f, direction * forceDamage, ForceMode.Impulse);
    }
    
    private void GetInput(out float speed)
    {

        // Read input
        HorizontalInput = UICharacterInputController.instan.joystick.Horizontal; //Input.GetAxis("Horizontal");
        //HorizontalInput = Input.GetAxis("Horizontal");
        VerticalInput = UICharacterInputController.instan.joystick.Vertical; //Input.GetAxis("Vertical");
        ///VerticalInput = Input.GetAxis("Vertical");

        
        

#if MFPSM
        if (bl_UtilityHelper.isMobile)
        {
            HorizontalInput = Joystick.Horizontal;
            VerticalInput = Joystick.Vertical;
            VerticalInput = VerticalInput * 1.25f;
        }
#endif
        if (State == PlayerState.Sliding)
        {
            VerticalInput = 1;
            HorizontalInput = 0;
        }
        else if(State == PlayerState.TakeDamage)
        {
            VerticalInput = damageDirection.z;
            HorizontalInput = damageDirection.x;
        }

        // m_Input = Vector2.Lerp(m_Input, new Vector2(HorizontalInput, VerticalInput), Time.deltaTime * 25);
        forwardCam = Vector3.Scale(camera.up, new Vector3(1, 0, 1)).normalized;
        move = VerticalInput * forwardCam + HorizontalInput * camera.right;
        if (move.magnitude > 1) move.Normalize();
        Vector3 localMove = transform.InverseTransformDirection(move);

        m_Input = new Vector2(localMove.x, localMove.z);
        float inputMagnitude = m_Input.magnitude;
        //if the player is dropping, the speed is calculated in the dropping function
        if (State == PlayerState.Dropping || State == PlayerState.Gliding) { speed = 0; return; }


        if (inputMagnitude > 0 && State != PlayerState.Crouching)
        {
            if (VelocityMagnitude > 0)
            {
                //print(VelocityMagnitude);
                //if (State != PlayerState.TakeDamage || VelocityMagnitude > 3)
                //{
                    State = PlayerState.Running;
               // }

            }
            else
            {
                if (State != PlayerState.Jumping)
                {
                    State = PlayerState.Idle;
                }
            }

        }
        else if (m_CharacterController.isGrounded)
        {
            if (State != PlayerState.Jumping && State != PlayerState.Crouching)
            {
                State = PlayerState.Idle;
            }
        }



        // set the desired speed to be walking or running
        speed = runSpeed;// (State == PlayerState.Running && m_CharacterController.isGrounded) ? runSpeed : 0;
        
        // normalize input if it exceeds 1 in combined length:
        if (inputMagnitude > 1)
        {
            m_Input.Normalize();
        }

        //if (RunFovEffect)
        //{
        //    float rf = (State == PlayerState.Running && m_CharacterController.isGrounded) ? runFOVAmount : 0;
        //    RunFov = Mathf.Lerp(RunFov, rf, Time.deltaTime * 6);
        //}
    }

    public Vector3 GetLocalVelocity() => m_Transform.InverseTransformDirection(Velocity);

    private int jumpCount;
    void Move()
    {
        //if the player is touching the surface
        if (m_CharacterController.isGrounded)
        {
            OnSurface();
            moveDirection.y = -m_StickToGroundForce;
            hasTouchGround = true;
            if (jumpCount > 0)
                jumpCount = 0;

            if ((jumpInput && (Time.time - lastJumpTime > JumpMinRate)) || hasPlatformJump)
            {
                m_Jump = true;
                DoJump();
            }
        }
        else//if the player is not touching the ground
        {
            OnAir();
            if (player.characterConfig.characterStats.doubleJump)
            {
                if (!jumpInput && jumpCount == 0)
                {
                    jumpCount = 1;
                }
                else if(jumpInput)
                {
                    if (!m_Jump && jumpCount == 1 && (Time.time - lastJumpTime > JumpMinRate / 2.5f))
                    {
                        m_Jump = true;
                        jumpCount = 2;
                        DoJump();
                    }
                }
            }
        }

        moveDirection = Vector3.Lerp(moveDirection, targetDirection, acceleration * Time.fixedDeltaTime);
        //apply the movement direction in the character controller
        m_CharacterController.Move(moveDirection * Time.fixedDeltaTime);
        
    }

    public void UpdateRotation()
    {
        //RotateLook(UICharacterInputController.instan.aimingJoystick.Horizontal, UICharacterInputController.instan.aimingJoystick.Vertical);
        //Vector3 lookDir = new Vector3(UICharacterInputController.instan.aimingJoystick.Horizontal, 0, UICharacterInputController.instan.aimingJoystick.Vertical);

        
        if (UICharacterInputController.instan.aimingJoystick.Direction.magnitude > 0)
        {
            Quaternion quaternion = Quaternion.LookRotation(new Vector3(UICharacterInputController.instan.aimingJoystick.Horizontal * teamRotate, 0, UICharacterInputController.instan.aimingJoystick.Vertical * teamRotate));
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, quaternion, rotateSpeed);
        }
        else if(move.magnitude > 0)
        {
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.LookRotation(move), rotateSpeed);
        }

       

        //transform.LookAt(transform.position + lookDir, Vector3.up);

    }

    public void RotateLook(float horizontal, float vertical)
    {
        //if (player.healthManager.death) return;
        //horizontalRotation = horizontal; //CameraController.instan.fpsDragController.GetXAxis();  //UltimateTouchpad.GetHorizontalAxis("Camera");
        //verticalRotation = vertical; // CameraController.instan.fpsDragController.GetYAxis(); //-UltimateTouchpad.GetVerticalAxis("Camera");

        // Rotate the camera by the h and v variables.
        //camTrans.Rotate(Vector3.up, h * Time.deltaTime);
        //camTrans.Rotate(Vector3.left, v * Time.deltaTime);

        // Finalize the rotation so that there is no Z rotation.
        //Quaternion finalRot = Quaternion.Euler(camTrans.rotation.eulerAngles.x, camTrans.rotation.eulerAngles.y, 0);
        //camTrans.rotation = finalRot;
        //horizontalRotation = input.x * CurrentSensitivity;


        //rotArrayX.Add(horizontalRotation);
        //if ((float)rotArrayX.Count >= framesOfSmoothing)
        //{
        //    rotArrayX.RemoveAt(0);
        //}
        //for (int i = 0; i < rotArrayX.Count; i++)
        //{
        //    rotAverageX += rotArrayX[i];
        //}
        //rotAverageX /= rotArrayX.Count;
        //rotAverageX += horizontalRotation;
        //rotAverageY += verticalRotation;
        //rotAverageY = Mathf.Clamp(rotAverageY, -80f, 88f);

        //transform.localRotation = Quaternion.Euler(0f, rotAverageX, 0f);

        
        
        //fpsRef.fpsRotate.localRotation = Quaternion.Euler(rotAverageY, 0f, 0f);
        //Debug.Log($"{verticalRotation}_{fpsRef.localRotation.x}");
        //if ((fpsRef.localRotation.x < 85 && verticalRotation < 0) || (fpsRef.localRotation.x > -85 && verticalRotation > 0))
        //    fpsRef.localRotation *= Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    private float timeNextStepSound;
    public float stepInteval;
    void OnSurface()
    {
        // always move along the camera forward as it is the direction that it being aimed at
        desiredMove = (m_Transform.forward * m_Input.y) + (m_Transform.right * m_Input.x);

        // get a normal for the surface that is being touched to move along it
        Physics.SphereCastNonAlloc(m_Transform.localPosition, capsuleRadious, Vector3.down, SurfaceRay, m_CharacterController.height * 0.5f, Physics.AllLayers, QueryTriggerInteraction.Ignore);

        //determine the movement angle based in the normal of the current player surface
        desiredMove = Vector3.ProjectOnPlane(desiredMove, SurfaceRay[0].normal);
        targetDirection.x = desiredMove.x * speed;
        targetDirection.z = desiredMove.z * speed;

        if(desiredMove.magnitude > 0 && Time.time > timeNextStepSound && !isTPS)
        {
            player.PlayFootStep();
            timeNextStepSound = Time.time + stepInteval;
        }

        SlopeControl();
    }


    void SlopeControl()
    {
        float angle = Vector3.Angle(Vector3.up, surfaceNormal);

        if (angle <= m_CharacterController.slopeLimit || angle >= 75) return;

        Vector3 direction = Vector3.up - surfaceNormal * Vector3.Dot(Vector3.up, surfaceNormal);
        float speed = slideSpeed + 1 + Time.deltaTime;

        targetDirection += direction * -speed;
        targetDirection.y = targetDirection.y - surfacePoint.y;
    }

    public void DoJump()
    {
        momentum = desiredMove * jumpMomentumBooster;
        
        moveDirection.y = (hasPlatformJump) ? PlatformJumpForce : jumpSpeed;
        targetDirection.y = moveDirection.y;
        //PlayJumpSound();
        m_Jump = false;
        m_Jumping = true;
        hasPlatformJump = false;
        State = PlayerState.Jumping;
        lastJumpTime = Time.time;
    }

    public void PlatformJump(float force)
    {
        hasPlatformJump = true;
        PlatformJumpForce = force;
        JumpInmune = true;
    }


    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        surfaceNormal = hit.normal;
        surfacePoint = hit.point;
        /// Enable this if you want player controller apply force on contact to rigidbodys
        /// is commented by default for performance matters.
        /* Rigidbody body = hit.collider.attachedRigidbody;
         //dont move the rigidbody if the character is on top of it
         if (m_CollisionFlags == CollisionFlags.Below)
         {
             return;
         }

         if (body == null || body.isKinematic)
         {
             return;
         }
         body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);*/
    }

    void OnDropping()
    {
        //get the camera upside down angle
        //float tilt = 1; //Mathf.InverseLerp(0, 90, mouseLook.VerticalAngle);
        //normalize it
        //tilt = Mathf.Clamp01(tilt);
        //if (mouseLook.VerticalAngle <= 0 || mouseLook.VerticalAngle >= 180) tilt = 0;
        //get the forward direction of the player camera
        //desiredMove = headRoot.forward * Mathf.Clamp01(m_Input.y);
        //if (desiredMove.y > 0) desiredMove.y = 0;

        //calculate the drop speed based in the upside down camera angle
        //float dropSpeed = Mathf.Lerp(m_GravityMultiplier * dropTiltSpeedRange.x, m_GravityMultiplier * dropTiltSpeedRange.y, tilt);
        targetDirection = Physics.gravity * dropSpeed * Time.fixedDeltaTime;
        //if the player press the vertical input -> add velocity in the direction where the camera is looking at
        targetDirection += desiredMove;

        //apply the movement direction in the character controller
        m_CollisionFlags = m_CharacterController.Move(targetDirection * Time.fixedDeltaTime);
    }

    
    void OnAir()
    {
        desiredMove = (m_Transform.forward * Mathf.Clamp01(m_Input.y)) + (m_Transform.right * m_Input.x);
        desiredMove += momentum;

        targetDirection += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
        targetDirection.x = (desiredMove.x * speed) * AirControlMultiplier;
        targetDirection.z = (desiredMove.z * speed) * AirControlMultiplier;

        momentum = Vector3.Lerp(momentum, Vector3.zero, Time.fixedDeltaTime * momentunDecaySpeed);
    }

    void GroundDetection()
    {
        //if the player has touch the ground after falling
        if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
        {
            OnLand();
        }
        else if (m_PreviouslyGrounded && !m_CharacterController.isGrounded)//when the player start jumping
        {
            if (!isFalling)
            {
                PostGroundVerticalPos = m_Transform.position.y;
                isFalling = true;
                fallingTime = Time.time;
            }
        }

        if (isFalling)
        {
            VerticalDirectionCheck();
        }

        if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
        {
            targetDirection.y = 0f;
        }


        m_PreviouslyGrounded = m_CharacterController.isGrounded;
    }

    void VerticalDirectionCheck()
    {
        if (m_Transform.position.y == PostGroundVerticalPos) return;

        //if the direction has not been decided yet
        if (JumpDirection == 0)
        {
            //is the player below or above from the surface he was?
            // 1 = above (jump), -1 = below (falling)
            JumpDirection = (m_Transform.position.y > PostGroundVerticalPos) ? 1 : -1;
        }
        else if (JumpDirection == 1)//if the player jump
        {
            //but not start falling
            if (m_Transform.position.y < PostGroundVerticalPos)
            {
                //get the higher point he reached jumping
                HigherPointOnJump = PostGroundVerticalPos;
            }
            else//if still going up
            {
                PostGroundVerticalPos = m_Transform.position.y;
            }
        }
        else//if the player was falling without jumping
        {

        }
    }

    /// <summary>
    /// Called when the player hit a surface after falling/jumping
    /// </summary>
    void OnLand()
    {
        //land camera effect
        StartCoroutine(m_JumpBob.DoBobCycle());

        //isFalling = false;
       // float fallDistance = CalculateFall();
        //bl_EventHandler.DispatchPlayerLandEvent(fallDistance);

        //haslanding = true;
        //JumpDirection = 0;
        targetDirection.y = 0f;
        m_Jumping = false;
        //m_CharacterController.stepOffset = defaultStepOffset;
        //if (State != PlayerState.Crouching)
        State = PlayerState.Idle;

    }


    public void Snip(bool snip)
    {
        if(currWeapon is Sniperrifle)
        {
            Sniperrifle sp = (Sniperrifle)currWeapon;
            if (snip)
            {
                player.playerNetwork.SyncFireDeployedStart();
                sp.deployed = true;
            }
            else
            {
                player.playerNetwork.SyncFireDeployedFinish();
                sp.deployed = false;
            }
              
        }
    }


}



