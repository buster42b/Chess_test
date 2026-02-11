using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class ChessPiece : MonoBehaviour, IPiece
{
    public Transform pieceTransform { get => transform;}
    private bool _isDead;
    public bool IsDead => _isDead;
    public Vector2Int Position { get; protected set; }
    public IPlayer Owner { get; private set; }
    [field: SerializeField]public virtual PieceType Type { get; private set; }
    public bool Promoted { get; set; }
 
    private bool _isFalling = false;
    private ChessInteractionHandlerExtended _interactionHandler;
    public virtual void Init(PieceType type, Vector2Int position, IPlayer owner)
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

    private void OnCollisionEnter(Collision collision)
    {
        if (_isFalling) return;
        
        ChessPiece otherPiece = collision.gameObject.GetComponent<ChessPiece>();
        if (otherPiece != null && otherPiece.Owner != Owner && !_isDead && !otherPiece.IsDead)
        {
            if (_interactionHandler != null)
            {
                _interactionHandler.HandleCollisionCapture(this, otherPiece);
            }
        }
    }

    public void SetInteractionHandler(ChessInteractionHandlerExtended handler)
    {
        _interactionHandler = handler;
    }

    private void Update()
    {
        if (_isFalling)
        {
            if (transform.position.y < -5f)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
