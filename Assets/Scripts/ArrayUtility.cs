using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BezierCurve
{
    public static class ArrayUtility
    {
        public static void Add<T>(ref T[] array, T value)
        {
            System.Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = value;
        }

        public static void Insert<T>(ref T[] array, T value, int index)
        {
            var newArray = new T[array.Length + 1];
            for (int i = 0; i < index; i++)
            {
                newArray[i] = array[i];
            }
            newArray[index] = value;
            for (int i = index + 1; i < newArray.Length; i++)
            {
                newArray[i] = array[i - 1];
            }
            array = newArray;
        }

        public static void RemoveAt<T>(ref T[] array, int index)
        {
            var newArray = new T[array.Length - 1];
            for (int i = 0; i < index; i++)
            {
                newArray[i] = array[i];
            }
            for (int i = index; i < newArray.Length; i++)
            {
                newArray[i] = array[i + 1];
            }
            array = newArray;
        }

        public static bool IsNullOr0Length(System.Array array)
        {
            return array == null || array.Length == 0;
        }

        public static NativeArray<T> Pin<T>(T[] array, out ulong gcHandle) where T : struct
        {
            NativeArray<T> nativeArray;
            unsafe
            {
                void* ptr = UnsafeUtility.PinGCArrayAndGetDataAddress(array, out gcHandle);
                nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, array.Length, Allocator.None);
            }
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, AtomicSafetyHandle.Create());
            return nativeArray;
        }

        public static void Release(ulong gcHandle)
        {
            UnsafeUtility.ReleaseGCObject(gcHandle);
        }
    }
}