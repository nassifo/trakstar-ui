using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TrakstarInterface
{
    public enum MESSAGE_TYPE
    {
        SIMPLE_MESSAGE,                         // short string describing error code
        VERBOSE_MESSAGE,                            // long string describing error code
    };

    public enum BIRD_ERROR_CODES
    {
        //	ERROR CODE DISPOSITION
        //    |		(Some error codes have been retired.
        //    |      The column below describes which codes 
        //	  |      have been retired and why. O = Obolete,
        //    V      I = handled internally)
        BIRD_ERROR_SUCCESS = 0,                 //00 < > No error	
        BIRD_ERROR_PCB_HARDWARE_FAILURE,        //01 < > indeterminate failure on PCB
        BIRD_ERROR_TRANSMITTER_EEPROM_FAILURE,  //02 <I> transmitter bad eeprom
        BIRD_ERROR_SENSOR_SATURATION_START,     //03 <I> sensor has gone into saturation
        BIRD_ERROR_ATTACHED_DEVICE_FAILURE,     //04 <O> either a sensor or transmitter reports bad
        BIRD_ERROR_CONFIGURATION_DATA_FAILURE,  //05 <O> device EEPROM detected but corrupt
        BIRD_ERROR_ILLEGAL_COMMAND_PARAMETER,   //06 < > illegal PARAMETER_TYPE passed to driver
        BIRD_ERROR_PARAMETER_OUT_OF_RANGE,      //07 < > PARAMETER_TYPE legal, but PARAMETER out of range
        BIRD_ERROR_NO_RESPONSE,                 //08 <O> no response at all from target card firmware
        BIRD_ERROR_COMMAND_TIME_OUT,            //09 < > time out before response from target board
        BIRD_ERROR_INCORRECT_PARAMETER_SIZE,    //10 < > size of parameter passed is incorrect
        BIRD_ERROR_INVALID_VENDOR_ID,           //11 <O> driver started with invalid PCI vendor ID
        BIRD_ERROR_OPENING_DRIVER,              //12 < > couldn't start driver
        BIRD_ERROR_INCORRECT_DRIVER_VERSION,    //13 < > wrong driver version found
        BIRD_ERROR_NO_DEVICES_FOUND,            //14 < > no BIRDs were found anywhere
        BIRD_ERROR_ACCESSING_PCI_CONFIG,        //15 < > couldn't access BIRDs config space
        BIRD_ERROR_INVALID_DEVICE_ID,           //16 < > device ID out of range
        BIRD_ERROR_FAILED_LOCKING_DEVICE,       //17 < > couldn't lock driver
        BIRD_ERROR_BOARD_MISSING_ITEMS,         //18 < > config space items missing
        BIRD_ERROR_NOTHING_ATTACHED,            //19 <O> card found but no sensors or transmitters attached
        BIRD_ERROR_SYSTEM_PROBLEM,              //20 <O> non specific system problem
        BIRD_ERROR_INVALID_SERIAL_NUMBER,       //21 <O> serial number does not exist in system
        BIRD_ERROR_DUPLICATE_SERIAL_NUMBER,     //22 <O> 2 identical serial numbers passed in set command
        BIRD_ERROR_FORMAT_NOT_SELECTED,         //23 <O> data format not selected yet
        BIRD_ERROR_COMMAND_NOT_IMPLEMENTED,     //24 < > valid command, not implemented yet
        BIRD_ERROR_INCORRECT_BOARD_DEFAULT,     //25 < > incorrect response to reading parameter
        BIRD_ERROR_INCORRECT_RESPONSE,          //26 <O> response received, but data,values in error
        BIRD_ERROR_NO_TRANSMITTER_RUNNING,      //27 < > there is no transmitter running
        BIRD_ERROR_INCORRECT_RECORD_SIZE,       //28 < > data record size does not match data format size
        BIRD_ERROR_TRANSMITTER_OVERCURRENT,     //29 <I> transmitter over-current detected
        BIRD_ERROR_TRANSMITTER_OPEN_CIRCUIT,    //30 <I> transmitter open circuit or removed
        BIRD_ERROR_SENSOR_EEPROM_FAILURE,       //31 <I> sensor bad eeprom
        BIRD_ERROR_SENSOR_DISCONNECTED,         //32 <I> previously good sensor has been removed
        BIRD_ERROR_SENSOR_REATTACHED,           //33 <I> previously good sensor has been reattached
        BIRD_ERROR_NEW_SENSOR_ATTACHED,         //34 <O> new sensor attached
        BIRD_ERROR_UNDOCUMENTED,                //35 <I> undocumented error code received from bird
        BIRD_ERROR_TRANSMITTER_REATTACHED,      //36 <I> previously good transmitter has been reattached
        BIRD_ERROR_WATCHDOG,                    //37 < > watchdog timeout
        BIRD_ERROR_CPU_TIMEOUT_START,           //38 <I> CPU ran out of time executing algorithm (start)
        BIRD_ERROR_PCB_RAM_FAILURE,             //39 <I> BIRD on-board RAM failure
        BIRD_ERROR_INTERFACE,                   //40 <I> BIRD PCI interface error
        BIRD_ERROR_PCB_EPROM_FAILURE,           //41 <I> BIRD on-board EPROM failure
        BIRD_ERROR_SYSTEM_STACK_OVERFLOW,       //42 <I> BIRD program stack overrun
        BIRD_ERROR_QUEUE_OVERRUN,               //43 <I> BIRD error message queue overrun
        BIRD_ERROR_PCB_EEPROM_FAILURE,          //44 <I> PCB bad EEPROM
        BIRD_ERROR_SENSOR_SATURATION_END,       //45 <I> Sensor has gone out of saturation
        BIRD_ERROR_NEW_TRANSMITTER_ATTACHED,    //46 <O> new transmitter attached
        BIRD_ERROR_SYSTEM_UNINITIALIZED,        //47 < > InitializeBIRDSystem not called yet
        BIRD_ERROR_12V_SUPPLY_FAILURE,          //48 <I > 12V Power supply not within specification
        BIRD_ERROR_CPU_TIMEOUT_END,             //49 <I> CPU ran out of time executing algorithm (end)
        BIRD_ERROR_INCORRECT_PLD,               //50 < > PCB PLD not compatible with this API DLL
        BIRD_ERROR_NO_TRANSMITTER_ATTACHED,     //51 < > No transmitter attached to this ID
        BIRD_ERROR_NO_SENSOR_ATTACHED,          //52 < > No sensor attached to this ID

        // new error codes added 2/27/03 
        // (Version 1,31,5,01)  multi-sensor, synchronized
        BIRD_ERROR_SENSOR_BAD,                  //53 < > Non-specific hardware problem
        BIRD_ERROR_SENSOR_SATURATED,            //54 < > Sensor saturated error
        BIRD_ERROR_CPU_TIMEOUT,                 //55 < > CPU unable to complete algorithm on current cycle
        BIRD_ERROR_UNABLE_TO_CREATE_FILE,       //56 < > Could not create and open file for saving setup
        BIRD_ERROR_UNABLE_TO_OPEN_FILE,         //57 < > Could not open file for restoring setup
        BIRD_ERROR_MISSING_CONFIGURATION_ITEM,  //58 < > Mandatory item missing from configuration file
        BIRD_ERROR_MISMATCHED_DATA,             //59 < > Data item in file does not match system value
        BIRD_ERROR_CONFIG_INTERNAL,             //60 < > Internal error in config file handler
        BIRD_ERROR_UNRECOGNIZED_MODEL_STRING,   //61 < > Board does not have a valid model string
        BIRD_ERROR_INCORRECT_SENSOR,            //62 < > Invalid sensor type attached to this board
        BIRD_ERROR_INCORRECT_TRANSMITTER,       //63 < > Invalid transmitter type attached to this board

        // new error code added 1/18/05
        // (Version 1.31.5.22) 
        //		multi-sensor, 
        //		synchronized-fluxgate, 
        //		integrating micro-sensor,
        //		flat panel transmitter
        BIRD_ERROR_ALGORITHM_INITIALIZATION,    //64 < > Flat panel algorithm initialization failed

        // new error code for multi-sync
        BIRD_ERROR_LOST_CONNECTION,             //65 < > USB connection has been lost
        BIRD_ERROR_INVALID_CONFIGURATION,       //66 < > Invalid configuration

        // VPD error code
        BIRD_ERROR_TRANSMITTER_RUNNING,         //67 < > TX running while reading/writing VPD

        BIRD_ERROR_MAXIMUM_VALUE = 0x7F         //	     ## value = number of error codes ##
    };



    public enum AGC_MODE_TYPE
    {
        TRANSMITTER_AND_SENSOR_AGC,     // Old style normal addressing mode
        SENSOR_AGC_ONLY                 // Old style extended addressing mode
    };

    public enum DEVICE_TYPES
    {
        STANDARD_SENSOR,                // 25mm standard sensor
        TYPE_800_SENSOR,                // 8mm sensor
        STANDARD_TRANSMITTER,           // TX for 25mm sensor
        MINIBIRD_TRANSMITTER,           // TX for 8mm sensor
        SMALL_TRANSMITTER,              // "compact" transmitter
        TYPE_500_SENSOR,                // 5mm sensor
        TYPE_180_SENSOR,                // 1.8mm microsensor
        TYPE_130_SENSOR,                // 1.3mm microsensor
        TYPE_TEM_SENSOR,                // 1.8mm, 1.3mm, 0.Xmm microsensors
        UNKNOWN_SENSOR,                 // default
        UNKNOWN_TRANSMITTER,            // default
        TYPE_800_BB_SENSOR,             // BayBird sensor
        TYPE_800_BB_STD_TRANSMITTER,    // BayBird standard TX
        TYPE_800_BB_SMALL_TRANSMITTER,  // BayBird small TX
        TYPE_090_BB_SENSOR              // Baybird 0.9 mm sensor
    };

    public enum TRANSMITTER_PARAMETER_TYPE
    {
        SERIAL_NUMBER_TX,       // attached transmitter's serial number
        REFERENCE_FRAME,        // structure of type DOUBLE_ANGLES_RECORD
        XYZ_REFERENCE_FRAME,    // boolean value to select/deselect mode
        VITAL_PRODUCT_DATA_TX,  // single byte parameter to be read/write from VPD section of xmtr EEPROM
        MODEL_STRING_TX,        // 11 byte null terminated character string
        PART_NUMBER_TX,         // 16 byte null terminated character string
        END_OF_TX_LIST
    };

    public enum SENSOR_PARAMETER_TYPE
    {
        DATA_FORMAT,            // enumerated constant of type DATA_FORMAT_TYPE
        ANGLE_ALIGN,            // structure of type DOUBLE_ANGLES_RECORD
        HEMISPHERE,             // enumerated constant of type HEMISPHERE_TYPE
        FILTER_AC_WIDE_NOTCH,   // boolean value to select/deselect filter
        FILTER_AC_NARROW_NOTCH, // boolean value to select/deselect filter
        FILTER_DC_ADAPTIVE,     // double value in range 0.0 (no filtering) to 1.0 (max)
        FILTER_ALPHA_PARAMETERS,// structure of type ADAPTIVE_PARAMETERS
        FILTER_LARGE_CHANGE,    // boolean value to select/deselect filter
        QUALITY,                // structure of type QUALITY_PARAMETERS
        SERIAL_NUMBER_RX,       // attached sensor's serial number
        SENSOR_OFFSET,          // structure of type DOUBLE_POSITION_RECORD
        VITAL_PRODUCT_DATA_RX,  // single byte parameter to be read/write from VPD section of sensor EEPROM
        VITAL_PRODUCT_DATA_PREAMP,  // single byte parameter to be read/write from VPD section of preamp EEPROM
        MODEL_STRING_RX,        // 11 byte null terminated character string
        PART_NUMBER_RX,         // 16 byte null terminated character string
        MODEL_STRING_PREAMP,    // 11 byte null terminated character string
        PART_NUMBER_PREAMP,     // 16 byte null terminated character string
        PORT_CONFIGURATION,     // enumerated constant of type PORT_CONFIGURATION_TYPE
        END_OF_RX_LIST
    };

    public enum BOARD_PARAMETER_TYPE
    {
        SERIAL_NUMBER_PCB,      // installed board's serial number
        BOARD_SOFTWARE_REVISIONS,   // BOARD_REVISIONS structure
        POST_ERROR_PCB,         // board POST_ERROR_PARAMETER
        DIAGNOSTIC_TEST_PCB,    // board DIAGNOSTIC_TEST_PARAMETER
        VITAL_PRODUCT_DATA_PCB, // single byte parameter to be read/write from VPD section of board EEPROM
        MODEL_STRING_PCB,       // 11 byte null terminated character string
        PART_NUMBER_PCB,        // 16 byte null terminated character string
        END_OF_PCB_LIST_BRD
    };

    public enum SYSTEM_PARAMETER_TYPE
    {
        SELECT_TRANSMITTER,     // short int equal to transmitterID of selected transmitter
        POWER_LINE_FREQUENCY,   // double value (range is hardware dependent)
        AGC_MODE,               // enumerated constant of type AGC_MODE_TYPE
        MEASUREMENT_RATE,       // double value (range is hardware dependent)
        MAXIMUM_RANGE,          // double value (range is hardware dependent)
        METRIC,                 // boolean value to select metric units for position
        VITAL_PRODUCT_DATA,     // single byte parameter to be read/write from VPD section of board EEPROM
        POST_ERROR,             // system (board 0) POST_ERROR_PARAMETER
        DIAGNOSTIC_TEST,        // system (board 0) DIAGNOSTIC_TEST_PARAMETER
        REPORT_RATE,            // single byte 1-127			
        COMMUNICATIONS_MEDIA,   // Media structure
        LOGGING,                // Boolean
        RESET,                  // Boolean
        AUTOCONFIG,             // BYTE 1-127
        AUXILIARY_PORT,         // structure of type AUXILIARY_PORT_PARAMETERS
        COMMUTATION_MODE,       // boolean value to select commutation of sensor data for interconnect pickup rejection
        END_OF_LIST             // end of list place holder
    };

    public enum FILTER_OPTION
    {
        NO_FILTER,
        DEFAULT_FLOCK_FILTER
    };

    public enum HEMISPHERE_TYPE
    {
        FRONT,
        BACK,
        TOP,
        BOTTOM,
        LEFT,
        RIGHT
    };

    public enum PORT_CONFIGURATION_TYPE
    {
        DOF_NOT_ACTIVE,                 // No sensor associated with this ID, e.g. ID 5 on a 6DOF port
        DOF_6_XYZ_AER,                  // 6 degrees of freedom
        DOF_5_XYZ_AE,                   // 5 degrees of freedom, no roll
    };

    public enum DATA_FORMAT_TYPE
    {
        NO_FORMAT_SELECTED = 0,

        // SHORT (integer) formats
        SHORT_POSITION,
        SHORT_ANGLES,
        SHORT_MATRIX,
        SHORT_QUATERNIONS,
        SHORT_POSITION_ANGLES,
        SHORT_POSITION_MATRIX,
        SHORT_POSITION_QUATERNION,

        // DOUBLE (floating point) formats
        DOUBLE_POSITION,
        DOUBLE_ANGLES,
        DOUBLE_MATRIX,
        DOUBLE_QUATERNIONS,
        DOUBLE_POSITION_ANGLES,     // system default
        DOUBLE_POSITION_MATRIX,
        DOUBLE_POSITION_QUATERNION,

        // DOUBLE (floating point) formats with time stamp appended
        DOUBLE_POSITION_TIME_STAMP,
        DOUBLE_ANGLES_TIME_STAMP,
        DOUBLE_MATRIX_TIME_STAMP,
        DOUBLE_QUATERNIONS_TIME_STAMP,
        DOUBLE_POSITION_ANGLES_TIME_STAMP,
        DOUBLE_POSITION_MATRIX_TIME_STAMP,
        DOUBLE_POSITION_QUATERNION_TIME_STAMP,

        // DOUBLE (floating point) formats with time stamp appended and quality #
        DOUBLE_POSITION_TIME_Q,
        DOUBLE_ANGLES_TIME_Q,
        DOUBLE_MATRIX_TIME_Q,
        DOUBLE_QUATERNIONS_TIME_Q,
        DOUBLE_POSITION_ANGLES_TIME_Q,
        DOUBLE_POSITION_MATRIX_TIME_Q,
        DOUBLE_POSITION_QUATERNION_TIME_Q,

        // These DATA_FORMAT_TYPE codes contain every format in a single structure
        SHORT_ALL,
        DOUBLE_ALL,
        DOUBLE_ALL_TIME_STAMP,
        DOUBLE_ALL_TIME_STAMP_Q,
        DOUBLE_ALL_TIME_STAMP_Q_RAW,    // this format contains a raw data matrix and
                                        // is for factory use only...

        // DOUBLE (floating point) formats with time stamp appended, quality # and button
        DOUBLE_POSITION_ANGLES_TIME_Q_BUTTON,
        DOUBLE_POSITION_MATRIX_TIME_Q_BUTTON,
        DOUBLE_POSITION_QUATERNION_TIME_Q_BUTTON,

        // New types for button and wrapper
        DOUBLE_POSITION_ANGLES_MATRIX_QUATERNION_TIME_Q_BUTTON,

        MAXIMUM_FORMAT_CODE
    };

    public enum BOARD_TYPES
    {
        ATC3DG_MEDSAFE,                 // Standalone, DSP, 4 sensor
        PCIBIRD_STD1,                   // single standard sensor
        PCIBIRD_STD2,                   // dual standard sensor
        PCIBIRD_8mm1,                   // single 8mm sensor
        PCIBIRD_8mm2,                   // dual 8mm sensor
        PCIBIRD_2mm1,                   // single 2mm sensor (microsensor)
        PCIBIRD_2mm2,                   // dual 2mm sensor (microsensor)
        PCIBIRD_FLAT,                   // flat transmitter, 8mm
        PCIBIRD_FLAT_MICRO1,            // flat transmitter, single TEM sensor (all types)
        PCIBIRD_FLAT_MICRO2,            // flat transmitter, dual TEM sensor (all types)
        PCIBIRD_DSP4,                   // Standalone, DSP, 4 sensor
        PCIBIRD_UNKNOWN,                // default
        ATC3DG_BB                       // BayBird
    };


    // STRUCTURES
    public struct SYSTEM_CONFIGURATION
    {
        public double measurementRate;
        public double powerLineFrequency;
        public double maximumRange;
        public AGC_MODE_TYPE agcMode;
        public int numberBoards;
        public int numberSensors;
        public int numberTransmitters;
        public int transmitterIDRunning;
        public int metric;
    };

    public struct SENSOR_CONFIGURATION
    {
        public uint serialNumber;
        public ushort boardNumber;
        public ushort channelNumber;
        public DEVICE_TYPES type;
        public int attached;
    };

    public struct TRANSMITTER_CONFIGURATION
    {
        public uint serialNumber;
        public ushort boardNumber;
        public ushort channelNumber;
        public DEVICE_TYPES type;
        public int attached;
    };

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DOUBLE_POSITION_ANGLES_TIME_Q_RECORD
    {
        public double x;
        
        public double y;
      
        public double z;
        
        public double a;
      
        public double e;
        
        public double r;
        
        public double time;
       
        public ushort quality;
    };
}
