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

            Transform rootTransform = transform.root;

            // Target where the crosshair is aiming, ignoring player/shooter colliders
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, 120f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            Vector3 targetPoint = ray.GetPoint(120f);
            for (int i = 0; i < hits.Length; i++)
            {
                var hitTransform = hits[i].transform;
                if (hitTransform == rootTransform || hitTransform.IsChildOf(rootTransform) || hits[i].collider.CompareTag("Player"))
                {
                    continue;
                }

                targetPoint = hits[i].point;
                break;
            }

            Vector3 spawnOrigin;
            if (muzzlePoint != null)
            {
                spawnOrigin = muzzlePoint.position;
            }
            else
            {
                Vector3 forward = playerCamera.transform.forward;
                Vector3 right = playerCamera.transform.right;
                Vector3 up = playerCamera.transform.up;

                Vector3 desired = playerCamera.transform.position
                                  + right * cameraRelativeOffset.x
                                  + up * cameraRelativeOffset.y
                                  + forward * cameraRelativeOffset.z;

                // If aiming close to world geometry, ensure spawn point doesn't penetrate walls/floors
                Vector3 camToDesired = desired - playerCamera.transform.position;
                float dist = camToDesired.magnitude;
                if (dist > 0.001f && Physics.Raycast(playerCamera.transform.position, camToDesired.normalized, out RaycastHit obstHit, dist, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (obstHit.transform != rootTransform && !obstHit.transform.IsChildOf(rootTransform) && !obstHit.collider.CompareTag("Player"))
                    {
                        desired = obstHit.point - camToDesired.normalized * 0.05f;
                    }
                }

                spawnOrigin = desired;
            }

            Vector3 shootDirection = (targetPoint - spawnOrigin).normalized;
            if (shootDirection == Vector3.zero)
            {
                shootDirection = playerCamera.transform.forward;
            }

            float launchSpeed = Mathf.Lerp(minLaunchForce, maxLaunchForce, ChargeNormalized);

            projectilePool.SpawnProjectile(spawnOrigin, shootDirection * launchSpeed, rootTransform);
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
