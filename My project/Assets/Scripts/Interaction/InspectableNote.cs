using UnityEngine;
using ProjectRac.UI;

namespace ProjectRac.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class InspectableNote : MonoBehaviour, IInteractable
    {
        [Header("Note Content (Inspector)")]
        [SerializeField] private string noteTitle = "Registro do Zelador";
        [SerializeField, TextArea(4, 12)] private string noteBody =
            "14 de Outubro\n\n" +
            "Tranquei as portas dos quartos do segundo andar antes de sair.\n" +
            "Ouvi passos vindos da biblioteca durante a madrugada, mas o hall estava vazio.\n\n" +
            "Se alguem encontrar este bilhete, verifique as gavetas e os armarios — deixei os suprimentos guardados por aqui.";

        [Header("Interaction Settings")]
        [SerializeField] private string inspectPrompt = "[E] Ler Nota";
        [SerializeField] private AudioClip paperSound;
        [SerializeField] private NoteInspectionUI inspectionUI;

        public string NoteTitle => noteTitle;
        public string NoteBody => noteBody;

        public void Configure(string title, string body, NoteInspectionUI ui = null)
        {
            noteTitle = title;
            noteBody = body;
            if (ui != null) inspectionUI = ui;
        }

        public string GetInteractionPrompt() => inspectPrompt;

        public void Interact()
        {
            if (inspectionUI == null)
            {
                inspectionUI = FindAnyObjectByType<NoteInspectionUI>(FindObjectsInactive.Include);
            }

            if (inspectionUI != null)
            {
                inspectionUI.OpenNote(noteTitle, noteBody, paperSound);
            }
            else
            {
                Debug.LogWarning("[InspectableNote] NoteInspectionUI nao encontrado na cena.");
            }
        }
    }
}
