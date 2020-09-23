# Unity Object Pool

## Usage

- Use the namespace ``` ObjectPool ```
- Your new prefab reference needs to be of type ``` PoolableGameObject ``` instead of ``` GameObject ```
- On your ``` Start ``` method, call the ``` Prewarm() ```function

```C#
using UnityEngine;
using ObjectPool;

public class TestObjectPool : MonoBehaviour
{
    public PoolableGameObject cubes;

    void Start()
    {
        cubes.Prewarm();
    }
}

```

- On unity editor, there is additional information to edit:

![PoolableGameObject data](https://gist.githubusercontent.com/rodrigoueda/c6a714d7cbbdc58641b89679e06d5efb/raw/5a93d4db1ace6263f98ab0dd587bd95aa89401e5/PoolableGameObject_Data.png)

- **Prefab**: The GameObject to be pooled
- **AmountToPool**: How many GameObjects should be instatiated
- **ShouldExpand**: Should expand the pool when every instantiated objects are already in use?
- **Disposable**: Should dispose when the pool is soft flushed?

## Retrieving

- Instead of using unity ``` Object.Instantiate ``` static method, switch to ``` ObjectPoolManager.Instance.Retrieve(PooledGameObject) ```
- ``` PooledGameObject.instance ``` is the now the way to access the created GameObject

```C#
void Update()
{
    if (Input.GetKeyUp(KeyCode.Space)) {
        PooledGameObject gameObj = ObjectPoolManager.Instance.Retrieve(cubes);

        gameObj.instance.transform.parent = this.transform;
        gameObj.instance.transform.position = Vector3.zero;
        gameObj.instance.SetActive(true);
    }
}
```

- There is a second optional parameter to the ``` Retrieve ``` function which creates the objects with a delay self recycle
- This parameter represents the time in seconds

```C#
void Update()
{
    if (Input.GetKeyUp(KeyCode.Space)) {
        PooledGameObject gameObj = ObjectPoolManager.Instance.Retrieve(cubes, 2f);

        gameObj.instance.transform.parent = this.transform;
        gameObj.instance.transform.position = Vector3.zero;
        gameObj.instance.SetActive(true);
    }
}
```

## Recycling

- Switch ``` Object.Destroy ``` to ``` ObjectPoolManager.Instance.Recycle(PooledGameObject) ```

```C#
void Update()
{
    if (Input.GetKeyUp(KeyCode.Escape)) {
        ObjectPoolManager.Instance.Recycle(gameObj);
    }
}
```

- Also, it's possible to delay the recycle using a second parameter, the value is in seconds

```C#
void Update()
{
    if (Input.GetKeyUp(KeyCode.Escape)) {
        ObjectPoolManager.Instance.Recycle(gameObj, 1f);
    }
}
```

## Flushing the pool

- Use the method ``` ObjectPoolManager.Instance.DisposePool(); ``` to empty the pool
- Remember that only the objects marked as Disposable will be destroyed
- To hard flush the pool, removing every single object, use a boolean parameter ``` ObjectPoolManager.Instance.DisposePool(true); ```

## IPoolable interface

- Optionally, the prefab script can implement IPoolable interface
- This way, every time it's instance is retrieved or recycled, the methods ``` Retrieve() ``` and ``` Recycle() ``` will be fired respectively.

```C#
using UnityEngine;
using ObjectPool;

public class CubeBehaviour : MonoBehaviour, IPoolable
{
    public void Retrieve()
    {
        print("Object Retrieved");
    }

    public void Recycle()
    {
        print("Object Recycled");
    }
}
```

## Managed Update

- When working with a great amount of objects, it's considered a good practice to center each individual ``` Update() ``` into a single behaviour in order to optimize performance
- ObjectPoolManager is also able to concentrate instanced objects updates, just extend IManagedUpdate interface
- It's possible to combine IPoolable and IManagedUpdate interfaces

```C#
using UnityEngine;
using ObjectPool;

public class CubeBehaviour : MonoBehaviour, IPoolable, IManagedUpdate
{
    public void Retrieve()
    {
        ObjectPoolManager.Instance.RegisterManagedUpdate(this);
    }

    public void Recycle()
    {
        ObjectPoolManager.Instance.UnregisterManagedUpdate(this);
    }

    public void ManagedUpdate(float deltaTime)
    {
        print(deltaTime);
    }

}
```
