using DG.Tweening;
using UnityEngine;
using Zenject;
using System;

public class ChessMoveExecutor
{
    private ChessPieceFactory _pieceFactory;
    private PromotionUIManager _promotionUIManager;
    
    [Inject]
    public void Construct(ChessPieceFactory pieceFactory, PromotionUIManager promotionUIManager)
    {
        _pieceFactory = pieceFactory;
        _promotionUIManager = promotionUIManager;
    }
    public bool IsMoveLegal(IBoard board, IPiece piece, Vector2Int target)
    {
        if (board == null || !board.IsValidTilePosition(target)) return false;

        var targetTile = board.GetTile(target);

        if (!targetTile.IsEmpty && targetTile.GetOccupiedPiece().Owner == piece.Owner)
            return false;

        return true;
    }

    public void ApplyMove(IBoard board, IPiece piece, Vector2Int target)
    {
        if (board == null || !IsMoveLegal(board, piece, target)) return;

        Vector2Int oldPosition = piece.Position;

        if (board.IsValidTilePosition(oldPosition))
            board.GetTile(oldPosition).SetOccupiedPiece(null);

        var targetTile = board.GetTile(target);

        if (!targetTile.IsEmpty)
        {
            var capturedPiece = targetTile.GetOccupiedPiece();
            Debug.Log($"Фигура {capturedPiece.Type} съедена!");
        }

        targetTile.SetOccupiedPiece(piece);

        piece.pieceTransform.position = board.GetTileWorldPosition(target);

        Debug.Log($"Фигура {piece.Type} перемещена с {oldPosition} на {target}");
    }

    public MoveResult TryMovePiece(IBoard board, IPiece piece, Vector2Int targetPosition,
        System.Action<IPlayer> onKingCaptured)
    {
        if (board == null) return MoveResult.Failed("Доска не задана");
        if (piece == null || piece.IsDead)
            return MoveResult.Failed("Фигура не существует");

        if (!board.IsValidTilePosition(targetPosition))
            return MoveResult.Failed("Неверная позиция");

        if (targetPosition == piece.Position)
            return MoveResult.Failed("Фигура уже на этой клетке");

        bool isValidChessMove = false;
        foreach (var availableMove in piece.GetAvailableMoves(board))
        {
            if (availableMove == targetPosition)
            {
                isValidChessMove = true;
                break;
            }
        }

        if (!isValidChessMove)
            return MoveResult.Failed("Недопустимый ход для этой фигуры");

        var targetTile = board.GetTile(targetPosition);

        if (!targetTile.IsEmpty && targetTile.GetOccupiedPiece().Owner == piece.Owner)
            return MoveResult.Failed("На клетке уже стоит ваша фигура");

        IPiece capturedPiece = null;
        bool wasCapture = false;
        Pawn pawn = piece as Pawn;
        
        if (!targetTile.IsEmpty && targetTile.GetOccupiedPiece().Owner != piece.Owner)
        {
            capturedPiece = targetTile.GetOccupiedPiece();
            wasCapture = true;
        }
        // Check for en passant capture
        
        
        else if (targetTile.IsEmpty && pawn)
        {
            Vector2Int direction = new Vector2Int(0, (int)piece.Owner.VirtualDirection.z);
            Vector2Int expectedPosition = pawn.Position + direction;
            bool isOnCorrectRank = direction.y == 1 ? expectedPosition.y == 4 : expectedPosition.y == 3;
            
            if (isOnCorrectRank && Mathf.Abs(targetPosition.x - pawn.Position.x) == 1 && targetPosition.y == expectedPosition.y)
            {
                Vector2Int adjacentPawnPos = new Vector2Int(targetPosition.x, pawn.Position.y);
                if (board.IsValidTilePosition(adjacentPawnPos))
                {
                    var adjacentTile = board.GetTile(adjacentPawnPos);
                    if (!adjacentTile.IsEmpty && adjacentTile.GetOccupiedPiece() is Pawn enemyPawn && enemyPawn.Owner != piece.Owner)
                    {
                        capturedPiece = enemyPawn;
                        wasCapture = true;
                        Debug.Log("En passant capture detected!");
                    }
                }
            }
        }

        ExecuteMove(board, piece, targetPosition, wasCapture, capturedPiece);

        if (pawn && pawn.CanPromote)
        {
            HandlePawnPromotion(board, pawn, targetPosition, onKingCaptured);
            return MoveResult.PromotionRequired(wasCapture, capturedPiece);
        }

        if (wasCapture && capturedPiece != null && capturedPiece.Type == PieceType.King)
            onKingCaptured?.Invoke(capturedPiece.Owner);

        return wasCapture && capturedPiece != null && capturedPiece.Type == PieceType.King
            ? MoveResult.KingCaptured(capturedPiece)
            : MoveResult.Success(wasCapture, capturedPiece);
    }

