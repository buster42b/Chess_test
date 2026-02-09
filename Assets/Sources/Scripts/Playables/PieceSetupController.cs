using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Zenject;

public class PieceSetupController
{
    private readonly ChessBoard _board;
    private bool _isSetupPhase;
    private int _currentSetupPlayerIndex;
    private Dictionary<IPlayer, int> _playerPieceIndex = new();
    private int _totalPlacedPieces;
    private Dictionary<IPlayer, Transform> _playerPieceParents = new();

    public ReactiveProperty<string> SetupMessage { get; } = new ("");
    public ReactiveProperty<bool> IsRestartMode { get; } = new (false);

    public PieceSetupController(ChessBoard board)
    {
        _board = board;
    }

    public void InitializePieceParents()
    {
        foreach (var player in _board.Players)
        {
            // Use cached parent transform from player instead of finding or creating new ones
            if (player.VirtualPosition != null)
            {
                _playerPieceParents[player] = player.VirtualPosition;
            }
            else
            {
                // Fallback: create new parent only if cached one not available
                var parentObject = new GameObject($"Player {player.ID + 1} Pieces");
                parentObject.transform.SetParent(_board.transform);
                _playerPieceParents[player] = parentObject.transform;
                
                // Cache it in the player for future use
                if (player is Player playerInstance)
                {
                    playerInstance.VirtualPosition = parentObject.transform;
                }
            }
        }
    }

    public bool IsSetupPhase => _isSetupPhase;

    public void StartSetupPhase()
    {
        _isSetupPhase = true;
        _currentSetupPlayerIndex = 0;
        _totalPlacedPieces = 0;
        _playerPieceIndex.Clear();

        foreach (var player in _board.Players)
            _playerPieceIndex[player] = 0;

        if (_playerPieceParents.Count == 0)
            InitializePieceParents();

        SetupMessage.Value = "Расставьте фигуры на доске. Кликайте по клетке.";
        StartCurrentPlayerSetup();
    }

    private void StartCurrentPlayerSetup()
    {
        if (!_isSetupPhase) return;

        var currentPlayer = _board.Players[_currentSetupPlayerIndex];
        var pieceIndex = _playerPieceIndex[currentPlayer];

        if (pieceIndex >= currentPlayer.Pieces.Count)
        {
            MoveToNextPlayerWithPieces();
            return;
        }

        var currentPiece = currentPlayer.Pieces[pieceIndex];
        _board.CurrentPlayer.Value = currentPlayer;

        SetupMessage.Value = $"Игрок {currentPlayer.ID + 1}: поставьте {currentPiece.Type} ({pieceIndex + 1}/{currentPlayer.Pieces.Count})";

        _board.OnTileClicked -= HandleSetupTileClick;
        _board.OnTileClicked += HandleSetupTileClick;
    }

    private void MoveToNextPlayerWithPieces()
    {
        int startIndex = _currentSetupPlayerIndex;

        do
        {
            _currentSetupPlayerIndex = (_currentSetupPlayerIndex + 1) % _board.Players.Count;
            var nextPlayer = _board.Players[_currentSetupPlayerIndex];

            if (_playerPieceIndex[nextPlayer] < nextPlayer.Pieces.Count)
            {
                StartCurrentPlayerSetup();
                return;
            }

            if (_currentSetupPlayerIndex == startIndex)
            {
                bool allDone = true;
                foreach (var player in _board.Players)
                {
                    if (_playerPieceIndex[player] < player.Pieces.Count)
                    {
                        allDone = false;
                        break;
                    }
                }

                if (allDone)
                {
                    EndSetupPhase();
                    return;
                }
            }
        } while (true);
    }

    private void HandleSetupTileClick(Vector2Int tilePos, ITile clickedTile)
    {
        if (!_isSetupPhase) return;
        
        var currentPlayer = _board.Players[_currentSetupPlayerIndex];
        var pieceIndex = _playerPieceIndex[currentPlayer];

        if (pieceIndex >= currentPlayer.Pieces.Count)
        {
            MoveToNextPlayerWithPieces();
            return;
        }

        var currentPiece = currentPlayer.Pieces[pieceIndex];

        if (_board.IsValidTilePosition(tilePos) && clickedTile.IsEmpty)
        {
            if (currentPiece is ChessPiece chessPiece)
            {
                chessPiece.SetPosition(tilePos);
                chessPiece.transform.position = _board.GetTileWorldPosition(tilePos);
                clickedTile.OccupiedBy.Value = currentPiece;
            }

            _playerPieceIndex[currentPlayer]++;
            _totalPlacedPieces++;

            _currentSetupPlayerIndex = (_currentSetupPlayerIndex + 1) % _board.Players.Count;
            StartCurrentPlayerSetup();
        }

        if (_totalPlacedPieces >= GetTotalPieceCount())
            EndSetupPhase();
    }

