using NaughtyAttributes;
using UnityEngine;
using Zenject;

public class ChessBoardRenderer : MonoBehaviour
{
    [SerializeField] private Transform boardTransform;
    private float TileSize => _chessBoard.TileSize;
    [SerializeField] private float borderWidth = 0.5f;
    private ChessBoard _chessBoard;
    
    private Bounds _playableBounds;
    private Vector3 _playableMin;
    private Vector3 _playableMax;
    private Bounds _totalBounds;
    private bool _showGizmos = true;
    
    public void Initialize(ChessBoard chessBoard)
    {
        _chessBoard = chessBoard;
        CalculateBoardBounds();
    }
    
    [Button()]
    private void CalculateBoardBounds()
    {
        if (boardTransform == null)
            boardTransform = transform;
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            _totalBounds = collider.bounds;
        }
        else
        {
            float totalWidth = (_chessBoard.Width * TileSize) + (2 * borderWidth);
            float totalHeight = (_chessBoard.Height * TileSize) + (2 * borderWidth);
            
            _totalBounds = new Bounds
            (
                boardTransform.position,
                new Vector3(totalWidth, 0.1f, totalHeight)
            );
        }

        _playableBounds = new Bounds
        (
            _totalBounds.center,
            new Vector3(
                _totalBounds.size.x - (2 * borderWidth),
                0.1f,
                _totalBounds.size.z - (2 * borderWidth)
            )
        );
        
