using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TrakstarInterface;

public class Example
{
    static public Trakstar bird;
    public static void Main()
    {

        if (bird == null) Console.WriteLine("Bird is null");
        Console.ReadKey();
    }

    class CustomException : Exception
    {
        public CustomException(string message)
        {
            
        }
    }


}