using UnityEngine;
using UnityEngine.InputSystem;
using ProjectRac.Player;
using ProjectRac.Interaction;
using ProjectRac.UI;

namespace ProjectRac.Weapons
{
    public class ShotgunWeapon : MonoBehaviour, IWeapon
    {
        [Header("Weapon Identity")]
        [SerializeField] private string weaponName = "Escopeta";
        [SerializeField] private int slotIndex = 2;

        [Header("3D Model / Viewmodel Customization (Inspector)")]
        [Tooltip("Drag & drop any custom 3D model/prefab here to replace the shotgun viewmodel!")]
        [SerializeField] private GameObject customViewmodelPrefab;
        [SerializeField] private Vector3 viewmodelOffset = new Vector3(0.24f, -0.22f, 0.45f);
        [SerializeField] private Vector3 viewmodelRotation = new Vector3(0f, 0f, 0f);
        [SerializeField] private Vector3 viewmodelScale = new Vector3(1f, 1f, 1f);

        [Header("Shoot Point (Inspector)")]
        [Tooltip("The exact muzzle/spawn transform where shotgun pellets originate.")]
        [SerializeField] private Transform shootPoint;

        [Header("Ammo & Reload Settings (Inspector)")]
        [SerializeField] private int maxAmmo = 6;
        [SerializeField] private int currentAmmo = 6;
        [SerializeField] private int reserveAmmo = 24;
        [SerializeField] private float reloadDuration = 1.8f;
        [SerializeField] private AudioClip reloadSound;

        [Header("Shotgun Ballistics")]
        [SerializeField] private int pelletsCount = 8;
        [SerializeField] private float spreadAngle = 4.0f;
        [SerializeField] private float fireCooldown = 0.75f;
        [SerializeField] private float recoilPitch = 2.2f;

        [Header("Audio & Visual Feedback")]
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private AudioClip emptySound;

        [Header("Pool & UI References")]
        [SerializeField] private ShotgunPelletPool pelletPool;
        [SerializeField] private AmmoHUD ammoHUD;

        public string WeaponName => weaponName;
        public int SlotIndex => slotIndex;
        public bool IsEquipped { get; private set; }
        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => maxAmmo;
        public int ReserveAmmo => reserveAmmo;
        public int TotalAmmo => currentAmmo + reserveAmmo;
        public bool IsReloading => _isReloading;
        public Transform ShootPoint => shootPoint;

        private Camera _playerCamera;
        private FirstPersonCameraLook _cameraLook;
        private PlayerInputHandler _inputHandler;
        private GameObject _viewmodelInstance;
        private AudioSource _audioSource;
        private float _lastFireTime = float.NegativeInfinity;
        private bool _isReloading;
        private float _reloadTimer;

        private void Awake()
        {
            ResolveReferences();
            SetupViewmodel();
        }

        public void BindReferences(Camera cam, PlayerInputHandler input, ShotgunPelletPool pool = null, AmmoHUD hud = null)
        {
            _playerCamera = cam;
            _inputHandler = input;
            if (pool != null) pelletPool = pool;
            if (hud != null) ammoHUD = hud;
        }

        public void SetShootPoint(Transform sp)
        {
            shootPoint = sp;
        }

        public void BindAmmoHUD(AmmoHUD hud)
        {
            ammoHUD = hud;
            if (IsEquipped) RefreshHUD();
        }

        public void BindPelletPool(ShotgunPelletPool pool)
        {
            pelletPool = pool;
        }

        private void ResolveReferences()
        {
            if (_playerCamera == null)
            {
                _playerCamera = GetComponentInParent<Camera>();
                if (_playerCamera == null) _playerCamera = Camera.main;
            }

            if (_cameraLook == null && _playerCamera != null)
            {
                _cameraLook = _playerCamera.GetComponent<FirstPersonCameraLook>();
            }

            if (_inputHandler == null)
            {
                _inputHandler = GetComponentInParent<PlayerInputHandler>();
                if (_inputHandler == null) _inputHandler = FindAnyObjectByType<PlayerInputHandler>();
            }

            if (pelletPool == null) pelletPool = FindAnyObjectByType<ShotgunPelletPool>();
            if (ammoHUD == null) ammoHUD = FindAnyObjectByType<AmmoHUD>();

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 0f;
                _audioSource.playOnAwake = false;
            }
        }

        private void SetupViewmodel()
        {
            Transform parentTransform = _playerCamera != null ? _playerCamera.transform : transform;

            if (customViewmodelPrefab != null)
            {
                _viewmodelInstance = Instantiate(customViewmodelPrefab, parentTransform);
            }
            else
            {
                _viewmodelInstance = CreateDefaultShotgunViewmodel(parentTransform);
            }

            _viewmodelInstance.name = "Shotgun_Viewmodel";
            _viewmodelInstance.transform.localPosition = viewmodelOffset;
            _viewmodelInstance.transform.localRotation = Quaternion.Euler(viewmodelRotation);
            _viewmodelInstance.transform.localScale = viewmodelScale;
            _viewmodelInstance.SetActive(IsEquipped);
        }

