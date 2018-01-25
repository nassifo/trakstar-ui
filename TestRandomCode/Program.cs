using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TrakstarInterface;

public class Example
{
    static public Trakstar bird;
    static void Main()
    {
        Task.Run(async () =>
       {
           bird = new Trakstar();

           Console.WriteLine("Init bird...");

           await bird.InitSystem();

           Console.WriteLine("finished init bird, running test");

           double[] buffer = new double[500];

           Stopwatch timer = new Stopwatch();

           timer.Start();

           for (int j = 0; j < 10000; j++)
           {
               DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[] records = new DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[8];

               records = bird.FetchData();

               shiftData(buffer, bird.getCoordinateFromRecords(records, 0, 0));
           }

           timer.Stop();

           long elapsed = timer.ElapsedMilliseconds;

           Console.WriteLine("elapsed time with array.copy: " + elapsed);

           timer.Reset();

           double[] buffers = new double[500];

           timer.Start();

           for (int j = 0; j < 10000; j++)
           {
               DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[] records = new DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[8];

               records = bird.FetchData();

               shiftDataS(buffers, bird.getCoordinateFromRecords(records, 0, 0));
           }

           timer.Stop();

           elapsed = timer.ElapsedMilliseconds;

           Console.WriteLine("elapsed time with slow shift: " + elapsed);

           bird.TrakstarOff();
           Console.ReadKey();
       }).GetAwaiter().GetResult();
    }

    private static void shiftDataS<T>(T[] data, T newValue)
    {
         
           for (int i = 1; i < data.Length; ++i)
               data[i - 1] = data[i];
           data[data.Length - 1] = newValue;
           
    }

    private static void shiftData<T>(T[] data, T newValue)
    {
        /*   
           for (int i = 1; i < data.Length; ++i)
               data[i - 1] = data[i];
           data[data.Length - 1] = newValue;
           */

        Array.Copy(data, 1, data, 0, data.Length - 1);

        data[data.Length - 1] = newValue;
    }


}