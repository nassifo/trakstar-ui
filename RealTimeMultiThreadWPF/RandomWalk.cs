///////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright 2016 Advanced Software Engineering Limited
//
// You may use and modify the code in this file in your application, provided the code and
// its modifications are used only in conjunction with ChartDirector. Usage of this software
// is subjected to the terms and condition of the ChartDirector license.
///////////////////////////////////////////////////////////////////////////////////////////////////   

using System;
using System.Threading;
using System.Diagnostics;
using TrakstarInterface;

namespace TrakstarGUI
{
    class RandomWalk
    {
        // The callback function to handle the generated data
        public delegate void DataHandler(double elapsedTime, double series0, double series1);
        private DataHandler handler;

        // Random number generator thread
        Thread pingThread;
        private bool stopThread;
        static public Trakstar bird = new Trakstar();

        // The period of the data series in milliseconds. This random series implementation just use the 
        // windows timer for timing. In many computers, the default windows timer resolution is 1/64 sec,
        // or 15.6ms. This means the interval may not be exactly accurate.
        int interval = (int)((1/bird.samplingFrequency)*1000);

        public RandomWalk(DataHandler handler)
        {
            this.handler = handler;
        }

        //
        // Start the thread
        //        
        public void start()
        {
            if (null != pingThread)
                return;

            pingThread = new Thread(threadProc);
            pingThread.Start();            
        }

        //
        // Stop the thread
        //
        public void stop()
        {
            stopThread = true;
            if (null != pingThread)
                pingThread.Join();
            pingThread = null;
            stopThread = false;
            bird.TrakstarOff();
        }

        //
        // The random generator thread
        //
        async void threadProc(object obj)
        {
            long currentTime = 0;
            long nextTime = 0;

            double series0 = 0;
            double series1 = 0;

            // Variables to keep track of the timing
            Stopwatch timer = new Stopwatch();
            timer.Start();
                        
            while (!stopThread)
            {
                // Compute the next data value
                currentTime = timer.Elapsed.Ticks / 10000;

                var records = await bird.FetchDataAsync();

                series0 = records[0].x;

                series1 = records[0].y;

                // Call the handler
                handler(currentTime / 1000.0, series0, series1);

                // Sleep until next walk
                if ((nextTime += interval) <= currentTime)
                    nextTime = currentTime + interval;

                Thread.Sleep((int)(nextTime - currentTime));
            }
        }
    }
}

