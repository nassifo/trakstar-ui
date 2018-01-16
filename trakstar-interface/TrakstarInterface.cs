using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TrakstarInterface
{
    public class Trakstar
    {
        // Todo: see if I can encapsulate the pinvoke methods in a cleaner way
        #region PInvoke Methods
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
        public static extern int SetSystemParameter(SYSTEM_PARAMETER_TYPE parameterType, ref short id, int bufferSize);

        [DllImport("ATC3DG64.DLL", EntryPoint = "SetSystemParameter")]
        public static extern int SetSystemParameter(SYSTEM_PARAMETER_TYPE parameterType, ref double val, int bufferSize);

        [DllImport("ATC3DG64.DLL", EntryPoint = "SetSystemParameter")]
        public static extern int SetSystemParameter(SYSTEM_PARAMETER_TYPE parameterType, ref byte val, int bufferSize);

        [DllImport("ATC3DG64.DLL", EntryPoint = "SetSensorParameter")]
        public static extern int SetSensorParameter(ushort sensorID, SENSOR_PARAMETER_TYPE parameterType, ref DATA_FORMAT_TYPE pBuffer, int bufferSize);

        [DllImport("ATC3DG64.DLL", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetSynchronousRecord(ushort sensorID, [Out] DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[] records, int recordSize);

        [DllImport("ATC3DG64.DLL")]
        public static extern int GetAsynchronousRecord(ushort sensorID, [Out] DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[] records, int recordSize);

        [DllImport("ATC3DG64.DLL")]
        public static extern ulong GetSensorStatus(ushort sensorID);
        
        #endregion

        #region Public Methods
        public Trakstar(double sampling_frequency = 100.0f)
        {
            // Set sampling frequency (default 100Hz)
            samplingFrequency = sampling_frequency;

            // Divider so we only get the data points corresponding to new samples
            decimationRate = 3;

            #region Initialize hardware
            // Initialize the BIRD system
            Console.WriteLine("Attempting to initialize trakstar system...");
            int errorCode = InitializeBIRDSystem();
            if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS)
            {
                Console.WriteLine("Error Code: " + errorCode);
                errorHandler(errorCode);
            }
            else
            {
                Console.WriteLine("Trakstar intialized successfully.");
            }

            // Retrieve Trakstar hardware configuration (such as number of sensors, xmtrs, etc)
            errorCode = GetBIRDSystemConfiguration(out tracker);
            if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS)
            {
                Console.WriteLine("Error Code: " + errorCode);
                errorHandler(errorCode);
            }
            else
            {
                Console.WriteLine("System configuration successfully retrieved.");
                Console.WriteLine("Tracker number of sensors: " + tracker.numberSensors);
                Console.WriteLine("Tracker number of transmitters: " + tracker.numberTransmitters);
            }

            // Retrieve sensor information (such as sensor ID, serial number, etc)
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

            // Retrieve Xmtr configuration (such as serial number, etc)
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
            #endregion

            /*
             * Sets the sampling rate of the device, decimation rate, turns on xmtr
             * to do: Check to see if we really do need the refs on the parameters going in like samplingFrequency etc
             */
            #region Set System Parameters
            Console.WriteLine("Setting system sampling rate to: " + samplingFrequency);
            double _samplingFrequency = samplingFrequency;
            errorCode = SetSystemParameter(SYSTEM_PARAMETER_TYPE.MEASUREMENT_RATE, ref _samplingFrequency, Marshal.SizeOf(samplingFrequency));
            if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS) errorHandler(errorCode);

            Console.WriteLine("Setting decimation rate.");
            byte _decimationRate = decimationRate;
            // Set decimation rate 
            errorCode = SetSystemParameter(SYSTEM_PARAMETER_TYPE.REPORT_RATE, ref _decimationRate, 2 * Marshal.SizeOf(decimationRate));
            if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS) errorHandler(errorCode);

            // Search for the first attached transmitter and turn it on
            for (short id = 0; id < tracker.numberTransmitters; id++)
            {
                if (pXmtr[id].attached == 1)
                {
                    errorCode = SetSystemParameter(SYSTEM_PARAMETER_TYPE.SELECT_TRANSMITTER, ref id, Marshal.SizeOf(id));
                    if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS) errorHandler(errorCode);
                    else Console.WriteLine("Turned on xmtr: " + pXmtr[id].serialNumber + " successfully! Press any key to continue..");
                    break;
                }
            }
            #endregion

            /*
             * Sets the sensor DATA_FORMAT_TYPE
             * To do: set it through another function instead of inside constructor
             */
            #region Set Sensor Parameters
            for (ushort i = 0; i < tracker.numberSensors; i++)
            {
                DATA_FORMAT_TYPE type = DATA_FORMAT_TYPE.DOUBLE_POSITION_ANGLES_TIME_Q;
                errorCode = SetSensorParameter(i, SENSOR_PARAMETER_TYPE.DATA_FORMAT, ref type, Marshal.SizeOf((int)type));
                if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS) errorHandler(errorCode);
                else Console.WriteLine("\nSetting data format of sensor " + i + " was successful.");
            }
            #endregion
        }

        // Turns off the Xmtr
        public void TrakstarOff()
        {
            // Turn off the transmitter using code -1
            short xMtrOff = -1;
            int errorCode = SetSystemParameter(SYSTEM_PARAMETER_TYPE.SELECT_TRANSMITTER, ref xMtrOff, Marshal.SizeOf(xMtrOff));
            if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS) errorHandler(errorCode);
            else Console.WriteLine("Turned off xmtr successfully!");
        }

        // Get a single data record
        public DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[] GetSyncRecord()
        {
            DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[] records = new DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[tracker.numberSensors];

            int errorCode = GetSynchronousRecord(0xffff, records, Marshal.SizeOf(records[0]) * tracker.numberSensors);
            if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS) errorHandler(errorCode);

            return records;
        }
        #endregion

        #region Private Members
        private SYSTEM_CONFIGURATION tracker;

        private SENSOR_CONFIGURATION[] pSensor;

        private TRANSMITTER_CONFIGURATION[] pXmtr;
        #endregion

        #region Public Members
        public double samplingFrequency
        {
            get; set;
        }

        public byte decimationRate
        {
            get; set;
        }

        public DATA_FORMAT_TYPE trackerDataFormat
        {
            get; set;
        }
        #endregion

        #region Private Methods
        // Turns a BIRD error code into an error message
        static private void errorHandler(int error)
        {
            // Not sure if 1024 is big enough size for the buffer, need to double check this eventually
            StringBuilder pBuffer = new StringBuilder(1024);

            while (error != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS)
            {
                error = GetErrorText(error, pBuffer, pBuffer.Capacity, MESSAGE_TYPE.VERBOSE_MESSAGE);
                using (StreamWriter stream = new StreamWriter("log.txt", true))
                {
                    stream.WriteLine(DateTime.Now.ToString() + ": " + pBuffer.ToString());
                }
            }
        }
        #endregion
    }
}