    private void ExecuteMove(IBoard board, IPiece piece, Vector2Int targetPosition,
        bool wasCapture, IPiece capturedPiece)
    {
        Vector2Int oldPosition = piece.Position;

        if (board.IsValidTilePosition(oldPosition))
            board.GetTile(oldPosition).SetOccupiedPiece(null);

        if (wasCapture && capturedPiece != null)
        {
            if (capturedPiece is ChessPiece capturedChessPiece)
            {
                int capturedIndex = CountCapturedPiecesOf(capturedPiece.Owner, capturedPiece);
                Vector3 capturedPos = board.GetPieceWorldPosition(capturedPiece.Owner, capturedIndex);

                
                capturedChessPiece.Kill();
                capturedChessPiece.transform.DOMove(capturedPos,.5f).SetEase(Ease.InOutQuad);
                
                // For en passant, remove the captured pawn from its original position
                if (piece is Pawn && targetPosition != capturedPiece.Position)
                {
                    var capturedTile = board.GetTile(capturedPiece.Position);
                    if (capturedTile.GetOccupiedPiece() == capturedPiece)
                    {
                        capturedTile.SetOccupiedPiece(null);
                        Debug.Log($"En passant: Removed pawn from {capturedPiece.Position}");
                    }
                }
            }
        }

        var targetTile = board.GetTile(targetPosition);
        targetTile.SetOccupiedPiece(piece);

        if (piece is ChessPiece chessPiece)
        {
            chessPiece.SetPosition(targetPosition);
            Vector3 targetWorldPosition = board.GetTileWorldPosition(targetPosition);
            chessPiece.transform.DOMove(targetWorldPosition, .5f).SetEase(Ease.InOutQuad);
        }
    }

    private static int CountCapturedPiecesOf(IPlayer owner, IPiece excludePiece)
    {
        int count = 0;
        foreach (var p in owner.Pieces)
            if (p != excludePiece && p.IsDead) count++;
        return count;
    }

    private void HandlePawnPromotion(IBoard board, Pawn pawn, Vector2Int position, System.Action<IPlayer> onKingCaptured)
    {
        _promotionUIManager.ShowPromotionDialog(pawn, position, (pieceType) => {
            PromotePawn(board, pawn, position, pieceType);
            
            var targetTile = board.GetTile(position);
            if (!targetTile.IsEmpty && targetTile.GetOccupiedPiece().Type == PieceType.King)
            {
                onKingCaptured?.Invoke(targetTile.GetOccupiedPiece().Owner);
            }
        });
    }

    private void PromotePawn(IBoard board, Pawn pawn, Vector2Int position, PieceType promotionType)
    {
        pawn.Owner.RemovePiece(pawn);
        
        ChessPiece newPiece = _pieceFactory.CreatePiece(promotionType, position, pawn.Owner);
        
        var tile = board.GetTile(position);
        tile.SetOccupiedPiece(newPiece);
        
        pawn.Owner.AddPiece(newPiece);
        
        GameObject.Destroy(pawn.gameObject);
        
        Debug.Log($"Pawn promoted to {promotionType} at {position}");
    }
}