    private int GetTotalPieceCount()
    {
        int total = 0;
        foreach (var player in _board.Players)
            total += player.Pieces.Count;
        return total;
    }

    private void EndSetupPhase()
    {
        _isSetupPhase = false;
        _board.OnTileClicked -= HandleSetupTileClick;

        SetupMessage.Value = "Все фигуры расставлены. Игра началась!";
        IsRestartMode.Value = true;

        if (_board.Players.Count > 0)
            _board.CurrentPlayer.Value = _board.Players[0];
    }

    public void FinishSetupPhase()
    {
        if (_isSetupPhase)
            EndSetupPhase();
    }

    public void ResetSetupPhase()
    {
        // Initialize piece parents if needed
        if (_playerPieceParents.Count == 0)
            InitializePieceParents();

        foreach (var tile in _board.TilesList)
            tile.OccupiedBy.Value = null;

        foreach (var player in _board.Players)
        {
            _playerPieceIndex[player] = 0;

            // Reset pieces in a row like when first created
            int indexInRow = 0;
            foreach (var piece in player.Pieces)
            {
                if (piece is not ChessPiece chessPiece) continue;
                
                if (_playerPieceParents.TryGetValue(player, out var parentTransform))
                {
                    chessPiece.transform.SetParent(parentTransform);
                    chessPiece.transform.rotation = Quaternion.LookRotation(player.VirtualDirection);
                    chessPiece.transform.localPosition = new Vector3(indexInRow * 1.0f, 0f, 0f); // Use same spacing as factory
                }
                
                chessPiece.SetPosition(new Vector2Int(-1, -1));
                indexInRow++;
            }
        }

        _totalPlacedPieces = 0;
        _currentSetupPlayerIndex = 0;
        _isSetupPhase = false;
    }

    public void SetupRandomPieces()
    {
        if (_isSetupPhase)
            FinishSetupPhase();

        List<IPiece> piecesToPlace = new List<IPiece>();

        foreach (var player in _board.Players)
        {
            foreach (var piece in player.Pieces)
            {
                if (!IsPiecePlacedOnBoard(piece))
                    piecesToPlace.Add(piece);
            }
        }

        if (piecesToPlace.Count == 0)
            return;
        
        List<Vector2Int> freeTiles = new List<Vector2Int>();

        for (int x = 0; x < _board.Width; x++)
        {
            for (int y = 0; y < _board.Height; y++)
            {
                var tile = _board.Tiles[x, y];
                if (tile.IsEmpty)
                    freeTiles.Add(new Vector2Int(x, y));
            }
        }

        if (freeTiles.Count < piecesToPlace.Count)
        {
            if (freeTiles.Count == 0) return;
            piecesToPlace = piecesToPlace.GetRange(0, Mathf.Min(piecesToPlace.Count, freeTiles.Count));
        }

        var random = new System.Random();
        ShuffleList(piecesToPlace, random);
        ShuffleList(freeTiles, random);

        int placedCount = 0;
        for (int i = 0; i < piecesToPlace.Count; i++)
        {
            if (i < freeTiles.Count)
            {
                var piece = piecesToPlace[i];
                var tilePos = freeTiles[i];

                if (PlacePieceOnTile(piece, tilePos))
                    placedCount++;
            }
        }

        if (AreAllPiecesPlaced())
            EndSetupPhase();
    }

    private bool IsPiecePlacedOnBoard(IPiece piece)
    {
        for (int x = 0; x < _board.Width; x++)
        {
            for (int y = 0; y < _board.Height; y++)
            {
                var tile = _board.Tiles[x, y];
                if (!tile.IsEmpty && tile.OccupiedBy.Value == piece)
                    return true;
            }
        }
        return false;
    }

    private bool AreAllPiecesPlaced()
    {
        foreach (var player in _board.Players)
        {
            foreach (var piece in player.Pieces)
            {
                if (!IsPiecePlacedOnBoard(piece))
                    return false;
            }
        }
        return true;
    }

    private static void ShuffleList<T>(List<T> list, System.Random random)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    private bool PlacePieceOnTile(IPiece piece, Vector2Int tilePos)
    {
        if (!_board.IsValidTilePosition(tilePos)) return false;

        var tile = _board.Tiles[tilePos.x, tilePos.y];
        if (!tile.IsEmpty) return false;

        if (piece is not ChessPiece chessPiece) return false;
        chessPiece.SetPosition(tilePos);
        chessPiece.transform.position = _board.GetTileWorldPosition(tilePos);
        tile.OccupiedBy.Value = piece;
        return true;
    }
}
