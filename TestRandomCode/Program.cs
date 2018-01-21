using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TrakstarInterface;

public class Example
{
    public static void Main()
    {
        Trakstar bird = new Trakstar("hello");

        int count = 0;

        while (count < 1000)
        {
            var recordsTask = bird.FetchDataAsync();
            recordsTask.ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    var records = task.Result;

                    foreach(var record in records)
                    {
                        Console.WriteLine("X: " + record.x + " Y: " + record.y);
                    }
                }
                else
                {
                    Console.WriteLine("Exception occured.");
                }
            });
            Console.WriteLine(count);
            count++;
        }

        Console.WriteLine("Finished.");
        Console.ReadKey();
        bird.TrakstarOff();
        Console.ReadKey();
    }





}