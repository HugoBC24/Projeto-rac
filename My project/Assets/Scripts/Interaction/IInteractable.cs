namespace ProjectRac.Interaction
{
    public interface IInteractable
    {
        string GetInteractionPrompt();
        void Interact();
    }
}
