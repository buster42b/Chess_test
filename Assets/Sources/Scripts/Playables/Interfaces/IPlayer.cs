using System.Collections.Generic;
using UniRx;
using UnityEngine;

public interface IPlayer
{
    public Color Color { get; }
    public int ID { get; }
    List<IPiece> Pieces { get; }
    List<PieceType> RemainingPieces { get; }
    Transform VirtualPosition { get; }

    Vector3 VirtualDirection { get; }
    public void AddPiece(IPiece piece);
    public void RemovePiece(IPiece piece);
}
