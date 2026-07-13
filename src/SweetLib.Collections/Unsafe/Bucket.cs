namespace SweetLib.Collections.Unsafe;

public unsafe struct Bucket
{
    public uint* Data;
    public uint Capacity;
    public byte Division;

    public Bucket(uint capacity, byte division)
    {
        Capacity = capacity;
        Division = division;
    }
}