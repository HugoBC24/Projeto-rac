using UnityEngine;
using ProjectRac.Interaction;
using ProjectRac.UI;

namespace ProjectRac.Inventory
{
    [RequireComponent(typeof(Collider))]
    public class ClockPiecePickup : MonoBehaviour, IInteractable
    {
        [Header("Clock Piece Settings")]
        [SerializeField] private string pieceId = "clock_piece_library";
        [SerializeField] private string promptText = "[E] Pegar Peca de Relogio";
        [SerializeField] private float spinSpeed = 48f;
        [SerializeField] private float bobAmplitude = 0.04f;
        [SerializeField] private float bobFrequency = 2.0f;
        [SerializeField] private Transform visualRoot;

        private bool _collected;
        private Vector3 _visualBaseLocalPos;

        private void Awake()
        {
            if (visualRoot == null) visualRoot = transform;
            _visualBaseLocalPos = visualRoot.localPosition;
        }

        private void Update()
        {
            if (_collected || visualRoot == null) return;

            visualRoot.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            if (visualRoot != transform)
            {
                float offsetY = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
                visualRoot.localPosition = _visualBaseLocalPos + new Vector3(0f, offsetY, 0f);
            }
        }

        public void Configure(string id, Transform visual)
        {
            pieceId = id;
            visualRoot = visual;
            if (visualRoot != null) _visualBaseLocalPos = visualRoot.localPosition;
        }

        public string GetInteractionPrompt()
        {
            return _collected ? "" : promptText;
        }

        public void Interact()
        {
            if (_collected) return;

            var inventory = FindAnyObjectByType<PlayerKeyItemInventory>();
            if (inventory == null)
            {
                GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                if (player != null) inventory = player.AddComponent<PlayerKeyItemInventory>();
            }

            if (inventory == null) return;

            inventory.AddClockPiece(pieceId);
            _collected = true;

            var crosshair = FindAnyObjectByType<CrosshairHUD>();
            crosshair?.ShowTemporaryMessage(
                $"Peca de Relogio obtida ({inventory.ClockPieceCount}/{inventory.TotalClockPieces})",
                3.5f);

            gameObject.SetActive(false);
        }
    }
}
