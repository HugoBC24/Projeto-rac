using UnityEngine;

namespace ProjectRac.Puzzle
{
    public class PlayerPuzzleComponentHolder : MonoBehaviour
    {
        public bool HasComponent { get; private set; }
        public ShapeComponent2D HeldComponent { get; private set; }
        public GeometricStatue SourceStatue { get; private set; }
        public int SourceSlotIndex { get; private set; } = -1;

        public void PickComponent(GeometricStatue statue, int slotIndex, ShapeComponent2D component)
        {
            if (SourceStatue != null)
            {
                SourceStatue.SetHighlightedSlot(-1);
            }

            SourceStatue = statue;
            SourceSlotIndex = slotIndex;
            HeldComponent = component;
            HasComponent = true;

            if (SourceStatue != null)
            {
                SourceStatue.SetHighlightedSlot(slotIndex);
            }
        }

        public void ClearComponent()
        {
            if (SourceStatue != null)
            {
                SourceStatue.SetHighlightedSlot(-1);
            }

            HasComponent = false;
            SourceStatue = null;
            SourceSlotIndex = -1;
        }
    }
}
