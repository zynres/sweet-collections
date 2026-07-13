// Copyright © 2026 Zynres.

using System.Runtime.InteropServices;

namespace SweetLib.Collections.Unsafe.HashSet;

public unsafe struct UnsafeHashSet<T> where T : unmanaged
{
    internal Bucket Bucket;
    public Slot<T>* Slots;

    public uint Length;
    public uint Capacity;

    public UnsafeHashSet(uint capacity, byte division = 2)
    {
        Bucket = new Bucket(Math.Max(1u, capacity / division), division);
        Capacity = capacity;

        Init(ref Slots, capacity, ref Bucket);
    }

    public void Add(in T value)
    {
        if (Length >= Capacity)
            Resize(Math.Max(Capacity * 2, Length + 1));

        int hash = value.GetHashCode();
        uint bucket_index = (uint)hash % Bucket.Capacity;
        uint* bucket = &Bucket.Data[bucket_index];

        while (*bucket != uint.MaxValue)
        {
            Slot<T>* linkedSlot = &Slots[*bucket];

            if (linkedSlot->Hash == hash && linkedSlot->Value.Equals(value))
                return;

            bucket = &linkedSlot->Next;
        }

        Slot<T>* slot = &Slots[Length];

        slot->Value = value;
        slot->Hash = hash;
        slot->Next = *bucket;

        *bucket = Length;
        Length++;
    }

    public void Set(uint index, in T value)
    {
        if (index >= Length)
            throw new IndexOutOfRangeException();

        // if you don't check for the existence of the value, it will lead to data duplication, 
        // which makes no sense for this map.
        if (Contains(in value))
            return;

        Slot<T>* slot = &Slots[index];

        uint bucket_index = (uint)slot->Hash % Bucket.Capacity;
        uint* bucket = &Bucket.Data[bucket_index];

        while (*bucket != uint.MaxValue)
        {
            Slot<T>* linkedSlot = &Slots[*bucket];

            if (linkedSlot == slot)
            {
                *bucket = slot->Next;

                break;
            }

            // point to the next element so that, if it matches the slot to be modified, 
            // we can update its next pointer to the next of the slot being modified.
            bucket = &linkedSlot->Next;
        }

        int hash = value.GetHashCode();
        bucket_index = (uint)hash % Bucket.Capacity;
        bucket = &Bucket.Data[bucket_index];

        slot->Next = *bucket;
        slot->Value = value;
        slot->Hash = hash;

        *bucket = index;
    }

    // return ref readonly because the map is built based on the hash of the value, 
    // changing the value without assigning it to a new bucket would break the map.
    public readonly ref readonly T Get(uint index)
    {
        if (index >= Length)
            throw new IndexOutOfRangeException();

        return ref Slots[index].Value;
    }

    public readonly ref readonly T this[uint index] => ref Get(index);

    public readonly bool Contains(in T value)
    {
        int hash = value.GetHashCode();
        uint bucket_index = (uint)hash % Bucket.Capacity;
        uint index = Bucket.Data[bucket_index];

        while (true)
        {
            if (index == uint.MaxValue)
                return false;

            Slot<T>* slot = &Slots[index];

            if (slot->Hash == hash && slot->Value.Equals(value))
                return true;

            index = slot->Next;
        }
    }

    private void Resize(uint newCapacity)
    {
        Capacity = newCapacity;

        Bucket newBucket = new(Math.Max(1u, newCapacity / Bucket.Division), Bucket.Division);
        Slot<T>* newSlot = null;

        Init(ref newSlot, newCapacity, ref newBucket);

        if (Slots != null)
        {
            Buffer.MemoryCopy(
                Slots, newSlot,
                newCapacity * sizeof(Slot<T>),
                Length * sizeof(Slot<T>));
        }

        NativeMemory.Free(Bucket.Data);
        NativeMemory.Free(Slots);

        // remapping values in the bucket because its size was changed
        for (uint i = 0; i < Length; i++)
        {
            Slot<T>* slot = &newSlot[i];

            uint bucket_index = (uint)slot->Hash % newBucket.Capacity;
            uint* bucket = &newBucket.Data[bucket_index];

            slot->Next = *bucket;

            *bucket = i;
        }

        Bucket = newBucket;
        Slots = newSlot;
    }

    private static void Init(ref Slot<T>* slot, uint capacity, ref Bucket bucket)
    {
        slot = (Slot<T>*)NativeMemory.Alloc((nuint)(sizeof(Slot<T>) * capacity));
        bucket.Data = (uint*)NativeMemory.Alloc(sizeof(uint) * bucket.Capacity);

        // Fill the bucket values to maximum values, 
        // because the check for emptiness is performed using uint.MaxValue.
        NativeMemory.Fill(bucket.Data, sizeof(uint) * bucket.Capacity, 0xFF);
    }

    public void Dispose()
    {
        if (Bucket.Data != null)
        {
            NativeMemory.Free(Bucket.Data);
            Bucket.Data = null;
        }

        if (Slots != null)
        {
            NativeMemory.Free(Slots);
            Slots = null;
        }

        Bucket.Capacity = 0;
        Capacity = 0;
        Length = 0;
    }
}