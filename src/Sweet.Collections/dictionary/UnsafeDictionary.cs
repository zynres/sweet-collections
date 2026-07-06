namespace Sweet.Collections.Dictionary;

public unsafe struct UnsafeDictionary<TKey, TValue> 
    where TKey : unmanaged 
    where TValue : unmanaged
{
    public int* Bucket;
    public Slot<TKey, TValue>* Slot;

    public int Lenght;
    public int Capacity;
}