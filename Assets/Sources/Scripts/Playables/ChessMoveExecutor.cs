using UnityEngine;

public class ChessMoveExecutor
{
    public bool IsMoveLegal(ChessBoard board, IPiece piece, Vector2Int target)
    {
        if (board == null || !board.IsValidTilePosition(target)) return false;

        var targetTile = board.Tiles[target.x, target.y];

        if (!targetTile.IsEmpty && targetTile.OccupiedBy.Value.Owner == piece.Owner)
            return false;

        return true;
    }

    public void ApplyMove(ChessBoard board, IPiece piece, Vector2Int target)
    {
        if (board == null || !IsMoveLegal(board, piece, target)) return;

        Vector2Int oldPosition = piece.Position;

        if (board.IsValidTilePosition(oldPosition))
            board.Tiles[oldPosition.x, oldPosition.y].OccupiedBy.Value = null;

        var targetTile = board.Tiles[target.x, target.y];

        if (!targetTile.IsEmpty)
        {
            var capturedPiece = targetTile.OccupiedBy.Value;
            Debug.Log($"Фигура {capturedPiece.Type} съедена!");
        }

        targetTile.OccupiedBy.Value = piece;

        if (piece is MonoBehaviour pieceMono)
            pieceMono.transform.position = board.GetTileWorldPosition(target);

        Debug.Log($"Фигура {piece.Type} перемещена с {oldPosition} на {target}");
    }

    public MoveResult TryMovePiece(ChessBoard board, IPiece piece, Vector2Int targetPosition,
        System.Action<IPlayer> onKingCaptured)
    {
        if (board == null) return MoveResult.Failed("Доска не задана");
        if (piece == null || piece.IsDead)
            return MoveResult.Failed("Фигура не существует");

        if (!board.IsValidTilePosition(targetPosition))
            return MoveResult.Failed("Неверная позиция");

        if (targetPosition == piece.Position)
            return MoveResult.Failed("Фигура уже на этой клетке");

        var targetTile = board.Tiles[targetPosition.x, targetPosition.y];

        if (!targetTile.IsEmpty && targetTile.OccupiedBy.Value.Owner == piece.Owner)
            return MoveResult.Failed("На клетке уже стоит ваша фигура");

        IPiece capturedPiece = null;
        bool wasCapture = false;

        if (!targetTile.IsEmpty && targetTile.OccupiedBy.Value.Owner != piece.Owner)
        {
            capturedPiece = targetTile.OccupiedBy.Value;
            wasCapture = true;
        }

        ExecuteMove(board, piece, targetPosition, wasCapture, capturedPiece);

        if (wasCapture && capturedPiece != null && capturedPiece.Type == PieceType.King)
            onKingCaptured?.Invoke(capturedPiece.Owner);

        return wasCapture && capturedPiece != null && capturedPiece.Type == PieceType.King
            ? MoveResult.KingCaptured(capturedPiece)
            : MoveResult.Success(wasCapture, capturedPiece);
    }

    private void ExecuteMove(ChessBoard board, IPiece piece, Vector2Int targetPosition,
        bool wasCapture, IPiece capturedPiece)
    {
        Vector2Int oldPosition = piece.Position;

        if (board.IsValidTilePosition(oldPosition))
            board.Tiles[oldPosition.x, oldPosition.y].OccupiedBy.Value = null;

        if (wasCapture && capturedPiece != null)
        {
            if (capturedPiece is ChessPiece capturedChessPiece)
            {
                int capturedIndex = CountCapturedPiecesOf(board, capturedPiece.Owner, capturedPiece);
                Vector3 capturedPos = board.GetCapturedPieceWorldPosition(capturedPiece.Owner, capturedIndex);

                if (capturedPiece.Type == PieceType.King)
                {
                    capturedChessPiece.Kill();
                    capturedChessPiece.transform.position = capturedPos;
                    Debug.Log($"КОРОЛЬ захвачен! Игра окончена.");
                }
                else
                {
                    capturedChessPiece.Kill();
                    capturedChessPiece.transform.position = capturedPos;
                    Debug.Log($"Фигура {capturedPiece.Type} захвачена!");
                }
            }
        }

        var targetTile = board.Tiles[targetPosition.x, targetPosition.y];
        targetTile.OccupiedBy.Value = piece;

        if (piece is ChessPiece chessPiece)
        {
            chessPiece.SetPosition(targetPosition);
            chessPiece.transform.position = board.GetTileWorldPosition(targetPosition);
        }

        Debug.Log($"{piece.Type} перемещен с {oldPosition} на {targetPosition}");
    }

    private static int CountCapturedPiecesOf(ChessBoard board, IPlayer owner, IPiece excludePiece)
    {
        int count = 0;
        foreach (var p in owner.Pieces)
            if (p != excludePiece && p.IsDead) count++;
        return count;
    }
}
