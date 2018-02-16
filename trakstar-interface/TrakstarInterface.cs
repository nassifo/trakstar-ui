using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        }

        public async Task InitSystem()
        {
            await Task.Factory.StartNew(() =>
            {
                string error_message = String.Empty;

                #region Initialize hardware
                // Initialize the BIRD system
                int errorCode = InitializeBIRDSystem();
                if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS)
                {
                    error_message = errorHandler(errorCode);
                    throw new Exception(error_message);
                }

                // Retrieve Trakstar hardware configuration (such as number of sensors, xmtrs, etc)
                errorCode = GetBIRDSystemConfiguration(out tracker);
                if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS)
                {
                    error_message = errorHandler(errorCode);
                    throw new Exception(error_message);
                }

                // Retrieve sensor information (such as sensor ID, serial number, etc)
                pSensor = new SENSOR_CONFIGURATION[tracker.numberSensors];
                for (ushort i = 0; i < tracker.numberSensors; i++)
                {
                    errorCode = GetSensorConfiguration(i, out pSensor[i]);
                    if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS)
                    {
                        error_message = errorHandler(errorCode);
                        throw new Exception(error_message);
                    }
                }

                // Retrieve Xmtr configuration (such as serial number, etc)
                pXmtr = new TRANSMITTER_CONFIGURATION[tracker.numberTransmitters];
                for (ushort i = 0; i < tracker.numberTransmitters; i++)
                {
                    errorCode = GetTransmitterConfiguration(i, out pXmtr[i]);
                    if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS)
                    {
                        error_message = errorHandler(errorCode);
                        throw new Exception(error_message);
                    }

                }
                #endregion

                #region Set System Parameters
                double _samplingFrequency = samplingFrequency;
                errorCode = SetSystemParameter(SYSTEM_PARAMETER_TYPE.MEASUREMENT_RATE, ref _samplingFrequency, Marshal.SizeOf(samplingFrequency));
                if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS) errorHandler(errorCode);

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
                        break;
                    }
                }
                #endregion

                #region Set Sensor Parameters
                for (ushort i = 0; i < tracker.numberSensors; i++)
                {
                    DATA_FORMAT_TYPE type = DATA_FORMAT_TYPE.DOUBLE_POSITION_ANGLES_TIME_Q;
                    errorCode = SetSensorParameter(i, SENSOR_PARAMETER_TYPE.DATA_FORMAT, ref type, Marshal.SizeOf((int)type));
                    if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS) errorHandler(errorCode);
                }
                #endregion
            });

            _IsActive = true;
        }

        // Turns off the Xmtr
        public void TrakstarOff()
        {
            string error_message;

            // Turn off the transmitter using code -1
            short xMtrOff = -1;
            int errorCode = SetSystemParameter(SYSTEM_PARAMETER_TYPE.SELECT_TRANSMITTER, ref xMtrOff, Marshal.SizeOf(xMtrOff));
            if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS) {
                error_message = errorHandler(errorCode);
                throw new Exception(error_message);
            }

            if (_IsActive) _IsActive = false;
        }

        // Get Data array from trakstar
        public DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[] FetchData()
        {
            string error_message;
            DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[] records = new DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[tracker.numberSensors];

            int errorCode = GetSynchronousRecord(0xffff, records, Marshal.SizeOf(records[0]) * tracker.numberSensors);
            if (errorCode != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS)
            {
                error_message = errorHandler(errorCode);
                throw new Exception(error_message);
            }

            return records;
        }

        // Get Data Async (OBSOLETE)
        [Obsolete]
        public async Task<DOUBLE_POSITION_ANGLES_TIME_Q_RECORD[]> FetchDataAsync()
        {
            return await Task.Factory.StartNew(() => FetchData());
        }

        // Get sampling rate in ms
        public int GetSamplingRate()
        {
            return (int)((1 / samplingFrequency) * 1000);
        }

        // Get number of attached sensors
        public int GetNumberOfSensors()
        {
            return tracker.numberSensors;
        }

        public bool IsActive()
        {
            return _IsActive;
        }
        #endregion

        #region Private Members
        private SYSTEM_CONFIGURATION tracker;

        private SENSOR_CONFIGURATION[] pSensor;

        private TRANSMITTER_CONFIGURATION[] pXmtr;

        private bool _IsActive = false;
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

        public uint getSensorSerialNumberByIndex(int index)
        {
            if (index < pSensor.Length) return pSensor[index].serialNumber;

            return 0;
        }
        #endregion

        #region Private Methods
        // Turns a BIRD error code into an error message
        static private string errorHandler(int error)
        {
            // Not sure if 1024 is big enough size for the buffer, need to double check this eventually
            StringBuilder pBuffer = new StringBuilder(1024);

            while (error != (int)BIRD_ERROR_CODES.BIRD_ERROR_SUCCESS)
            {
                error = GetErrorText(error, pBuffer, pBuffer.Capacity, MESSAGE_TYPE.SIMPLE_MESSAGE);
                using (StreamWriter stream = new StreamWriter("log.txt", true))
                {
                    stream.WriteLine(DateTime.Now.ToString() + ": " + pBuffer.ToString());
                }
            }

            return pBuffer.ToString();
        }
        #endregion
    }
}
