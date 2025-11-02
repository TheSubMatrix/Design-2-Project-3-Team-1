using System;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

[Serializable]
public class ArrowPool : IObjectPool<Arrow>
{
    [SerializeField] bool m_collectionCheck = true;
    [SerializeField] int m_defaultCapacity = 10;
    [SerializeField] int m_maxSize = 100;
    [SerializeField] Arrow m_arrowPrefab;
    ObjectPool<Arrow> m_pool;
    
    public ArrowPool()
    {
        Setup();
    }

    public void Setup()
    {
        m_pool = new ObjectPool<Arrow>(
            () =>
            {
                GameObject arrow =  Object.Instantiate(m_arrowPrefab.gameObject);
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
        return m_pool.Get(out arrow);
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