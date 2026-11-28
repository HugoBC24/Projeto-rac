using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectRac.Inventory
{
    public class PlayerKeyItemInventory : MonoBehaviour
    {
        public const int TotalClockPiecesRequired = 3;

        [Header("Clock Pieces")]
        [SerializeField] private int clockPieceCount = 0;

        [Header("Collected Key Items")]
        [SerializeField] private List<string> collectedItemIds = new List<string>();

        public int ClockPieceCount => clockPieceCount;
        public int TotalClockPieces => TotalClockPiecesRequired;
        public bool HasAllClockPieces => clockPieceCount >= TotalClockPiecesRequired;
        public IReadOnlyList<string> CollectedItemIds => collectedItemIds;

        public event Action<int, int> OnClockPiecesChanged;

        public bool HasItem(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && collectedItemIds.Contains(itemId);
        }

        public bool AddClockPiece(string pieceId)
        {
            if (!string.IsNullOrEmpty(pieceId) && collectedItemIds.Contains(pieceId))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(pieceId))
            {
                collectedItemIds.Add(pieceId);
            }

            clockPieceCount = Mathf.Clamp(clockPieceCount + 1, 0, TotalClockPiecesRequired);
            OnClockPiecesChanged?.Invoke(clockPieceCount, TotalClockPiecesRequired);
            Debug.Log($"[PlayerKeyItemInventory] Peca de Relogio coletada ({clockPieceCount}/{TotalClockPiecesRequired}) [id={pieceId}]");
            return true;
        }
    }
}
