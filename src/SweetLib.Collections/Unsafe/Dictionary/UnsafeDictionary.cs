// Copyright © 2026 Zynres.

using System.Runtime.InteropServices;

namespace SweetLib.Collections.Unsafe.Dictionary;

public unsafe struct UnsafeDictionary<TKey, TValue>
    where TKey : unmanaged
    where TValue : unmanaged
{
    internal Bucket Bucket;
    public Slot<TKey, TValue>* Slot;

    public uint Length;
    public uint Capacity;

    public UnsafeDictionary(uint capacity, byte division = 2)
    {
        Bucket = new(Math.Max(1u, capacity / division), division);
        Capacity = capacity;

        Init(ref Slot, capacity, ref Bucket);
    }

    public void Add(in TKey key, in TValue value)
    {
        if (Length >= Capacity)
            Resize(Math.Max(Capacity * 2, Length + 1));

        int hash = key.GetHashCode();
        uint bucket_index = (uint)hash % Bucket.Capacity;
        uint* bucket = &Bucket.Data[bucket_index];

        while (*bucket != uint.MaxValue)
        {
            Slot<TKey, TValue>* linkedSlot = &Slot[*bucket];

            if (linkedSlot->Hash == hash && linkedSlot->Key.Equals(key))
                return;

            bucket = &linkedSlot->Next;
        }

        Slot<TKey, TValue>* slot = &Slot[Length];

        slot->Key = key;
        slot->Hash = hash;
        slot->Next = *bucket;
        slot->Value = value;

        *bucket = Length;
        Length++;
    }

    public void Set(in TKey key, in TValue value)
    {
        Slot<TKey, TValue>* slot = GetSlot(in key);
        slot->Value = value;
    }

    public readonly ref TValue Get(uint index)
    {
        if (index >= Length)
            throw new IndexOutOfRangeException();

        return ref Slot[index].Value;
    }

    public readonly ref TValue Get(in TKey key)
    {
        int hash = key.GetHashCode();
        uint bucket_index = (uint)hash % Bucket.Capacity;
        uint index = Bucket.Data[bucket_index];

        while (true)
        {
            if (index == uint.MaxValue)
                throw new KeyNotFoundException();

            Slot<TKey, TValue>* slot = &Slot[index];

            if (slot->Hash == hash && slot->Key.Equals(key))
                return ref slot->Value;

            index = slot->Next;
        }
    }

    public readonly bool TryGetValue(TKey key, out TValue value)
    {
        int hash = key.GetHashCode();
        uint bucket_index = (uint)hash % Bucket.Capacity;
        uint index = Bucket.Data[bucket_index];

        while (true)
        {
            if (index == uint.MaxValue)
            {
                value = default;
                return false;
            }

            Slot<TKey, TValue>* slot = &Slot[index];

            if (slot->Hash == hash && slot->Key.Equals(key))
            {
                value = slot->Value;
                return true;
            }

            index = slot->Next;
        }
    }

    public ref TValue Emplace(in TKey key)
    {
        int hash = key.GetHashCode();
        uint bucket_index = (uint)hash % Bucket.Capacity;
        uint* index = &Bucket.Data[bucket_index];

        while (true)
        {
            if (*index == uint.MaxValue)
            {
                if (Length >= Capacity)
                    Resize(Math.Max(Capacity * 2, Length + 1));

                Slot<TKey, TValue>* slot = &Slot[Length];

                slot->Key = key;
                slot->Hash = hash;
                slot->Next = *index;

                *index = Length;
                Length++;
            }

            Slot<TKey, TValue>* linkedSlot = &Slot[*index];

            if (linkedSlot->Hash == hash && linkedSlot->Key.Equals(key))
                return ref linkedSlot->Value;

            index = &linkedSlot->Next;
        }
    }

    public readonly bool Contains(in TKey key)
    {
        int hash = key.GetHashCode();
        uint bucket_index = (uint)hash % Bucket.Capacity;
        uint index = Bucket.Data[bucket_index];

        while (true)
        {
            if (index == uint.MaxValue)
                return false;

            Slot<TKey, TValue>* slot = &Slot[index];

            if (slot->Hash == hash && slot->Key.Equals(key))
                return true;

            index = slot->Next;
        }
    }

    /// <summary>
    ///     the method return value from -Emplace- if key not found he add it
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public ref TValue this[in TKey key] => ref Emplace(in key);

    private void Resize(uint newCapacity)
    {
        Capacity = newCapacity;

        Bucket newBucket = new(Math.Max(1u, newCapacity / Bucket.Division), Bucket.Division);
        Slot<TKey, TValue>* newSlot = null;

        Init(ref newSlot, newCapacity, ref newBucket);

        if (Slot != null)
        {
            Buffer.MemoryCopy(
                Slot, newSlot,
                newCapacity * sizeof(Slot<TKey, TValue>),
                Length * sizeof(Slot<TKey, TValue>));
        }

        NativeMemory.Free(Bucket.Data);
        NativeMemory.Free(Slot);

        // remapping keys in the bucket because its size was changed
        for (uint i = 0; i < Length; i++)
        {
            Slot<TKey, TValue>* slot = &newSlot[i];

            uint bucket_index = (uint)slot->Hash % newBucket.Capacity;
            uint* bucket = &newBucket.Data[bucket_index];

            slot->Next = *bucket;

            *bucket = i;
        }

        Slot = newSlot;
        Bucket = newBucket;
    }

    private static void Init(ref Slot<TKey, TValue>* slot, uint capacity, ref Bucket bucket)
    {
        slot = (Slot<TKey, TValue>*)NativeMemory.Alloc((nuint)(sizeof(Slot<TKey, TValue>) * capacity));
        bucket.Data = (uint*)NativeMemory.Alloc(sizeof(uint) * bucket.Capacity);

        // Fill the bucket values to maximum values, 
        // because the check for emptiness is performed using uint.MaxValue.
        NativeMemory.Fill(bucket.Data, sizeof(uint) * bucket.Capacity, 0xFF);
    }

    private readonly Slot<TKey, TValue>* GetSlot(in TKey key)
    {
        int hash = key.GetHashCode();
        uint bucket_index = (uint)hash % Bucket.Capacity;
        uint index = Bucket.Data[bucket_index];

        while (true)
        {
            if (index == uint.MaxValue)
                throw new KeyNotFoundException();

            Slot<TKey, TValue>* slot = &Slot[index];

            if (slot->Hash == hash && slot->Key.Equals(key))
                return slot;

            index = slot->Next;
        }
    }

    public void Dispose()
    {
        if (Bucket.Data != null)
        {
            NativeMemory.Free(Bucket.Data);
            Bucket.Data = null;
        }

        if (Slot != null)
        {
            NativeMemory.Free(Slot);
            Slot = null;
        }

        Bucket.Capacity = 0;
        Capacity = 0;
        Length = 0;
    }
}