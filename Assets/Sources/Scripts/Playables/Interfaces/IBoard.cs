using System.Collections.Generic;
using UniRx;
using UnityEngine;

public interface IBoard
{
    public ITile[,] Tiles { get; }
    IReadOnlyList<IPlayer> Players { get; }
    ReactiveProperty<IPlayer> CurrentPlayer { get; }
    bool IsGameOver { get; }
    bool IsValidTilePosition(Vector2Int vector);
    ITile GetTile(Vector2Int position);
    Vector2Int GetTileTileOnBoard(Vector3 worldPosition);
    Vector3 GetTileWorldPosition(Vector2Int boardPosition);
    Vector3 GetPieceWorldPosition(IPlayer owner, int index);
    void HandleTileClick(Vector2Int tilePos);
    MoveResult TryMovePiece(IPiece selectedPiece, Vector2Int targetTile);
}
