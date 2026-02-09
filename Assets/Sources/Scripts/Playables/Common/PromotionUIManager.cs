using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using UniRx;

public class PromotionUIManager : MonoBehaviour
{
    [SerializeField] private GameObject promotionPanel;
    [SerializeField] private Button queenButton;
    [SerializeField] private Button rookButton;
    [SerializeField] private Button bishopButton;
    [SerializeField] private Button knightButton;
    
    private ChessBoard _chessBoard;
    private ChessPieceFactory _pieceFactory;
    private Pawn _promotingPawn;
    private Vector2Int _promotionPosition;
    private Action<PieceType> _onPromotionSelected;
    
    public bool IsPromotionActive { get; private set; }

    [Inject]
    public void Construct(ChessBoard chessBoard, ChessPieceFactory pieceFactory)
    {
        _chessBoard = chessBoard;
        _pieceFactory = pieceFactory;
    }

    private void Start()
    {
        SetupButtons();
        HidePromotionPanel();
    }

    private void SetupButtons()
    {
        queenButton?.onClick.AddListener(() => OnPromotionPieceSelected(PieceType.Queen));
        rookButton?.onClick.AddListener(() => OnPromotionPieceSelected(PieceType.Rook));
        bishopButton?.onClick.AddListener(() => OnPromotionPieceSelected(PieceType.Bishop));
        knightButton?.onClick.AddListener(() => OnPromotionPieceSelected(PieceType.Knight));
    }

    public void ShowPromotionDialog(Pawn pawn, Vector2Int position, Action<PieceType> onPromotionSelected)
    {
        if (promotionPanel == null)
        {
            Debug.LogError("Promotion panel not assigned!");
            return;
        }

        _promotingPawn = pawn;
        _promotionPosition = position;
        _onPromotionSelected = onPromotionSelected;
        
        promotionPanel.SetActive(true);
        IsPromotionActive = true;
        
        Time.timeScale = 0f;
    }

    private void OnPromotionPieceSelected(PieceType pieceType)
    {
        if (_onPromotionSelected != null)
        {
            _onPromotionSelected.Invoke(pieceType);
        }
        
        HidePromotionPanel();
        
        if (!_chessBoard.IsGameOver)
        {
            SwitchToNextPlayer();
        }
    }

    private void SwitchToNextPlayer()
    {
        if (_chessBoard == null || _chessBoard.Players.Count == 0) return;

        int currentIndex = _chessBoard.CurrentPlayer.Value?.ID ?? 0;
        int nextIndex = (currentIndex + 1) % _chessBoard.Players.Count;
        _chessBoard.CurrentPlayer.Value = _chessBoard.Players[nextIndex];
    }

    private void HidePromotionPanel()
    {
        if (promotionPanel != null)
            promotionPanel.SetActive(false);
            
        IsPromotionActive = false;
        Time.timeScale = 1f;
        
        _promotingPawn = null;
        _onPromotionSelected = null;
    }

    private void OnDestroy()
    {
        queenButton?.onClick.RemoveAllListeners();
        rookButton?.onClick.RemoveAllListeners();
        bishopButton?.onClick.RemoveAllListeners();
        knightButton?.onClick.RemoveAllListeners();
    }
}
