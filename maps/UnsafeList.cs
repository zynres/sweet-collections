using System.Runtime.InteropServices;

namespace unsafe_maps.maps;

public unsafe struct UnsafeList<T> : IUnsafeMap<T>, IDisposable where T : unmanaged
{
    public T* Data { get; set; }
    public int Length { get; set; }
    public int Capacity { get; set; }

    public UnsafeList(int capacity)
    {
        Length = 0;
        Capacity = capacity;
        Data = (T*)Marshal.AllocHGlobal(sizeof(T) * Capacity);

        new Span<T>(Data, Capacity).Clear();
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

    private void Resize(int newCapacity)
    {
        T* newData = (T*)Marshal.AllocHGlobal(sizeof(T) * newCapacity);

        Buffer.MemoryCopy(
            Data, newData,
            newCapacity * sizeof(T),
            Length * sizeof(T));

        Marshal.FreeHGlobal((IntPtr)Data);

        Data = newData;
        Capacity = newCapacity;
    }

    public readonly void Set(int index, T value)
    {
        if (index > Length)
            throw new IndexOutOfRangeException();

        *(Data + index) = value;
    }

    public readonly T* this[int index] => &Data[index];

    public readonly void CopyTo(UnsafeArray<T>* map)
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
            Marshal.FreeHGlobal((IntPtr)Data);
            Data = null;
        }
    }
}