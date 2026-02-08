using System.Collections.Generic;
using System.Linq;
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
    
    public void PlacePiecesRandomly()
    {
        foreach (var piece in Pieces)
        {
            
        }
    }
}
