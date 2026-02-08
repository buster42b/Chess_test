using UnityEngine;
using Zenject;

public class ChessGameController
{
    private readonly ChessBoard _board;
    private readonly PieceSetupController _setupController;
    private readonly ChessUIManager _uiManager;
    private readonly IInteractionHandler _interactionHandler;

    public ChessGameController(ChessBoard board, PieceSetupController setupController,
        [InjectOptional] ChessUIManager uiManager,
        [InjectOptional] IInteractionHandler interactionHandler)
    {
        _board = board;
        _setupController = setupController;
        _uiManager = uiManager;
        _interactionHandler = interactionHandler;
    }

    public void StartNewGame()
    {
        Debug.Log("=== НАЧАЛО НОВОЙ ИГРЫ ===");

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
        Debug.Log($"<color=red>КОРОЛЬ ИГРОКА {defeatedPlayer.ID + 1} ЗАХВАЧЕН! ИГРА ОКОНЧЕНА.</color>");

        IPlayer winner = null;
        foreach (var player in _board.Players)
        {
            if (player != defeatedPlayer)
            {
                winner = player;
                Debug.Log($"<color=green>Игрок {player.ID + 1} победил!</color>");
                break;
            }
        }

        if (_uiManager != null)
            _uiManager.UpdateMessage(winner != null
                ? $"Игра окончена! Король захвачен. Победил игрок {winner.ID + 1}."
                : "Игра окончена! Король захвачен.");

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
        if (_uiManager != null)
        {
            _uiManager.SwitchToSetupMode();
            _uiManager.UpdateMessage("Кликайте по клеткам чтобы расставить фигуры");
            _uiManager.RefreshUI();
        }
    }
}
