using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR && UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace ObjectPool
{
    [System.Serializable]
    public class PoolableGameObject
    {
#if ODIN_INSPECTOR && UNITY_EDITOR
        [ValidateInput("PrefabValidation")]
#endif
        public GameObject prefab;
        public int amountToPool;
        [Tooltip("Should expand the pool when every instantiated objects are already in use?")]
        public bool shouldExpand;
        [Tooltip("Should dispose when the pool is flushed?")]
        public bool disposable;

        [HideInInspector]
        public int typeID;

        public void Prewarm()
        {
            typeID = ObjectPoolManager.Instance.Instantiate(
                prefab, amountToPool, shouldExpand, disposable
            );
        }

#if ODIN_INSPECTOR && UNITY_EDITOR
        private static bool PrefabValidation(GameObject property)
        {
            return UnityEditor.PrefabUtility.GetPrefabAssetType(property) !=
                UnityEditor.PrefabAssetType.NotAPrefab;
        }
#endif
    }
}
