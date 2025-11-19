using System;
using CustomNamespace.GenericDatatypes;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

[Serializable]
public class Quiver : IObjectPool<Arrow>
{
    [SerializeField] bool m_collectionCheck = true;
    [SerializeField] int m_defaultCapacity = 10;
    [SerializeField] int m_maxSize = 100;
    [field: FormerlySerializedAs("m_arrowPrefab")] [field: SerializeField] public Arrow ArrowPrefab { get; protected set; }
    public uint CurrentAmmo { get; private set; }
    
    ObjectPool<Arrow> m_pool;
    public void ReleaseAndAddBack(Arrow arrow)
    {
        Release(arrow);
        CurrentAmmo++;
    }
    public Quiver()
    {
        
    }

    public Quiver(Arrow arrowPrefab, uint ammoForQuiver)
    {
        ArrowPrefab = arrowPrefab;
        CurrentAmmo = ammoForQuiver;
    }
    public void Setup(ILevelDataProvider levelData)
    {
        m_pool = new ObjectPool<Arrow>(
            () =>
            {
                GameObject arrow = Object.Instantiate(ArrowPrefab.gameObject);
                arrow.SetActive(false);
                return arrow.GetComponent<Arrow>();
            },
            OnGet,
            OnRelease,
            OnDestroy,
            collectionCheck: m_collectionCheck,
            m_defaultCapacity,
            m_maxSize
        );
    }
    void OnGet(Arrow arrow)
    {
        arrow.gameObject.SetActive(true);
        CurrentAmmo--;
    }

    void OnRelease(Arrow arrow)
    {
        arrow.gameObject.SetActive(false);
    }

    void OnDestroy(Arrow arrow)
    {
        Object.Destroy(arrow.gameObject);
    }
    
    public Arrow Get()
    {
        return m_pool.Get();
    }

    public PooledObject<Arrow> Get(out Arrow arrow)
    {
        if (CurrentAmmo > 0) return m_pool.Get(out arrow);
        arrow = null;
        return default;
    }

    public void Release(Arrow arrow)
    {
        m_pool.Release(arrow);
    }

    public void Clear()
    {
        m_pool.Clear();
    }

    public int CountInactive => m_pool.CountInactive;
}