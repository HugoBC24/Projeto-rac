using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ProjectRac.Enemy;
using ProjectRac.Player;
using ProjectRac.UI;

namespace ProjectRac.Interaction
{
    // Triggers when the player closes the fireplace note: spawns the Boneca melee enemy behind the player
    // in Idle animation, turns the camera to face her, plays a Visual Novel dialogue, then starts the chase.
    public class FireplaceNoteAmbushTrigger : MonoBehaviour
    {
        [Header("Trigger & Enemy Setup")]
        [SerializeField] private InspectableNote targetNote;
        [SerializeField] private GameObject meleeEnemyPrefab;
        [SerializeField] private VisualNovelDialogueUI visualNovelUI;
        [SerializeField] private float spawnDistanceBehind = 3.2f;
        [SerializeField] private Vector3 fallbackSpawnPosition = new Vector3(14.5f, 0f, -6.3f);
        [SerializeField] private bool turnPlayerToFaceEnemy = true;
        [SerializeField] private float cameraFacePitch = 3f;

        [Header("Ambush Visual Novel Dialogues")]
        [SerializeField] private List<VisualNovelDialogueLine> ambushDialogues = new List<VisualNovelDialogueLine>();

        public bool HasTriggered => _hasTriggered;
        public EnemyMeleeAI SpawnedEnemy => _spawnedEnemy;
        public int DialogueCount => ambushDialogues != null ? ambushDialogues.Count : 0;

        private bool _hasTriggered;
        private EnemyMeleeAI _spawnedEnemy;

        private void Awake()
        {
            if (targetNote == null) targetNote = GetComponent<InspectableNote>();
        }

        private void OnEnable()
        {
            NoteInspectionUI.OnNoteClosed += HandleNoteClosed;
        }

        private void OnDisable()
        {
            NoteInspectionUI.OnNoteClosed -= HandleNoteClosed;
        }

        public void Configure(
            InspectableNote note,
            GameObject enemyPrefab,
            VisualNovelDialogueUI vnUI,
            List<VisualNovelDialogueLine> dialogues,
            Vector3 fallbackPos)
        {
            targetNote = note;
            meleeEnemyPrefab = enemyPrefab;
            visualNovelUI = vnUI;
            ambushDialogues = dialogues ?? new List<VisualNovelDialogueLine>();
            fallbackSpawnPosition = fallbackPos;
        }

        private void HandleNoteClosed(InspectableNote closedNote)
        {
            if (_hasTriggered) return;
            if (targetNote != null && closedNote != targetNote) return;

            TriggerAmbush();
        }

        public void TriggerAmbush()
        {
            if (_hasTriggered) return;
            _hasTriggered = true;

            GameObject playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (playerObj == null)
            {
                Debug.LogWarning("[FireplaceNoteAmbushTrigger] Player nao encontrado na cena.");
                return;
            }

            Transform playerTransform = playerObj.transform;
            Vector3 spawnPos = ResolveSpawnBehindPlayer(playerTransform);
            _spawnedEnemy = SpawnBonecaInIdle(spawnPos, playerTransform);

            if (turnPlayerToFaceEnemy && _spawnedEnemy != null)
            {
                TurnPlayerTowardEnemy(playerTransform, _spawnedEnemy.transform.position);
            }

            if (visualNovelUI == null)
            {
                visualNovelUI = FindAnyObjectByType<VisualNovelDialogueUI>(FindObjectsInactive.Include);
            }

            if (visualNovelUI != null && ambushDialogues != null && ambushDialogues.Count > 0)
            {
                visualNovelUI.StartDialogue(ambushDialogues, () => BeginChaseAfterDialogue(playerTransform));
            }
            else
            {
                BeginChaseAfterDialogue(playerTransform);
            }
        }

        private Vector3 ResolveSpawnBehindPlayer(Transform player)
        {
            Vector3 flatForward = player.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.right;
            flatForward.Normalize();

            // Prefer exact spot behind player; fall back to closer distances or open room center if blocked.
            float[] distances = { spawnDistanceBehind, spawnDistanceBehind * 0.75f, 2.0f };
            for (int i = 0; i < distances.Length; i++)
            {
                Vector3 candidate = player.position - flatForward * distances[i];
                candidate.y = player.position.y;
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                {
                    if (Vector3.Distance(hit.position, player.position) >= 1.5f)
                    {
                        return hit.position;
                    }
                }
            }

            if (NavMesh.SamplePosition(fallbackSpawnPosition, out NavMeshHit fallbackHit, 4.0f, NavMesh.AllAreas))
            {
                return fallbackHit.position;
            }

            return fallbackSpawnPosition;
        }

        private EnemyMeleeAI SpawnBonecaInIdle(Vector3 spawnPos, Transform player)
        {
            if (meleeEnemyPrefab == null)
            {
                Debug.LogWarning("[FireplaceNoteAmbushTrigger] meleeEnemyPrefab nao configurado.");
                return null;
            }

            Vector3 lookDir = player.position - spawnPos;
            lookDir.y = 0f;
            Quaternion rot = lookDir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(lookDir.normalized) : Quaternion.identity;

            GameObject instance = Instantiate(meleeEnemyPrefab, spawnPos, rot);
            instance.name = "Boneca_AmbushEnemy";

            var agent = instance.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh) agent.Warp(spawnPos);

            var ai = instance.GetComponent<EnemyMeleeAI>();
            if (ai != null) ai.EnterAmbushIdle(player);
            return ai;
        }

        private void TurnPlayerTowardEnemy(Transform player, Vector3 enemyPos)
        {
            Vector3 toEnemy = enemyPos - player.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > 0.001f)
            {
                player.rotation = Quaternion.LookRotation(toEnemy.normalized);
            }

            var camLook = player.GetComponentInChildren<FirstPersonCameraLook>(true);
            if (camLook != null)
            {
                camLook.SetPitch(cameraFacePitch);
            }
            else
            {
                var cam = player.GetComponentInChildren<Camera>(true);
                if (cam != null) cam.transform.localRotation = Quaternion.Euler(cameraFacePitch, 0f, 0f);
            }
        }

        private void BeginChaseAfterDialogue(Transform player)
        {
            if (_spawnedEnemy != null)
            {
                _spawnedEnemy.StartRelentlessChase(player);
            }
        }
    }
}
