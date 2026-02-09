using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using Zenject;

public class Player : IPlayer
{
    [Inject] private ChessPieceFactory _factory;
    public class Factory : PlaceholderFactory<int, Color, Player> { }
    public Color Color { get; }
    public int ID { get; }
    private List<IPiece> _pieces = new(); 
    public IReadOnlyList<IPiece> Pieces => _pieces.AsReadOnly();
    public List<PieceType> RemainingPieces { get; }
    public Vector3 VirtualDirection { get; }
    public Transform VirtualPosition { get; set; }

    public Player(int id, Color color, Vector3 direction)
    {
        ID = id; 
        Color = color;
        VirtualDirection = direction;
    }
    
    [Inject] public void Initialize()
    {
        _pieces = _factory.CreateFullSet(this);
    }

    public void AddPiece(IPiece piece)
    {
        if (piece != null && !_pieces.Contains(piece))
        {
            _pieces.Add(piece);
        }
    }

    public void RemovePiece(IPiece piece)
    {
        if (piece != null && _pieces.Contains(piece))
        {
            _pieces.Remove(piece);
        }
    }
}
