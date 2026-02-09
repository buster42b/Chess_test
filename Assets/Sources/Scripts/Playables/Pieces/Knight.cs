using System.Collections.Generic;
using UnityEngine;

public class Knight : ChessPiece
{
    public override IEnumerable<Vector2Int> GetAvailableMoves(IBoard board)
    {
        Vector2Int[] knightMoves = {
            new (2, 1),
            new (2, -1),
            new (-2, 1),
            new (-2, -1),
            new (1, 2),
            new (1, -2),
            new (-1, 2),
            new (-1, -2)
        };

        foreach (var move in knightMoves)
        {
            Vector2Int targetPos = Position + move;
            
            if (board.IsValidTilePosition(targetPos))
            {
                var tile = board.Tiles[targetPos.x, targetPos.y];
                
                if (tile.IsEmpty || tile.OccupiedBy.Value.Owner != Owner)
                    yield return targetPos;
            }
        }
    }
}
