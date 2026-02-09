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
    [field: SerializeField] public int Width { get; private set; } = 8;
    [field: SerializeField] public int Height { get; private set; } = 8;
    public float TileSize => tileSize;
    public ReactiveProperty<IPlayer> CurrentPlayer { get; } = new ReactiveProperty<IPlayer>();
    public bool IsGameOver { get; private set; }

    public event Action<Vector2Int, ITile> OnTileClicked;

    private List<ITile> _tilesList = new();
    private List<IPlayer> _playersInternal;

    [Inject] private DiContainer _container;
    [Inject] private IInteractionHandler _clickHandler;

    private ChessMoveExecutor _moveExecutor;
    private PieceSetupController _setupController;
    private ChessGameController _gameController;

    public void SetDependencies(ChessMoveExecutor moveExecutor, PieceSetupController setupController, ChessGameController gameController)
    {
        _moveExecutor = moveExecutor;
        _setupController = setupController;
        _gameController = gameController;
    }

    [SerializeField] private ChessBoardRenderer _boardRenderer;
    [SerializeField] private float tileSize = 1f;

    public void SetGameOver(bool value) => IsGameOver = value;

    public void Initialize()
    {
        _boardRenderer = GetComponent<ChessBoardRenderer>();

        InitializeVirtualTiles();
        InitPlayers(2);

        _clickHandler?.Initialize(this);
        if (_clickHandler is ChessClickHandler chessClickHandler)
        {
            if (_setupController != null)
                chessClickHandler.SetSetupController(_setupController);
        }
        _boardRenderer?.Initialize(this);
    }

    public void InitPlayers(int numPlayers)
    {
        _playersInternal = new List<IPlayer>();
        for (int i = 0; i < numPlayers; i++)
        {
            var player = _container.Instantiate<Player>(new object[] { i, GetColorForIndex(i), GetDirectionForIndex(i) });
            _playersInternal.Add(player);
        }
        Players = new ReadOnlyCollection<IPlayer>(_playersInternal);

        if (Players.Count > 0)
            CurrentPlayer.Value = Players[0];
    }

    private Color GetColorForIndex(int index)
    {
        Color[] colors = { Color.white, Color.black };
        return colors[index % colors.Length];
    }

    private Vector3 GetDirectionForIndex(int index)
    {
        Vector3[] directions = { Vector3.back, Vector3.forward };
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

    public void HandleTileClick(Vector2Int tilePos)
    {
        if (_boardRenderer == null) return;
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

    public ITile GetTile(Vector2Int position)
    {
        if (!IsValidTilePosition(position))
            return null;
        
        return Tiles[position.x, position.y];
    }

    public Vector3 GetTileWorldPosition(Vector2Int tilePosition)
    {
        return _boardRenderer.TileToWorldPosition(tilePosition);
    }

    public Vector3 GetPieceWorldPosition(IPlayer owner, int indexInRow)
    {
        return _boardRenderer != null ? _boardRenderer.GetPieceWorldPosition(owner, indexInRow) : transform.position;
    }

    public Vector2Int GetTileTileOnBoard(Vector3 worldPosition)
    {
        return _boardRenderer.GetTileAtWorldPosition(worldPosition);
    }

    public bool IsMoveLegal(IPiece piece, Vector2Int target)
    {
        return _moveExecutor.IsMoveLegal(this, piece, target);
    }

    public void ApplyMove(IPiece piece, Vector2Int target)
    {
        _moveExecutor.ApplyMove(this, piece, target);
    }

    public MoveResult TryMovePiece(IPiece piece, Vector2Int targetPosition)
    {
        return _moveExecutor.TryMovePiece(this, piece, targetPosition, _gameController.OnKingCaptured);
    }

    public void SetupRandomPieces() => _setupController.SetupRandomPieces();
    public void StartNewGame() => _gameController.StartNewGame();
}
