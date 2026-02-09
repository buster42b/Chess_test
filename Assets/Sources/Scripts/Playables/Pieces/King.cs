using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class King : ChessPiece
{
    public override IEnumerable<Vector2Int> GetAvailableMoves(IBoard board)
    {
        Vector2Int[] directions = {
            new (1, 1),
            new (0, 1),
            new (-1, 1),
            new (1, 0),
            new (-1, 0),
            new (1, -1),
            new (0, -1),
            new (-1, -1)
        };

        foreach (var direction in directions)
        {
            Vector2Int targetPos = Position + direction;

            if (!board.IsValidTilePosition(targetPos)) continue;
            var tile = board.Tiles[targetPos.x, targetPos.y];

            if (!tile.IsEmpty && tile.OccupiedBy.Value.Owner == Owner) continue;
            if (!WouldBeInCheck(targetPos, board))
                yield return targetPos;
        }

        foreach (var castleMove in GetCastlingMoves(board))
        {
            yield return castleMove;
        }
    }

    private IEnumerable<Vector2Int> GetCastlingMoves(IBoard board)
    {
        if (HasMoved || IsInCheck(Position, board))
            yield break;

        Vector2Int kingsideRookPos = new Vector2Int(7, Position.y);
        if (CanCastle(kingsideRookPos, new Vector2Int[] { Position + new Vector2Int(1, 0), Position + new Vector2Int(2, 0) }, board))
            yield return Position + new Vector2Int(2, 0);

        Vector2Int queensideRookPos = new Vector2Int(0, Position.y);
        if (CanCastle(queensideRookPos, new Vector2Int[] { Position - new Vector2Int(1, 0), Position - new Vector2Int(2, 0), Position - new Vector2Int(3, 0) }, board))
            yield return Position - new Vector2Int(2, 0);
    }

    private bool CanCastle(Vector2Int rookPos, Vector2Int[] pathSquares, IBoard board)
    {
        if (!board.IsValidTilePosition(rookPos))
            return false;
            
        var rookTile = board.Tiles[rookPos.x, rookPos.y];
        if (rookTile.IsEmpty || !(rookTile.OccupiedBy.Value is Rook rook) || rook.HasMoved)
            return false;

        foreach (var square in pathSquares)
        {
            if (!board.IsValidTilePosition(square) || !board.Tiles[square.x, square.y].IsEmpty)
                return false;
            
            if (WouldBeInCheck(square, board))
                return false;
        }

        return true;
    }

    private Vector2Int StartingPosition { get; set; }
    public bool HasMoved => Position != StartingPosition;

    public override void Init(PieceType type, Vector2Int position, IPlayer owner)
    {
        base.Init(type, position, owner);
        StartingPosition = position;
    }

    private bool IsInCheck(Vector2Int position, IBoard board)
    {
        foreach (var player in board.Players.Where(p => p != Owner))
        {
            foreach (var piece in player.Pieces.Where(p => !p.IsDead))
            {
                if (piece is King)
                {
                    Vector2Int diff = piece.Position - position;
                    if (Mathf.Abs(diff.x) <= 1 && Mathf.Abs(diff.y) <= 1)
                        return true;
                    continue;
                }
                
                if (piece.GetAvailableMoves(board).Contains(position))
                    return true;
            }
        }
        return false;
    }

    private bool WouldBeInCheck(Vector2Int targetPos, IBoard board)
    {
        Vector2Int originalPos = Position;
        Position = targetPos;
        
        bool inCheck = IsInCheck(targetPos, board);
        
        Position = originalPos;
        return inCheck;
    }
}
