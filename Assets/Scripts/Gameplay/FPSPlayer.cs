using System.Collections;
using UnityEngine;

public sealed class FPSPlayer : MonoBehaviour
{
    public bool IsAlive { get; private set; } = true;
    public int Health { get; private set; } = 100;
    public float HorizontalSpeed { get; private set; }

    private CharacterController controller;
    private Camera playerCamera;
    private WeaponSystem weaponSystem;
    private float yaw;
    private float pitch;
    private float verticalVelocity;
    private float kick;
    private float damageFlash;
    private Vector3 cameraBaseLocal;
    private float bobTimer;
    private float stepTimer;
    private AudioSource footAudio;
    private bool initialized;

    public Camera ViewCamera => playerCamera;
    public WeaponSystem Weapons => weaponSystem;

    public void Initialize()
    {
        if (initialized) return;
        initialized = true;
        Application.targetFrameRate = 60;

        controller = gameObject.AddComponent<CharacterController>();
        controller.height = 1.82f;
        controller.radius = 0.32f;
        controller.center = new Vector3(0, 0.91f, 0);
        controller.stepOffset = 0.36f;
        controller.slopeLimit = 48f;
        controller.skinWidth = 0.035f;
        controller.minMoveDistance = 0.001f;

        GameObject cameraObj = new GameObject("PLAYER_CAMERA");
        cameraObj.transform.SetParent(transform, false);
        cameraObj.transform.localPosition = new Vector3(0, 1.56f, 0);
        playerCamera = cameraObj.AddComponent<Camera>();
        playerCamera.fieldOfView = 70f;
        playerCamera.nearClipPlane = 0.04f;
        playerCamera.farClipPlane = 180f;
        playerCamera.allowHDR = false;
        playerCamera.allowMSAA = false;
        playerCamera.depthTextureMode = DepthTextureMode.None;
        playerCamera.backgroundColor = new Color(0.028f,0.039f,0.052f);
        cameraBaseLocal = cameraObj.transform.localPosition;

        weaponSystem = gameObject.AddComponent<WeaponSystem>();
        weaponSystem.Initialize(this, playerCamera, SITFGame.Instance.Materials);
        footAudio = gameObject.AddComponent<AudioSource>();
        footAudio.playOnAwake = false;
        footAudio.spatialBlend = 0.15f;

        yaw = transform.eulerAngles.y;
        pitch = 0f;
    }

    private void Update()
    {
        if (!initialized || !IsAlive) return;
        ReadLook();
        ReadCombat();
        UpdateCameraEffects();
    }

    private void FixedUpdate()
    {
        if (!initialized || !IsAlive) return;
        Move();
    }

    private void ReadLook()
    {
        Vector2 look = Vector2.zero;
        if (MobileControls.Active != null)
            look = MobileControls.Active.ConsumeLookDelta();

        if (look.sqrMagnitude < 0.000001f)
        {
            look.x = Input.GetAxis("Mouse X") * 2.6f;
            look.y = Input.GetAxis("Mouse Y") * 2.6f;
        }

        float sensitivity = PlayerPrefs.GetFloat("SITF_SENS", 1.0f);
        yaw += look.x * 0.08f * sensitivity;
        pitch -= look.y * 0.06f * sensitivity;
        pitch = Mathf.Clamp(pitch, -83f, 83f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void ReadCombat()
    {
        bool mobileFire = MobileControls.Active != null && MobileControls.Active.FireHeld;
        bool mobileAim = MobileControls.Active != null && MobileControls.Active.AimHeld;
        weaponSystem.SetFire(mobileFire || Input.GetMouseButton(0));
        weaponSystem.SetAim(mobileAim || Input.GetMouseButton(1));

        if (Input.GetKeyDown(KeyCode.R)) weaponSystem.Reload();
        if (Input.GetKeyDown(KeyCode.Alpha1)) weaponSystem.SwitchWeapon(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) weaponSystem.SwitchWeapon(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) weaponSystem.SwitchWeapon(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) weaponSystem.SwitchWeapon(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) weaponSystem.SwitchWeapon(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) weaponSystem.SwitchWeapon(5);
    }

    private void Move()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (MobileControls.Active != null && MobileControls.Active.MoveInput.sqrMagnitude > 0.001f)
            input = MobileControls.Active.MoveInput;

        input = Vector2.ClampMagnitude(input, 1f);
        Vector3 motion = transform.forward * input.y + transform.right * input.x;
        bool sprint = input.y > 0.72f && !weaponSystem.IsAiming;
        float targetSpeed = sprint ? 6.3f : 4.5f;
        HorizontalSpeed = Mathf.Lerp(HorizontalSpeed, motion.magnitude * targetSpeed, Time.fixedDeltaTime * 9f);

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -1.5f;
        verticalVelocity += Physics.gravity.y * Time.fixedDeltaTime;

        Vector3 delta = motion * targetSpeed;
        delta.y = verticalVelocity;
        controller.Move(delta * Time.fixedDeltaTime);
    }

    private void UpdateCameraEffects()
    {
        float speed01 = Mathf.Clamp01(HorizontalSpeed / 6.3f);
        bobTimer += Time.deltaTime * Mathf.Lerp(3f, 10f, speed01);

        if (HorizontalSpeed > 2.0f && controller.isGrounded && !weaponSystem.IsReloading)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                stepTimer = HorizontalSpeed > 5.3f ? 0.32f : 0.44f;
                footAudio.PlayOneShot(ProceduralAudio.Footstep(), 0.18f);
            }
        }
        else stepTimer = Mathf.Min(stepTimer, 0.12f);

        float bob = Mathf.Sin(bobTimer) * 0.008f * speed01;
        float side = Mathf.Cos(bobTimer * 0.5f) * 0.005f * speed01;
        Vector3 target = cameraBaseLocal + new Vector3(side, bob, 0f);

        if (damageFlash > 0f)
        {
            damageFlash -= Time.deltaTime;
            target += Random.insideUnitSphere * 0.018f;
        }

        playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, target, Time.deltaTime * 14f);

        float kickTarget = kick;
        kick = Mathf.Lerp(kick, 0f, Time.deltaTime * 9f);
        Quaternion desiredRotation = Quaternion.Euler(pitch - kickTarget * 12f, 0f, kickTarget * 3f);
        playerCamera.transform.localRotation = Quaternion.Slerp(playerCamera.transform.localRotation, desiredRotation, Time.deltaTime * 16f);
    }

    public void AddWeaponKick(float value) => kick = Mathf.Clamp(kick + value, 0f, 0.35f);

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        Health = Mathf.Max(0, Health - Mathf.Max(0, amount));
        damageFlash = 0.16f;

        if (Health <= 0)
        {
            IsAlive = false;
            weaponSystem.SetFire(false);
            if (MobileControls.Active != null) MobileControls.Active.ClearCombat();
            SITFGame.Instance.PlayerDied();
        }
    }
}
