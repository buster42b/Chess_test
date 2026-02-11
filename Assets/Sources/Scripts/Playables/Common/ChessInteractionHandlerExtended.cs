using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class ChessInteractionHandlerExtended : ChessInteractionHandler
{
    [SerializeField] private float mouseDeltaTolerance = 5f;
    private InputAction _positionAction;
    private ChessPiece _draggedPiece = null;
    private bool _isDragging = false;
    private bool _dragStarted = false;
    private Vector3 _dragOffset;
    private float _dragHeight = 0.5f;
    private Vector2 _dragStartPos;
    
    private PieceSetupController _setupController;

    public override void Initialize(IBoard board)
    {
        _mainCamera = Camera.main;
        _chessBoard = board;
        
        _clickAction = new InputAction("Click");
        _clickAction.AddBinding("<Mouse>/leftButton");
        _clickAction.started += OnSelection;
        _clickAction.performed += OnSelection;
        _clickAction.canceled += OnSelection;
        _clickAction.Enable();
        
        _positionAction = new InputAction("Position");
        _positionAction.AddBinding("<Mouse>/position");
        _positionAction.Enable();
    }

    public override void OnSelection(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            StartDragDetection();
        }
        else if (context.phase == InputActionPhase.Performed)
        {
            if (!_isDragging)
            {
                base.OnSelection(context);
            }
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            EndDragDetection();
        }
    }
    
    private void StartDragDetection()
    {
        _dragStartPos = Mouse.current.position.ReadValue();
        _dragStarted = true;
        
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);
        
        RaycastHit pieceHit;
        if (Physics.Raycast(ray, out pieceHit, Mathf.Infinity, pieceLayer))
        {
            ChessPiece piece = pieceHit.collider.GetComponent<ChessPiece>();
            if (piece != null && !piece.IsDead)
            {
                if (_chessBoard.CurrentPlayer.Value == null || piece.Owner != _chessBoard.CurrentPlayer.Value)
                {
                    InteractionMessage.Value = "Не ваш ход! Выберите свою фигуру.";
                    _dragStarted = false;
                    return;
                }
                
                _draggedPiece = piece;
                
                Vector3 pieceWorldPos = piece.transform.position;
                Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, pieceWorldPos.z));
                _dragOffset = pieceWorldPos - mouseWorldPos;
            }
        }
    }
    
    private void EndDragDetection()
    {
        if (!_dragStarted) return;
        
        if (_isDragging && _draggedPiece != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = _mainCamera.ScreenPointToRay(mousePos);
            RaycastHit boardHit;
            if (Physics.Raycast(ray, out boardHit, Mathf.Infinity, boardLayer))
            {
                Vector2Int tilePos = _chessBoard.GetTileTileOnBoard(boardHit.point);
                
                if (_chessBoard.IsValidTilePosition(tilePos))
                {
                    TryMoveSelectedPiece(tilePos);
                }
                else
                {
                    ReturnPieceToOriginalPosition();
                }
            }
            else
            {
                ReturnPieceToOriginalPosition();
            }
        }
        
        _isDragging = false;
        _dragStarted = false;
        _draggedPiece = null;
    }

    private void ReturnPieceToOriginalPosition()
    {
        if (_draggedPiece == null) return;
        Vector3 originalPos = _chessBoard.GetTileWorldPosition(_draggedPiece.Position);
        _draggedPiece.transform.position = originalPos;
        ClearSelection();
    }

    private void Update()
    {
        if (_dragStarted && !_isDragging && _draggedPiece != null)
        {
            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            float mouseDelta = Vector2.Distance(currentMousePos, _dragStartPos);
            if (mouseDelta > mouseDeltaTolerance)
            {
                _isDragging = true;
                ClearSelection();
                SelectPiece(_draggedPiece);
                
                Vector3 pieceWorldPos = _draggedPiece.transform.position;
                _draggedPiece.transform.position = new Vector3(pieceWorldPos.x, pieceWorldPos.y + _dragHeight, pieceWorldPos.z);
            }
        }

        if (!_isDragging || _draggedPiece == null) return;
        Vector2 mousePos = _positionAction.ReadValue<Vector2>();
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);
            
        RaycastHit boardHit;
        if (Physics.Raycast(ray, out boardHit, Mathf.Infinity, boardLayer))
        {
            Vector3 targetPos = boardHit.point;
            targetPos.y = _draggedPiece.transform.position.y;
            _draggedPiece.transform.position = targetPos;
        }
        else
        {
            Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, _draggedPiece.transform.position.z));
            Vector3 targetPos = mouseWorldPos + _dragOffset;
            targetPos.y = _draggedPiece.transform.position.y;
            _draggedPiece.transform.position = targetPos;
        }
    }

    protected override void TryMoveSelectedPiece(Vector2Int targetTile)
    {
        if (_draggedPiece == null || !_isDragging) 
        {
            base.TryMoveSelectedPiece(targetTile);
            return;
        }

        var result = _chessBoard.TryMovePiece(_draggedPiece, targetTile);

        if (result.IsSuccess)
        {
            ClearSelection();
            _isDragging = false;
            _draggedPiece = null;

            if (!_chessBoard.IsGameOver && !result.WasKingCaptured && !result.RequiresPromotion)
                SwitchToNextPlayer();
        }
        else
        {
            InteractionMessage.Value = result.Message;
            ReturnPieceToOriginalPosition();
            _isDragging = false;
            _draggedPiece = null;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_clickAction != null)
        {
            _clickAction.started -= OnSelection;
            _clickAction.performed -= OnSelection;
            _clickAction.canceled -= OnSelection;
            _clickAction.Disable();
            _clickAction.Dispose();
        }

        if (_positionAction != null)
        {
            _positionAction.Disable();
            _positionAction.Dispose();
        }
    }
}
