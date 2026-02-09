using UnityEngine;

public class MoveResult
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; }
    public bool WasCapture { get; private set; }
    public IPiece CapturedPiece { get; private set; }
    public bool WasKingCaptured { get; private set; }
    public bool RequiresPromotion { get; private set; }

    private MoveResult(bool success, string message, bool wasCapture,
                      IPiece capturedPiece, bool wasKingCaptured, bool requiresPromotion = false)
    {
        IsSuccess = success;
        Message = message;
        WasCapture = wasCapture;
        CapturedPiece = capturedPiece;
        WasKingCaptured = wasKingCaptured;
        RequiresPromotion = requiresPromotion;
    }

    public static MoveResult Success(bool wasCapture, IPiece capturedPiece)
    {
        return new MoveResult(true, "Ход успешен", wasCapture, capturedPiece, false);
    }

    public static MoveResult KingCaptured(IPiece capturedKing)
    {
        return new MoveResult(true, "Король захвачен!", true, capturedKing, true);
    }

    public static MoveResult Failed(string message)
    {
        return new MoveResult(false, message, false, null, false);
    }

    public static MoveResult PromotionRequired(bool wasCapture, IPiece capturedPiece)
    {
        return new MoveResult(true, "Пешка достигла последней горизонтали - выберите фигуру для превращения", 
                              wasCapture, capturedPiece, false, true);
    }
}
