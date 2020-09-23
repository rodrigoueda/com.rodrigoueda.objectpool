namespace ObjectPool
{
    public interface IPoolable
    {
        void Retrieve();
        void Recycle();
    }
}
