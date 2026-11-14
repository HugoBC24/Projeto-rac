using UnityEngine;
using ProjectRac.Player;

namespace ProjectRac.Weapons
{
    public class SlingshotWeapon : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SlingshotProjectilePool projectilePool;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private Transform muzzlePoint;

        [Header("Launch Force & Charge")]
        [SerializeField] private float minLaunchForce = 15f;
        [SerializeField] private float maxLaunchForce = 35f;
        [SerializeField] private float maxChargeTime = 1.0f;
        [SerializeField] private float fireCooldown = 0.25f;

        [Header("Spawn Offset (if muzzlePoint is null)")]
        [SerializeField] private Vector3 cameraRelativeOffset = new Vector3(0.2f, -0.15f, 0.4f);

        public float ChargeNormalized { get; private set; }
        public bool IsCharging { get; private set; }

        private float _chargeTimer;
        private float _lastFireTime;
        private Texture2D _chargeTex;

        private void Awake()
        {
            ResolveReferences();
            _chargeTex = new Texture2D(1, 1);
            _chargeTex.SetPixel(0, 0, Color.white);
            _chargeTex.Apply();
        }

        private void OnDestroy()
        {
            if (_chargeTex != null)
            {
                Destroy(_chargeTex);
            }
        }

        public void BindPool(SlingshotProjectilePool pool)
        {
            projectilePool = pool;
        }

        private void ResolveReferences()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null) playerCamera = Camera.main;
            }

            if (inputHandler == null)
            {
                inputHandler = GetComponentInParent<PlayerInputHandler>();
                if (inputHandler == null) inputHandler = FindAnyObjectByType<PlayerInputHandler>();
            }

            if (projectilePool == null)
            {
                projectilePool = FindAnyObjectByType<SlingshotProjectilePool>();
            }
        }

        private void Update()
        {
            if (inputHandler == null || projectilePool == null) return;

            HandleChargeAndShoot();
        }

        private void HandleChargeAndShoot()
        {
            if (inputHandler.PrimaryActionHeld && Time.time >= _lastFireTime + fireCooldown)
            {
                IsCharging = true;
                _chargeTimer += Time.deltaTime;
                ChargeNormalized = Mathf.Clamp01(_chargeTimer / maxChargeTime);
            }

            if (inputHandler.PrimaryActionReleased && IsCharging)
            {
                Fire();
                IsCharging = false;
                _chargeTimer = 0f;
                ChargeNormalized = 0f;
            }
        }

        private void Fire()
        {
            if (playerCamera == null || projectilePool == null) return;

            // Target where the crosshair is aiming
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, 120f, ~0, QueryTriggerInteraction.Ignore)
                ? hit.point
                : ray.GetPoint(120f);

            Vector3 spawnOrigin = muzzlePoint != null
                ? muzzlePoint.position
                : playerCamera.transform.position
                  + playerCamera.transform.right * cameraRelativeOffset.x
                  + playerCamera.transform.up * cameraRelativeOffset.y
                  + playerCamera.transform.forward * cameraRelativeOffset.z;

            Vector3 shootDirection = (targetPoint - spawnOrigin).normalized;
            float launchSpeed = Mathf.Lerp(minLaunchForce, maxLaunchForce, ChargeNormalized);

            projectilePool.SpawnProjectile(spawnOrigin, shootDirection * launchSpeed);
            _lastFireTime = Time.time;
        }

        private void OnGUI()
        {
            if (!IsCharging || _chargeTex == null || Cursor.lockState != CursorLockMode.Locked) return;

            // Draw elastic tension charge indicator beneath crosshair
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;
            float bgWidth = 50f;
            float barHeight = 4f;

            // Background
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
            GUI.DrawTexture(new Rect(centerX - bgWidth / 2f, centerY + 16f, bgWidth, barHeight), _chargeTex);

            // Fill color transitions from yellow to bright orange/red as tension peaks
            Color tensionColor = Color.Lerp(Color.yellow, new Color(1f, 0.35f, 0.1f), ChargeNormalized);
            GUI.color = tensionColor;
            GUI.DrawTexture(new Rect(centerX - bgWidth / 2f, centerY + 16f, bgWidth * ChargeNormalized, barHeight), _chargeTex);
            GUI.color = Color.white;
        }
    }
}
