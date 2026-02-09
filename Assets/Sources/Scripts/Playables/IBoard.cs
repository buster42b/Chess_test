using System.Collections.Generic;
using UniRx;
using UnityEngine;

public interface IBoard
{
    public ITile[,] Tiles { get; }
    IReadOnlyList<IPlayer> Players { get; }
    int Width { get; }
    int Height { get; }
    ReactiveProperty<IPlayer> CurrentPlayer { get; }
    bool IsGameOver { get; }
    bool IsMoveLegal(IPiece piece, Vector2Int target);
    void ApplyMove(IPiece piece, Vector2Int target);
    void InitPlayers(int numPlayers);
    bool IsValidTilePosition(Vector2Int vector);
    ITile GetTile(Vector2Int position);
    Vector2Int GetTileTileOnBoard(Vector3 worldPosition);
    Vector3 GetTileWorldPosition(Vector2Int boardPosition);
    Vector3 GetPieceWorldPosition(IPlayer owner, int index);
}
