using System.Collections.Generic;
using UnityEngine;
using ProjectRac.Enemy;
using ProjectRac.Player;
using ProjectRac.UI;

namespace ProjectRac.Interaction
{
    // One-shot trigger covering Sala de Jantar (triggered when entering via either door):
    // smoothly turns camera to the Mordomo beside the dining table, plays Visual Novel dialogue, then starts chase.
    [RequireComponent(typeof(Collider))]
    public class DiningRoomEncounterTrigger : MonoBehaviour
    {
        [Header("Encounter References")]
        [SerializeField] private EnemyRangedAI mordomoEnemy;
        [SerializeField] private VisualNovelDialogueUI visualNovelUI;
        [SerializeField] private float cameraTurnDuration = 0.65f;
        [SerializeField] private float cameraFacePitch = 2f;

        [Header("Encounter Visual Novel Dialogues")]
        [SerializeField] private List<VisualNovelDialogueLine> encounterDialogues = new List<VisualNovelDialogueLine>();

        public bool HasTriggered => _hasTriggered;
        public EnemyRangedAI MordomoEnemy => mordomoEnemy;
        public int DialogueCount => encounterDialogues != null ? encounterDialogues.Count : 0;

        private bool _hasTriggered;

        public void Configure(
            EnemyRangedAI enemy,
            VisualNovelDialogueUI vnUI,
            List<VisualNovelDialogueLine> dialogues)
        {
            mordomoEnemy = enemy;
            visualNovelUI = vnUI;
            encounterDialogues = dialogues ?? new List<VisualNovelDialogueLine>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasTriggered) return;
            if (!IsPlayerCollider(other)) return;

            TriggerEncounter(other.transform.root);
        }

        public void TriggerEncounter(Transform playerTransform = null)
        {
            if (_hasTriggered) return;
            _hasTriggered = true;

            if (playerTransform == null)
            {
                var p = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (playerTransform == null) return;

            if (mordomoEnemy != null)
            {
                mordomoEnemy.EnterEncounterIdle(playerTransform);
                Vector3 focusPoint = mordomoEnemy.transform.position + Vector3.up * 1.45f;
                SmoothCameraTurn.TurnToTarget(
                    this,
                    playerTransform,
                    focusPoint,
                    cameraTurnDuration,
                    cameraFacePitch,
                    () => StartEncounterDialogue(playerTransform));
            }
            else
            {
                StartEncounterDialogue(playerTransform);
            }
        }

        private void StartEncounterDialogue(Transform playerTransform)
        {
            if (visualNovelUI == null)
            {
                visualNovelUI = FindAnyObjectByType<VisualNovelDialogueUI>(FindObjectsInactive.Include);
            }

            if (visualNovelUI != null && encounterDialogues != null && encounterDialogues.Count > 0)
            {
                visualNovelUI.StartDialogue(encounterDialogues, () => BeginChase(playerTransform));
            }
            else
            {
                BeginChase(playerTransform);
            }
        }

        private void BeginChase(Transform playerTransform)
        {
            if (mordomoEnemy != null)
            {
                mordomoEnemy.StartRelentlessChase(playerTransform);
            }
        }

        private static bool IsPlayerCollider(Collider col)
        {
            if (col == null) return false;
            return col.CompareTag("Player") || col.GetComponentInParent<FirstPersonController>() != null;
        }
    }
}
