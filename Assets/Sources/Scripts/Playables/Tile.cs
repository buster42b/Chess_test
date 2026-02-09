using UniRx;
using UnityEngine;

public class Tile : ITile
{
    public Vector2Int Position { get; }
    public ReactiveProperty<IPiece> OccupiedBy { get; }
    public bool IsEmpty => OccupiedBy.Value == null;
    public bool IsWalkable { get; set; }
    
    public Tile(Vector2Int position)
    {
        Position = position;
        OccupiedBy = new ReactiveProperty<IPiece>();
        IsWalkable = true;
    }
    
    public void SetPiece(IPiece piece)
    {
        OccupiedBy.Value = piece;
    }
    
    public void Clear()
    {
        OccupiedBy.Value = null;
    }
    
    public bool CanBeOccupiedBy(IPiece piece)
    {
        if (IsEmpty) return true;
        return OccupiedBy.Value.Owner != piece.Owner;
    }
}