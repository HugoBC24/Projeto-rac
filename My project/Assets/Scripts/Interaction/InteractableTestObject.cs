using UnityEngine;

namespace ProjectRac.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class InteractableTestObject : MonoBehaviour, IInteractable
    {
        [Header("Interaction Settings")]
        [SerializeField] private string promptText = "Pressione [E] para interagir";
        [SerializeField] private Color[] colorPalette = new Color[]
        {
            Color.cyan,
            Color.magenta,
            Color.green,
            Color.yellow,
            Color.red
        };

        private Renderer _renderer;
        private int _currentColorIndex = 0;
        private MaterialPropertyBlock _propBlock;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        public string GetInteractionPrompt()
        {
            return promptText;
        }

        public void Interact()
        {
            if (_renderer != null && colorPalette != null && colorPalette.Length > 0)
            {
                _currentColorIndex = (_currentColorIndex + 1) % colorPalette.Length;
                Color newColor = colorPalette[_currentColorIndex];

                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", newColor);
                _renderer.SetPropertyBlock(_propBlock);

                Debug.Log($"[Interactable] {gameObject.name} interagido! Cor alterada para índice {_currentColorIndex}.");
            }
        }
    }
}
