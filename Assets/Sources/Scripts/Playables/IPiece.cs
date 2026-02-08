using System;
using System.Collections.Generic;
using UnityEngine;

public interface IPiece
{
    bool IsValidMove(Vector2Int target, IBoard board);
    bool IsDead { get; }
    IEnumerable<Vector2Int> GetAvailableMoves(IBoard board);
    Vector2Int Position { get; }
    IPlayer Owner { get; }
    PieceType Type { get; }
    void SetPosition(Vector2Int newPosition);
}

public enum PieceType
{
    Pawn,
    Rook,
    Knight,
    Bishop,
    Queen,
    King
}



[Serializable]
public class PieceData
{
    public Vector2Int Position;
    public int OwnerId;
    public string Type;
}
