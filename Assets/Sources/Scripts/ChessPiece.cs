using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class ChessPiece : MonoBehaviour, IPiece
{
    private bool _isDead;
    public bool IsDead => _isDead;
    
    public void Init(PieceType type, Vector2Int position, IPlayer owner)
    {
        Type = type;
        Position = position;
        Owner = owner;
        GetComponent<Renderer>().material.color = owner.Color;
    }

    public bool IsValidMove(Vector2Int target, IBoard board)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<Vector2Int> GetAvailableMoves(IBoard board)
    {
        throw new System.NotImplementedException();
    }

    public Vector2Int Position { get; private set; }
    public IPlayer Owner { get; private set; }
    [field: SerializeField] public PieceType Type { get; private set; }
}