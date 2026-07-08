// Copyright © 2026 Zynres.

using System.Runtime.InteropServices;
using Sweet.Collections.Unsafe.Array;

namespace Sweet.Collections.Unsafe.List;

public unsafe struct UnsafeList<T> : IDisposable where T : unmanaged
{
    public T* Data;

    public uint Length;
    public uint Capacity;

    public UnsafeList(uint capacity)
    {
        Length = 0;
        Capacity = capacity;
        Data = (T*)NativeMemory.Alloc((nuint)(sizeof(T) * capacity));

        NativeMemory.Clear(Data, (nuint)sizeof(T) * Capacity);
    }

    public void Add(T value)
    {
        if (Length >= Capacity)
        {
            Resize(Capacity * 2);
        }

        Data[Length] = value;
        Length++;
    }

    public void AddRange(UnsafeArray<T> values)
    {
        if (values.Length == 0)
            return;
        if (values.Data == Data)
            throw new InvalidOperationException("self add range not supported");

        if (Length + values.Length >= Capacity)
        {
            Resize(Math.Max(Capacity * 2, Length + values.Length));
        }

        Buffer.MemoryCopy(
            values.Data,
            Data + Length,
            (Capacity - Length) * sizeof(T),
            values.Length * sizeof(T));

        Length += values.Length;
    }

    private void Resize(uint newCapacity)
    {
        T* newData = (T*)NativeMemory.Alloc((nuint)(sizeof(T) * newCapacity));

        Buffer.MemoryCopy(
            Data, newData,
            newCapacity * sizeof(T),
            Length * sizeof(T));

        NativeMemory.Free(Data);

        Data = newData;
        Capacity = newCapacity;
    }

    public void Set(uint index, T value)
    {
        if (index >= Length)
            throw new IndexOutOfRangeException();

        Data[index] = value;
    }

    public readonly ref T Get(uint index)
    {
        if (index >= Length)
            throw new IndexOutOfRangeException();

        return ref Data[index];
    }

    public readonly ref T this[uint index] => ref Get(index);

    public readonly void CopyTo(UnsafeArray<T>* map)
    {
        if (Data == null || map->Data == null)
            return;

        if (map->Length < Length)
            throw new Exception("Overflow");

        Buffer.MemoryCopy(
            Data, map->Data,
            map->Length * sizeof(T),
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