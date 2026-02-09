using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pawn : ChessPiece
{
    private int Direction => (int)Owner.VirtualDirection.z;
    private Vector2Int StartingPosition { get; set; }
    private bool HasMoved => Position != StartingPosition;

    public override void Init(PieceType type, Vector2Int position, IPlayer owner)
    {
        base.Init(type, position, owner);
        StartingPosition = position;
    }

    public override IEnumerable<Vector2Int> GetAvailableMoves(IBoard board)
    {
        Vector2Int forwardPos = Position + new Vector2Int(0, Direction);
        if (board.IsValidTilePosition(forwardPos))
        {
            var forwardTile = board.Tiles[forwardPos.x, forwardPos.y];
            if (forwardTile.IsEmpty)
                yield return forwardPos;
            
            if (!HasMoved)
            {
                Vector2Int doubleForwardPos = Position + new Vector2Int(0, Direction * 2);
                if (board.IsValidTilePosition(doubleForwardPos))
                {
                    var doubleForwardTile = board.Tiles[doubleForwardPos.x, doubleForwardPos.y];
                    if (doubleForwardTile.IsEmpty)
                        yield return doubleForwardPos;
                }
            }
        }

        Vector2Int[] captureMoves = {
            new (1, Direction),
            new (-1, Direction)
        };

        foreach (var captureMove in captureMoves)
        {
            Vector2Int capturePos = Position + captureMove;
            if (!board.IsValidTilePosition(capturePos)) continue;
            var captureTile = board.Tiles[capturePos.x, capturePos.y];
                
            if (!captureTile.IsEmpty && captureTile.OccupiedBy.Value.Owner != Owner)
                yield return capturePos;

            // En passant capture
            else if (captureTile.IsEmpty)
            {
                Vector2Int adjacentPawnPos = Position + new Vector2Int(captureMove.x, 0);
                if (board.IsValidTilePosition(adjacentPawnPos))
                {
                    var adjacentTile = board.Tiles[adjacentPawnPos.x, adjacentPawnPos.y];
                    if (adjacentTile.IsEmpty || adjacentTile.OccupiedBy.Value is not Pawn enemyPawn ||
                        enemyPawn.Owner == Owner) continue;
                    bool isOnCorrectRank = Direction == 1 ? Position.y == 4 : Position.y == 3;
                    if (isOnCorrectRank)
                        yield return capturePos;
                }
            }
        }
    }
}
