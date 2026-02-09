using UnityEngine;

[CreateAssetMenu(fileName = "ChessPiecesConfig", menuName = "Chess/Pieces Config")]
public class ChessPiecesConfig : ScriptableObject 
{
    [System.Serializable]
    public class PieceSet 
    {
        public PieceType Type;
        public ChessPiece Prefab;
        [Min(1)] public int CountPerPlayer = 2;
    }
    
    public PieceSet[] Pieces = new PieceSet[]
    {
        new() { Type = PieceType.Pawn, CountPerPlayer = 8 },
        new() { Type = PieceType.Rook, CountPerPlayer = 2 },
        new() { Type = PieceType.Knight, CountPerPlayer = 2 },
        new() { Type = PieceType.Bishop, CountPerPlayer = 2 },
        new() { Type = PieceType.Queen, CountPerPlayer = 1 },
        new() { Type = PieceType.King, CountPerPlayer = 1 }
    };
}