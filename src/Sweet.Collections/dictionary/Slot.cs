namespace Sweet.Collections.Dictionary;

public struct Slot<TKey, TValue> 
    where TKey : unmanaged 
    where TValue : unmanaged
{
    public int Hash;
    public int Next;

    public TKey Key;
    public TValue Value;
}