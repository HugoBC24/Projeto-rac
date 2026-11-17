using UnityEngine;
using ProjectRac.Player;
using ProjectRac.UI;

namespace ProjectRac.Weapons
{
    public class SlingshotWeapon : MonoBehaviour, IWeapon
    {
        [Header("Weapon Identity")]
        [SerializeField] private string weaponName = "Estilingue";
        [SerializeField] private int slotIndex = 1;

        [Header("3D Model / Viewmodel Customization (Inspector)")]
        [Tooltip("Drag & drop any custom 3D model/prefab here to replace the slingshot viewmodel!")]
        [SerializeField] private GameObject customViewmodelPrefab;
        [SerializeField] private Vector3 viewmodelOffset = new Vector3(0.2f, -0.2f, 0.45f);
        [SerializeField] private Vector3 viewmodelRotation = new Vector3(0f, 0f, 0f);
        [SerializeField] private Vector3 viewmodelScale = new Vector3(1f, 1f, 1f);

        [Header("References")]
        [SerializeField] private SlingshotProjectilePool projectilePool;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private AmmoHUD ammoHUD;
        [SerializeField] private SlingshotChargeHUD chargeHUD;

        [Header("Launch Force & Charge Settings")]
        [SerializeField] private float minLaunchForce = 22f;
        [SerializeField] private float maxLaunchForce = 40f;
        [SerializeField] private float maxChargeTime = 0.8f;
        [SerializeField] private float fireCooldown = 0.25f;

        [Header("Audio")]
        [SerializeField] private AudioClip shootSound;

        public string WeaponName => weaponName;
        public int SlotIndex => slotIndex;
        public bool IsEquipped { get; private set; } = true;
        public float ChargeNormalized { get; private set; }
        public bool IsCharging { get; private set; }

        private GameObject _viewmodelInstance;
        private float _chargeTimer;
        private float _lastFireTime;
        private AudioSource _audioSource;

        private void Awake()
        {
            ResolveReferences();
            SetupViewmodel();
        }

        private void SetupViewmodel()
        {
            Transform parentTransform = playerCamera != null ? playerCamera.transform : transform;
            if (customViewmodelPrefab != null)
            {
                _viewmodelInstance = Instantiate(customViewmodelPrefab, parentTransform);
                _viewmodelInstance.name = "Slingshot_Viewmodel";
                _viewmodelInstance.transform.localPosition = viewmodelOffset;
                _viewmodelInstance.transform.localRotation = Quaternion.Euler(viewmodelRotation);
                _viewmodelInstance.transform.localScale = viewmodelScale;
                _viewmodelInstance.SetActive(IsEquipped);
            }
        }

        public void Equip()
        {
            IsEquipped = true;
            enabled = true;
            if (_viewmodelInstance != null) _viewmodelInstance.SetActive(true);
            RefreshHUD();
        }

        public void Unequip()
        {
            IsEquipped = false;
            enabled = false;
            IsCharging = false;
            _chargeTimer = 0f;
            ChargeNormalized = 0f;
            chargeHUD?.UpdateCharge(false, 0f);
            if (_viewmodelInstance != null) _viewmodelInstance.SetActive(false);
        }

        public void BindPool(SlingshotProjectilePool pool)
        {
            projectilePool = pool;
        }

        public void BindAmmoHUD(AmmoHUD hud)
        {
            ammoHUD = hud;
            if (IsEquipped) RefreshHUD();
        }

        public void BindChargeHUD(SlingshotChargeHUD hud)
        {
            chargeHUD = hud;
        }

        public void SetCustomViewmodelPrefab(GameObject prefab)
        {
            customViewmodelPrefab = prefab;
        }

        private void ResolveReferences()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInParent<Camera>();
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

            if (ammoHUD == null)
            {
                ammoHUD = FindAnyObjectByType<AmmoHUD>();
            }

            if (chargeHUD == null)
            {
                chargeHUD = FindAnyObjectByType<SlingshotChargeHUD>();
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 0f;
                _audioSource.playOnAwake = false;
            }
        }

        private void Update()
        {
            if (!IsEquipped || inputHandler == null || projectilePool == null) return;

            HandleChargeAndShoot();
        }

        private void HandleChargeAndShoot()
        {
            if (inputHandler.PrimaryActionHeld && Time.time >= _lastFireTime + fireCooldown)
            {
                IsCharging = true;
                _chargeTimer += Time.deltaTime;
                ChargeNormalized = Mathf.Clamp01(_chargeTimer / maxChargeTime);
                chargeHUD?.UpdateCharge(true, ChargeNormalized);
            }

            if (inputHandler.PrimaryActionReleased && IsCharging)
            {
                Fire();
                IsCharging = false;
                _chargeTimer = 0f;
                ChargeNormalized = 0f;
                chargeHUD?.UpdateCharge(false, 0f);
            }
        }

        private void Fire()
        {
            if (playerCamera == null || projectilePool == null) return;

            Transform rootTransform = transform.root;
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

            Vector3 spawnOrigin = muzzlePoint != null
                ? muzzlePoint.position
                : playerCamera.transform.position + playerCamera.transform.right * 0.2f + playerCamera.transform.up * -0.15f + playerCamera.transform.forward * 0.4f;

            Vector3 shootDirection = (targetPoint - spawnOrigin).normalized;
            if (shootDirection == Vector3.zero)
            {
                shootDirection = playerCamera.transform.forward;
            }

            float launchSpeed = Mathf.Lerp(minLaunchForce, maxLaunchForce, ChargeNormalized);
            projectilePool.SpawnProjectile(spawnOrigin, shootDirection * launchSpeed, rootTransform);
            _lastFireTime = Time.time;

            if (shootSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(shootSound, 0.7f);
            }
        }

        private void RefreshHUD()
        {
            if (ammoHUD != null && IsEquipped)
            {
                ammoHUD.UpdateAmmo(weaponName, 1, -1, false);
            }
        }
    }
}
