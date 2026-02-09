using TMPro;
using System;
using UnityEngine.UI;
using Zenject;
using UniRx;
using UnityEngine;

public class ChessUIManager : MonoBehaviour, IInitializable
{
    [SerializeField] private Image playerIndicator;
    [SerializeField] private Color defaultIndicatorColor = Color.white;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;
    [SerializeField] private string defaultActionText = "Начать заново";
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button exitButton;

    [Inject] private ChessBoard chessBoard;
    [InjectOptional] private ChessSaveLoadService _saveLoadService;
    [Inject] private PieceSetupController _setupController;
    [Inject] private ChessGameController _gameController;
    [Inject] private IInteractionHandler _clickHandler;

    private Action _currentAction;
    private bool _isGameStarted = false;

    public void Initialize()
    {
        if (playerIndicator)
        {
            playerIndicator.color = defaultIndicatorColor;
            chessBoard.CurrentPlayer.Subscribe(player =>
            {
                if (player != null)
                    UpdatePlayerIndicator(player);
            });
        }

        _setupController.SetupMessage.Subscribe(message => {
            if (messageText)
                messageText.text = message;
        });

        _gameController.GameMessage.Subscribe(message => {
            if (messageText)
                messageText.text = message;
        });

        _clickHandler.InteractionMessage.Subscribe(message => {
            if (messageText)
                messageText.text = message;
        });

        chessBoard.CurrentPlayer.Subscribe(player => {
            if (playerIndicator)
                playerIndicator.color = player.Color;
        });

        if (actionButton)
        {
            actionButton.onClick.RemoveAllListeners();
            SetActionButton(defaultActionText, OnRestartClicked);
        }

        if (exitButton)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(QuitApplication);
        }
    }

    public void UpdateMessage(string message)
    {
        if (messageText)
            messageText.text = message;
    }

    public string GetMessage() => messageText != null ? messageText.text : "";
    
    public string GetActionButtonText() => actionButtonText != null ? actionButtonText.text : "";
    
    public bool GetIsRestartMode() => _isGameStarted;
    
    public void SetActionButtonTextOnly(string text)
    {
        if (actionButtonText) actionButtonText.text = text;
    }

    public void ApplySavedUIState(string message, string savedActionButtonText, bool isRestartMode)
    {
        _isGameStarted = true;
        SetActionButton("Начать заново", OnRestartClicked);

        if (!string.IsNullOrEmpty(savedActionButtonText))
            SetActionButtonTextOnly(savedActionButtonText);
            
        UpdateMessage(message ?? "");
    }

    public void UpdatePlayerIndicator(Color playerColor)
    {
        if (playerIndicator)
            playerIndicator.color = playerColor;
    }

    public void UpdatePlayerIndicator(IPlayer player)
    {
        if (player != null && playerIndicator)
            playerIndicator.color = player.Color;
    }

    public void SetActionButton(string buttonText, Action onClickAction)
    {
        if (actionButtonText) actionButtonText.text = buttonText;
        actionButton?.onClick.RemoveAllListeners();
        
        if (onClickAction != null)
        {
            actionButton?.onClick.AddListener(() => onClickAction?.Invoke());
            _currentAction = onClickAction;
        }
    }

    public void SubscribeToActionButton(Action action)
    {
        actionButton?.onClick.RemoveAllListeners();
        actionButton?.onClick.AddListener(() => action?.Invoke());
        _currentAction = action;
    }

    public void UnsubscribeFromActionButton()
    {
        actionButton?.onClick.RemoveAllListeners();
        _currentAction = null;
    }

    public void SwitchToRestartMode()
    {
        SetActionButton("Начать заново", OnRestartClicked);
        _isGameStarted = true;
    }

    public void QuitApplication()
    {
        _saveLoadService?.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnRestartClicked()
    {
        if (!chessBoard) return;

        chessBoard.StartNewGame();
        SwitchToRestartMode();
    }

    public void SetUIVisible(bool visible)
    {
        if (playerIndicator != null)
            playerIndicator.gameObject.SetActive(visible);
        if (actionButton != null)
            actionButton.gameObject.SetActive(visible);
        if (exitButton != null)
            exitButton.gameObject.SetActive(visible);
    }

    public void RefreshUI()
    {
        if (chessBoard != null && chessBoard.CurrentPlayer.Value != null)
            UpdatePlayerIndicator(chessBoard.CurrentPlayer.Value);
    }

    void OnDestroy()
    {
        UnsubscribeFromActionButton();
        exitButton?.onClick.RemoveAllListeners();
    }
}
