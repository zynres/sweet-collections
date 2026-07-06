namespace Sweet.Collections.Unsafe.HashSet;

public struct Slot<T>
{
    public int Hash;
    public uint Next;

    public T Value;
}