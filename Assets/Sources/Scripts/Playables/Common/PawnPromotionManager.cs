using UnityEngine;
using Zenject;
using UniRx;
using System;
using Object = UnityEngine.Object;

public class PawnPromotionManager : MonoBehaviour
{
    [Inject] private ChessBoard _board;
    [Inject] private ChessPieceFactory _pieceFactory;
    [Inject] private ChessUIManager _uiManager;
    [InjectOptional] private PawnPromotionUI _promotionUIPrefab;

    private bool _isPromotionActive = false;
    private Pawn _pawnToPromote = null;
    private PawnPromotionUI _currentUI = null;

    public bool IsPromotionActive => _isPromotionActive;

    public event Action OnPromotionCompleted;

    public bool TryStartPromotion(Pawn pawn)
    {
        if (_isPromotionActive || pawn == null || !pawn.CanPromote(_board))
            return false;

        _pawnToPromote = pawn;
        _isPromotionActive = true;

        if (_promotionUIPrefab != null)
        {
            ShowPromotionUI();
        }
        else
        {
            AutoPromoteToQueen();
        }

        return true;
    }

    private void ShowPromotionUI()
    {
        Vector3 pawnWorldPos = _board.GetTileWorldPosition(_pawnToPromote.Position);
        _currentUI = Instantiate(_promotionUIPrefab);
        
        _currentUI.Initialize(pawnWorldPos, _pawnToPromote.Owner);
        _currentUI.OnPieceSelected += HandlePieceSelection;

        _uiManager.UpdateMessage("Пешка достигла последней линии! Выберите фигуру для превращения.");
    }

    private void AutoPromoteToQueen()
    {
        PromotePawn(PieceType.Queen);
        CompletePromotion();
        _uiManager.UpdateMessage("Пешка автоматически превращена в ферзя!");
    }

    private void HandlePieceSelection(PieceType selectedType)
    {
        if (!_isPromotionActive || _pawnToPromote == null)
            return;

        PromotePawn(selectedType);
        CompletePromotion();
    }

    private void PromotePawn(PieceType newType)
    {
        if (_pawnToPromote == null) return;

        Vector2Int position = _pawnToPromote.Position;
        IPlayer owner = _pawnToPromote.Owner;

        ChessPiece newPiece = _pieceFactory.CreatePiece(newType, position, owner);
        
        if (newPiece != null)
        {
            ITile tile = _board.GetTile(position);
            if (tile != null)
            {
                tile.SetOccupiedPiece(newPiece);
            }

            if (owner is Player player)
            {
                player.RemovePiece(_pawnToPromote);
                player.AddPiece(newPiece);
            }

            Destroy(_pawnToPromote.gameObject);

            _uiManager.UpdateMessage($"Пешка превращена в {newType}!");
        }
    }

    private void CompletePromotion()
    {
        _isPromotionActive = false;
        _pawnToPromote = null;
        
        if (_currentUI != null)
        {
            _currentUI.OnPieceSelected -= HandlePieceSelection;
            _currentUI = null;
        }
        
        OnPromotionCompleted?.Invoke();
    }

    public void CancelPromotion()
    {
        if (_isPromotionActive)
        {
            if (_currentUI != null)
            {
                _currentUI.OnPieceSelected -= HandlePieceSelection;
                Destroy(_currentUI.gameObject);
                _currentUI = null;
            }
            CompletePromotion();
        }
    }

    private void OnDestroy()
    {
        CancelPromotion();
    }
}
