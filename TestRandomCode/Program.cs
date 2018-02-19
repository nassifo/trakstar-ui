using System;
using System.Collections.Generic;
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
        Dictionary<int, string> test = new Dictionary<int, string>();
        test.Add(2, "hello");

        Console.WriteLine(test[1]);
        Console.ReadKey();
    }

}