using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : AnimatorBase
{
    #region Public members
    //public AnimationCurve dropTiltAngleCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public float blendSmoothness = 100;
    #endregion

    public bool useFootSteps { get; set; } = false;
    public float VelocityMagnitude
    {
        get;
        set;
    }

    public RemotePlayer remotePlayer;
    #region Private members
    private RaycastHit footRay;
    private float reloadSpeed = 1;
    private PlayerState lastBodyState = PlayerState.Idle;
    private float deltaTime = 0.02f;
    private Transform m_Transform;
    private Dictionary<string, int> animatorHashes;
    private bool HitType = false;
    
    private float vertical, horizontal;
    private Transform PlayerRoot;
    private float turnSpeed;
    private float TurnLerp = 0;
    private float lastYRotation;
    private float movementSpeed;
    //[SerializeField]
    //private GameObject PlayerReferences;
    public float runSpeed;
    #endregion

    /// <summary>
    /// 
    /// </summary>
    protected void Awake()
    {
        if (animatorHashes == null)
        {
            FetchHashes();
        }
    }

    public void Init(TF2Player player)
    {
        //base.Awake();

        m_Transform = transform;
        // useFootSteps = bl_GameData.Instance.CalculateNetworkFootSteps;
        PlayerRoot = player.transform;
        //if (useFootSteps)
        //{
        //    footstep = PlayerReferences.firstPersonController.GetFootStep();
        //}
        if (animatorHashes == null)
        {
            FetchHashes();
        }
        this.enabled = true;
    }

    /// <summary>
    /// 
    /// </summary>
    //protected override void OnEnable()
    //{
    //    base.OnEnable();
    //    bl_AnimatorReloadEvent.OnTPReload += OnTPReload;
    //}

    ///// <summary>
    ///// 
    ///// </summary>
    //protected override void OnDisable()
    //{
    //    base.OnDisable();
    //    bl_AnimatorReloadEvent.OnTPReload -= OnTPReload;
    //}

    /// <summary>
    /// 
    /// </summary>
    void FetchHashes()
    {
        // cache the hashes in a Array will be more appropriate but to be more readable for other users
        // I decide to cached them in a Dictionary with the key name indicating the parameter that contain
        animatorHashes = new Dictionary<string, int>();
        animatorHashes.Add("BodyState", Animator.StringToHash("BodyState"));
        animatorHashes.Add("Vertical", Animator.StringToHash("Vertical"));
        animatorHashes.Add("Horizontal", Animator.StringToHash("Horizontal"));
        animatorHashes.Add("Speed", Animator.StringToHash("Speed"));
        animatorHashes.Add("SwitchWeapon", Animator.StringToHash("SwitchWeapon"));
        animatorHashes.Add("Fire", Animator.StringToHash("Fire"));
        animatorHashes.Add("UpperState", Animator.StringToHash("UpperState"));
        animatorHashes.Add("Move", Animator.StringToHash("Move"));
        animatorHashes.Add("WeaponType", Animator.StringToHash("WeaponType"));
        animatorHashes.Add("Reload", Animator.StringToHash("Reload"));
        animatorHashes.Add("FireLoop", Animator.StringToHash("FireLoop"));
        animatorHashes.Add("Deployed", Animator.StringToHash("Deployed"));
        animatorHashes.Add("RandomInt", Animator.StringToHash("RandomInt"));
    }

    /// <summary>
    /// 
    /// </summary>
    //void OnTPReload(bool enter, Animator theAnimator, AnimatorStateInfo stateInfo)
    //{
    //    if (theAnimator != Animator || CurrentNetworkGun == null || CurrentNetworkGun.LocalGun == null) return;

    //    float duration = CurrentNetworkGun.LocalGun != null ? CurrentNetworkGun.LocalGun.GetReloadTime() : CurrentNetworkGun.Info.ReloadTime;
    //    reloadSpeed = enter ? (stateInfo.length / duration) : 1;
    //    Animator.SetFloat("ReloadSpeed", reloadSpeed);
    //}

    /// <summary>
    /// 
    /// </summary>
    /// 

    public void Update()
    {
        deltaTime = Time.deltaTime;
        ControllerInfo();
        Animate();

        //UpperControll();
        //UpdateFootstep();
       // DropPlayerAngle();
    }
    private float timeFootStep;

    /// <summary>
    /// 
    /// </summary>
    /// p
    void ControllerInfo()
    {
        if (PlayerRoot != null)
            LocalVelocity = PlayerRoot.InverseTransformDirection(Velocity);

        float lerp = deltaTime * blendSmoothness;
        vertical = Mathf.Lerp(vertical, LocalVelocity.z, lerp);
        horizontal = Mathf.Lerp(horizontal, LocalVelocity.x, lerp);

        VelocityMagnitude = Velocity.magnitude;
        movementSpeed = Mathf.Lerp(movementSpeed, VelocityMagnitude, lerp);

        turnSpeed = Mathf.DeltaAngle(lastYRotation, PlayerRoot.localEulerAngles.y);
        TurnLerp = Mathf.Lerp(TurnLerp, turnSpeed, lerp);

        if (Time.frameCount % 7 == 0) lastYRotation = PlayerRoot.localEulerAngles.y;

        if(VelocityMagnitude > 0 && Time.time > timeFootStep)
        {
            remotePlayer.player.PlayFootStep();
            timeFootStep = Time.time + 0.4f;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void Animate()
    {
        if (Animator == null)
            return;

        //CheckPlayerStates();

        Animator.SetInteger(animatorHashes["BodyState"], (int)BodyState);
        Animator.SetFloat(animatorHashes["Vertical"], vertical);
        Animator.SetFloat(animatorHashes["Horizontal"], horizontal);
        Animator.SetInteger(animatorHashes["UpperState"], (int)FPState);
        //Animator.SetInteger(animatorHashes["WeaponType"], (int)remotePlayer.weaponType);
        //Animator.SetFloat(animatorHashes["Speed"], movementSpeed);
        //Animator.SetFloat(animatorHashes["Turn"], TurnLerp);
        //Animator.SetBool(animatorHashes["isGround"], IsGrounded);
    }

    public void SwitchWeapon(WeaponType weaponType)
    {
        Animator.SetInteger(animatorHashes["WeaponType"], (int)weaponType);
        Animator.SetTrigger(animatorHashes["SwitchWeapon"]);
        //StartCoroutine(SwitchWeaponDelay());   
    }

    //IEnumerator SwitchWeaponDelay()
    //{
    //    yield return new WaitForSeconds(0.1f);
    //    Animator.SetTrigger(animatorHashes["SwitchWeapon"]);
    //}

    /// <summary>
    /// 
    /// </summary>
    void CheckPlayerStates()
    {
        if (BodyState != lastBodyState)
        {
            if (lastBodyState == PlayerState.Sliding && BodyState != PlayerState.Sliding)
            {
                Animator.CrossFade(animatorHashes["Move"], 0.2f, 0);
            }
            //if (BodyState == PlayerState.Sliding)
            //{
            //    Animator.Play("Slide", 0, 0);
            //}
            //else if (OnEnterPlayerState(PlayerState.Dropping))
            //{
            //    Animator.Play("EmptyUpper", 1, 0);
            //}
            //else if (OnEnterPlayerState(PlayerState.Gliding))
            //{
            //    Animator.Play("EmptyUpper", 1, 0);
            //    Animator.CrossFade("gliding-1", 0.33f, 0);
            //}

            //if (OnExitPlayerState(PlayerState.Dropping))
            //{
            //    m_Transform.localRotation = Quaternion.identity;
            //}

            lastBodyState = BodyState;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public bool OnEnterPlayerState(PlayerState playerState)
    {
        if (BodyState == playerState && lastBodyState != playerState)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    public bool OnExitPlayerState(PlayerState playerState)
    {
        if (lastBodyState == playerState && BodyState != playerState)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    void UpperControll()
    {
        int _fpState = (int)FPState;
        //if (_fpState == 9) { _fpState = 1; }
        Animator.SetInteger(animatorHashes["UpperState"], _fpState);
    }


    public void FireAnimator()
    {
        //Animator.Play(remotePlayer.weapon.weaponConfig.weaponType.ToString(), 1, 0);
        Animator.SetInteger(animatorHashes["RandomInt"], Random.Range(0, 3));
        Animator.SetTrigger(animatorHashes["Fire"]);
        //Animator.SetBool("FireLoop", true);
    }

    public void FireLoopStartAnimator()
    {
        Animator.SetBool(animatorHashes["FireLoop"], true);
    }

    public void FireLoopFinishAnimator()
    {
        //Animator.Play(remotePlayer.weapon.weaponConfig.weaponType.ToString(), 1, 0);
        Animator.SetBool(animatorHashes["FireLoop"], false);
        //Animator.SetBool("FireLoop", true);
    }

    public void WeaponDeployedAnimator()
    {
        Animator.SetBool(animatorHashes["Deployed"], true);
    }

    public void WeaponDeployFinishAnimator()
    {
        Animator.SetBool(animatorHashes["Deployed"], false);
    }



    public void ReloadAnimator()
    {
        Animator.SetTrigger(animatorHashes["Reload"]);
    }

    public void PlayTaunt(Taunt taunt)
    {
        Animator.Play(taunt.ToString());
    }
    
    public void EndTaunt()
    {
        Animator.SetTrigger("EndTaunt");
    }


    /// <summary>
    /// 
    /// </summary>
    void DropPlayerAngle()
    {
        //if (BodyState != PlayerState.Dropping) return;

        //Vector3 pangle = m_Transform.localEulerAngles;
        //float tilt = dropTiltAngleCurve.Evaluate(Mathf.Clamp01(VelocityMagnitude / (runSpeed - 10)));
        //pangle.x = Mathf.Lerp(0, 70, tilt);
        //m_Transform.localRotation = Quaternion.Slerp(m_Transform.localRotation, Quaternion.Euler(pangle), deltaTime * 4);
    }

    /// <summary>
    /// 
    /// </summary>
    //public override void BlockWeapons(int blockState)
    //{
    //    bool baredHands = blockState == 1;

    //    // Do not control the arms with IK when the player is not using weapons.
    //    PlayerReferences.playerIK.ControlArmsWithIK = !baredHands;

    //    if (blockState != 2)
    //    {
    //        // -1 is the ID for play the bared arms animations in the player animator controller.
    //        int id = baredHands ? -1 : (int)cacheWeaponType;

    //        Animator.SetInteger(animatorHashes["GunType"], id);
    //    }
    //}

    ///// <summary>
    ///// 
    ///// </summary>
    //public override void OnGetHit()
    //{
    //    int r = Random.Range(0, 2);
    //    string hit = (r == 1) ? "Right Hit" : "Left Hit";
    //    Animator.Play(hit, 2, 0);
    //}

    ///// <summary>
    ///// 
    ///// </summary>
    //private void UpdateFootstep()
    //{
    //    if (!useFootSteps) return;
    //    if (VelocityMagnitude < 0.3f) return;
    //    bool isClimbing = (BodyState == PlayerState.Climbing);
    //    if ((!IsGrounded && !isClimbing) || BodyState == PlayerState.Sliding)
    //        return;

    //    footstep?.UpdateStep(movementSpeed);
    //}

    ///// <summary>
    ///// 
    ///// </summary>
    ///// <param name="typ"></param>
    //public override void PlayFireAnimation(GunType typ)
    //{
    //    // Check if the network weapon is using custom animations
    //    if (CurrentNetworkGun != null && CurrentNetworkGun.useCustomPlayerAnimations)
    //    {
    //        if (!string.IsNullOrEmpty(CurrentNetworkGun.customFireAnimationName))
    //        {
    //            Animator.Play(CurrentNetworkGun.customFireAnimationName, 1, 0);
    //            return;
    //        }
    //    }

    //    switch (typ)
    //    {
    //        case GunType.Knife:
    //            Animator.Play("FireKnife", 1, 0);
    //            break;
    //        case GunType.Machinegun:
    //            Animator.Play("RifleFire", 1, 0);
    //            break;
    //        case GunType.Burst:
    //            Animator.Play("BurstFire", 1, 0);
    //            break;
    //        case GunType.Pistol:
    //            Animator.Play("PistolFire", 1, 0);
    //            break;
    //        case GunType.Launcher:
    //            Animator.Play("LauncherFire", 1, 0);
    //            break;
    //    }
    //}

    ///// <summary>
    ///// 
    ///// </summary>
    //public void HitPlayer()
    //{
    //    if (Animator != null)
    //    {
    //        HitType = !HitType;
    //        int ht = (HitType) ? 1 : 0;
    //        Animator.SetInteger("HitType", ht);
    //        Animator.SetTrigger("Hit");
    //    }
    //}

    /// <summary>
    /// 
    /// </summary>
    public override void UpdateAnimatorParameters()
    {
        //ControllerInfo();
        Animate();
        //UpperControll();
    }

    /// <summary>
    /// 
    /// </summary>
    //public override void SetNetworkGun(GunType weaponType, bl_NetworkGun networkGun)
    //{
    //    cacheWeaponType = weaponType;
    //    CurrentNetworkGun = networkGun;

    //    Animator?.SetInteger(animatorHashes["GunType"], networkGun.GetUpperStateID());
    //    Animator.Play("Equip", 1, 0);

    //    if (CurrentNetworkGun == null || CurrentNetworkGun.LocalGun == null)
    //    {
    //        reloadSpeed = 1;
    //    }

    //    // Do not control the arms with IK while the 'Equip' animation is playing.
    //    PlayerReferences.playerIK.ControlArmsWithIK = false;

    //    // @TODO: Find the issue that cause Missing Reference exception when this conditional is not present.
    //    if (this != null)
    //    {
    //        CancelInvoke(nameof(ResetHandsIK));
    //        Invoke(nameof(ResetHandsIK), 0.3f);
    //    }
    //}
    ///// <summary>
    ///// 
    ///// </summary>
    //void ResetHandsIK() { PlayerReferences.playerIK.ControlArmsWithIK = true; }

    ///// <summary>
    ///// 
    ///// </summary>
    ///// <returns></returns>
    //public GunType GetCurretWeaponType() { return cacheWeaponType; }

    //private bl_PlayerReferences _playerReferences = null;
   // private bl_PlayerReferences PlayerReferences
    //{
    //    get
    //    {
    //        if (_playerReferences == null) _playerReferences = transform.GetComponentInParent<bl_PlayerReferences>();
    //        return _playerReferences;
    //    }
    //}
}


public enum PlayerState : byte
{
    Idle = 0,
    Walking = 1,
    Running = 2,
    //Deployed = 3,
    Jumping = 4,
    Climbing = 5,
    Sliding = 6,
    Dropping = 7,
    Gliding = 8,
    InVehicle = 9,
    TakeDamage,
    Crouching,
    Teleport
}

public enum PlayerFPState : byte
{
    Idle = 0,
    Firing = 1,
    Reloading = 2,
    Deployed = 3,
    Running = 4,
    FireAiming = 9,
}

public enum PlayerRunToAimBehave
{
    BlockAim = 0,
    StopRunning = 1,
    AimWhileRunning = 2,
}

public enum Team : byte
{
    red = 0,
    blue = 1,
    None
}