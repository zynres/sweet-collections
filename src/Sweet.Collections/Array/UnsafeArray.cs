using System.Runtime.InteropServices;
using Sweet.Collections.List;

namespace Sweet.Collections.Array;

public unsafe struct UnsafeArray<T> : IDisposable where T : unmanaged
{
    public T* Data { get; set; }

    public int Length { get; set; }
    public int Capacity { get; set; }

    public UnsafeArray(int capasity)
    {
        Length = 0;
        Capacity = capasity;
        Data = (T*)NativeMemory.Alloc((nuint)(sizeof(T) * capasity));

        new Span<T>(Data, capasity).Clear();
    }

    public void Set(int index, T value)
    {
        if (index > Capacity)
            throw new IndexOutOfRangeException();

        *(Data + index) = value;

        if (index >= Length)
            Length = index + 1;
    }

    public void SetLength(int length)
    {
        Length = length;
    }

    public readonly T* this[int index] => &Data[index];

    public readonly void CopyTo(UnsafeList<T>* map)
    {
        if (Data == null || map->Data == null)
            return;

        if (map->Capacity < Length)
            throw new Exception("Overflow");

        Buffer.MemoryCopy(
            Data, map->Data,
            map->Capacity * sizeof(T),
            Length * sizeof(T));

        map->Length = Length;
    }

    public void Dispose()
    {
        if (Data != null)
        {
            NativeMemory.Free(Data);
            Data = null;
        }
    }
}
