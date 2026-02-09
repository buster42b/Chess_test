using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class PieceSetupController
{
    private readonly ChessBoard _board;
    private readonly ChessUIManager _uiManager;
    private bool _isSetupPhase;
    private int _currentSetupPlayerIndex;
    private Dictionary<IPlayer, int> _playerPieceIndex = new();
    private int _totalPlacedPieces;

    public PieceSetupController(ChessBoard board, [InjectOptional] ChessUIManager uiManager)
    {
        _board = board;
        _uiManager = uiManager;
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

        Debug.Log("Начинается фаза ручной расстановки фигур!");
        Debug.Log("Правила: можно ставить на любую свободную клетку доски");
        if (_uiManager != null)
            _uiManager.UpdateMessage("Расставьте фигуры на доске. Кликайте по клетке.");
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

        Debug.Log($"Ход игрока {currentPlayer.ID + 1}: " +
                  $"ставьте {currentPiece.Type} " +
                  $"({pieceIndex + 1}/{currentPlayer.Pieces.Count})");

        if (_uiManager != null)
            _uiManager.UpdateMessage($"Игрок {currentPlayer.ID + 1}: поставьте {currentPiece.Type} ({pieceIndex + 1}/{currentPlayer.Pieces.Count})");

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
        if (!_isSetupPhase)
        {
            Debug.LogWarning("Получен клик по клетке, но фаза расстановки не активна!");
            return;
        }

        var currentPlayer = _board.Players[_currentSetupPlayerIndex];
        var pieceIndex = _playerPieceIndex[currentPlayer];

        if (pieceIndex >= currentPlayer.Pieces.Count)
        {
            Debug.LogWarning($"У игрока {currentPlayer.ID + 1} нет больше фигур для расстановки");
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

                Debug.Log($"Расставлена {chessPiece.Type} игрока {currentPlayer.ID + 1} на {tilePos}");
            }

            _playerPieceIndex[currentPlayer]++;
            _totalPlacedPieces++;

            _currentSetupPlayerIndex = (_currentSetupPlayerIndex + 1) % _board.Players.Count;
            StartCurrentPlayerSetup();
        }
        else
        {
            Debug.Log($"Клетка {tilePos} занята или невалидна");
        }

        if (_totalPlacedPieces >= GetTotalPieceCount())
        {
            Debug.Log($"Все фигуры расставлены! ({_totalPlacedPieces}/{GetTotalPieceCount()})");
            EndSetupPhase();
        }
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

        if (_uiManager != null)
        {
            _uiManager.UpdateMessage("Все фигуры расставлены. Игра началась!");
            _uiManager.SwitchToRestartMode();
        }

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

        foreach (var tile in _board.TilesList)
            tile.OccupiedBy.Value = null;

        foreach (var player in _board.Players)
        {
            _playerPieceIndex[player] = 0;

            foreach (var piece in player.Pieces)
            {
                if (piece is ChessPiece chessPiece)
                {
                    chessPiece.transform.SetParent(
                        GameObject.Find($"Player {player.ID + 1} Pieces").transform
                    );
                    chessPiece.transform.localPosition = Vector3.zero;
                    chessPiece.SetPosition(new Vector2Int(-1, -1));
                }
            }
        }

        _totalPlacedPieces = 0;
        _currentSetupPlayerIndex = 0;
        _isSetupPhase = false;

        Debug.Log("Доска полностью сброшена");
    }

    public void SetupRandomPieces()
    {
        if (_isSetupPhase)
            FinishSetupPhase();

        Debug.Log("Начинаем случайную расстановку оставшихся фигур...");

        List<IPiece> piecesToPlace = new List<IPiece>();

        foreach (var player in _board.Players)
        {
            foreach (var piece in player.Pieces)
            {
                if (!IsPiecePlacedOnBoard(piece))
                {
                    piecesToPlace.Add(piece);
                    Debug.Log($"Добавляем к расстановке: {piece.Type} игрока {player.ID + 1}");
                }
                else
                {
                    Debug.Log($"Фигура {piece.Type} игрока {player.ID + 1} уже на доске");
                }
            }
        }

        if (piecesToPlace.Count == 0)
        {
            Debug.Log("Все фигуры уже расставлены!");
            return;
        }

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
            Debug.LogWarning($"Недостаточно свободных клеток! Нужно: {piecesToPlace.Count}, есть: {freeTiles.Count}");
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
                {
                    placedCount++;
                    Debug.Log($"Расставлена {piece.Type} игрока {piece.Owner.ID + 1} на {tilePos}");
                }
            }
        }

        Debug.Log($"Расставлено {placedCount} фигур из {piecesToPlace.Count} оставшихся");

        if (AreAllPiecesPlaced())
        {
            Debug.Log("Все фигуры расставлены!");
            EndSetupPhase();
        }
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
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    private bool PlacePieceOnTile(IPiece piece, Vector2Int tilePos)
    {
        if (!_board.IsValidTilePosition(tilePos)) return false;

        var tile = _board.Tiles[tilePos.x, tilePos.y];
        if (!tile.IsEmpty) return false;

        if (piece is ChessPiece chessPiece)
        {
            chessPiece.SetPosition(tilePos);
            chessPiece.transform.position = _board.GetTileWorldPosition(tilePos);
            tile.OccupiedBy.Value = piece;
            return true;
        }

        return false;
    }
}
