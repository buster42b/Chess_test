using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UniRx;
using UnityEngine;
using Zenject;

public class ChessBoard : MonoBehaviour, IBoard, IInitializable
{
    public ITile[,] Tiles { get; private set; }
    public IReadOnlyList<ITile> TilesList => _tilesList;
    public IReadOnlyList<IPlayer> Players { get; private set; }
    [field:SerializeField] public int Width { get; private set; } = 8;
    [field:SerializeField] public int Height { get; private set; } = 8;
    public float TileSize => tileSize;
    public ReactiveProperty<IPlayer> CurrentPlayer { get; } = new ReactiveProperty<IPlayer>();
    public bool IsGameOver { get; private set; }
    
    public event Action<Vector2Int, ITile> OnTileClicked;
    
    private List<ITile> _tilesList = new();
    private List<IPlayer> _playersInternal;
    
    [Inject] private DiContainer _container;
    [SerializeField] private ChessBoardInputHandler _inputHandler;
    [SerializeField] private ChessBoardRenderer _boardRenderer;
    
    [SerializeField] private float tileSize = 1f;
    
    public void Initialize()
    {
        _inputHandler = GetComponent<ChessBoardInputHandler>();
        _boardRenderer = GetComponent<ChessBoardRenderer>();
        
        InitializeVirtualTiles();
        InitPlayers(2);
        
        _inputHandler?.Initialize(this);
        _boardRenderer?.Initialize(this);
    }
    
    public void InitPlayers(int numPlayers)
    {
        _playersInternal = new();
        for (int i = 0; i < numPlayers; i++)
        {
            var player = _container.Instantiate<Player>(new object[] { i, GetColorForIndex(i), GetDirectionForIndex(i)});
            _playersInternal.Add(player);
        }
        Players = new ReadOnlyCollection<IPlayer>(_playersInternal);
        
        if (Players.Count > 0)
            CurrentPlayer.Value = Players[0];
    }
    
    private Color GetColorForIndex(int index)
    {
        Color[] colors = { Color.white, Color.black};
        return colors[index % colors.Length];
    }
    
    private Vector3 GetDirectionForIndex(int index)
    {
        Vector3[] directions = { Vector3.back, Vector3.forward};
        return directions[index % directions.Length];
    }
    
    private void InitializeVirtualTiles()
    {
        Tiles = new ITile[Width, Height];
        _tilesList.Clear();
        
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var tile = new Tile(new Vector2Int(x, y));
                Tiles[x, y] = tile;
                _tilesList.Add(tile);
            }
        }
    }
    
    public void HandleTileClick(Vector3 worldPosition)
    {
        if (_boardRenderer == null) return;
        
        Vector2Int tilePos = _boardRenderer.GetTileAtWorldPosition(worldPosition);
        
        if (!IsValidTilePosition(tilePos)) return;
        
        ITile clickedTile = Tiles[tilePos.x, tilePos.y];
        OnTileClicked?.Invoke(tilePos, clickedTile);
        _boardRenderer?.HighlightTile(tilePos);
    }
    
    public bool IsValidTilePosition(Vector2Int position)
    {
        return position.x >= 0 && position.x < Width && 
               position.y >= 0 && position.y < Height;
    }
    
    public Vector3 GetTileWorldPosition(Vector2Int tilePosition)
    {
        return _boardRenderer.TileToWorldPosition(tilePosition);
    }
    
    public bool IsMoveLegal(IPiece piece, Vector2Int target)
    {
        if (!IsValidTilePosition(target)) return false;
        
        ITile targetTile = Tiles[target.x, target.y];
        
        if (!targetTile.IsEmpty && targetTile.OccupiedBy.Value.Owner == piece.Owner)
            return false;
            
        return true;
    }

    public void ApplyMove(IPiece piece, Vector2Int target)
    {
        if (!IsMoveLegal(piece, target)) return;
        
        Vector2Int oldPosition = piece.Position;
        
        if (IsValidTilePosition(oldPosition))
        {
            Tiles[oldPosition.x, oldPosition.y].OccupiedBy.Value = null;
        }
        
        ITile targetTile = Tiles[target.x, target.y];
        
        if (!targetTile.IsEmpty)
        {
            var capturedPiece = targetTile.OccupiedBy.Value;
            Debug.Log($"Фигура {capturedPiece.Type} съедена!");
        }
        
        targetTile.OccupiedBy.Value = piece;
        
        if (piece is MonoBehaviour pieceMono)
        {
            pieceMono.transform.position = GetTileWorldPosition(target);
        }
        
        Debug.Log($"Фигура {piece.Type} перемещена с {oldPosition} на {target}");
    }
}