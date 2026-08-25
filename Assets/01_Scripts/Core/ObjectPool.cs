using System.Collections.Generic;
using UnityEngine;

namespace WitchHour.Core
{
    /// <summary>
    /// 제네릭 오브젝트 풀. 웨이브당 수십 마리씩 나오는 InvaderUnit을 매번 Instantiate/Destroy하면
    /// 모바일에서 GC 스파이크가 튀기 때문에 재사용한다.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _inactive = new Stack<T>();

        public ObjectPool(T prefab, Transform parent, int prewarm = 0)
        {
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < prewarm; i++)
                Release(Object.Instantiate(_prefab, _parent));
        }

        public T Get()
        {
            T instance = _inactive.Count > 0 ? _inactive.Pop() : Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Release(T instance)
        {
            instance.gameObject.SetActive(false);
            _inactive.Push(instance);
        }
    }
}