        private void Update()
        {
            if (!IsEquipped) return;

            HandleReload();
            HandleShooting();
        }

        private void HandleReload()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[Key.R].wasPressedThisFrame && !_isReloading && currentAmmo < maxAmmo)
            {
                StartReload();
                return;
            }

            if (_isReloading)
            {
                _reloadTimer += Time.deltaTime;
                if (_reloadTimer >= reloadDuration)
                {
                    CompleteReload();
                }
            }
        }

        public void SetTotalAmmo(int totalAmmo)
        {
            int clamped = Mathf.Max(0, totalAmmo);
            currentAmmo = Mathf.Min(maxAmmo, clamped);
            reserveAmmo = Mathf.Max(0, clamped - currentAmmo);
            RefreshHUD();
        }

        public void StartReload()
        {
            if (_isReloading || currentAmmo >= maxAmmo || reserveAmmo <= 0) return;

            _isReloading = true;
            _reloadTimer = 0f;
            if (reloadSound != null && _audioSource != null) _audioSource.PlayOneShot(reloadSound, 0.8f);
            RefreshHUD();
        }

        private void CompleteReload()
        {
            _isReloading = false;
            int needed = maxAmmo - currentAmmo;
            int loaded = Mathf.Min(needed, reserveAmmo);
            currentAmmo += loaded;
            reserveAmmo -= loaded;
            RefreshHUD();
        }

        private void HandleShooting()
        {
            if (_inputHandler == null) return;

            if (_inputHandler.PrimaryActionTriggered && Time.time >= _lastFireTime + fireCooldown)
            {
                if (_isReloading) return;

                if (currentAmmo <= 0)
                {
                    if (emptySound != null && _audioSource != null)
                    {
                        _audioSource.PlayOneShot(emptySound, 0.6f);
                    }
                    StartReload();
                    return;
                }

                Fire();
            }
        }

        private void Fire()
        {
            if (_playerCamera == null) return;

            currentAmmo--;
            _lastFireTime = Time.time;
            RefreshHUD();

            Transform rootTransform = transform.root;

            // 1. Raycast to find target point where crosshair aims
            Ray aimRay = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 targetPoint = aimRay.GetPoint(80f);

            RaycastHit[] hits = Physics.RaycastAll(aimRay, 80f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform != rootTransform && !hits[i].transform.IsChildOf(rootTransform) && !hits[i].collider.CompareTag("Player"))
                {
                    targetPoint = hits[i].point;
                    break;
                }
            }

            // 2. Muzzle spawn origin
            Vector3 spawnOrigin = shootPoint != null
                ? shootPoint.position
                : _playerCamera.transform.position + _playerCamera.transform.right * 0.22f + _playerCamera.transform.up * -0.18f + _playerCamera.transform.forward * 0.55f;

            Vector3 baseDir = (targetPoint - spawnOrigin).normalized;
            if (baseDir == Vector3.zero) baseDir = _playerCamera.transform.forward;

            // 3. Spawn pellets in a camera-relative conical spread
            Transform camTransform = _playerCamera.transform;
            for (int i = 0; i < pelletsCount; i++)
            {
                float spreadX = Random.Range(-spreadAngle, spreadAngle);
                float spreadY = Random.Range(-spreadAngle, spreadAngle);
                Quaternion spreadRot = Quaternion.AngleAxis(spreadY, camTransform.up)
                                     * Quaternion.AngleAxis(spreadX, camTransform.right);
                Vector3 shootDir = spreadRot * baseDir;

                if (pelletPool != null)
                {
                    pelletPool.SpawnPellet(spawnOrigin, shootDir, rootTransform);
                }
            }

            if (_cameraLook != null && recoilPitch > 0f) _cameraLook.AddRecoil(recoilPitch);
            if (fireSound != null && _audioSource != null) _audioSource.PlayOneShot(fireSound, 0.85f);
        }

        public void Equip()
        {
            ResolveReferences();
            IsEquipped = true;
            enabled = true;
            if (_viewmodelInstance == null) SetupViewmodel();
            if (_viewmodelInstance != null) _viewmodelInstance.SetActive(true);
            RefreshHUD();
        }

        public void Unequip()
        {
            IsEquipped = false;
            enabled = false;
            if (_viewmodelInstance != null) _viewmodelInstance.SetActive(false);
        }

        private void RefreshHUD()
        {
            if (ammoHUD != null && IsEquipped)
            {
                ammoHUD.UpdateAmmo(weaponName, currentAmmo, reserveAmmo, _isReloading);
            }
        }

        public static GameObject CreateDefaultShotgunViewmodel(Transform parent)
        {
            return ShotgunModelGenerator.CreateDefaultShotgunViewmodel(parent);
        }
    }
}
