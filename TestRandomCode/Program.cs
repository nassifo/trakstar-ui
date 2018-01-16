using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TrakstarInterface;

public class Example
{
    public static void Main()
    {
        Trakstar J = new Trakstar();

        var watch = System.Diagnostics.Stopwatch.StartNew();

        int delay = 1;
        var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        var listener = Task.Factory.StartNew(() =>
        {
            while (true)
            {
                //poll HW
                Task. J.GetSyncRecord();
                Thread.Sleep(delay);
                if (token.IsCancellationRequested)
                    break;
            }

            //cleanup
        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;

        Console.WriteLine("Total time (ms):" + elapsedMs);
        J.TrakstarOff();
    }
}