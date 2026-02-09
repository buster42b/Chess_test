using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class ChessBoardInputHandler : MonoBehaviour
{
    [SerializeField] private LayerMask boardLayer = 1 << 0;
    private ChessBoard _chessBoard;
    
    private Camera _mainCamera;
    private InputAction _clickAction;
    
    public void Initialize(ChessBoard chessBoard)
    {
        _chessBoard = chessBoard;
        _mainCamera = Camera.main;
        SetupInputSystem();
    }
    
    private void SetupInputSystem()
    {
        _clickAction = new InputAction("Click", InputActionType.Button, "<Mouse>/leftButton");
        _clickAction.performed += OnClickPerformed;
        _clickAction.Enable();
    }
    
    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        HandleTileClick();
    }
    
    private void HandleTileClick()
    {
        if (_mainCamera == null || _chessBoard == null) return;
        
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, boardLayer)) 
            return;
        
        _chessBoard.HandleTileClick(hit.point);
    }
    
    void OnDestroy()
    {
        if (_clickAction == null) return;
        _clickAction.performed -= OnClickPerformed;
        _clickAction.Disable();
        _clickAction.Dispose();
    }
}