        _playableMin = _playableBounds.min;
        _playableMax = _playableBounds.max;
    }
    
    public Vector3 TileToWorldPosition(Vector2Int tilePosition)
    {
        if (!_chessBoard.IsValidTilePosition(tilePosition))
            return Vector3.zero;
        
        float xPercent = (tilePosition.x + 0.5f) / _chessBoard.Width;
        float yPercent = (tilePosition.y + 0.5f) / _chessBoard.Height;
        
        float worldX = Mathf.Lerp(_playableMin.x, _playableMax.x, xPercent);
        float worldZ = Mathf.Lerp(_playableMin.z, _playableMax.z, yPercent);
        
        return new Vector3(worldX, _playableBounds.center.y, worldZ);
    }

    public Vector3 GetPieceWorldPosition(IPlayer owner, int indexInRow)
    {
        float margin = TileSize * 2.5f;
        float z = owner.ID == 0 ? _playableMin.z - margin : _playableMax.z + margin;
        float rowLength = _playableMax.x - _playableMin.x;
        const int maxPiecesPerSide = 16;
        float spacing = rowLength / maxPiecesPerSide;
        float x = _playableMin.x + (indexInRow + 0.5f) * spacing;
        return new Vector3(x, _playableBounds.center.y, z);
    }
    
    public Vector2Int GetTileAtWorldPosition(Vector3 worldPosition)
    {
        if (!IsPointInPlayableArea(worldPosition))
            return new Vector2Int(-1, -1);
        
        float xPercent = Mathf.InverseLerp(_playableMin.x, _playableMax.x, worldPosition.x);
        float yPercent = Mathf.InverseLerp(_playableMin.z, _playableMax.z, worldPosition.z);
        
        int tileX = Mathf.FloorToInt(xPercent * _chessBoard.Width);
        int tileY = Mathf.FloorToInt(yPercent * _chessBoard.Height);
        
        tileX = Mathf.Clamp(tileX, 0, _chessBoard.Width - 1);
        tileY = Mathf.Clamp(tileY, 0, _chessBoard.Height - 1);
        
        return new Vector2Int(tileX, tileY);
    }
    
    private bool IsPointInPlayableArea(Vector3 worldPoint)
    {
        Vector3 localPoint = worldPoint - _playableBounds.center;
        return Mathf.Abs(localPoint.x) <= _playableBounds.extents.x &&
               Mathf.Abs(localPoint.z) <= _playableBounds.extents.z;
    }
    
    public void HighlightTile(Vector2Int position)
    {
        Vector3 worldPos = TileToWorldPosition(position);
        Vector3 size = new Vector3(TileSize * 0.8f, 0.1f, TileSize * 0.8f);
        
        Debug.DrawLine(worldPos - size/2, worldPos + size/2, Color.yellow, 0.5f);
    }
    
    void OnDrawGizmos()
    {
        if (!_showGizmos || _chessBoard == null) return;
        
        if (Application.isPlaying)
        {
            CalculateBoardBounds();
        }
        else
        {
            Vector3 boardCenter = transform.position;
            float totalWidth = (_chessBoard.Width * TileSize) + (2 * borderWidth);
            float totalHeight = (_chessBoard.Height * TileSize) + (2 * borderWidth);
            
            _totalBounds = new Bounds(boardCenter, new Vector3(totalWidth, 0.1f, totalHeight));
            _playableBounds = new Bounds
            (
                boardCenter,
                new Vector3(_chessBoard.Width * TileSize, 0.1f, _chessBoard.Height * TileSize)
            );
            
            _playableMin = _playableBounds.min;
            _playableMax = _playableBounds.max;
        }
        
        DrawBoardGizmos();
    }
    
    private void DrawBoardGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.3f, 0.1f, 0.3f);
        Gizmos.DrawCube(_totalBounds.center, _totalBounds.size);
        Gizmos.color = new Color(0.3f, 0.2f, 0.1f, 1f);
        Gizmos.DrawWireCube(_totalBounds.center, _totalBounds.size);
        
        Gizmos.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        Gizmos.DrawWireCube(_playableBounds.center, _playableBounds.size);
        
        for (int x = 0; x < _chessBoard.Width; x++)
        {
            for (int y = 0; y < _chessBoard.Height; y++)
            {
                DrawTileGizmo(new Vector2Int(x, y));
            }
        }
        
        Gizmos.color = Color.red;
        for (int x = 0; x < _chessBoard.Width; x++)
        {
            for (int y = 0; y < _chessBoard.Height; y++)
            {
                Vector3 center = TileToWorldPosition(new Vector2Int(x, y));
                Gizmos.DrawSphere(center, 0.05f);
            }
        }
        
        Gizmos.color = Color.green;
        DrawBounds(_playableBounds);
    }
    
    private void DrawTileGizmo(Vector2Int position)
    {
        Vector3 center = TileToWorldPosition(position);
        bool isDark = (position.x + position.y) % 2 == 1;
        
        Gizmos.color = isDark ? new Color(0.3f, 0.3f, 0.3f, 0.5f) : new Color(0.8f, 0.8f, 0.8f, 0.5f);
        Vector3 size = new Vector3(TileSize * 0.95f, 0.05f, TileSize * 0.95f);
        Gizmos.DrawCube(center, size);
        
        Gizmos.color = isDark ? Color.gray : Color.white;
        Gizmos.DrawWireCube(center, size);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.black;
        UnityEditor.Handles.Label(center + Vector3.up * 0.1f, $"{position.x},{position.y}");
        #endif
    }
    
    private void DrawBounds(Bounds bounds)
    {
        Vector3[] corners = 
        {
            new(bounds.min.x, bounds.center.y, bounds.min.z),
            new(bounds.max.x, bounds.center.y, bounds.min.z),
            new(bounds.max.x, bounds.center.y, bounds.max.z),
            new(bounds.min.x, bounds.center.y, bounds.max.z)
        };
        
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!_showGizmos || _chessBoard == null) return;
        
        DrawSelectedGizmos();
    }
    
    private void DrawSelectedGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(_totalBounds.center, _totalBounds.size + Vector3.one * 0.1f);
        
        Gizmos.color = Color.yellow;
        Vector3[] borderLines = GetBorderLines();
        for (int i = 0; i < borderLines.Length; i += 2)
            Gizmos.DrawLine(borderLines[i], borderLines[i + 1]);
    }
    
    private Vector3[] GetBorderLines()
    {
        return new Vector3[] {
            new(_totalBounds.min.x, 0, _playableBounds.min.z),
            new(_playableBounds.min.x, 0, _playableBounds.min.z),
            new(_playableBounds.max.x, 0, _playableBounds.min.z),
            new(_totalBounds.max.x, 0, _playableBounds.min.z),
            new(_totalBounds.min.x, 0, _playableBounds.max.z),
            new(_playableBounds.min.x, 0, _playableBounds.max.z),
            new(_playableBounds.max.x, 0, _playableBounds.max.z),
            new(_totalBounds.max.x, 0, _playableBounds.max.z),
        };
    }
}