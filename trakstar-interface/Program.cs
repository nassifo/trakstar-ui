using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TrakstarInterface
{
    class Program
    {
        // Calling in the functions from DLL
        [DllImport("ATC3DG64.DLL")]
        public static extern int InitializeBIRDSystem();

        [DllImport("ATC3DG64.DLL")]
        public static extern int GetErrorText(int errorCode, StringBuilder pBuffer, int bufferSize, MESSAGE_TYPE type);

        [DllImport("ATC3DG64.DLL", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetBIRDSystemConfiguration(out SYSTEM_CONFIGURATION tracker);
        [DllImport("ATC3DG64.DLL")]
        public static extern int GetSensorConfiguration(ushort sensorID, out SENSOR_CONFIGURATION sensorConfiguration);
        [DllImport("ATC3DG64.DLL")]
        public static extern int GetTransmitterConfiguration(ushort transmitterID, out TRANSMITTER_CONFIGURATION sensorConfiguration);

        [DllImport("ATC3DG64.DLL", EntryPoint = "SetSystemParameter")]
        public static extern int SetSystemParameterShort(SYSTEM_PARAMETER_TYPE parameterType, ref short id, int bufferSize);

        [DllImport("ATC3DG64.DLL", EntryPoint = "SetSystemParameter")]
        public static extern int SetSystemParameterDouble(SYSTEM_PARAMETER_TYPE parameterType, ref double val, int bufferSize);

        [DllImport("ATC3DG64.DLL", EntryPoint = "SetSystemParameter")]
        public static extern int SetSystemParameterByte(SYSTEM_PARAMETER_TYPE parameterType, ref byte val, int bufferSize);

        [DllImport("ATC3DG64.DLL", EntryPoint = "SetSensorParameter")]
        public static extern int SetSensorParameterData(ushort sensorID, SENSOR_PARAMETER_TYPE parameterType, ref DATA_FORMAT_TYPE pBuffer, int bufferSize);

        [DllImport("ATC3DG64.DLL", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetSynchronousRecord(ushort sensorID, [Out] DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[] records, int recordSize);

        [DllImport("ATC3DG64.DLL")]
        public static extern int GetAsynchronousRecord(ushort sensorID, [Out] DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[] records, int recordSize);

        [DllImport("ATC3DG64.DLL")]
        public static extern ulong GetSensorStatus(ushort sensorID);
        static void Main(string[] args)
        {
            int maxRecords = 10000;
            double rate = 100.0f; // Sampling rate in Hz
            byte decimationRate = 3;

            Stopwatch stopWatch = new Stopwatch();

            Console.WriteLine("Attempting to initialize trakstar system...");
            int errorCode = InitializeBIRDSystem();

            if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS)
            {
                Console.WriteLine("Error Code: " + errorCode);
                errorHandler(errorCode);
            }

            Console.WriteLine("Found trakstar!" + MESSAGE_TYPE.SIMPLE_MESSAGE);

            // Check if these struct layouts need to be sequential ------------------------------------------------------------
            SYSTEM_CONFIGURATION tracker;

            SENSOR_CONFIGURATION[] pSensor;

            TRANSMITTER_CONFIGURATION[] pXmtr;

            Console.WriteLine("Setup configuration classes successfully.");

            errorCode = GetBIRDSystemConfiguration(out tracker);
            if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS)
            {
                Console.WriteLine("Error Code: " + errorCode);
                errorHandler(errorCode);
            }
            else
            {
                Console.WriteLine("No errors getting system configuration");
                Console.WriteLine("Tracker number of sensors: " + tracker.numberSensors);
                Console.WriteLine("Tracker number of transmitters: " + tracker.numberTransmitters);
            }

            pSensor = new SENSOR_CONFIGURATION[tracker.numberSensors];

            for (ushort i = 0; i < tracker.numberSensors; i++)
            {
                errorCode = GetSensorConfiguration(i, out pSensor[i]);
                if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS)
                {
                    Console.WriteLine("Sensor " + i + " configuration failed.");
                    errorHandler(errorCode);
                }
                else
                {
                    Console.WriteLine("Sensor " + i + "= attached:" + pSensor[i].attached + " channel num: " + pSensor[i].channelNumber + " board num: " + pSensor[i].boardNumber + " serial num: " + pSensor[i].serialNumber.ToString());
                }
            }

            Console.WriteLine("Finished collecting sensor configuration stage, getting xmtr info now");

            pXmtr = new TRANSMITTER_CONFIGURATION[tracker.numberTransmitters];
            for (ushort i = 0; i < tracker.numberTransmitters; i++)
            {
                errorCode = GetTransmitterConfiguration(i, out pXmtr[i]);
                if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS)
                {
                    Console.WriteLine("Transmitter " + i + " configuration failed.");
                    errorHandler(errorCode);
                }
                else
                {
                    Console.WriteLine("Sensor " + i + "= attached:" + pXmtr[i].attached + " channel num: " + pXmtr[i].channelNumber + " board num: " + pXmtr[i].boardNumber + " serial num: " + pXmtr[i].serialNumber.ToString());
                }
            }

            Console.WriteLine("Setting system rate.");
            // Assuming system was init correctly and sensors and transmitters are found correctly, we can start collecting data:
            // Set system rate
            errorCode = SetSystemParameterDouble(SYSTEM_PARAMETER_TYPE.MEASUREMENT_RATE, ref rate, Marshal.SizeOf(rate));
            if(errorCode!= (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS) errorHandler(errorCode);

            Console.WriteLine("Setting decimation rate.");
            // Set decimation rate 
            errorCode = SetSystemParameterByte(SYSTEM_PARAMETER_TYPE.REPORT_RATE, ref decimationRate, 2*Marshal.SizeOf(decimationRate));
            if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS) errorHandler(errorCode);

            // Search for the first attached transmitter and turn it on
            for (short id = 0; id < tracker.numberTransmitters; id++)
            {
                if (pXmtr[id].attached == 1)
                {
                    errorCode = SetSystemParameterShort(SYSTEM_PARAMETER_TYPE.SELECT_TRANSMITTER, ref id, Marshal.SizeOf(id));
                    if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS) errorHandler(errorCode);
                    else Console.WriteLine("Turned on xmtr: " + pXmtr[id].serialNumber + " successfully! Press any key to continue..");
                    break;
                }
            }
            
            Console.ReadKey();

            // Set the data format type for each attached sensor.
            for (ushort i = 0; i < tracker.numberSensors; i++)
            {
                DATA_FORMAT_TYPE type = DATA_FORMAT_TYPE.DOUBLE_POSITION_ANGLES_TIME_Q;
                errorCode = SetSensorParameterData(i, SENSOR_PARAMETER_TYPE.DATA_FORMAT, ref type, Marshal.SizeOf((int)type));
                if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS) errorHandler(errorCode);
                else Console.WriteLine("\nSetting data format of sensor " + i + " was successful.");
            }

            Console.WriteLine("Press any key to start record");
            Console.ReadKey();

            StreamWriter outfile = new StreamWriter("data.txt", append: false);

            DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[] records = new DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[tracker.numberSensors];
            
            int count = 0;
            stopWatch.Start();

            while (count<maxRecords)
            {
                count++;

                errorCode = GetSynchronousRecord(0xffff, records, Marshal.SizeOf(records[0]) * tracker.numberSensors);
                if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS)
                {
                    errorHandler(errorCode);
                }
                
                // scan the sensors and request a record if the sensor is physically attached
                for (ushort sensorID = 0; sensorID < 1; sensorID++)
                {
                    // get the status of the last data record
                    // only report the data if everything is okay
                    ulong status = GetSensorStatus(sensorID);
                    
                    if (status == 0x00000000)
                    {
                        /*
                            outfile.WriteLine("ID:" + sensorID +
                            "    coordinates: x:" +
                            records[sensorID].x + " y: " +
                            records[sensorID].y + " z: " +
                            records[sensorID].z + " a: " +
                            records[sensorID].a + " e: " +
                            records[sensorID].e + " r: " +
                            records[sensorID].r + " time: " +
                            records[sensorID].time);
                            */

                        outfile.WriteLine(count + ", " + sensorID + ", " + records[sensorID].x + ", " + records[sensorID].y + ", " + records[sensorID].z + ", ");
                    }
                }
                
                Console.WriteLine("Data point: " + count);

            }
            stopWatch.Stop();

            TimeSpan ts = stopWatch.Elapsed;
            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("\n\nRunTime " + elapsedTime);

            #region Disposal Block/ Closing Block
            // Turn off the transmitter using code -1
            short xMtrOff = -1;
            errorCode = SetSystemParameterShort(SYSTEM_PARAMETER_TYPE.SELECT_TRANSMITTER, ref xMtrOff, Marshal.SizeOf(xMtrOff));
            if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS) errorHandler(errorCode);
            else Console.WriteLine("Turned off xmtr successfully!");

            // Close file
            outfile.Close();
            #endregion
            Console.WriteLine("\nagc mode: " + tracker.agcMode);
            Console.ReadKey(); 
        }

        static private void errorHandler(int error)
        {
            StringBuilder pBuffer = new StringBuilder(1024);

            while (error != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS)
            {
                error = GetErrorText(error, pBuffer, pBuffer.Capacity, MESSAGE_TYPE.SIMPLE_MESSAGE);
                Console.WriteLine(pBuffer.ToString());
            }
        }
    }
}
