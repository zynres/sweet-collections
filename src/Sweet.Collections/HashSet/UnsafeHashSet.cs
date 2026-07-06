using System.Runtime.InteropServices;

namespace Sweet.Collections.HashSet;

public unsafe struct UnsafeHashSet<T> : IDisposable where T : unmanaged
{
    public uint?* Bucket;
    public Slot<T>* Slot;

    private uint bucketCapacity;
    private readonly byte division;

    public uint Length;
    public uint Capacity;

    public UnsafeHashSet(uint capacity, byte division = 2)
    {
        this.division = division;

        Capacity = capacity;
        bucketCapacity = capacity / division;

        Slot = (Slot<T>*)NativeMemory.Alloc((nuint)(sizeof(Slot<T>) * capacity));
        Bucket = (uint?*)NativeMemory.Alloc(sizeof(uint) * bucketCapacity);

        NativeMemory.Clear(Slot, (nuint)sizeof(Slot<T>) * capacity);
        NativeMemory.Clear(Bucket, sizeof(uint) * bucketCapacity);
    }

    public void Add(in T value)
    {
        if (Length >= Capacity)
            Resize(Capacity * 2);

        int hash = value.GetHashCode();
        uint bucket_index = (uint)hash % bucketCapacity;
        uint?* bucket = &Bucket[bucket_index];

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
        uint?* bucket = &Bucket[bucket_index];

        while (*bucket != null)
        {
            Slot<T>* linkedSlot = &Slot[bucket->Value];

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

        uint?* index = &Bucket[bucket_index];

        while(true)
        {
            if (*index == null)
                return false;

            Slot<T>* slot = &Slot[index->Value];

            if (slot->Value.Equals(value))
                return true;

            index = &slot->Next;
        }
    }
    
    public readonly ref T this[uint index] => ref Get(index);

    private void Resize(uint newCapacity)
    {
        uint newBucketCapacity = newCapacity / division;
        Capacity = newCapacity;

        Slot<T>* newSlot = (Slot<T>*)NativeMemory.Alloc((nuint)(sizeof(Slot<T>) * newCapacity));
        uint?* newBucket = (uint?*)NativeMemory.Alloc(sizeof(uint) * newBucketCapacity);

        NativeMemory.Clear(newSlot, (nuint)sizeof(Slot<T>) * newCapacity);
        NativeMemory.Clear(newBucket, sizeof(uint) * newBucketCapacity);

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
            uint?* bucket = &newBucket[bucket_index];

            slot->Next = *bucket;

            *bucket = i;
        }

        Slot = newSlot;
        Bucket = newBucket;
        bucketCapacity = newBucketCapacity;
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