using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class ChessInteractionHandlerExtended : ChessInteractionHandler
{
    [SerializeField] private float mouseDeltaTolerance = 5f;
    private InputAction _positionAction;
    private ChessPiece _draggedPiece = null;
    private Rigidbody _draggedPieceRb = null;
    private List<Rigidbody> _capturablePieceRbs = new List<Rigidbody>();
    private bool _isDragging = false;
    private bool _dragStarted = false;
    private Vector3 _dragOffset;
    private float _dragHeight = 0.5f;
    private Vector2 _dragStartPos;
    private bool _isProcessingCapture = false;

    public override void Initialize(IBoard board)
    {
        _mainCamera = Camera.main;
        _chessBoard = board;
        
        SetupInteractionHandlerReferences();
        
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

    private void SetupInteractionHandlerReferences()
    {
        foreach (var tile in _chessBoard.Tiles)
        {
            if (!tile.IsEmpty && tile.OccupiedBy.Value is ChessPiece piece)
            {
                piece.SetInteractionHandler(this);
            }
        }
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
        
        var pieceHit = GetRaycastHitFromMouse(pieceLayer);
        if (pieceHit.HasValue)
        {
            ChessPiece piece = pieceHit.Value.collider.GetComponent<ChessPiece>();
            if (piece != null && !piece.IsDead)
            {
                if (_chessBoard.CurrentPlayer.Value == null || piece.Owner != _chessBoard.CurrentPlayer.Value)
                {
                    InteractionMessage.Value = "Не ваш ход! Выберите свою фигуру.";
                    _dragStarted = false;
                    return;
                }
                
                _draggedPiece = piece;
                _draggedPieceRb = piece.GetComponent<Rigidbody>();
                piece.SetInteractionHandler(this);
                
                Vector3 pieceWorldPos = piece.transform.position;
                Vector3 mouseWorldPos = GetMouseWorldPosition(pieceWorldPos.z);
                _dragOffset = pieceWorldPos - mouseWorldPos;
            }
        }
    }
    
    private void EndDragDetection()
    {
        if (!_dragStarted) return;
        
        if (_isDragging && _draggedPiece != null)
        {
            var boardHit = GetRaycastHitFromMouse(boardLayer);
            if (boardHit.HasValue)
            {
                Vector2Int tilePos = _chessBoard.GetTileTileOnBoard(boardHit.Value.point);
                
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
        _draggedPieceRb = null;
        
        ResetAllPhysicsState();
    }

    private void ResetRigidbodyToKinematic(Rigidbody rb)
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.constraints = RigidbodyConstraints.None;
        }
    }

    private void ResetCapturablePiecesToKinematic()
    {
        foreach (var rb in _capturablePieceRbs)
        {
            ResetRigidbodyToKinematic(rb);
        }
        _capturablePieceRbs.Clear();
    }

    private void SetupDraggedPieceForPhysics()
    {
        if (_draggedPieceRb != null)
        {
            _draggedPieceRb.isKinematic = false;
            _draggedPieceRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _draggedPieceRb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    private void ResetAllPhysicsState()
    {
        ResetRigidbodyToKinematic(_draggedPieceRb);
        ResetCapturablePiecesToKinematic();
    }

    private RaycastHit? GetRaycastHitFromMouse(LayerMask layerMask)
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);
        
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            return hit;
        }
        return null;
    }

    private Vector3 GetMouseWorldPosition(float zPosition)
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        return _mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, zPosition));
    }

    private void MovePieceSafely(Vector3 targetPos)
    {
        if (_draggedPieceRb != null)
        {
            Vector3 dir = targetPos - _draggedPieceRb.position;
            float dist = dir.magnitude;

            RaycastHit hit;
            if (!_draggedPieceRb.SweepTest(dir.normalized, out hit, dist))
            {
                _draggedPieceRb.MovePosition(targetPos);
            }
            else
            {
                var hitRb = hit.collider.GetComponent<Rigidbody>();
                if (hitRb != null && hitRb.isKinematic)
                {
                    _draggedPieceRb.MovePosition(_draggedPieceRb.position + dir.normalized * (hit.distance - 0.01f));
                }
                else
                {
                    _draggedPieceRb.MovePosition(targetPos);
                }
            }
        }
        else
        {
            _draggedPiece.transform.position = targetPos;
        }
    }

    private void ReturnPieceToOriginalPosition()
    {
        if (_draggedPiece == null) return;
        
        ResetAllPhysicsState();
        
        Vector3 originalPos = _chessBoard.GetTileWorldPosition(_draggedPiece.Position);
        if (_draggedPieceRb != null)
        {
            _draggedPieceRb.MovePosition(originalPos);
        }
        else
        {
            _draggedPiece.transform.position = originalPos;
        }
        ClearSelection();
    }

    private void CheckDragStart()
    {
        if (_dragStarted && !_isDragging && _draggedPiece != null)
        {
            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            float mouseDelta = Vector2.Distance(currentMousePos, _dragStartPos);
            if (mouseDelta > mouseDeltaTolerance)
            {
                StartActualDrag();
            }
        }
    }

    private void StartActualDrag()
    {
        _isDragging = true;
        ClearSelection();
        SelectPiece(_draggedPiece);
        
        SetupDraggedPieceForPhysics();
        SetupCapturablePieces();
        
        Vector3 pieceWorldPos = _draggedPiece.transform.position;
        Vector3 targetPos = new Vector3(pieceWorldPos.x, pieceWorldPos.y + _dragHeight, pieceWorldPos.z);
        
        if (_draggedPieceRb != null)
        {
            _draggedPieceRb.MovePosition(targetPos);
        }
        else
        {
            _draggedPiece.transform.position = targetPos;
        }
    }

    private void SetupCapturablePieces()
    {
        _capturablePieceRbs.Clear();
        var availableMoves = _draggedPiece.GetAvailableMoves(_chessBoard);
        foreach (var move in availableMoves)
        {
            var targetTile = _chessBoard.Tiles[move.x, move.y];
            if (!targetTile.IsEmpty && targetTile.OccupiedBy.Value.Owner != _draggedPiece.Owner)
            {
                var capturableRb = targetTile.OccupiedBy.Value.pieceTransform.GetComponent<Rigidbody>();
                if (capturableRb != null)
                {
                    capturableRb.isKinematic = false;
                    capturableRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    _capturablePieceRbs.Add(capturableRb);
                }
            }
        }
    }

    private void UpdateDragPosition()
    {
        if (!_isDragging || _draggedPiece == null) return;
        
        var boardHit = GetRaycastHitFromMouse(boardLayer);
        if (boardHit.HasValue)
        {
            Vector3 targetPos = boardHit.Value.point;
            targetPos.y = _draggedPiece.transform.position.y;
            MovePieceSafely(targetPos);
        }
        else
        {
            Vector2 mousePos = _positionAction.ReadValue<Vector2>();
            Vector3 mouseWorldPos = GetMouseWorldPosition(_draggedPiece.transform.position.z);
            Vector3 targetPos = mouseWorldPos + _dragOffset;
            targetPos.y = _draggedPiece.transform.position.y;
            MovePieceSafely(targetPos);
        }
    }

    private void Update()
    {
        CheckDragStart();
        UpdateDragPosition();
    }

    protected override void TryMoveSelectedPiece(Vector2Int targetTile)
    {
        if (_draggedPiece == null || !_isDragging) 
        {
            base.TryMoveSelectedPiece(targetTile);
            return;
        }

        ResetAllPhysicsState();

        var result = _chessBoard.TryMovePiece(_draggedPiece, targetTile);

        if (result.IsSuccess)
        {
            ClearSelection();
            _isDragging = false;
            _draggedPiece = null;
            _draggedPieceRb = null;

            if (!_chessBoard.IsGameOver && !result.WasKingCaptured && !result.RequiresPromotion)
                SwitchToNextPlayer();
        }
        else
        {
            InteractionMessage.Value = result.Message;
            ReturnPieceToOriginalPosition();
            _isDragging = false;
            _draggedPiece = null;
            _draggedPieceRb = null;
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

    private bool CanCapturePiece(ChessPiece attackingPiece, ChessPiece defendingPiece)
    {
        var availableMoves = attackingPiece.GetAvailableMoves(_chessBoard);
        foreach (var move in availableMoves)
        {
            var targetTile = _chessBoard.Tiles[move.x, move.y];
            if (!targetTile.IsEmpty && targetTile.OccupiedBy.Value == defendingPiece)
            {
                return true;
            }
        }
        return false;
    }

    private void CompleteCapture(ChessPiece attackingPiece, ChessPiece defendingPiece)
    {
        Vector2Int targetPosition = defendingPiece.Position;
        
        ResetAllPhysicsState();

        var result = _chessBoard.TryMovePiece(attackingPiece, targetPosition);

        if (result.IsSuccess)
        {
            ClearSelection();
            _isDragging = false;
            _draggedPiece = null;
            _draggedPieceRb = null;

            if (!_chessBoard.IsGameOver && !result.WasKingCaptured && !result.RequiresPromotion)
                SwitchToNextPlayer();
        }
        else
        {
            InteractionMessage.Value = result.Message;
            ReturnPieceToOriginalPosition();
            _isDragging = false;
            _draggedPiece = null;
            _draggedPieceRb = null;
        }
    }

    public void HandleCollisionCapture(ChessPiece attackingPiece, ChessPiece defendingPiece)
    {
        if (!_isDragging || _draggedPiece != attackingPiece || _isProcessingCapture) return;

        if (!CanCapturePiece(attackingPiece, defendingPiece)) return;

        _isProcessingCapture = true;
        
        var defendingRb = defendingPiece.GetComponent<Rigidbody>();
        if (defendingRb != null)
        {
            Vector3 randomTorque = new Vector3(
                Random.Range(-50f, 50f),
                Random.Range(-100f, 100f),
                Random.Range(-50f, 50f)
            );
            defendingRb.AddTorque(randomTorque, ForceMode.Impulse);
        }
        
        StartCoroutine(ProcessCaptureAfterDelay(attackingPiece, defendingPiece));
    }

    private System.Collections.IEnumerator ProcessCaptureAfterDelay(ChessPiece attackingPiece, ChessPiece defendingPiece)
    {
        yield return new WaitForSeconds(0.5f);
        CompleteCapture(attackingPiece, defendingPiece);
        _isProcessingCapture = false;
    }
}
