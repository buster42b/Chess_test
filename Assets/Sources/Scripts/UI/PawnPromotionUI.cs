using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;
using System;

public class PawnPromotionUI : MonoBehaviour
{
    [SerializeField] private Button queenButton;
    [SerializeField] private Button rookButton;
    [SerializeField] private Button bishopButton;
    [SerializeField] private Button knightButton;
    [SerializeField] private TMP_Text titleText;

    public event Action<PieceType> OnPieceSelected;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void Start()
    {
        SetupButtons();
    }

    private void SetupButtons()
    {
        queenButton?.onClick.AddListener(() => SelectPiece(PieceType.Queen));
        rookButton?.onClick.AddListener(() => SelectPiece(PieceType.Rook));
        bishopButton?.onClick.AddListener(() => SelectPiece(PieceType.Bishop));
        knightButton?.onClick.AddListener(() => SelectPiece(PieceType.Knight));
    }

    private void SelectPiece(PieceType pieceType)
    {
        OnPieceSelected?.Invoke(pieceType);
        Destroy(gameObject);
    }

    public void Initialize(Vector3 worldPosition, IPlayer owner)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        screenPos.y += 100f;
        
        transform.position = screenPos;
        
        if (titleText != null)
            titleText.text = $"Выберите фигуру для превращения (Игрок {owner.ID + 1})";
            
        gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        queenButton?.onClick.RemoveAllListeners();
        rookButton?.onClick.RemoveAllListeners();
        bishopButton?.onClick.RemoveAllListeners();
        knightButton?.onClick.RemoveAllListeners();
    }
}
