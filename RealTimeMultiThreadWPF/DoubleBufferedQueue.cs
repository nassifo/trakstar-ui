///////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright 2016 Advanced Software Engineering Limited
//
// You may use and modify the code in this file in your application, provided the code and
// its modifications are used only in conjunction with ChartDirector. Usage of this software
// is subjected to the terms and condition of the ChartDirector license.
///////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace ChartDirectorSampleCode
{
    public class DoubleBufferedQueue<T>
    {
        private T[] buffer;
        private T[] buffer0;
        private T[] buffer1;
        private int bufferLen;
        private object mutex = new object();

        public DoubleBufferedQueue(int bufferSize)
        {
            buffer0 = buffer = new T[bufferSize];
            buffer1 = new T[bufferSize];
        }
        public DoubleBufferedQueue() : this(10000)
        {
        }

        //
        // Add an item to the queue. Returns true if successful, false if the buffer is full.
        //
        public bool put(T datum)
        {
            lock(mutex)
            {
                bool canWrite = bufferLen < buffer.Length;
                if (canWrite) buffer[bufferLen++] = datum;
                return canWrite;
            }
	    }

        //
        // Get all the items in the queue.
        //
        public ArraySegment<T> get()
        {
            lock(mutex)
            {
                ArraySegment<T> ret = new ArraySegment<T>(buffer, 0, bufferLen);
                buffer = (buffer == buffer0) ? buffer1 : buffer0;
                bufferLen = 0;
                return ret;
            }
        }
    }
}
