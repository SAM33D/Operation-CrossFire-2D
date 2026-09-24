using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates one prefab type up front and reuses the instances.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    [SerializeField] private PooledObject prefab;

    [Tooltip("Size for the Critical phase. The peak is logged at round end.")]
    [SerializeField] private int prewarmCount = 10;

    private Stack<PooledObject> available;
    private int activeCount;
    private int peakActiveCount;

    public void Prewarm()
    {
        available = new Stack<PooledObject>(prewarmCount);
        for (int i = 0; i < prewarmCount; i++)
        {
            available.Push(CreateObject());
        }
    }

    public T Get<T>() where T : PooledObject
    {
        PooledObject obj;
        if (available.Count > 0)
        {
            obj = available.Pop();
        }
        else
        {
            obj = CreateObject();
            Debug.LogWarning($"[ObjectPool] '{name}' ran out and created an extra object. Raise its Prewarm Count.", this);
        }

        activeCount++;
        if (activeCount > peakActiveCount) peakActiveCount = activeCount;

        return (T)obj;
    }

    public void Return(PooledObject obj)
    {
        obj.gameObject.SetActive(false);
        available.Push(obj);
        activeCount--;
    }

    public void LogPeakUsage()
    {
        Debug.Log($"[ObjectPool] '{name}': peak {peakActiveCount} in use at once (prewarmed {prewarmCount}).", this);
    }

    private PooledObject CreateObject()
    {
        PooledObject obj = Instantiate(prefab, transform);
        obj.OwnerPool = this;
        obj.gameObject.SetActive(false);
        return obj;
    }
}
