// Copyright © 2026 Zynres.

using System.Runtime.InteropServices;

namespace Sweet.Collections.Unsafe.HashSet;

public unsafe struct UnsafeHashSet<T> : IDisposable where T : unmanaged
{
    public uint* Bucket;
    public Slot<T>* Slot;

    private uint bucketCapacity;
    private readonly byte division;

    public uint Length;
    public uint Capacity;

    public UnsafeHashSet(uint capacity, byte division = 2)
    {
        this.division = division;

        Capacity = capacity;
        bucketCapacity = Math.Max(1u, capacity / division);

        Init(ref Slot, capacity, ref Bucket, bucketCapacity);
    }

    public void Add(in T value)
    {
        if (Length >= Capacity)
            Resize(Capacity * 2);

        if (Constaint(in value))
            return;

        int hash = value.GetHashCode();
        uint bucket_index = (uint)hash % bucketCapacity;
        uint* bucket = &Bucket[bucket_index];

        Slot<T>* slot = &Slot[Length];

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

        Slot<T>* slot = &Slot[index];

        uint bucket_index = (uint)slot->Hash % bucketCapacity;
        uint* bucket = &Bucket[bucket_index];

        while (*bucket != uint.MaxValue)
        {
            Slot<T>* linkedSlot = &Slot[*bucket];

            if (linkedSlot == slot)
            {    
                *bucket = slot->Next;

                break;
            }

            bucket = &linkedSlot->Next;
        }

        int hash = value.GetHashCode();
        bucket_index = (uint)hash % bucketCapacity;
        bucket = &Bucket[bucket_index];

        slot->Next = *bucket;
        slot->Value = value;
        slot->Hash = hash;

        *bucket = index;
    }

    public readonly ref T Get(uint index)
    {
        if (index >= Length)
            throw new IndexOutOfRangeException();

        return ref Slot[index].Value;
    }

    public ref T Emplace(uint index)
    {
        if (index >= Capacity)
            throw new ArgumentOutOfRangeException();
            
        if (index >= Length)
            Length = index + 1;

        return ref Slot[index].Value;
    }

    public readonly bool Constaint(in T value)
    {
        int hash = value.GetHashCode();
        uint bucket_index = (uint)hash % bucketCapacity;

        uint* index = &Bucket[bucket_index];

        while(true)
        {
            if (*index == uint.MaxValue)
                return false;

            Slot<T>* slot = &Slot[*index];

            if (slot->Value.Equals(value))
                return true;

            index = &slot->Next;
        }
    }
    
    public readonly ref T this[uint index] => ref Get(index);

    private void Resize(uint newCapacity)
    {
        uint newBucketCapacity = Math.Max(1u, newCapacity / division);
        Capacity = newCapacity;

        Slot<T>* newSlot = null;
        uint* newBucket = null;

        Init(ref newSlot, newCapacity, ref newBucket, newBucketCapacity);

        Buffer.MemoryCopy(
            Slot, newSlot,
            newCapacity * sizeof(Slot<T>),
            Length * sizeof(Slot<T>));

        NativeMemory.Free(Bucket);
        NativeMemory.Free(Slot);

        for (uint i = 0; i < Length; i++)
        {
            Slot<T>* slot = &newSlot[i];

            uint bucket_index = (uint)slot->Hash % newBucketCapacity;
            uint* bucket = &newBucket[bucket_index];

            slot->Next = *bucket;

            *bucket = i;
        }

        Slot = newSlot;
        Bucket = newBucket;
        bucketCapacity = newBucketCapacity;
    }

    private static void Init(ref Slot<T>* slot, uint capacity, ref uint* bucket, uint bucketCapacity)
    {
        slot = (Slot<T>*)NativeMemory.Alloc((nuint)(sizeof(Slot<T>) * capacity));
        bucket = (uint*)NativeMemory.Alloc(sizeof(uint) * bucketCapacity);

        NativeMemory.Clear(slot, (nuint)sizeof(Slot<T>) * capacity);
        NativeMemory.Fill(bucket, sizeof(uint) * bucketCapacity, 0xFF);
    }

    public void Dispose()
    {
        if (Bucket != null)
        {
            NativeMemory.Free(Bucket);
            Bucket = null;
        }

        if (Slot != null)
        {
            NativeMemory.Free(Slot);
            Slot = null;
        }
    }
}