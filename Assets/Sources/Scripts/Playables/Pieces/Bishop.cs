using System.Collections.Generic;
using UnityEngine;

public class Bishop : ChessPiece
{
    public override IEnumerable<Vector2Int> GetAvailableMoves(IBoard board)
    {
        Vector2Int[] directions = {
            new (1, 1),
            new (-1, 1),
            new (1, -1),
            new (-1, -1)
        };

        foreach (var direction in directions)
        {
            for (int i = 1; i < 8; i++)
            {
                Vector2Int targetPos = Position + direction * i;
                
                if (!board.IsValidTilePosition(targetPos))
                    break;
                    
                var tile = board.Tiles[targetPos.x, targetPos.y];
                
                if (!tile.IsEmpty)
                {
                    if (tile.OccupiedBy.Value.Owner != Owner)
                        yield return targetPos;
                    break;
                }
                
                yield return targetPos;
            }
        }
    }
}
