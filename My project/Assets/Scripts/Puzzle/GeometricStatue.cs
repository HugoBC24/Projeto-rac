using UnityEngine;

namespace ProjectRac.Puzzle
{
    public class GeometricStatue : MonoBehaviour
    {
        [Header("Statue Identity")]
        [SerializeField] private string statueLabel = "Estatua I";

        [Header("Visual References")]
        [SerializeField] private Transform shapeAnchor;
        [SerializeField] private MeshFilter shapeMeshFilter;
        [SerializeField] private MeshRenderer shapeMeshRenderer;
        [SerializeField] private StatueComponentSlot[] slots = new StatueComponentSlot[2];

        [Header("Materials")]
        [SerializeField] private Material defaultHoloMaterial;
        [SerializeField] private Material highlightHoloMaterial;
        [SerializeField] private Material solvedHoloMaterial;

        [Header("Animation")]
        [SerializeField] private float rotationSpeed = 28f;
        [SerializeField] private float bobAmplitude = 0.05f;
        [SerializeField] private float bobFrequency = 1.6f;

        private readonly ShapeComponent2D[] _components = new ShapeComponent2D[2];
        private GeometricPuzzleManager _manager;
        private Vector3 _anchorBaseLocalPos;
        private int _highlightedSlot = -1;
        private bool _isLocked;

        public GeometricShape3D CurrentShape => ShapeRules.ResolveShape(_components[0], _components[1]);
        public ShapeComponent2D GetComponentAt(int index) => _components[Mathf.Clamp(index, 0, 1)];

        private void Awake()
        {
            if (shapeAnchor != null) _anchorBaseLocalPos = shapeAnchor.localPosition;
        }

        private void Update()
        {
            if (shapeAnchor == null) return;

            shapeAnchor.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            float yOffset = Mathf.Sin(Time.time * bobFrequency + transform.position.x) * bobAmplitude;
            shapeAnchor.localPosition = _anchorBaseLocalPos + new Vector3(0f, yOffset, 0f);
        }

        public void ConfigureEditorReferences(
            string label,
            Transform anchor,
            MeshFilter mf,
            MeshRenderer mr,
            StatueComponentSlot slot0,
            StatueComponentSlot slot1,
            Material defaultMat,
            Material highlightMat,
            Material solvedMat)
        {
            statueLabel = label;
            shapeAnchor = anchor;
            shapeMeshFilter = mf;
            shapeMeshRenderer = mr;
            slots = new[] { slot0, slot1 };
            defaultHoloMaterial = defaultMat;
            highlightHoloMaterial = highlightMat;
            solvedHoloMaterial = solvedMat;
        }

        public void Initialize(GeometricPuzzleManager manager, ShapeComponent2D compA, ShapeComponent2D compB)
        {
            _manager = manager;
            _components[0] = compA;
            _components[1] = compB;
            _highlightedSlot = -1;
            _isLocked = false;
            RefreshVisuals();
        }

        public void SetComponentAt(int index, ShapeComponent2D comp)
        {
            _components[Mathf.Clamp(index, 0, 1)] = comp;
            RefreshVisuals();
        }

        public void SetHighlightedSlot(int slotIndex)
        {
            _highlightedSlot = slotIndex;
            RefreshVisuals();
        }

        public void SetLockedSolved()
        {
            _isLocked = true;
            _highlightedSlot = -1;
            RefreshVisuals();
        }

        public string GetSlotInteractionPrompt(int slotIndex)
        {
            if (_isLocked)
            {
                return $"{statueLabel}: {ShapeRules.GetShapeName(CurrentShape)} (Alinhada)";
            }

            var holder = FindHolder();
            string compName = ShapeRules.GetComponentName(GetComponentAt(slotIndex));
            string shapeName = ShapeRules.GetShapeName(CurrentShape);

            if (holder == null || !holder.HasComponent)
            {
                return $"[E] Retirar {compName} ({shapeName})";
            }

            if (holder.SourceStatue == this)
            {
                string heldName = ShapeRules.GetComponentName(holder.HeldComponent);
                return $"[E] Devolver {heldName}";
            }

            string held = ShapeRules.GetComponentName(holder.HeldComponent);
            return $"[E] Trocar {held} por {compName} ({shapeName})";
        }

        public void InteractSlot(int slotIndex)
        {
            if (_isLocked) return;

            var holder = FindHolder();
            if (holder == null) return;

            if (!holder.HasComponent)
            {
                holder.PickComponent(this, slotIndex, GetComponentAt(slotIndex));
                return;
            }

            if (holder.SourceStatue == this)
            {
                holder.ClearComponent();
                return;
            }

            // Perform Verity 1-for-1 dissection swap between SourceStatue and this statue
            GeometricStatue otherStatue = holder.SourceStatue;
            int otherSlot = holder.SourceSlotIndex;
            ShapeComponent2D incomingComp = holder.HeldComponent;
            ShapeComponent2D outgoingComp = GetComponentAt(slotIndex);

            SetComponentAt(slotIndex, incomingComp);
            otherStatue.SetComponentAt(otherSlot, outgoingComp);
            holder.ClearComponent();

            _manager?.OnStatuesSwapped();
        }

        public void RefreshVisuals()
        {
            GeometricShape3D shape = CurrentShape;

            if (shapeMeshFilter != null)
            {
                shapeMeshFilter.sharedMesh = StatueShapeMeshBuilder.GetShape3DMesh(shape);
            }

            if (shapeMeshRenderer != null)
            {
                Material shapeMat = _isLocked ? solvedHoloMaterial
                    : (_highlightedSlot >= 0 ? highlightHoloMaterial : defaultHoloMaterial);
                if (shapeMat != null) shapeMeshRenderer.sharedMaterial = shapeMat;
            }

            if (slots == null) return;
            for (int i = 0; i < slots.Length && i < 2; i++)
            {
                if (slots[i] == null) continue;
                Material slotMat = _isLocked ? solvedHoloMaterial
                    : (_highlightedSlot == i ? highlightHoloMaterial : defaultHoloMaterial);
                slots[i].SetVisual(_components[i], slotMat);
            }
        }

        private static PlayerPuzzleComponentHolder FindHolder()
        {
            var holder = FindAnyObjectByType<PlayerPuzzleComponentHolder>();
            if (holder != null) return holder;

            GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            return player != null ? player.AddComponent<PlayerPuzzleComponentHolder>() : null;
        }
    }
}
