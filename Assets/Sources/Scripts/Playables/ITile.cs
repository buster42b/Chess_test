using System.Collections.Generic;
using UniRx;
using UnityEngine;

public interface ITile
{
    Vector2Int Position { get; }
    ReactiveProperty<IPiece> OccupiedBy { get; }
    bool IsEmpty { get; }
    bool IsWalkable { get; }
}
