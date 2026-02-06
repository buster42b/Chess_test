using System.Collections.Generic;
using UnityEngine;

public interface IPlayer
{
    public Color Color { get; }
    public int ID { get; }
    IReadOnlyList<IPiece> Pieces { get; }
    List<PieceType> RemainingPieces { get; }

    Vector3 VirtualDirection { get; }
}
