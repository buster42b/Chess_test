using System.Linq;
using UniRx;
using UnityEngine;
using Zenject;

public class ChessGameController
{
    private readonly ChessBoard _board;
    private readonly PieceSetupController _setupController;
    private readonly IInteractionHandler _interactionHandler;

    public ReactiveProperty<string> GameMessage { get; } = new ("");
    public ReactiveProperty<bool> IsRestartMode { get; } = new (false);

    public ChessGameController(ChessBoard board, PieceSetupController setupController,
        [InjectOptional] IInteractionHandler interactionHandler)
    {
        _board = board;
        _setupController = setupController;
        _interactionHandler = interactionHandler;
    }

    public void StartNewGame()
    {
        _board.SetGameOver(false);
        EnableAllPieces();

        foreach (var player in _board.Players)
        {
            foreach (var piece in player.Pieces)
            {
                if (piece is ChessPiece chessPiece)
                {
                    chessPiece.gameObject.SetActive(true);
                    var renderer = chessPiece.GetComponent<Renderer>();
                    if (renderer != null)
                        renderer.material.color = player.Color;
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
}
