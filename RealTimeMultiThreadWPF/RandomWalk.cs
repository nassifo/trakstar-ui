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

namespace ChartDirectorSampleCode
{
    class RandomWalk
    {
        // The callback function to handle the generated data
        public delegate void DataHandler(double elapsedTime, double series0, double series1);
        private DataHandler handler;

        // Random number generator thread
        Thread pingThread;
        private bool stopThread;
        
        // The period of the data series in milliseconds. This random series implementation just use the 
        // windows timer for timing. In many computers, the default windows timer resolution is 1/64 sec,
        // or 15.6ms. This means the interval may not be exactly accurate.
        const int interval = 100;

        public RandomWalk(DataHandler handler)
        {
            this.handler = handler;
        }

        //
        // Start the random generator thread
        //        
        public void start()
        {
            if (null != pingThread)
                return;

            pingThread = new Thread(threadProc);
            pingThread.Start();            
        }

        //
        // Stop the random generator thread
        //
        public void stop()
        {
            stopThread = true;
            if (null != pingThread)
                pingThread.Join();
            pingThread = null;
            stopThread = false;
        }

        //
        // The random generator thread
        //
        void threadProc(object obj)
        {
            long currentTime = 0;
            long nextTime = 0;

            // Random walk variables
            Random rand = new Random(9);
            double series0 = 32;
            double series1 = 63;
            double upperLimit = 94;
            double scaleFactor = Math.Sqrt(interval * 0.1);

            // Variables to keep track of the timing
            Stopwatch timer = new Stopwatch();
            timer.Start();
                        
            while (!stopThread)
            {
                // Compute the next data value
                currentTime = timer.Elapsed.Ticks / 10000;

                if ((series0 = Math.Abs(series0 + (rand.NextDouble() - 0.5) * scaleFactor)) > upperLimit)
                    series0 = upperLimit * 2 - series0;
                if ((series1 = Math.Abs(series1 + (rand.NextDouble() - 0.5) * scaleFactor)) > upperLimit)
                    series1 = upperLimit * 2 - series1;

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

