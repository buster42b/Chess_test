using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IInteractionHandler
{
    void Initialize(IBoard board);
    void OnSelection(InputAction.CallbackContext context);
    void ClearSelection();
    void SetEnabled(bool enabled);
    public ReactiveProperty<string> InteractionMessage { get; }
}
