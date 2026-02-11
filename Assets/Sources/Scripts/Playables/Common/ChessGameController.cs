using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using Zenject;

public class ChessGameController
{
    private readonly ChessBoard _board;
    private readonly PieceSetupController _setupController;
    private readonly IInteractionHandler _interactionHandler;
    private readonly ChessPieceFactory _pieceFactory;

    [InjectOptional] private ChessSaveLoadService _saveLoadService;

    public ReactiveProperty<string> GameMessage { get; } = new ("");
    public ReactiveProperty<bool> IsRestartMode { get; } = new (false);

    public ChessGameController(ChessBoard board, PieceSetupController setupController,
        [InjectOptional] IInteractionHandler interactionHandler, ChessPieceFactory pieceFactory)
    {
        _board = board;
        _setupController = setupController;
        _interactionHandler = interactionHandler;
        _pieceFactory = pieceFactory;
    }

    public void StartNewGame()
    {
        _board.SetGameOver(false);
        EnableAllPieces();

        ConvertPromotedPiecesBackToPawns();

        foreach (var player in _board.Players)
        {
            foreach (var piece in player.Pieces)
            {
                if (piece is ChessPiece chessPiece)
                {
                    chessPiece.Reset();
                    chessPiece.gameObject.SetActive(true);
                    var renderer = chessPiece.GetComponent<Renderer>();
                    if (renderer != null)
                        renderer.material.color = player.Color;

                    Vector2Int startingPos = Vector2Int.zero;
                    if (piece is Pawn pawn)
                        startingPos = pawn.StartingPosition;
                    else if (piece is King king)
                        startingPos = king.StartingPosition;
                    else if (piece is Rook rook)
                        startingPos = rook.StartingPosition;
                    else
                    {
                        startingPos = piece.Position;
                        if (startingPos.x == -1 || startingPos.y == -1)
                            continue;
                    }

                    if (startingPos.x >= 0 && startingPos.x < _board.Width && 
                        startingPos.y >= 0 && startingPos.y < _board.Height)
                    {
                        var tile = _board.Tiles[startingPos.x, startingPos.y];
                        tile.OccupiedBy.Value = piece;
                        chessPiece.SetPosition(startingPos);
                        chessPiece.transform.position = _board.GetTileWorldPosition(startingPos);
                    }
                }
            }
        }

        _setupController.ResetSetupPhase();
        _setupController.StartSetupPhase();

        if (_board.Players.Count > 0)
            _board.CurrentPlayer.Value = _board.Players[0];

        UpdateUIForNewGame();
    }

    public void OnKingCaptured(IPlayer defeatedPlayer)
    {
        _board.SetGameOver(true);

        IPlayer winner = _board.Players.FirstOrDefault(player => player != defeatedPlayer);

        GameMessage.Value = winner != null
                ? $"Игра окончена! Король захвачен. Победил игрок {winner.ID + 1}."
                : "Игра окончена! Король захвачен.";

        LockAllPieces();
        
        _saveLoadService?.Save();
    }

    public void LockAllPieces()
    {
        _interactionHandler?.SetEnabled(false);
    }

    public void EnableAllPieces()
    {
        _interactionHandler?.SetEnabled(true);
    }

    private void UpdateUIForNewGame()
    {
        IsRestartMode.Value = false;
        GameMessage.Value = "Кликайте по клеткам чтобы расставить фигуры";
    }

    private void ConvertPromotedPiecesBackToPawns()
    {
        var piecesToReplace = new List<(ChessPiece promotedPiece, IPlayer owner, Vector2Int position)>();

        foreach (var player in _board.Players)
        {
            foreach (var piece in player.Pieces)
            {
                if (piece is ChessPiece chessPiece && chessPiece.Promoted)
                {
                    piecesToReplace.Add((chessPiece, player, piece.Position));
                }
            }
        }

        foreach (var (promotedPiece, owner, position) in piecesToReplace)
        {
            owner.RemovePiece(promotedPiece);
            
            if (position.x >= 0 && position.x < _board.Width && 
                position.y >= 0 && position.y < _board.Height)
            {
                var tile = _board.Tiles[position.x, position.y];
                if (tile.OccupiedBy.Value == promotedPiece)
                    tile.OccupiedBy.Value = null;
            }

            var newPawn = _pieceFactory.CreatePiece(PieceType.Pawn, position, owner);
            newPawn.Promoted = false;
            
            owner.AddPiece(newPawn);

            if (position.x >= 0 && position.x < _board.Width && 
                position.y >= 0 && position.y < _board.Height)
            {
                var tile = _board.Tiles[position.x, position.y];
                tile.OccupiedBy.Value = newPawn;
            }

            GameObject.Destroy(promotedPiece.gameObject);
        }
    }
}
