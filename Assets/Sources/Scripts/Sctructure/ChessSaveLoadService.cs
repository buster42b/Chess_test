using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Zenject;

[Serializable]
public class GameSaveData
{
    public int width;
    public int height;
    public int currentPlayerIndex;
    public int isGameOver;
    public TileSaveEntry[] tiles;
    public string message;
    public string actionButtonText;
    public int isRestartMode;
}

[Serializable]
public class TileSaveEntry
{
    public int x;
    public int y;
    public int playerIndex;
    public int pieceIndex;
    public int pieceType;
    public int startingPositionX;
    public int startingPositionY;
    public bool isCaptured;
}

public class ChessSaveLoadService : MonoBehaviour, IInitializable
{
    private const string SaveFileName = "chess_save.json";

    [Inject] private ChessBoard _board;
    [Inject] private PieceSetupController _setupController;
    [InjectOptional] private ChessUIManager _uiManager;

    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public void Initialize()
    {
        TryLoadAndStartSetup();
    }

    public void TryLoadAndStartSetup()
    {
        var data = LoadFromDisk();
        if (data != null && ApplyLoad(data))
        {
            Debug.Log("Игра загружена из сохранения.");
            return;
        }

        _setupController.SetupStandardPieces();
    }

    public void Save()
    {
        if (_setupController != null && _setupController.IsSetupPhase)
        {
            DeleteSaveIfExists();
            return;
        }

        var data = CompileSaveData();
        if (data == null) return;

        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Не удалось сохранить игру: {e.Message}");
        }
    }

    private void DeleteSaveIfExists()
    {
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Не удалось удалить сохранение: {e.Message}");
        }
    }

    private GameSaveData CompileSaveData()
    {
        if (_board == null || _board.Tiles == null) return null;

        var entries = new List<TileSaveEntry>();

        // Save all pieces (both alive and captured)
        foreach (var player in _board.Players)
        {
            for (int pieceIndex = 0; pieceIndex < player.Pieces.Count; pieceIndex++)
            {
                var piece = player.Pieces[pieceIndex];
                
                // Get starting position for pieces that need it (Pawn, King, Rook)
                Vector2Int startingPos = Vector2Int.zero;
                if (piece is Pawn pawn)
                    startingPos = pawn.StartingPosition;
                else if (piece is King king)
                    startingPos = king.StartingPosition;
                else if (piece is Rook rook)
                    startingPos = rook.StartingPosition;

                // For alive pieces, use their current position
                // For captured pieces, use -1, -1 as position
                Vector2Int position = piece.IsDead ? new Vector2Int(-1, -1) : piece.Position;

                entries.Add(new TileSaveEntry
                {
                    x = position.x,
                    y = position.y,
                    playerIndex = FindPlayerIndex(player),
                    pieceIndex = pieceIndex,
                    pieceType = (int)piece.Type,
                    startingPositionX = startingPos.x,
                    startingPositionY = startingPos.y,
                    isCaptured = piece.IsDead
                });
            }
        }

        int currentPlayerIndex = 0;
        for (int i = 0; i < _board.Players.Count; i++)
        {
            if (_board.Players[i] == _board.CurrentPlayer.Value)
            {
                currentPlayerIndex = i;
                break;
            }
        }

        string message = "";
        string actionButtonText = "";
        int isRestartMode = 0;
        
        if (_uiManager != null)
        {
            message = _uiManager.GetMessage();
            actionButtonText = _uiManager.GetActionButtonText();
            isRestartMode = _uiManager.GetIsRestartMode() ? 1 : 0;
        }

        return new GameSaveData
        {
            width = _board.Width,
            height = _board.Height,
            currentPlayerIndex = currentPlayerIndex,
            isGameOver = _board.IsGameOver ? 1 : 0,
            tiles = entries.ToArray(),
            message = message ?? "",
            actionButtonText = actionButtonText ?? "",
            isRestartMode = isRestartMode
        };
    }

    private int FindPlayerIndex(IPlayer player)
    {
        for (int i = 0; i < _board.Players.Count; i++)
            if (_board.Players[i] == player) return i;
        return 0;
    }

    private int FindPieceIndexInPlayer(IPlayer player, IPiece piece)
    {
        for (int i = 0; i < player.Pieces.Count; i++)
            if (player.Pieces[i] == piece) return i;
        return -1;
    }

    private static int CountCapturedPiecesOf(IPlayer owner, IPiece excludePiece)
    {
        int count = 0;
        foreach (var p in owner.Pieces)
            if (p != excludePiece && p.IsDead) count++;
        return count;
    }

    private GameSaveData LoadFromDisk()
    {
        if (!File.Exists(SavePath)) return null;

        try
        {
            string json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<GameSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Не удалось загрузить сохранение: {e.Message}");
            return null;
        }
    }

    private bool ApplyLoad(GameSaveData data)
    {
        if (data == null || _board == null) return false;
        if (data.width != _board.Width || data.height != _board.Height) return false;
        if (data.tiles == null) return false;

        int totalPieces = 0;
        int alivePieces = 0;
        foreach (var player in _board.Players)
        {
            totalPieces += player.Pieces.Count;
            foreach (var piece in player.Pieces)
                if (!piece.IsDead) alivePieces++;
        }
        
        // Check if saved pieces count matches total pieces
        if (data.tiles.Length != totalPieces)
            return false;

        foreach (var tile in _board.TilesList)
            tile.OccupiedBy.Value = null;

        // Reset all pieces first
        foreach (var player in _board.Players)
        {
            foreach (var piece in player.Pieces)
            {
                if (piece is ChessPiece chessPiece)
                {
                    chessPiece.Reset();
                    chessPiece.SetPosition(new Vector2Int(-1, -1));
                    var parent = GameObject.Find($"Player {player.ID + 1} Pieces");
                    if (parent != null)
                        chessPiece.transform.SetParent(parent.transform);
                    chessPiece.transform.localPosition = Vector3.zero;
                }
            }
        }

        // Restore pieces from save data
        foreach (var entry in data.tiles)
        {
            if (entry.playerIndex < 0 || entry.playerIndex >= _board.Players.Count) continue;
            var player = _board.Players[entry.playerIndex];
            if (entry.pieceIndex < 0 || entry.pieceIndex >= player.Pieces.Count) continue;

            var piece = player.Pieces[entry.pieceIndex];
            if ((int)piece.Type != entry.pieceType) continue;

            // Restore starting position for pieces that need it
            // Handle backward compatibility for old save files
            Vector2Int startingPos = (entry.startingPositionX == 0 && entry.startingPositionY == 0) 
                ? new Vector2Int(entry.x, entry.y)  // Default to current position for old saves
                : new Vector2Int(entry.startingPositionX, entry.startingPositionY);
                
            if (piece is Pawn pawn)
                pawn.StartingPosition = startingPos;
            else if (piece is King king)
                king.StartingPosition = startingPos;
            else if (piece is Rook rook)
                rook.StartingPosition = startingPos;

            if (entry.isCaptured)
            {
                // Handle captured pieces
                if (piece is ChessPiece chessPiece)
                {
                    chessPiece.Kill();
                    // Move captured pieces to their captured position
                    int capturedIndex = CountCapturedPiecesOf(player, piece);
                    Vector3 capturedPos = _board.GetPieceWorldPosition(player, capturedIndex);
                    chessPiece.transform.position = capturedPos;
                }
            }
            else
            {
                // Handle alive pieces
                if (entry.x >= 0 && entry.x < _board.Width && entry.y >= 0 && entry.y < _board.Height)
                {
                    var tile = _board.Tiles[entry.x, entry.y];
                    tile.OccupiedBy.Value = piece;

                    if (piece is ChessPiece cp)
                    {
                        cp.SetPosition(new Vector2Int(entry.x, entry.y));
                        cp.transform.position = _board.GetTileWorldPosition(new Vector2Int(entry.x, entry.y));
                    }
                }
            }
        }

        if (data.currentPlayerIndex >= 0 && data.currentPlayerIndex < _board.Players.Count)
            _board.CurrentPlayer.Value = _board.Players[data.currentPlayerIndex];

        _board.SetGameOver(data.isGameOver != 0);

        if (_uiManager != null)
        {
            _uiManager.ApplySavedUIState(
                data.message ?? "",
                data.actionButtonText ?? "",
                data.isRestartMode != 0);
        }

        return true;
    }

    private void OnApplicationQuit()
    {
        Save();
    }
}
