using DG.Tweening;
using UnityEngine;

public class ChessMoveExecutor
{
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

        // Check if move follows chess rules for this piece type
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

        if (!targetTile.IsEmpty && targetTile.GetOccupiedPiece().Owner != piece.Owner)
        {
            capturedPiece = targetTile.GetOccupiedPiece();
            wasCapture = true;
        }

        ExecuteMove(board, piece, targetPosition, wasCapture, capturedPiece);

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
}
