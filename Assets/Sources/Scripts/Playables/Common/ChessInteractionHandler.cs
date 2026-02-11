using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class ChessInteractionHandler: MonoBehaviour, IInteractionHandler
{
    [SerializeField] protected LayerMask boardLayer = 1 << 0;
    [SerializeField] protected LayerMask pieceLayer = 1 << 0;

    protected Camera _mainCamera;
    protected IBoard _chessBoard;
    private InputAction _clickAction;
    private PieceSetupController _setupController;
    private IPiece _selectedPiece = null;
    private bool _hasSelection = false;

    [InjectOptional] private ChessSaveLoadService _saveLoadService;

    public ReactiveProperty<string> InteractionMessage { get; } = new ("");
    public ReactiveProperty<Color> PlayerIndicatorColor { get; } = new ();

    public virtual void Initialize(IBoard board)
    {
        _mainCamera = Camera.main;
        _chessBoard = board;
        _clickAction = new InputAction("Click");
        _clickAction.AddBinding("<Mouse>/leftButton");
        _clickAction.performed += OnSelection;
        _clickAction.Enable();
    }

    public void SetSetupController(PieceSetupController setupController)
    {
        _setupController = setupController;
    }

 
    public virtual void OnSelection(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) return;
        if (_mainCamera == null || _chessBoard == null || _chessBoard.IsGameOver) return;
 
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);
 
        RaycastHit pieceHit;
        if (Physics.Raycast(ray, out pieceHit, Mathf.Infinity, pieceLayer))
        {
            ChessPiece piece = pieceHit.collider.GetComponent<ChessPiece>();
            if (piece != null)
            {
                HandlePieceSelection(piece);
                return;
            }
        }
 
        RaycastHit boardHit;
        if (Physics.Raycast(ray, out boardHit, Mathf.Infinity, boardLayer))
        {
            Vector2Int tilePos = _chessBoard.GetTileTileOnBoard(boardHit.point);
 
            if (_chessBoard.IsValidTilePosition(tilePos))
            {
                ITile tile = _chessBoard.Tiles[tilePos.x, tilePos.y];
                if (!tile.IsEmpty && tile.OccupiedBy.Value is ChessPiece pieceOnTile)
                {
                    HandlePieceSelection(pieceOnTile);
                    return;
                }
            }
            HandleTileSelection(tilePos);
        }
    }
 
    protected void HandlePieceSelection(ChessPiece piece)
    {
        if (_chessBoard.IsGameOver) return;
        if (piece.IsDead) return;
        if (_setupController != null && _setupController.IsSetupPhase) return;
 
        if (_hasSelection && _selectedPiece != null)
        {
            if (ReferenceEquals(piece, _selectedPiece) || piece.Owner == _selectedPiece.Owner)
            {
                ClearSelection();
                return;
            }
 
            TryMoveSelectedPiece(piece.Position);
            return;
        }
 
        if (!_hasSelection)
        {
            if (_chessBoard.CurrentPlayer.Value == null ||
                piece.Owner != _chessBoard.CurrentPlayer.Value)
            {
                InteractionMessage.Value = "Не ваш ход! Выберите свою фигуру.";
                return;
            }
 
            SelectPiece(piece);
        }
    }
 
    protected void HandleTileSelection(Vector2Int tilePos)
    {
        if (_chessBoard.IsGameOver) return;
 
        if (_hasSelection && _selectedPiece != null)
        {
            TryMoveSelectedPiece(tilePos);
            return;
        }
 
        _chessBoard.HandleTileClick(tilePos);
    }
 
    protected void SelectPiece(ChessPiece piece)
    {
        ClearSelection();
 
        _selectedPiece = piece;
        _hasSelection = true;
 
        InteractionMessage.Value = $"Выбрана фигура: {piece.Type} на {piece.Position}";
 
        HighlightPiece(piece, true);
    }
 
    protected void TryMoveSelectedPiece(Vector2Int targetTile)
    {
        if (_selectedPiece == null || !_hasSelection) return;
 
        var result = _chessBoard.TryMovePiece(_selectedPiece, targetTile);
 
        if (result.IsSuccess)
        {
            ClearSelection();

            if (!_chessBoard.IsGameOver && !result.WasKingCaptured && !result.RequiresPromotion)
                SwitchToNextPlayer();
            
            _saveLoadService?.Save();
        }
        else
        {
            InteractionMessage.Value = result.Message;

            ClearSelection();
        }
    }
 
    public void SwitchToNextPlayer()
    {
        if (_chessBoard == null || _chessBoard.Players.Count == 0) return;
 
        int currentIndex = _chessBoard.CurrentPlayer.Value?.ID ?? 0;
        int nextIndex = (currentIndex + 1) % _chessBoard.Players.Count;
        _chessBoard.CurrentPlayer.Value = _chessBoard.Players[nextIndex];
 
        UpdateUI();
    }
 
    private void HighlightPiece(ChessPiece piece, bool highlight)
    {
        if (piece == null) return;
 
        var renderer = piece.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = highlight ? Color.yellow : piece.Owner.Color;
        }
    }
 
    public void ClearSelection()
    {
        if (_selectedPiece != null)
        {
            HighlightPiece(_selectedPiece as ChessPiece, false);
        }
 
        _selectedPiece = null;
        _hasSelection = false;
    }
 
    public void SetEnabled(bool enabled) => this.enabled = enabled;
 
    private void UpdateUI()
    {
        PlayerIndicatorColor.Value = _chessBoard.CurrentPlayer.Value.Color;
    }
 
    protected virtual void OnDestroy()
    {
        if (_clickAction != null)
        {
            _clickAction.performed -= OnSelection;
            _clickAction.Disable();
            _clickAction.Dispose();
        }
    }
}