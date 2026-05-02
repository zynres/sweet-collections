namespace unsafe_maps;

public unsafe interface IUnsafeMap<T> where T : unmanaged
{
    T* Data { get; set; }
    int Length { get; set; }
    int Capacity { get; set; }
}