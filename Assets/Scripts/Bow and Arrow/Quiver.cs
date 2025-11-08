using System;
using CustomNamespace.GenericDatatypes;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

[Serializable]
public class Quiver : IObjectPool<Arrow>
{
    [SerializeField] Observer<uint> m_currentAmmo;
    [SerializeField] bool m_collectionCheck = true;
    [SerializeField] int m_defaultCapacity = 10;
    [SerializeField] int m_maxSize = 100;
    [SerializeField] Arrow m_arrowPrefab;
    
    ObjectPool<Arrow> m_pool;
    public void ReleaseAndAddBack(Arrow arrow)
    {
        Release(arrow);
        m_currentAmmo.Value++;
    }
    public Quiver()
    {
        
    }

    public void Setup(ILevelDataProvider levelData)
    {
        m_currentAmmo = new Observer<uint>(0);
        m_pool = new ObjectPool<Arrow>(
            () =>
            {
                GameObject arrow = Object.Instantiate(m_arrowPrefab.gameObject);
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
        m_currentAmmo.Value = levelData.GetArrowCounts(m_arrowPrefab);
    }
    void OnGet(Arrow arrow)
    {
        arrow.gameObject.SetActive(true);
        m_currentAmmo.Value--;
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
        if (m_currentAmmo > 0) return m_pool.Get(out arrow);
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