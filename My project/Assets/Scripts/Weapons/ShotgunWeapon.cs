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

        [Header("Ammo & Reload Settings (Inspector)")]
        [SerializeField] private int maxAmmo = 6;
        [SerializeField] private int currentAmmo = 6;
        [SerializeField] private float reloadDuration = 1.8f;
        [SerializeField] private AudioClip reloadSound;

        [Header("Shotgun Ballistics")]
        [SerializeField] private int pelletsCount = 8;
        [SerializeField] private float spreadAngle = 4.2f;
        [SerializeField] private float fireCooldown = 0.75f;

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
        public bool IsReloading => _isReloading;

        private Camera _playerCamera;
        private PlayerInputHandler _inputHandler;
        private GameObject _viewmodelInstance;
        private AudioSource _audioSource;
        private float _lastFireTime;
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

            if (_inputHandler == null)
            {
                _inputHandler = GetComponentInParent<PlayerInputHandler>();
                if (_inputHandler == null) _inputHandler = FindAnyObjectByType<PlayerInputHandler>();
            }

            if (pelletPool == null)
            {
                pelletPool = FindAnyObjectByType<ShotgunPelletPool>();
            }

            if (ammoHUD == null)
            {
                ammoHUD = FindAnyObjectByType<AmmoHUD>();
            }

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

        public void StartReload()
        {
            if (_isReloading || currentAmmo >= maxAmmo) return;

            _isReloading = true;
            _reloadTimer = 0f;

            if (reloadSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(reloadSound, 0.8f);
            }

            RefreshHUD();
        }

        private void CompleteReload()
        {
            _isReloading = false;
            currentAmmo = maxAmmo;
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
            Vector3 camPos = _playerCamera.transform.position;
            Vector3 forward = _playerCamera.transform.forward;
            Vector3 muzzlePos = camPos + _playerCamera.transform.right * 0.22f + _playerCamera.transform.up * -0.18f + forward * 0.45f;

            for (int i = 0; i < pelletsCount; i++)
            {
                float spreadX = Random.Range(-spreadAngle, spreadAngle);
                float spreadY = Random.Range(-spreadAngle, spreadAngle);
                Vector3 shootDir = Quaternion.Euler(spreadX, spreadY, 0f) * forward;

                if (pelletPool != null)
                {
                    pelletPool.SpawnPellet(muzzlePos, shootDir, rootTransform);
                }
            }

            if (fireSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(fireSound, 0.85f);
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
            if (_viewmodelInstance != null) _viewmodelInstance.SetActive(false);
        }

        private void RefreshHUD()
        {
            if (ammoHUD != null && IsEquipped)
            {
                ammoHUD.UpdateAmmo(weaponName, currentAmmo, maxAmmo, _isReloading);
            }
        }

        public static GameObject CreateDefaultShotgunViewmodel(Transform parent)
        {
            GameObject gun = new GameObject("DefaultShotgunModel");
            gun.transform.SetParent(parent, false);

            GameObject barrel1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel1.name = "Barrel_Left";
            barrel1.transform.SetParent(gun.transform, false);
            barrel1.transform.localPosition = new Vector3(-0.02f, 0f, 0.3f);
            barrel1.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel1.transform.localScale = new Vector3(0.035f, 0.25f, 0.035f);
            Destroy(barrel1.GetComponent<Collider>());

            GameObject barrel2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel2.name = "Barrel_Right";
            barrel2.transform.SetParent(gun.transform, false);
            barrel2.transform.localPosition = new Vector3(0.02f, 0f, 0.3f);
            barrel2.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel2.transform.localScale = new Vector3(0.035f, 0.25f, 0.035f);
            Destroy(barrel2.GetComponent<Collider>());

            GameObject stock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stock.name = "Stock";
            stock.transform.SetParent(gun.transform, false);
            stock.transform.localPosition = new Vector3(0f, -0.04f, 0.05f);
            stock.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            stock.transform.localScale = new Vector3(0.06f, 0.09f, 0.25f);
            Destroy(stock.GetComponent<Collider>());

            var grayBlock = new MaterialPropertyBlock();
            grayBlock.SetColor("_BaseColor", new Color(0.25f, 0.26f, 0.28f));
            barrel1.GetComponent<Renderer>()?.SetPropertyBlock(grayBlock);
            barrel2.GetComponent<Renderer>()?.SetPropertyBlock(grayBlock);

            var woodBlock = new MaterialPropertyBlock();
            woodBlock.SetColor("_BaseColor", new Color(0.48f, 0.28f, 0.16f));
            stock.GetComponent<Renderer>()?.SetPropertyBlock(woodBlock);

            return gun;
        }
    }
}
