using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class ChessPieceFactory 
{
    private readonly ChessPiecesConfig _config;
    private Dictionary<PieceType, ChessPiece> _prefabsCache;
    private readonly Transform _piecesParent;
    [Inject] private ChessBoard _board;
    [Inject]
    public ChessPieceFactory(ChessPiecesConfig config) 
    {
        _config = config;
        CachePrefabs();
    }
    
    private void CachePrefabs() 
    {
        _prefabsCache = new Dictionary<PieceType, ChessPiece>();
        foreach (var preset in _config.Pieces) 
        {
            if (preset.Prefab != null) 
                _prefabsCache[preset.Type] = preset.Prefab;
        }
    }
    
    private const float PieceRowSpacing = 1f;

    public IPiece Create(PieceType type, Vector2Int position, IPlayer owner, Transform playerVirtualPosition, int indexInRow = 0) 
    {
        if (!_prefabsCache.TryGetValue(type, out ChessPiece prefab))
        {
            Debug.LogError($"No prefab for {type}");
            return null;
        }
    
        var piece = Object.Instantiate(prefab, playerVirtualPosition);
    
        piece.transform.rotation = Quaternion.LookRotation(owner.VirtualDirection);
        piece.transform.localPosition = new Vector3(indexInRow * PieceRowSpacing, 0f, 0f);
    
        piece.Init(type, position, owner);
        return piece;
    }
    
    public List<IPiece> CreateFullSet(IPlayer owner) 
    {
        var result = new List<IPiece>();
        Transform virtualPosition = new GameObject($"Player {owner.ID+1} Pieces").transform;
    
        virtualPosition.rotation = Quaternion.LookRotation(owner.VirtualDirection);
        virtualPosition.position -= owner.VirtualDirection * 10;
        
        // Cache the parent transform in the player for reuse
        if (owner is Player player)
        {
            player.VirtualPosition = virtualPosition;
        }
    
        int indexInRow = 0;
        foreach (var set in _config.Pieces)
        {
            for (int i = 0; i < set.CountPerPlayer; i++)
            {
                result.Add(Create(set.Type, Vector2Int.zero, owner, virtualPosition, indexInRow));
                indexInRow++;
            }
        }
    
        return result;
    }
}