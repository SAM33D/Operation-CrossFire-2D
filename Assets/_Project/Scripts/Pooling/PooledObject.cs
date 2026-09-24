using UnityEngine;

/// <summary>
/// Base class for anything that lives in an ObjectPool.
/// </summary>
public abstract class PooledObject : MonoBehaviour
{
    public ObjectPool OwnerPool { get; set; }
    public bool IsActive { get; private set; }

    protected void MarkActive() => IsActive = true;

    public void ReturnToPool()
    {
        // Guards against being returned twice in the same frame
        if (!IsActive) return;

        IsActive = false;
        OwnerPool.Return(this);
    }
}
