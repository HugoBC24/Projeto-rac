using UnityEngine;
using ProjectRac.Player;
using ProjectRac.Interaction;

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

        [Header("Shotgun Ballistics")]
        [SerializeField] private int pelletsCount = 8;
        [SerializeField] private float spreadAngle = 4.2f;
        [SerializeField] private float maxRange = 35f;
        [SerializeField] private float pelletImpulse = 4.5f;
        [SerializeField] private float fireCooldown = 0.75f;
        [SerializeField] private float cameraRecoilPitch = 1.8f;

        [Header("Audio & Visual Feedback")]
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private Color muzzleFlashColor = new Color(1f, 0.8f, 0.3f, 1f);

        public string WeaponName => weaponName;
        public int SlotIndex => slotIndex;
        public bool IsEquipped { get; private set; }

        private Camera _playerCamera;
        private PlayerInputHandler _inputHandler;
        private FirstPersonCameraLook _cameraLook;
        private GameObject _viewmodelInstance;
        private float _lastFireTime;
        private AudioSource _audioSource;

        private void Awake()
        {
            ResolveReferences();
            SetupViewmodel();
        }

        public void BindReferences(Camera cam, PlayerInputHandler input, FirstPersonCameraLook look)
        {
            _playerCamera = cam;
            _inputHandler = input;
            _cameraLook = look;
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

            if (_cameraLook == null)
            {
                _cameraLook = GetComponentInParent<FirstPersonCameraLook>();
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
            if (!IsEquipped || _inputHandler == null) return;

            if (_inputHandler.PrimaryActionTriggered && Time.time >= _lastFireTime + fireCooldown)
            {
                Fire();
            }
        }

        private void Fire()
        {
            if (_playerCamera == null) return;

            _lastFireTime = Time.time;
            Transform rootTransform = transform.root;
            Vector3 camPos = _playerCamera.transform.position;
            Vector3 forward = _playerCamera.transform.forward;

            for (int i = 0; i < pelletsCount; i++)
            {
                // Conical random spread
                float spreadX = Random.Range(-spreadAngle, spreadAngle);
                float spreadY = Random.Range(-spreadAngle, spreadAngle);
                Vector3 shootDir = Quaternion.Euler(spreadX, spreadY, 0f) * forward;

                Ray ray = new Ray(camPos, shootDir);
                RaycastHit[] hits = Physics.RaycastAll(ray, maxRange, ~0, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                for (int h = 0; h < hits.Length; h++)
                {
                    Transform hitTrans = hits[h].transform;
                    if (hitTrans == rootTransform || hitTrans.IsChildOf(rootTransform) || hits[h].collider.CompareTag("Player"))
                    {
                        continue;
                    }

                    // Physical impact on Rigidbody
                    if (hits[h].rigidbody != null)
                    {
                        hits[h].rigidbody.AddForceAtPosition(shootDir * pelletImpulse, hits[h].point, ForceMode.Impulse);
                    }

                    // Interactable trigger
                    var interactable = hits[h].collider.GetComponentInParent<IInteractable>();
                    if (interactable != null)
                    {
                        interactable.Interact();
                    }

                    break;
                }
            }

            // Recoil kick & audio
            _cameraLook?.AddRecoil(cameraRecoilPitch);

            if (fireSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(fireSound, 0.85f);
            }
        }

        public void Equip()
        {
            IsEquipped = true;
            gameObject.SetActive(true);
            if (_viewmodelInstance != null) _viewmodelInstance.SetActive(true);
        }

        public void Unequip()
        {
            IsEquipped = false;
            if (_viewmodelInstance != null) _viewmodelInstance.SetActive(false);
            gameObject.SetActive(false);
        }

        public static GameObject CreateDefaultShotgunViewmodel(Transform parent)
        {
            GameObject gun = new GameObject("DefaultShotgunModel");
            gun.transform.SetParent(parent, false);

            // Double barrels
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

            // Wooden Stock
            GameObject stock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stock.name = "Stock";
            stock.transform.SetParent(gun.transform, false);
            stock.transform.localPosition = new Vector3(0f, -0.04f, 0.05f);
            stock.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            stock.transform.localScale = new Vector3(0.06f, 0.09f, 0.25f);
            Destroy(stock.GetComponent<Collider>());

            // Colors
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
