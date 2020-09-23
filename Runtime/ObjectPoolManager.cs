using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityBaseCode;
using UnityEngine.Assertions;

namespace ObjectPool
{
    public struct PooledGameObject
    {
        public GameObject instance;
        public int instanceID;
        public int typeID;
        public IPoolable behaviour;
        public bool enabled;
        public float countDown;
    }

    public delegate void ManagedUpdate(float deltaTime);

    public class ObjectPoolManager : Singleton<ObjectPoolManager>
    {
        private Dictionary<GameObject, int> _typeList = new Dictionary<GameObject, int>();
        private Dictionary<int, bool> _shouldExpandList = new Dictionary<int, bool>();
        private List<(int, bool)> _disposableList = new List<(int, bool)>();
        private Dictionary<int, List<PooledGameObject>> _instancedObjects =
            new Dictionary<int, List<PooledGameObject>>();

        private List<PooledGameObject> _recycleCountdown = new List<PooledGameObject>();

        private PooledGameObject _auxPooledGameObject = new PooledGameObject();
        private int _typeCounter = -1;

        private ManagedUpdate _updateCallback = null;

        public int NewType
        {
            get {
                return ++_typeCounter;
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            if (_updateCallback != null) {
                _updateCallback(deltaTime);
            }

            if (_recycleCountdown.Count <= 0) {
                return;
            }

            for (int i = 0; i < _recycleCountdown.Count; i++) {
                _auxPooledGameObject = _recycleCountdown[i];

                if (_auxPooledGameObject.countDown <= deltaTime) {
                    _recycleCountdown.Remove(_auxPooledGameObject);
                    Recycle(_auxPooledGameObject);
                } else {
                    _auxPooledGameObject.countDown -= deltaTime;
                    _recycleCountdown[i] = _auxPooledGameObject;
                }
            }
        }

        public int Instantiate(GameObject gameObject, int amountToPool, bool shouldExpand, bool disposable)
        {
            Assert.IsTrue(amountToPool > 0);
            Assert.IsNotNull(gameObject);

            bool typeExists = (_typeList.ContainsKey(gameObject));
            int typeID;

            if (typeExists) {
                typeID = _typeList[gameObject];
            } else {
                typeID = NewType;
                _typeList.Add(gameObject, typeID);
                _shouldExpandList.Add(typeID, shouldExpand);
                _disposableList.Add((typeID, disposable));
            }

            for (int i = 0; i < amountToPool; i++) {
                _auxPooledGameObject.instance = Instantiate(
                    gameObject, Vector3.zero, Quaternion.identity, this.transform
                );
                _auxPooledGameObject.instanceID =
                    _auxPooledGameObject.instance.GetInstanceID();
                _auxPooledGameObject.behaviour =
                    _auxPooledGameObject.instance.GetComponent<IPoolable>();
                _auxPooledGameObject.typeID = typeID;
                _auxPooledGameObject.enabled = false;
                _auxPooledGameObject.countDown = -1f;

                _auxPooledGameObject.instance.transform.localPosition = Vector3.zero;
                _auxPooledGameObject.instance.SetActive(false);

                if (!_instancedObjects.ContainsKey(typeID)) {
                    _instancedObjects.Add(typeID, new List<PooledGameObject>());
                }
                _instancedObjects[typeID].Add(_auxPooledGameObject);
            }

            return typeID;
        }

        public PooledGameObject Retrieve(PoolableGameObject obj, float countDown = -1f)
        {
            if (!_instancedObjects.ContainsKey(obj.typeID)) {
                Instantiate(obj.prefab, 1, obj.shouldExpand, obj.disposable);
            }

            for (int i = 0; i < _instancedObjects[obj.typeID].Count; i++) {
                _auxPooledGameObject = _instancedObjects[obj.typeID][i];

                if (!_auxPooledGameObject.enabled) {
                    _auxPooledGameObject.enabled = true;
                    _instancedObjects[obj.typeID][i] = _auxPooledGameObject;

                    if (_auxPooledGameObject.behaviour != null) {
                        _auxPooledGameObject.behaviour.Retrieve();
                    }

                    if (countDown > 0f) {
                        _auxPooledGameObject.countDown = countDown;
                        _recycleCountdown.Add(_auxPooledGameObject);
                    }

                    return _auxPooledGameObject;
                }
            }

            if (_shouldExpandList[obj.typeID]) {
                Instantiate(obj.prefab, 1, obj.shouldExpand, obj.disposable);

                _auxPooledGameObject =
                    _instancedObjects[obj.typeID][_instancedObjects[obj.typeID].Count - 1];

                if (!_auxPooledGameObject.enabled) {
                    _auxPooledGameObject.enabled = true;
                    _instancedObjects[obj.typeID][_instancedObjects[obj.typeID].Count - 1] =
                        _auxPooledGameObject;

                    if (_auxPooledGameObject.behaviour != null) {
                        _auxPooledGameObject.behaviour.Retrieve();
                    }

                    if (countDown > 0f) {
                        _auxPooledGameObject.countDown = countDown;
                        _recycleCountdown.Add(_auxPooledGameObject);
                    }

                    return _auxPooledGameObject;
                }
            } else {
                Debug.LogWarning("Every pooled GameObjects are already in use.");
            }

            Debug.LogError("Unable to retrieve a valid pooled instance.");
            return _auxPooledGameObject;
        }

        public void Recycle(PooledGameObject pooledObject, float countDown = -1f)
        {
            if (countDown > 0f) {
                pooledObject.countDown = countDown;
                _recycleCountdown.Add(pooledObject);

                return;
            }

            for (int i = 0; i < _instancedObjects[pooledObject.typeID].Count; i++)
            {
                _auxPooledGameObject = _instancedObjects[pooledObject.typeID][i];

                if (_auxPooledGameObject.instanceID == pooledObject.instanceID) {
                    if (_auxPooledGameObject.behaviour != null) {
                        _auxPooledGameObject.behaviour.Recycle();
                    }

                    _auxPooledGameObject.instance.transform.parent = this.transform;
                    _auxPooledGameObject.instance.transform.localPosition = Vector3.zero;
                    _auxPooledGameObject.instance.transform.rotation = Quaternion.identity;
                    _auxPooledGameObject.instance.SetActive(false);
                    _auxPooledGameObject.enabled = false;
                    _auxPooledGameObject.countDown = -1f;

                    _instancedObjects[pooledObject.typeID][i] = _auxPooledGameObject;
                }
            }
        }

        public void DisposePool(bool hardFlush = false)
        {
            if (hardFlush) {
                List<int> typeList = new List<int>(_instancedObjects.Keys);

                for (int i = 0; i < typeList.Count; i++) {
                    for (int j = 0; j < _instancedObjects[typeList[i]].Count; j++) {
                        Destroy(_instancedObjects[i][j].instance);
                    }
                }

                _instancedObjects.Clear();

                return;
            }

            for (int i = 0; i < _disposableList.Count; i++) {
                if (_disposableList[i].Item2) {
                    for (int j = 0; j < _instancedObjects[_disposableList[i].Item1].Count; j++) {
                        Destroy(_instancedObjects[i][j].instance);
                    }
                }

                _instancedObjects.Remove(_disposableList[i].Item1);
            }
        }

        public void RegisterManagedUpdate(IManagedUpdate update)
        {
            _updateCallback += update.ManagedUpdate;
        }

        public void UnregisterManagedUpdate(IManagedUpdate update)
        {
            _updateCallback -= update.ManagedUpdate;
        }
    }
}
