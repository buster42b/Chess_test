using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class ChessPiece : MonoBehaviour, IPiece
{
    private bool _isDead;
    public bool IsDead => _isDead;
    public Vector2Int Position { get; private set; }
    public IPlayer Owner { get; private set; }
    [field: SerializeField] public PieceType Type { get; private set; }
    public void Init(PieceType type, Vector2Int position, IPlayer owner)
    {
        Type = type;
        Position = position;
        Owner = owner;
        GetComponent<Renderer>().material.color = owner.Color;
    }

    public virtual bool IsValidMove(Vector2Int target, IBoard board)
    {
        if (!board.IsValidTilePosition(target)) return false;

        var tile = board.Tiles[target.x, target.y];

        if (!tile.IsEmpty && tile.OccupiedBy.Value.Owner == Owner)
            return false;

        return true;
    }

    public virtual IEnumerable<Vector2Int> GetAvailableMoves(IBoard board)
    {
        yield break;
    }

    public void SetPosition(Vector2Int newPosition)
    {
        Position = newPosition;
    }

    public void Kill()
    {
        _isDead = true;
    }

    public void Reset()
    {
        _isDead = false;
    }
}
