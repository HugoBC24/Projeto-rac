using UnityEngine;
using ProjectRac.Interaction;

namespace ProjectRac.Puzzle
{
    [RequireComponent(typeof(Collider))]
    public class StatueComponentSlot : MonoBehaviour, IInteractable
    {
        [SerializeField] private GeometricStatue ownerStatue;
        [SerializeField] private int slotIndex;
        [SerializeField] private MeshFilter symbolMeshFilter;
        [SerializeField] private MeshRenderer symbolMeshRenderer;

        public int SlotIndex => slotIndex;

        public void Configure(GeometricStatue owner, int index, MeshFilter mf, MeshRenderer mr)
        {
            ownerStatue = owner;
            slotIndex = index;
            symbolMeshFilter = mf;
            symbolMeshRenderer = mr;
        }

        public void SetVisual(ShapeComponent2D component, Material mat)
        {
            if (symbolMeshFilter != null)
            {
                symbolMeshFilter.sharedMesh = StatueShapeMeshBuilder.GetSymbol2DMesh(component);
            }

            if (symbolMeshRenderer != null && mat != null)
            {
                symbolMeshRenderer.sharedMaterial = mat;
            }
        }

        public string GetInteractionPrompt()
        {
            if (ownerStatue == null) return "";
            return ownerStatue.GetSlotInteractionPrompt(slotIndex);
        }

        public void Interact()
        {
            if (ownerStatue != null)
            {
                ownerStatue.InteractSlot(slotIndex);
            }
        }
    }
}
