using UnityEngine;
using ProjectRac.Inventory;
using ProjectRac.UI;

namespace ProjectRac.Puzzle
{
    public class GeometricPuzzleManager : MonoBehaviour
    {
        [Header("Statues (Left -> Center -> Right)")]
        [SerializeField] private GeometricStatue[] statues = new GeometricStatue[3];

        [Header("Target Solution (Fixed)")]
        [SerializeField] private GeometricShape3D[] targetSolution =
        {
            GeometricShape3D.Pyramid,  // Statue I (Left):   Triangle + Triangle
            GeometricShape3D.Cylinder, // Statue II (Center): Square + Circle
            GeometricShape3D.Prism     // Statue III (Right): Square + Triangle
        };

        [Header("Reward")]
        [SerializeField] private GameObject rewardSealCover;
        [SerializeField] private ClockPiecePickup rewardClockPiece;

        public bool IsSolved { get; private set; }

        private void Start()
        {
            if (rewardClockPiece != null)
            {
                rewardClockPiece.gameObject.SetActive(false);
            }

            if (rewardSealCover != null)
            {
                rewardSealCover.SetActive(true);
            }

            InitializeRandomizedSolvableState();
        }

        public void Configure(GeometricStatue[] statueList, GameObject sealCover, ClockPiecePickup clockPiece)
        {
            statues = statueList;
            rewardSealCover = sealCover;
            rewardClockPiece = clockPiece;
        }

        private void InitializeRandomizedSolvableState()
        {
            if (statues == null || statues.Length < 3) return;

            ShapeComponent2D[] pool = ShapeRules.CreateSolutionComponentPool();
            const int maxAttempts = 40;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Shuffle(pool);
                GeometricShape3D s0 = ShapeRules.ResolveShape(pool[0], pool[1]);
                GeometricShape3D s1 = ShapeRules.ResolveShape(pool[2], pool[3]);
                GeometricShape3D s2 = ShapeRules.ResolveShape(pool[4], pool[5]);

                if (s0 == targetSolution[0] && s1 == targetSolution[1] && s2 == targetSolution[2])
                {
                    continue;
                }

                statues[0].Initialize(this, pool[0], pool[1]);
                statues[1].Initialize(this, pool[2], pool[3]);
                statues[2].Initialize(this, pool[4], pool[5]);
                return;
            }

            // Deterministic non-solved fallback from the exact same invariant pool {T, T, T, Q, Q, C}
            // Cube (Q+Q), Cone (C+T), Pyramid (T+T)
            statues[0].Initialize(this, ShapeComponent2D.Square, ShapeComponent2D.Square);
            statues[1].Initialize(this, ShapeComponent2D.Circle, ShapeComponent2D.Triangle);
            statues[2].Initialize(this, ShapeComponent2D.Triangle, ShapeComponent2D.Triangle);
        }

        public void OnStatuesSwapped()
        {
            if (IsSolved || statues == null || statues.Length < 3) return;

            for (int i = 0; i < 3; i++)
            {
                if (statues[i] == null || statues[i].CurrentShape != targetSolution[i])
                {
                    return;
                }
            }

            SolvePuzzle();
        }

        private void SolvePuzzle()
        {
            IsSolved = true;

            for (int i = 0; i < statues.Length; i++)
            {
                if (statues[i] != null) statues[i].SetLockedSolved();
            }

            if (rewardSealCover != null)
            {
                rewardSealCover.SetActive(false);
            }

            if (rewardClockPiece != null)
            {
                rewardClockPiece.gameObject.SetActive(true);
            }

            var crosshair = FindAnyObjectByType<CrosshairHUD>();
            crosshair?.ShowTemporaryMessage("Enigma da Biblioteca resolvido! Um compartimento se abriu.", 4f);
            Debug.Log("[GeometricPuzzleManager] Puzzle resolvido! Peca de Relogio (1/3) liberada.");
        }

        private static void Shuffle(ShapeComponent2D[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
}
