using UnityEngine;

namespace MergesChef.Map
{
    public interface IPlaceable
    {
        void Move(Vector2Int newPosition, Vector3 worldPosition);
        void Upgrade();
    }
}
