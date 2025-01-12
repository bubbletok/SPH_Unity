using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public struct TestStruct
{
    public int id;

    public int2[] array;
    //public NativeArray<int2> array;

    /*public bool Equals(TestStruct other)
    {
        return id.Equals(other.id);
    }

    public void Dispose()
    {
        if (array.IsCreated) array.Dispose();
    }*/

    public void DoWork()
    {
        int len = array.Length;
        for (int i = 0; i < len; i++)
        {
            array[i] *= array[i];
        }
    }

    //public bool IsCreated => array.IsCreated;
}

public class Test : MonoBehaviour
{
    public int totalSize;

    private void Awake()
    {
        print("Task Start");
        Task.Run(() => TestFunc());
        print("Task End");
    }

    void TestFunc()
    {
        print("TestFunc Start");
        try
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            int2[] pos = new int2[totalSize];
            for (int i = 0; i < totalSize; i++)
            {
                pos[i] = new int2(i * i - 1, i * i + 1);
            }

            var testStruct = new TestStruct()
            {
                array = pos
            };

            testStruct.DoWork();
            watch.Stop();
            print($"Elapsed Time: {watch.ElapsedMilliseconds}ms");
            //if (testStruct.IsCreated) testStruct.Dispose();
        }
        catch (Exception e)
        {
            Debug.LogError($"TestFunc could not be completed due to {e}");
        }

        print("TestFunc End");
        //if (pos.IsCreated) pos.Dispose();
    }
}