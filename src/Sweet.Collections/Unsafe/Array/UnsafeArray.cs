// Copyright © 2026 Zynres.

using System.Runtime.InteropServices;
using Sweet.Collections.Unsafe.List;

namespace Sweet.Collections.Unsafe.Array;

public unsafe struct UnsafeArray<T> : IDisposable where T : unmanaged
{
    public T* Data;

    public uint Length;

    public UnsafeArray(uint capasity)
    {
        Length = capasity;
        Data = (T*)NativeMemory.Alloc((nuint)(sizeof(T) * capasity));

        NativeMemory.Clear(Data, (nuint)sizeof(T) * capasity);
    }

    public void Set(uint index, T value)
    {
        if (index >= Length)
            throw new IndexOutOfRangeException();

        *(Data + index) = value;
    }

    public readonly ref T Get(uint index)
    {
        if (index >= Length)
            throw new IndexOutOfRangeException();

        return ref Data[index];
    }

    public readonly ref T this[uint index] => ref Get(index);

    public void SetLength(uint length)
    {
        Length = length;
    }

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
