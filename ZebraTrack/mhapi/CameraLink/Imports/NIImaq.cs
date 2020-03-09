using System.Runtime.InteropServices;
using System.Text;
using System;

namespace MHApi.CameraLink.Imports
{

    #region Enumerations

    public enum ImaqStatus :uint
    {
        IMG_ERR_GOOD = 0,
        IMG_ERR_NOT_SUPPORTED = (0xBFF60000 + 0x0001),  // function not implemented
        IMG_ERR_SYSTEM_MEMORY_FULL = (0xBFF60000 + 0x0003),  // could not allocate memory in user mode =(calloc failed),
        IMG_ERR_BUFFER_SIZE_TOO_SMALL = (0xBFF60000 + 0x000E),  // buffer size used is too small for minimum acquisition frame
        IMG_ERR_BUFFER_LIST_NOT_LOCKED = (0xBFF60000 + 0x0013),  // buffer list is not locked
        IMG_ERR_BAD_INTERFACE_FILE = (0xBFF60000 + 0x0015),  // bad interface
        IMG_ERR_BAD_USER_RECT = (0xBFF60000 + 0x0017),  // bad region of interest; check width, heigh, rowpixels, and scaling
        IMG_ERR_BAD_CAMERA_FILE = (0xBFF60000 + 0x0018),  // bad camera file =(check syntax),
        IMG_ERR_NO_BUFFERS_CONFIGURED = (0xBFF60000 + 0x001A),  // invalid action - no buffers configured for session
        IMG_ERR_BAD_BUFFER_LIST_FINAL_COMMAND = (0xBFF60000 + 0x001B),  // buffer list does not contain a valid final command
        IMG_ERR_BAD_BUFFER_LIST_COMMAND = (0xBFF60000 + 0x001C),  // buffer list does contains an invalid command
        IMG_ERR_BAD_BUFFER_POINTER = (0xBFF60000 + 0x001D),  // a buffer list buffer is null
        IMG_ERR_BOARD_NOT_RUNNING = (0xBFF60000 + 0x001E),  // no acquisition in progress
        IMG_ERR_VIDEO_LOCK = (0xBFF60000 + 0x001F),  // can't get video lock
        IMG_ERR_BOARD_RUNNING = (0xBFF60000 + 0x0021),  // can't perform request - acquisition in progress
        IMG_ERR_TIMEOUT = (0xBFF60000 + 0x0022),  // wait timed out - acquisition not complete
        IMG_ERR_ZERO_BUFFER_SIZE = (0xBFF60000 + 0x0024),  // zero buffer size - no bytes filled
        IMG_ERR_NO_INTERFACE_FOUND = (0xBFF60000 + 0x0027),  // no interface found
        IMG_ERR_FIFO_OVERFLOW = (0xBFF60000 + 0x002C),  // fifo overflow caused acquisition to halt
        IMG_ERR_MEMORY_PAGE_LOCK_FAULT = (0xBFF60000 + 0x002D),  // memory lock error - the system could not page lock the buffer=(s),
        IMG_ERR_BAD_CLOCK_FREQUENCY = (0xBFF60000 + 0x0038),  // Bad frequency values
        IMG_ERR_BAD_CAMERA_TYPE = (0xBFF60000 + 0x0034),  // Bad camera type - =(Not a NTSC or PAL),
        IMG_ERR_HARDWARE_NOT_CAPABLE = (0xBFF60000 + 0x003A),  // Hardware not capable of supporting this
        IMG_ERR_ATTRIBUTE_NOT_SETTABLE = (0xBFF60000 + 0x0044),  // non settable attribute
        IMG_ERR_ONBOARD_MEMORY_FULL = (0xBFF60000 + 0x004D),  // not enough onboard memory to perform operation
        IMG_ERR_BUFFER_NOT_RELEASED = (0xBFF60000 + 0x0051),  // Already called ExamineBuffer once.  Call ReleaseBuffer.
        IMG_ERR_BAD_LUT_TYPE = (0xBFF60000 + 0x0052),  // Invalid LUT type
        IMG_ERR_ATTRIBUTE_NOT_READABLE = (0xBFF60000 + 0x0053),  // non readable attribute
        IMG_ERR_BOARD_NOT_SUPPORTED = (0xBFF60000 + 0x0054),  // This version of the driver doesn't support the board.
        IMG_ERR_BAD_FRAME_FIELD = (0xBFF60000 + 0x0055),  // The value for frame/field was invalid.
        IMG_ERR_INVALID_ATTRIBUTE = (0xBFF60000 + 0x0056),  // The requested attribute is invalid.
        IMG_ERR_BAD_LINE_MAP = (0xBFF60000 + 0x0057),  // The line map is invalid
        IMG_ERR_BAD_CHANNEL = (0xBFF60000 + 0x0059),  // The requested channel is invalid.
        IMG_ERR_BAD_CHROMA_FILTER = (0xBFF60000 + 0x005A),  // The value for the anti-chrominance filter is invalid.
        IMG_ERR_BAD_SCALE = (0xBFF60000 + 0x005B),  // The value for scaling is invalid.
        IMG_ERR_BAD_TRIGGER_MODE = (0xBFF60000 + 0x005D),  // The value for trigger mode is invalid.
        IMG_ERR_BAD_CLAMP_START = (0xBFF60000 + 0x005E),  // The value for clamp start is invalid.
        IMG_ERR_BAD_CLAMP_STOP = (0xBFF60000 + 0x005F),  // The value for clamp stop is invalid.
        IMG_ERR_BAD_BRIGHTNESS = (0xBFF60000 + 0x0060),  // The brightness level is out of range
        IMG_ERR_BAD_CONTRAST = (0xBFF60000 + 0x0061),  // The constrast  level is out of range
        IMG_ERR_BAD_SATURATION = (0xBFF60000 + 0x0062),  // The saturation level is out of range
        IMG_ERR_BAD_TINT = (0xBFF60000 + 0x0063),  // The tint level is out of range
        IMG_ERR_BAD_HUE_OFF_ANGLE = (0xBFF60000 + 0x0064),  // The hue offset angle is out of range.
        IMG_ERR_BAD_ACQUIRE_FIELD = (0xBFF60000 + 0x0065),  // The value for acquire field is invalid.
        IMG_ERR_BAD_LUMA_BANDWIDTH = (0xBFF60000 + 0x0066),  // The value for luma bandwidth is invalid.
        IMG_ERR_BAD_LUMA_COMB = (0xBFF60000 + 0x0067),  // The value for luma comb is invalid.
        IMG_ERR_BAD_CHROMA_PROCESS = (0xBFF60000 + 0x0068),  // The value for chroma processing is invalid.
        IMG_ERR_BAD_CHROMA_BANDWIDTH = (0xBFF60000 + 0x0069),  // The value for chroma bandwidth is invalid.
        IMG_ERR_BAD_CHROMA_COMB = (0xBFF60000 + 0x006A),  // The value for chroma comb is invalid.
        IMG_ERR_BAD_RGB_CORING = (0xBFF60000 + 0x006B),  // The value for RGB coring is invalid.
        IMG_ERR_BAD_HUE_REPLACE_VALUE = (0xBFF60000 + 0x006C),  // The value for HSL hue replacement is out of range.
        IMG_ERR_BAD_RED_GAIN = (0xBFF60000 + 0x006D),  // The value for red gain is out of range.
        IMG_ERR_BAD_GREEN_GAIN = (0xBFF60000 + 0x006E),  // The value for green gain is out of range.
        IMG_ERR_BAD_BLUE_GAIN = (0xBFF60000 + 0x006F),  // The value for blue gain is out of range.
        IMG_ERR_BAD_START_FIELD = (0xBFF60000 + 0x0070),  // Invalid start field
        IMG_ERR_BAD_TAP_DIRECTION = (0xBFF60000 + 0x0071),  // Invalid tap scan direction
        IMG_ERR_BAD_MAX_IMAGE_RECT = (0xBFF60000 + 0x0072),  // Invalid maximum image rect
        IMG_ERR_BAD_TAP_TYPE = (0xBFF60000 + 0x0073),  // Invalid tap configuration type
        IMG_ERR_BAD_SYNC_RECT = (0xBFF60000 + 0x0074),  // Invalid sync rect
        IMG_ERR_BAD_ACQWINDOW_RECT = (0xBFF60000 + 0x0075),  // Invalid acquisition window
        IMG_ERR_BAD_HSL_CORING = (0xBFF60000 + 0x0076),  // The value for HSL coring is out of range.
        IMG_ERR_BAD_TAP_0_VALID_RECT = (0xBFF60000 + 0x0077),  // Invalid tap 0 valid rect
        IMG_ERR_BAD_TAP_1_VALID_RECT = (0xBFF60000 + 0x0078),  // Invalid tap 1 valid rect
        IMG_ERR_BAD_TAP_2_VALID_RECT = (0xBFF60000 + 0x0079),  // Invalid tap 2 valid rect
        IMG_ERR_BAD_TAP_3_VALID_RECT = (0xBFF60000 + 0x007A),  // Invalid tap 3 valid rect
        IMG_ERR_BAD_TAP_RECT = (0xBFF60000 + 0x007B),  // Invalid tap rect
        IMG_ERR_BAD_NUM_TAPS = (0xBFF60000 + 0x007C),  // Invalid number of taps
        IMG_ERR_BAD_TAP_NUM = (0xBFF60000 + 0x007D),  // Invalid tap number
        IMG_ERR_BAD_QUAD_NUM = (0xBFF60000 + 0x007E),  // Invalid Scarab quadrant number
        IMG_ERR_BAD_NUM_DATA_LINES = (0xBFF60000 + 0x007F),  // Invalid number of requested data lines
        IMG_ERR_BAD_BITS_PER_COMPONENT = (0xBFF60000 + 0x0080),  // The value for bits per component is invalid.
        IMG_ERR_BAD_NUM_COMPONENTS = (0xBFF60000 + 0x0081),  // The value for number of components is invalid.
        IMG_ERR_BAD_BIN_THRESHOLD_LOW = (0xBFF60000 + 0x0082),  // The value for the lower binary threshold is out of range.
        IMG_ERR_BAD_BIN_THRESHOLD_HIGH = (0xBFF60000 + 0x0083),  // The value for the upper binary threshold is out of range.
        IMG_ERR_BAD_BLACK_REF_VOLT = (0xBFF60000 + 0x0084),  // The value for the black reference voltage is out of range.
        IMG_ERR_BAD_WHITE_REF_VOLT = (0xBFF60000 + 0x0085),  // The value for the white reference voltage is out of range.
        IMG_ERR_BAD_FREQ_STD = (0xBFF60000 + 0x0086),  // The value for the 6431 frequency standard is out of range.
        IMG_ERR_BAD_HDELAY = (0xBFF60000 + 0x0087),  // The value for HDELAY is out of range.
        IMG_ERR_BAD_LOCK_SPEED = (0xBFF60000 + 0x0088),  // Invalid lock speed.
        IMG_ERR_BAD_BUFFER_LIST = (0xBFF60000 + 0x0089),  // Invalid buffer list
        IMG_ERR_BOARD_NOT_INITIALIZED = (0xBFF60000 + 0x008A),  // An attempt was made to access the board before it was initialized.
        IMG_ERR_BAD_PCLK_SOURCE = (0xBFF60000 + 0x008B),  // Invalid pixel clock source
        IMG_ERR_BAD_VIDEO_LOCK_CHANNEL = (0xBFF60000 + 0x008C),  // Invalid video lock source
        IMG_ERR_BAD_LOCK_SEL = (0xBFF60000 + 0x008D),  // Invalid locking mode
        IMG_ERR_BAD_BAUD_RATE = (0xBFF60000 + 0x008E),  // Invalid baud rate for the UART
        IMG_ERR_BAD_STOP_BITS = (0xBFF60000 + 0x008F),  // The number of stop bits for the UART is out of range.
        IMG_ERR_BAD_DATA_BITS = (0xBFF60000 + 0x0090),  // The number of data bits for the UART is out of range.
        IMG_ERR_BAD_PARITY = (0xBFF60000 + 0x0091),  // Invalid parity setting for the UART
        IMG_ERR_TERM_STRING_NOT_FOUND = (0xBFF60000 + 0x0092),  // Couldn't find the termination string in a serial read
        IMG_ERR_SERIAL_READ_TIMEOUT = (0xBFF60000 + 0x0093),  // Exceeded the user specified timeout for a serial read
        IMG_ERR_SERIAL_WRITE_TIMEOUT = (0xBFF60000 + 0x0094),  // Exceeded the user specified timeout for a serial write
        IMG_ERR_BAD_SYNCHRONICITY = (0xBFF60000 + 0x0095),  // Invalid setting for whether the acquisition is synchronous.
        IMG_ERR_BAD_INTERLACING_CONFIG = (0xBFF60000 + 0x0096),  // Bad interlacing configuration
        IMG_ERR_BAD_CHIP_CODE = (0xBFF60000 + 0x0098),  // Bad chip code.  Couldn't find a matching chip.
        IMG_ERR_LUT_NOT_PRESENT = (0xBFF60000 + 0x0099),  // The LUT chip doesn't exist
        IMG_ERR_DSPFILTER_NOT_PRESENT = (0xBFF60000 + 0x009A),  // The DSP filter doesn't exist
        IMG_ERR_DEVICE_NOT_FOUND = (0xBFF60000 + 0x009B),  // The IMAQ device was not found
        IMG_ERR_ONBOARD_MEM_CONFIG = (0xBFF60000 + 0x009C),  // There was a problem while configuring onboard memory
        IMG_ERR_BAD_POINTER = (0xBFF60000 + 0x009D),  // The pointer is bad.  It might be NULL when it shouldn't be NULL or non-NULL when it should be NULL.
        IMG_ERR_BAD_BUFFER_LIST_INDEX = (0xBFF60000 + 0x009E),  // The given buffer list index is invalid
        IMG_ERR_INVALID_BUFFER_ATTRIBUTE = (0xBFF60000 + 0x009F),  // The given buffer attribute is invalid
        IMG_ERR_INVALID_BUFFER_PTR = (0xBFF60000 + 0x00A0),  // The given buffer wan't created by the NI-IMAQ driver
        IMG_ERR_BUFFER_LIST_ALREADY_LOCKED = (0xBFF60000 + 0x00A1),  // A buffer list is already locked down in memory for this device
        IMG_ERR_BAD_DEVICE_TYPE = (0xBFF60000 + 0x00A2),  // The type of IMAQ device is invalid
        IMG_ERR_BAD_BAR_SIZE = (0xBFF60000 + 0x00A3),  // The size of one or more BAR windows is incorrect
        IMG_ERR_NO_VALID_COUNTER_RECT = (0xBFF60000 + 0x00A5),  // Couldn't settle on a valid counter rect
        IMG_ERR_ACQ_STOPPED = (0xBFF60000 + 0x00A6),  // The wait terminated because the acquisition stopped.
        IMG_ERR_BAD_TRIGGER_ACTION = (0xBFF60000 + 0x00A7),  // The trigger action is invalid.
        IMG_ERR_BAD_TRIGGER_POLARITY = (0xBFF60000 + 0x00A8),  // The trigger polarity is invalid.
        IMG_ERR_BAD_TRIGGER_NUMBER = (0xBFF60000 + 0x00A9),  // The requested trigger line is invalid.
        IMG_ERR_BUFFER_NOT_AVAILABLE = (0xBFF60000 + 0x00AA),  // The requested buffer has been overwritten and is no longer available.
        IMG_ERR_BAD_PULSE_ID = (0xBFF60000 + 0x00AC),  // The given pulse id is invalid
        IMG_ERR_BAD_PULSE_TIMEBASE = (0xBFF60000 + 0x00AD),  // The given timebase is invalid.
        IMG_ERR_BAD_PULSE_GATE = (0xBFF60000 + 0x00AE),  // The given gate signal for the pulse is invalid.
        IMG_ERR_BAD_PULSE_GATE_POLARITY = (0xBFF60000 + 0x00AF),  // The polarity of the gate signal is invalid.
        IMG_ERR_BAD_PULSE_OUTPUT = (0xBFF60000 + 0x00B0),  // The given output signal for the pulse is invalid.
        IMG_ERR_BAD_PULSE_OUTPUT_POLARITY = (0xBFF60000 + 0x00B1),  // The polarity of the output signal is invalid.
        IMG_ERR_BAD_PULSE_MODE = (0xBFF60000 + 0x00B2),  // The given pulse mode is invalid.
        IMG_ERR_NOT_ENOUGH_RESOURCES = (0xBFF60000 + 0x00B3),  // There are not enough resources to complete the requested operation.
        IMG_ERR_INVALID_RESOURCE = (0xBFF60000 + 0x00B4),  // The requested resource is invalid
        IMG_ERR_BAD_FVAL_ENABLE = (0xBFF60000 + 0x00B5),  // Invalid enable mode for FVAL
        IMG_ERR_BAD_WRITE_ENABLE_MODE = (0xBFF60000 + 0x00B6),  // Invalid combination of enables to write to DRAM
        IMG_ERR_COMPONENT_MISMATCH = (0xBFF60000 + 0x00B7),  // Internal Error: The installed components of the driver are incompatible.  Reinstall the driver.
        IMG_ERR_FPGA_PROGRAMMING_FAILED = (0xBFF60000 + 0x00B8),  // Internal Error: Downloading the program to an FPGA didn't work.
        IMG_ERR_CONTROL_FPGA_FAILED = (0xBFF60000 + 0x00B9),  // Internal Error: The Control FPGA didn't initialize properly
        IMG_ERR_CHIP_NOT_READABLE = (0xBFF60000 + 0x00BA),  // Internal Error: Attempt to read a write-only chip.
        IMG_ERR_CHIP_NOT_WRITABLE = (0xBFF60000 + 0x00BB),  // Internal Error: Attempt to write a read-only chip.
        IMG_ERR_I2C_BUS_FAILED = (0xBFF60000 + 0x00BC),  // Internal Error: The I2C bus didn't respond correctly.
        IMG_ERR_DEVICE_IN_USE = (0xBFF60000 + 0x00BD),  // The requested IMAQ device is already open
        IMG_ERR_BAD_TAP_DATALANES = (0xBFF60000 + 0x00BE),  // The requested data lanes on a particular tap are invalid
        IMG_ERR_BAD_VIDEO_GAIN = (0xBFF60000 + 0x00BF),  // Bad video gain value
        IMG_ERR_VHA_MODE_NOT_ALLOWED = (0xBFF60000 + 0x00C0),  // VHA mode not allowed, based upon the current configuration
        IMG_ERR_BAD_TRACKING_SPEED = (0xBFF60000 + 0x00C1),  // Bad color video tracking speed
        IMG_ERR_BAD_COLOR_INPUT_SELECT = (0xBFF60000 + 0x00C2),  // Invalid input select for the 1411
        IMG_ERR_BAD_HAV_OFFSET = (0xBFF60000 + 0x00C3),  // Invalid HAV offset
        IMG_ERR_BAD_HS1_OFFSET = (0xBFF60000 + 0x00C4),  // Invalid HS1 offset
        IMG_ERR_BAD_HS2_OFFSET = (0xBFF60000 + 0x00C5),  // Invalid HS2 offset
        IMG_ERR_BAD_IF_CHROMA = (0xBFF60000 + 0x00C6),  // Invalid chroma IF compensation
        IMG_ERR_BAD_COLOR_OUTPUT_FORMAT = (0xBFF60000 + 0x00C7),  // Invalid format for color output
        IMG_ERR_BAD_SAMSUNG_SCHCMP = (0xBFF60000 + 0x00C8),  // Invalid phase constant
        IMG_ERR_BAD_SAMSUNG_CDLY = (0xBFF60000 + 0x00C9),  // Invalid chroma path group delay
        IMG_ERR_BAD_SECAM_DETECT = (0xBFF60000 + 0x00CA),  // Invalid method for secam detection
        IMG_ERR_BAD_FSC_DETECT = (0xBFF60000 + 0x00CB),  // Invalid method for fsc detection
        IMG_ERR_BAD_SAMSUNG_CFTC = (0xBFF60000 + 0x00CC),  // Invalid chroma frequency tracking time constant
        IMG_ERR_BAD_SAMSUNG_CGTC = (0xBFF60000 + 0x00CD),  // Invalid chroma gain tracking time constant
        IMG_ERR_BAD_SAMSUNG_SAMPLE_RATE = (0xBFF60000 + 0x00CE),  // Invalid pixel sampling rate
        IMG_ERR_BAD_SAMSUNG_VSYNC_EDGE = (0xBFF60000 + 0x00CF),  // Invalid edge for vsync to follow
        IMG_ERR_SAMSUNG_LUMA_GAIN_CTRL = (0xBFF60000 + 0x00D0),  // Invalid method to control the luma gain
        IMG_ERR_BAD_SET_COMB_COEF = (0xBFF60000 + 0x00D1),  // Invalid way to set the chroma comb coefficients
        IMG_ERR_SAMSUNG_CHROMA_TRACK = (0xBFF60000 + 0x00D2),  // Invalid method to track chroma
        IMG_ERR_SAMSUNG_DROP_LINES = (0xBFF60000 + 0x00D3),  // Invalid algorithm to drop video lines
        IMG_ERR_VHA_OPTIMIZATION_NOT_ALLOWED = (0xBFF60000 + 0x00D4),  // VHA optimization not allowed, based upon the current configuration
        IMG_ERR_BAD_PG_TRANSITION = (0xBFF60000 + 0x00D5),  // A pattern generation transition is invalid
        IMG_ERR_TOO_MANY_PG_TRANSITIONS = (0xBFF60000 + 0x00D6),  // User is attempting to generate more pattern generation transitions than we support
        IMG_ERR_BAD_CL_DATA_CONFIG = (0xBFF60000 + 0x00D7),  // Invalid data configuration for the Camera Link chips
        IMG_ERR_BAD_OCCURRENCE = (0xBFF60000 + 0x00D8),  // The given occurrence is not valid.
        IMG_ERR_BAD_PG_MODE = (0xBFF60000 + 0x00D9),  // Invalid pattern generation mode
        IMG_ERR_BAD_PG_SOURCE = (0xBFF60000 + 0x00DA),  // Invalid pattern generation source signal
        IMG_ERR_BAD_PG_GATE = (0xBFF60000 + 0x00DB),  // Invalid pattern generation gate signal
        IMG_ERR_BAD_PG_GATE_POLARITY = (0xBFF60000 + 0x00DC),  // Invalid pattern generation gate polarity
        IMG_ERR_BAD_PG_WAVEFORM_INITIAL_STATE = (0xBFF60000 + 0x00DD),  // Invalid pattern generation waveform initial state
        IMG_ERR_INVALID_CAMERA_ATTRIBUTE = (0xBFF60000 + 0x00DE),  // The requested camera attribute is invalid
        IMG_ERR_BOARD_CLOSED = (0xBFF60000 + 0x00DF),  // The request failed because the board was closed
        IMG_ERR_FILE_NOT_FOUND = (0xBFF60000 + 0x00E0),  // The requested file could not be found
        IMG_ERR_BAD_1409_DSP_FILE = (0xBFF60000 + 0x00E1),  // The dspfilter1409.bin file was corrupt or missing
        IMG_ERR_BAD_SCARABXCV200_32_FILE = (0xBFF60000 + 0x00E2),  // The scarabXCV200.bin file was corrupt or missing
        IMG_ERR_BAD_SCARABXCV200_16_FILE = (0xBFF60000 + 0x00E3),  // The scarab16bit.bin file was corrupt or missing
        IMG_ERR_BAD_CAMERA_LINK_FILE = (0xBFF60000 + 0x00E4),  // The data1428.bin file was corrupt or missing
        IMG_ERR_BAD_1411_CSC_FILE = (0xBFF60000 + 0x00E5),  // The colorspace.bin file was corrupt or missing
        IMG_ERR_BAD_ERROR_CODE = (0xBFF60000 + 0x00E6),  // The error code passed into imgShowError was unknown.
        IMG_ERR_DRIVER_TOO_OLD = (0xBFF60000 + 0x00E7),  // The board requires a newer version of the driver.
        IMG_ERR_INSTALLATION_CORRUPT = (0xBFF60000 + 0x00E8),  // A driver piece is not present =(.dll, registry entry, etc),.
        IMG_ERR_NO_ONBOARD_MEMORY = (0xBFF60000 + 0x00E9),  // There is no onboard memory, thus an onboard acquisition cannot be performed.
        IMG_ERR_BAD_BAYER_PATTERN = (0xBFF60000 + 0x00EA),  // The Bayer pattern specified is invalid.
        IMG_ERR_CANNOT_INITIALIZE_BOARD = (0xBFF60000 + 0x00EB),  // The board is not operating correctly and cannot be initialized.
        IMG_ERR_CALIBRATION_DATA_CORRUPT = (0xBFF60000 + 0x00EC),  // The stored calibration data has been corrupted.
        IMG_ERR_DRIVER_FAULT = (0xBFF60000 + 0x00ED),  // The driver attempted to perform an illegal operation.
        IMG_ERR_ADDRESS_OUT_OF_RANGE = (0xBFF60000 + 0x00EE),  // An attempt was made to access a chip beyond it's addressable range.
        IMG_ERR_ONBOARD_ACQUISITION = (0xBFF60000 + 0x00EF),  // The requested operation is not valid for onboard acquisitions.
        IMG_ERR_NOT_AN_ONBOARD_ACQUISITION = (0xBFF60000 + 0x00F0),  // The requested operation is only valid for onboard acquisitions.
        IMG_ERR_BOARD_ALREADY_INITIALIZED = (0xBFF60000 + 0x00F1),  // An attempt was made to call an initialization function on a board that was already initialized.
        IMG_ERR_NO_SERIAL_PORT = (0xBFF60000 + 0x00F2),  // Tried to use the serial port on a board that doesn't have one
        IMG_ERR_BAD_VENABLE_GATING_MODE = (0xBFF60000 + 0x00F3),  // The VENABLE gating mode selection is invalid
        IMG_ERR_BAD_1407_LUT_FILE = (0xBFF60000 + 0x00F4),  // The lutfpga1407.bin was corrupt or missing
        IMG_ERR_BAD_SYNC_DETECT_LEVEL = (0xBFF60000 + 0x00F5),  // The detect sync level is out of range for the 1407 rev A-D
        IMG_ERR_BAD_1405_GAIN_FILE = (0xBFF60000 + 0x00F6),  // The gain1405.bin file was corrupt or missing
        IMG_ERR_CLAMP_DAC_NOT_PRESENT = (0xBFF60000 + 0x00F7),  // The device doesn't have a clamp DAC
        IMG_ERR_GAIN_DAC_NOT_PRESENT = (0xBFF60000 + 0x00F8),  // The device doesn't have a gain DAC
        IMG_ERR_REF_DAC_NOT_PRESENT = (0xBFF60000 + 0x00F9),  // The device doesn't have a reference DAC
        IMG_ERR_BAD_SCARABXC2S200_FILE = (0xBFF60000 + 0x00FA),  // The scarab16bit.bin file was corrupt or missing
        IMG_ERR_BAD_LUT_GAIN = (0xBFF60000 + 0x00FB),  // The desired LUT gain is invalid
        IMG_ERR_BAD_MAX_BUF_LIST_ITER = (0xBFF60000 + 0x00FC),  // The desired maximum number of buffer list iterations to store on onboard memory is invalid
        IMG_ERR_BAD_PG_LINE_NUM = (0xBFF60000 + 0x00FD),  // The desired pattern generation line number is invalid
        IMG_ERR_BAD_BITS_PER_PIXEL = (0xBFF60000 + 0x00FE),  // The desired number of bits per pixel is invalid
        IMG_ERR_TRIGGER_ALARM = (0xBFF60000 + 0x00FF),  // Triggers are coming in too fast to handle them and maintain system responsiveness.  Check for glitches on your trigger line.
        IMG_ERR_BAD_SCARABXC2S200_03052009_FILE = (0xBFF60000 + 0x0100),  // The scarabXC2S200_03052009.bin file was corrupt or missing
        IMG_ERR_LUT_CONFIG = (0xBFF60000 + 0x0101),  // There was an error configuring the LUT
        IMG_ERR_CONTROL_FPGA_REQUIRES_NEWER_DRIVER = (0xBFF60000 + 0x0102),  // The Control FPGA requires a newer version of the driver than is currently installed.  This can happen when field upgrading the Control FPGA.
        IMG_ERR_CONTROL_FPGA_PROGRAMMING_FAILED = (0xBFF60000 + 0x0103), // The FlashCPLD reported that the Control FPGA did not program successfully.
        IMG_ERR_BAD_TRIGGER_SIGNAL_LEVEL = (0xBFF60000 + 0x0104), // A trigger signalling level is invalid.
        IMG_ERR_CAMERA_FILE_REQUIRES_NEWER_DRIVER = (0xBFF60000 + 0x0105), // The camera file requires a newer version of the driver
        IMG_ERR_DUPLICATED_BUFFER = (0xBFF60000 + 0x0106), // The same image was put in the buffer list twice.  LabVIEW only.
        IMG_ERR_NO_ERROR = (0xBFF60000 + 0x0107), // No error.  Never returned by the driver.
        IMG_ERR_INTERFACE_NOT_SUPPORTED = (0xBFF60000 + 0x0108), // The camera file does not support the board that is trying to open it.
        IMG_ERR_BAD_PCLK_POLARITY = (0xBFF60000 + 0x0109), // The requested polarity for the pixel clock is invalid.
        IMG_ERR_BAD_ENABLE_POLARITY = (0xBFF60000 + 0x010A), // The requested polarity for the enable line is invalid.
        IMG_ERR_BAD_PCLK_SIGNAL_LEVEL = (0xBFF60000 + 0x010B), // The requested signaling level for the pixel clock is invalid.
        IMG_ERR_BAD_ENABLE_SIGNAL_LEVEL = (0xBFF60000 + 0x010C), // The requested signaling level for the enable line is invalid.
        IMG_ERR_BAD_DATA_SIGNAL_LEVEL = (0xBFF60000 + 0x010D), // The requested signaling level for the data lines is invalid.
        IMG_ERR_BAD_CTRL_SIGNAL_LEVEL = (0xBFF60000 + 0x010E), // The requested signaling level for the control lines is invalid.
        IMG_ERR_BAD_WINDOW_HANDLE = (0xBFF60000 + 0x010F), // The given window handle is invalid
        IMG_ERR_CANNOT_WRITE_FILE = (0xBFF60000 + 0x0110), // Cannot open the requested file for writing.
        IMG_ERR_CANNOT_READ_FILE = (0xBFF60000 + 0x0111), // Cannot open the requested file for reading.
        IMG_ERR_BAD_SIGNAL_TYPE = (0xBFF60000 + 0x0112), // The signal passed into imgSessionWaitSignal=(Async), was invalid.
        IMG_ERR_BAD_SAMPLES_PER_LINE = (0xBFF60000 + 0x0113), // Invalid samples per line
        IMG_ERR_BAD_SAMPLES_PER_LINE_REF = (0xBFF60000 + 0x0114), // Invalid samples per line reference
        IMG_ERR_USE_EXTERNAL_HSYNC = (0xBFF60000 + 0x0115), // The current video signal requires an external HSYNC to be used to lock the signal.
        IMG_ERR_BUFFER_NOT_ALIGNED = (0xBFF60000 + 0x0116), // An image buffer is not properly aligned.  It must be aligned to a DWORD boundary.
        IMG_ERR_ROWPIXELS_TOO_SMALL = (0xBFF60000 + 0x0117), // The number of pixels per row is less than the region of interest width.
        IMG_ERR_ROWPIXELS_NOT_ALIGNED = (0xBFF60000 + 0x0118), // The number of pixels per row is not properly aligned.  The total number of bytes per row must be aligned to a DWORD boundary.
        IMG_ERR_ROI_WIDTH_NOT_ALIGNED = (0xBFF60000 + 0x0119), // The ROI width is not properly aligned.  The total number of bytes bounded by ROI width must be aligned to a DWORD boundary.
        IMG_ERR_LINESCAN_NOT_ALLOWED = (0xBFF60000 + 0x011A), // Linescan mode is not allowed for this tap configuration.
        IMG_ERR_INTERFACE_FILE_REQUIRES_NEWER_DRIVER = (0xBFF60000 + 0x011B), // The camera file requires a newer version of the driver
        IMG_ERR_BAD_SKIP_COUNT = (0xBFF60000 + 0x011C), // The requested skip count value is out of range.
        IMG_ERR_BAD_NUM_X_ZONES = (0xBFF60000 + 0x011D), // The number of X-zones is invalid
        IMG_ERR_BAD_NUM_Y_ZONES = (0xBFF60000 + 0x011E), // The number of Y-zones is invalid
        IMG_ERR_BAD_NUM_TAPS_PER_X_ZONE = (0xBFF60000 + 0x011F), // The number of taps per X-zone is invalid
        IMG_ERR_BAD_NUM_TAPS_PER_Y_ZONE = (0xBFF60000 + 0x0120), // The number of taps per Y-zone is invalid
        IMG_ERR_BAD_TEST_IMAGE_TYPE = (0xBFF60000 + 0x0121), // The requested test image type is invalid
        IMG_ERR_CANNOT_ACQUIRE_FROM_CAMERA = (0xBFF60000 + 0x0122), // This firmware is not capable of acquiring from a camera
        IMG_ERR_BAD_CTRL_LINE_SOURCE = (0xBFF60000 + 0x0123), // The selected source for one of the camera control lines is bad
        IMG_ERR_BAD_PIXEL_EXTRACTOR = (0xBFF60000 + 0x0124), // The desired pixel extractor is invalid
        IMG_ERR_BAD_NUM_TIME_SLOTS = (0xBFF60000 + 0x0125), // The desired number of time slots is invalid
        IMG_ERR_BAD_PLL_VCO_DIVIDER = (0xBFF60000 + 0x0126), // The VCO divide by number was invalide for the ICS1523
        IMG_ERR_CRITICAL_TEMP = (0xBFF60000 + 0x0127), // The device temperature exceeded the critical temperature threshold
        IMG_ERR_BAD_DPA_OFFSET = (0xBFF60000 + 0x0128), // The requested dynamic phase aligner offset is invalid
        IMG_ERR_BAD_NUM_POST_TRIGGER_BUFFERS = (0xBFF60000 + 0x0129), // The requested number of post trigger buffers is invalid
        IMG_ERR_BAD_DVAL_MODE = (0xBFF60000 + 0x012A), // The requested DVAL mode is invalid
        IMG_ERR_BAD_TRIG_GEN_REARM_SOURCE = (0xBFF60000 + 0x012B), // The requested trig gen rearm source signal is invalid
        IMG_ERR_BAD_ASM_GATE_SOURCE = (0xBFF60000 + 0x012C), // The requested ASM gate signal is invalid
        IMG_ERR_TOO_MANY_BUFFERS = (0xBFF60000 + 0x012D), // The requested number of buffer list buffers is not supported by this IMAQ device
        IMG_ERR_BAD_TAP_4_VALID_RECT = (0xBFF60000 + 0x012E), // Invalid tap 4 valid rect
        IMG_ERR_BAD_TAP_5_VALID_RECT = (0xBFF60000 + 0x012F), // Invalid tap 5 valid rect
        IMG_ERR_BAD_TAP_6_VALID_RECT = (0xBFF60000 + 0x0130), // Invalid tap 6 valid rect
        IMG_ERR_BAD_TAP_7_VALID_RECT = (0xBFF60000 + 0x0131), // Invalid tap 7 valid rect
        IMG_ERR_FRONT_END_BANDWIDTH_EXCEEDED = (0xBFF60000 + 0x0132), // The camera is providing image data faster than the IMAQ device can receive it.
        IMG_ERR_BAD_PORT_NUMBER = (0xBFF60000 + 0x0133), // The requested port number does not exist.
        IMG_ERR_PORT_CONFIG_CONFLICT = (0xBFF60000 + 0x0134), // The requested port cannot be cannot be configured due to a conflict with another port that is currently opened.
        IMG_ERR_BITSTREAM_INCOMPATIBLE = (0xBFF60000 + 0x0135), // The requested bitstream is not compatible with the IMAQ device.
        IMG_ERR_SERIAL_PORT_IN_USE = (0xBFF60000 + 0x0136), // The requested serial port is currently in use and is not accessible.
        IMG_ERR_BAD_ENCODER_DIVIDE_FACTOR = (0xBFF60000 + 0x0137), // The requested encoder divide factor is invalid.
        IMG_ERR_ENCODER_NOT_SUPPORTED = (0xBFF60000 + 0x0138), // Encoder support is not present for this IMAQ device.  Please verify that this device is capable of handling encoder signals and that phase A and B are connected.
        IMG_ERR_BAD_ENCODER_POLARITY = (0xBFF60000 + 0x0139), // The requested encoder phase signal polarity is invalid.
        IMG_ERR_BAD_ENCODER_FILTER = (0xBFF60000 + 0x013A), // The requested encoder filter setting is invalid.
        IMG_ERR_ENCODER_POSITION_NOT_SUPPORTED = (0xBFF60000 + 0x013B), // This IMAQ device does not support reading the absolute encoder position.
        IMG_ERR_IMAGE_IN_USE = (0xBFF60000 + 0x013C), // The IMAQ image appears to be in use.  Please name the images differently to avoid this situation.
        IMG_ERR_BAD_SCARABXL4000_FILE = (0xBFF60000 + 0x013D), // The scarab.bin file is corrupt or missing
        IMG_ERR_BAD_CAMERA_ATTRIBUTE_VALUE = (0xBFF60000 + 0x013E), // The requested camera attribute value is invalid.  For numeric camera attributes, please ensure that the value is properly aligned and within the allowable range.
        IMG_ERR_BAD_PULSE_WIDTH = (0xBFF60000 + 0x013F), // The requested pulse width is invalid.
        IMG_ERR_FPGA_FILE_NOT_FOUND = (0xBFF60000 + 0x0140), // The requested FPGA bitstream file could not be found.
        IMG_ERR_FPGA_FILE_CORRUPT = (0xBFF60000 + 0x0141), // The requested FPGA bitstream file is corrupt.
        IMG_ERR_BAD_PULSE_DELAY = (0xBFF60000 + 0x0142), // The requested pulse delay is invalid.
        IMG_ERR_BAD_PG_IDLE_SIGNAL_LEVEL = (0xBFF60000 + 0x0143), // On SecondGen boards tristating the idle state is all or nothing.
        IMG_ERR_BAD_PG_WAVEFORM_IDLE_STATE = (0xBFF60000 + 0x0144), // Invalid pattern generation waveform idle state
        IMG_ERR_64_BIT_MEMORY_NOT_SUPPORTED = (0xBFF60000 + 0x0145), // This device only supports acquiring into the 32-bit address space; however, portions of the image buffer list reside outside of 32-bit physical memory.
        IMG_ERR_64_BIT_MEMORY_UPDATE_AVAILABLE = (0xBFF60000 + 0x0146), // This 32-bit device is operating on a 64-bit OS with more than 3GB of physical memory.  An update is available to allow acquisitions into 64-bit physical memory.  Launch the updater from the menu in MAX:  Tools >> NI Vision >> NI-IMAQ Firmware Updater.
        IMG_ERR_32_BIT_MEMORY_LIMITATION = (0xBFF60000 + 0x0147), // This 32-bit device is operating on a 64-bit OS with more than 3GB of physical memory.  This configuration could allocate 64-bit memory which is unsupported by the device.  To solve this problem, reduce the amount of physical memory in the system.
        IMG_ERR_KERNEL_NOT_LOADED = (0xBFF60000 + 0x0148), // The kernel component of the driver, niimaqk.sys, is not loaded.  Verify that an IMAQ device is in your system or reinstall the driver.
        IMG_ERR_BAD_SENSOR_SHUTTER_PERIOD = (0xBFF60000 + 0x0149), // The sensor shutter period is invalid.  Check the horizontal and vertical shutter period values.
        IMG_ERR_BAD_SENSOR_CCD_TYPE = (0xBFF60000 + 0x014A), // The sensor CCD type is invalid.
        IMG_ERR_BAD_SENSOR_PARTIAL_SCAN = (0xBFF60000 + 0x014B), // The sensor partial scan mode is invalid.
        IMG_ERR_BAD_SENSOR_BINNING = (0xBFF60000 + 0x014C), // The sensor binning mode is invalid.
        IMG_ERR_BAD_SENSOR_GAIN = (0xBFF60000 + 0x014D), // The sensor gain value is invalid.
        IMG_ERR_BAD_SENSOR_BRIGHTNESS = (0xBFF60000 + 0x014E), // The sensor brightness value is invalid.
        IMG_ERR_BAD_LED_STATE = (0xBFF60000 + 0x014F), // The LED state is invalid.
        IMG_ERR_64_BIT_NOT_SUPPORTED = (0xBFF60000 + 0x0150), // The operation is not supported for 64-bit applications. 
        IMG_ERR_BAD_TRIGGER_DELAY = (0xBFF60000 + 0x0151), // The requested trigger delay value is not supported
        IMG_ERR_LIGHTING_CURRENT_EXCEEDS_LIMITS = (0xBFF60000 + 0x0152), // The configured lighting current exceeds the hardware or user's configured limits
        IMG_ERR_LIGHTING_INVALID_MODE = (0xBFF60000 + 0x0153), // The configured lighting mode is invalid
        IMG_ERR_LIGHTING_EXTERNAL_INVALID_MODE = (0xBFF60000 + 0x0154), // The configured external lighting mode is invalid
        IMG_ERR_BAD_SENSOR_EXPOSURE = (0xBFF60000 + 0x0155), // The sensor exposure time is invalid.
        IMG_ERR_BAD_FRAME_RATE = (0xBFF60000 + 0x0156), // The frame rate is invalid for the current configuration.
        IMG_ERR_BAD_SENSOR_PARTIAL_SCAN_BINNING_COMBINATION = (0xBFF60000 + 0x0157), // The partial scan mode / binning mode combination is invalid.
        IMG_ERR_SOFTWARE_TRIGGER_NOT_CONFIGURED = (0xBFF60000 + 0x0158), // The requested software trigger is not configured.
        IMG_ERR_FREE_RUN_MODE_NOT_ALLOWED = (0xBFF60000 + 0x0159), // Free-run mode is not allowed in the current configuration.  This is typically caused by simultaneously enabling free-run mode and a triggered acquisition.
        IMG_ERR_BAD_LIGHTING_RAMPUP = (0xBFF60000 + 0x015A), // The lighting ramp-up delay is either less than the minimum value allowed or larger than the maximum time that the light is allowed to be on.
        IMG_ERR_AFE_CONFIG_TIMEOUT = (0xBFF60000 + 0x015B), // Internal Error: A write to the AFEConfig register did not complete properly.
        IMG_ERR_LIGHTING_ARM_TIMEOUT = (0xBFF60000 + 0x015C), // Internal Error: The arming of the lighting controller did not complete properly.
        IMG_ERR_LIGHTING_SHORT_CIRCUIT = (0xBFF60000 + 0x015D), // A short circuit has been detected in the internal lighting current controller.  Remove the short circuit before restarting the acquisition.
        IMG_ERR_BAD_BOARD_HEALTH = (0xBFF60000 + 0x015E), // The board health register has indicated an internal problem.
        IMG_ERR_LIGHTING_BAD_CONTINUOUS_CURRENT_LIMIT = (0xBFF60000 + 0x015F), // The requested continuous current limit for the lighting controller is less than the minimum allowed current.
        IMG_ERR_LIGHTING_BAD_STROBE_DUTY_CYCLE_LIMIT = (0xBFF60000 + 0x0160), // The requested duty cycle limit for the lighting controller strobe mode is invalid.
        IMG_ERR_LIGHTING_BAD_STROBE_DURATION_LIMIT = (0xBFF60000 + 0x0161), // The requested duration limit for the lighting controller strobe mode is invalid.
        IMG_ERR_BAD_LIGHTING_CURRENT_EXPOSURE_COMBINATION = (0xBFF60000 + 0x0162), // The lighting current is invalid because the exposure time plus the lighting ramp-up time exceeds either the strobe duration limit or the strobe duty cycle limit.
        IMG_ERR_LIGHTING_HEAD_CONFIG_NOT_FOUND = (0xBFF60000 + 0x0163), // The configuration for the desired light head was not found
        IMG_ERR_LIGHTING_HEAD_DATA_CORRUPT = (0xBFF60000 + 0x0164), // The data for the desired light head is invalid or corrupt.
        IMG_ERR_LIGHTING_ABORT_TIMEOUT = (0xBFF60000 + 0x0165), // Internal Error: The abort of the lighting controller did not complete properly.
        IMG_ERR_LIGHTING_BAD_STROBE_CURRENT_LIMIT = (0xBFF60000 + 0x0166), // The requested strobe current limit for the lighting controller is less than the minimum allowed current.
        IMG_ERR_DMA_ENGINE_UNRESPONSIVE = (0xBFF60000 + 0x0167), // Internal Error: The DMA engine is unresponsive.  Reboot the target.  If the problem persists contact National Instruments.
        IMG_ERR_IMAGE_NOT_32BYTE_ALIGNED = (0xBFF60000 + 0x0168), // Image widths must be 32-byte aligned to work correctly with Vision algorithms. Used when optimizing the PCIe latency.
        IMG_ERR_IMAGE_BORDER_NONZERO = (0xBFF60000 + 0x0169), // The image border is not zero. Used when optimizing the PCIe latency.
        IMG_ERR_POCL_FAULT = (0xBFF60000 + 0x0170), // The PoCL circuitry has faulted.
        IMG_ERR_POCL_VIDEO_LOCK = (0xBFF60000 + 0x0171), // The PoCL circuitry is providing power but can't get a video lock.
        IMG_ERR_POCL_BAD_FUSE = (0xBFF60000 + 0x0172), // The PoCL circuitry cannot provide power due to a blown fuse.
        IMG_ERR_POCL_NO_AUX_POWER = (0xBFF60000 + 0x0173), // The PoCL circuitry cannot provide power due to a missing auxilliary power connection.
        IMG_ERR_PULSE_UPDATE_NOT_SUPPORTED = (0xBFF60000 + 0x0174), // This IMAQ device does not support updating an existing pulse.
        IMG_ERR_LAST_ERROR = (0xBFF60000 + 0x174),
    }

    public enum ImaqAttribute : uint
    {
        IMG_ATTR_INTERFACE_TYPE = (0x3FF60000 + 0x0001), // id of board - see constants below
        IMG_ATTR_PIXDEPTH = (0x3FF60000 + 0x0002), // pixel depth in bits
        IMG_ATTR_COLOR = (0x3FF60000 + 0x0003), // true = supports color
        IMG_ATTR_HASRAM = (0x3FF60000 + 0x0004), // true = has on-board SRAM
        IMG_ATTR_RAMSIZE = (0x3FF60000 + 0x0005), // SRAM size
        IMG_ATTR_CHANNEL = (0x3FF60000 + 0x0006),
        IMG_ATTR_FRAME_FIELD = (0x3FF60000 + 0x0007), // supports frame and field
        IMG_ATTR_HORZ_RESOLUTION = (0x3FF60000 + 0x0009),
        IMG_ATTR_VERT_RESOLUTION = (0x3FF60000 + 0x000A),
        IMG_ATTR_LUT = (0x3FF60000 + 0x000B),
        IMG_ATTR_LINESCAN = (0x3FF60000 + 0x000C),
        IMG_ATTR_GAIN = (0x3FF60000 + 0x000D),
        IMG_ATTR_CHROMA_FILTER = (0x3FF60000 + 0x000E),
        IMG_ATTR_WHITE_REF = (0x3FF60000 + 0x000F),
        IMG_ATTR_BLACK_REF = (0x3FF60000 + 0x0010),
        IMG_ATTR_DATALINES = (0x3FF60000 + 0x0011), // pass in uInt32 array of size up to 16 elements to swap incoming data lines =(0,1,2...15), - must be 16 x uInt32 array
        IMG_ATTR_NUM_EXT_LINES = (0x3FF60000 + 0x0012),
        IMG_ATTR_NUM_RTSI_LINES = (0x3FF60000 + 0x0013),
        IMG_ATTR_NUM_RTSI_IN_USE = (0x3FF60000 + 0x0014),
        IMG_ATTR_MEM_LOCKED = (0x3FF60000 + 0x0065),
        IMG_ATTR_BITSPERPIXEL = (0x3FF60000 + 0x0066),
        IMG_ATTR_BYTESPERPIXEL = (0x3FF60000 + 0x0067),
        IMG_ATTR_ACQWINDOW_LEFT = (0x3FF60000 + 0x0068),
        IMG_ATTR_ACQWINDOW_TOP = (0x3FF60000 + 0x0069),
        IMG_ATTR_ACQWINDOW_WIDTH = (0x3FF60000 + 0x006A),
        IMG_ATTR_ACQWINDOW_HEIGHT = (0x3FF60000 + 0x006B),
        IMG_ATTR_LINE_COUNT = (0x3FF60000 + 0x0070),
        IMG_ATTR_FREE_BUFFERS = (0x3FF60000 + 0x0071),
        IMG_ATTR_HSCALE = (0x3FF60000 + 0x0072),
        IMG_ATTR_VSCALE = (0x3FF60000 + 0x0073),
        IMG_ATTR_ACQ_IN_PROGRESS = (0x3FF60000 + 0x0074),
        IMG_ATTR_START_FIELD = (0x3FF60000 + 0x0075),
        IMG_ATTR_FRAME_COUNT = (0x3FF60000 + 0x0076),
        IMG_ATTR_LAST_VALID_BUFFER = (0x3FF60000 + 0x0077),
        IMG_ATTR_ROWBYTES = (0x3FF60000 + 0x0078),
        IMG_ATTR_CALLBACK = (0x3FF60000 + 0x007B),
        IMG_ATTR_CURRENT_BUFLIST = (0x3FF60000 + 0x007C),
        IMG_ATTR_FRAMEWAIT_MSEC = (0x3FF60000 + 0x007D),
        IMG_ATTR_TRIGGER_MODE = (0x3FF60000 + 0x007E),
        IMG_ATTR_INVERT = (0x3FF60000 + 0x0082),
        IMG_ATTR_XOFF_BUFFER = (0x3FF60000 + 0x0083),
        IMG_ATTR_YOFF_BUFFER = (0x3FF60000 + 0x0084),
        IMG_ATTR_NUM_BUFFERS = (0x3FF60000 + 0x0085),
        IMG_ATTR_LOST_FRAMES = (0x3FF60000 + 0x0088),
        IMG_ATTR_COLOR_WHITE_REF = (0x3FF60000 + 0x008F), // Color white reference
        IMG_ATTR_COLOR_BLACK_REF = (0x3FF60000 + 0x0090), // Color black reference
        IMG_ATTR_COLOR_CLAMP_START = (0x3FF60000 + 0x0091), // Color clamp start
        IMG_ATTR_COLOR_CLAMP_STOP = (0x3FF60000 + 0x0092), // Color clamp stop
        IMG_ATTR_COLOR_ZERO_START = (0x3FF60000 + 0x0093), // Color zero start
        IMG_ATTR_COLOR_ZERO_STOP = (0x3FF60000 + 0x0094), // Color zero stop
        IMG_ATTR_COLOR_AVG_COUNT = (0x3FF60000 + 0x0095), // Color averaging count
        IMG_ATTR_COLOR_SW_CHROMA_FILTER = (0x3FF60000 + 0x0096), // Color SW chroma filter
        IMG_ATTR_COLOR_NTSC_SETUP_ENABLE = (0x3FF60000 + 0x0097), // Color NTSC Setup enable
        IMG_ATTR_COLOR_NTSC_SETUP_VALUE = (0x3FF60000 + 0x0098), // Color NTSC Setup value
        IMG_ATTR_COLOR_BRIGHTNESS = (0x3FF60000 + 0x0099), // Color brightness
        IMG_ATTR_COLOR_CONTRAST = (0x3FF60000 + 0x009A), // Color contrast
        IMG_ATTR_COLOR_SATURATION = (0x3FF60000 + 0x009B), // Color saturation
        IMG_ATTR_COLOR_TINT = (0x3FF60000 + 0x009C), // Color tint =(chroma phase),
        IMG_ATTR_COLOR_SW_POST_GAIN = (0x3FF60000 + 0x009D), // Color SW post-gain
        IMG_ATTR_COLOR_BURST_START = (0x3FF60000 + 0x009E), // Color burst start
        IMG_ATTR_COLOR_BURST_STOP = (0x3FF60000 + 0x009F), // Color burst stop
        IMG_ATTR_COLOR_BLANK_START = (0x3FF60000 + 0x00A0), // Color blank start
        IMG_ATTR_COLOR_BLANK_STOP = (0x3FF60000 + 0x00A1), // Color blank stop
        IMG_ATTR_COLOR_IMAGE_X_SHIFT = (0x3FF60000 + 0x00A2), // Color acquisition left shift
        IMG_ATTR_COLOR_GAIN = (0x3FF60000 + 0x00A3), // Color HW pre-gain
        IMG_ATTR_COLOR_CLAMP_START_REF = (0x3FF60000 + 0x00A5), // Color clamp start reference
        IMG_ATTR_COLOR_CLAMP_STOP_REF = (0x3FF60000 + 0x00A6), // Color clamp stop reference
        IMG_ATTR_COLOR_ZERO_START_REF = (0x3FF60000 + 0x00A7), // Color zero start reference
        IMG_ATTR_COLOR_ZERO_STOP_REF = (0x3FF60000 + 0x00A8), // Color zero stop reference
        IMG_ATTR_COLOR_BURST_START_REF = (0x3FF60000 + 0x00A9), // Color burst start reference
        IMG_ATTR_COLOR_BURST_STOP_REF = (0x3FF60000 + 0x00AA), // Color burst stop reference
        IMG_ATTR_COLOR_BLANK_START_REF = (0x3FF60000 + 0x00AB), // Color blank start reference
        IMG_ATTR_COLOR_BLANK_STOP_REF = (0x3FF60000 + 0x00AC), // Color blank stop reference
        IMG_ATTR_COLOR_MODE = (0x3FF60000 + 0x00AD), // Color acquisition mode
        IMG_ATTR_COLOR_IMAGE_REP = (0x3FF60000 + 0x00AE), // Color Image representation
        IMG_ATTR_GENLOCK_SWITCH_CHAN = (0x3FF60000 + 0x00AF), // switch channel fast
        IMG_ATTR_CLAMP_START = (0x3FF60000 + 0x00B0), // clamp start
        IMG_ATTR_CLAMP_STOP = (0x3FF60000 + 0x00B1), // clamp stop
        IMG_ATTR_ZERO_START = (0x3FF60000 + 0x00B2), // zero start
        IMG_ATTR_ZERO_STOP = (0x3FF60000 + 0x00B3), // zero stop
        IMG_ATTR_COLOR_HUE_OFFS_ANGLE = (0x3FF60000 + 0x00B5), // Color hue offset angle
        IMG_ATTR_COLOR_IMAGE_X_SHIFT_REF = (0x3FF60000 + 0x00B6), // Color acquisition left shift reference
        IMG_ATTR_LAST_VALID_FRAME = (0x3FF60000 + 0x00BA), // returns the cummulative buffer index =(frame#),
        IMG_ATTR_CLOCK_FREQ = (0x3FF60000 + 0x00BB), // returns the max clock freq of the board
        IMG_ATTR_BLACK_REF_VOLT = (0x3FF60000 + 0x00BC), // defines the black reference in volts
        IMG_ATTR_WHITE_REF_VOLT = (0x3FF60000 + 0x00BD), // defines the white reference in volts
        IMG_ATTR_COLOR_LOW_REF_VOLT = (0x3FF60000 + 0x00BE), // defines the color low reference in volts
        IMG_ATTR_COLOR_HIGH_REF_VOLT = (0x3FF60000 + 0x00BF),
        IMG_ATTR_GETSERIAL = (0x3FF60000 + 0x00C0), // get the serial number of the board
        IMG_ATTR_ROWPIXELS = (0x3FF60000 + 0x00C1),
        IMG_ATTR_ACQUIRE_FIELD = (0x3FF60000 + 0x00C2),
        IMG_ATTR_PCLK_DETECT = (0x3FF60000 + 0x00C3),
        IMG_ATTR_VHA_MODE = (0x3FF60000 + 0x00C4), // Variable Height Acquisition mode
        IMG_ATTR_BIN_THRESHOLD_LOW = (0x3FF60000 + 0x00C5), // Binary threshold low
        IMG_ATTR_BIN_THRESHOLD_HIGH = (0x3FF60000 + 0x00C6), // Binary threshold hi
        IMG_ATTR_COLOR_LUMA_BANDWIDTH = (0x3FF60000 + 0x00C7), // selects different bandwidths for the luminance signal
        IMG_ATTR_COLOR_CHROMA_TRAP = (0x3FF60000 + 0x00C8), // enables and disables the chroma trap filter in the luma signal path
        IMG_ATTR_COLOR_LUMA_COMB = (0x3FF60000 + 0x00C9), // select the type of comb filter used in the luma path
        IMG_ATTR_COLOR_PEAKING_ENABLE = (0x3FF60000 + 0x00CA), // enable the peaking filter in the luma path
        IMG_ATTR_COLOR_PEAKING_LEVEL = (0x3FF60000 + 0x00CB),
        IMG_ATTR_COLOR_CHROMA_PROCESS = (0x3FF60000 + 0x00CC), // specifies the processing applied to the chroma signal
        IMG_ATTR_COLOR_CHROMA_BANDWIDTH = (0x3FF60000 + 0x00CD), // bandwidth of the chroma information of the image
        IMG_ATTR_COLOR_CHROMA_COMB = (0x3FF60000 + 0x00CE), // select the type of comb filter used in the chroma path
        IMG_ATTR_COLOR_CHROMA_PHASE = (0x3FF60000 + 0x00CF), // sets value of correction angle applied to the chroma vector.  Active only for NTSC cameras
        IMG_ATTR_COLOR_RGB_CORING_LEVEL = (0x3FF60000 + 0x00D0), // select RGB coring level
        IMG_ATTR_COLOR_HSL_CORING_LEVEL = (0x3FF60000 + 0x00D1), // select HSL coring level
        IMG_ATTR_COLOR_HUE_REPLACE_VALUE = (0x3FF60000 + 0x00D2), // hue value to replace if saturation value is less than HSL coring level
        IMG_ATTR_COLOR_GAIN_RED = (0x3FF60000 + 0x00D3), // Red Gain
        IMG_ATTR_COLOR_GAIN_GREEN = (0x3FF60000 + 0x00D4), // Green Gian
        IMG_ATTR_COLOR_GAIN_BLUE = (0x3FF60000 + 0x00D5), // Blue Gain
        IMG_ATTR_CALIBRATION_DATE_LV = (0x3FF60000 + 0x00D6), // 0 if board is uncalibrated, else seconds since Jan 1 1904
        IMG_ATTR_CALIBRATION_DATE = (0x3FF60000 + 0x00D7), // 0 if board is uncalibrated, else seconds since Jan 1 1970
        IMG_ATTR_IMAGE_TYPE = (0x3FF60000 + 0x00D8), // return the IMAQ Vision image type for LabVIEW
        IMG_ATTR_DYNAMIC_RANGE = (0x3FF60000 + 0x00D9), // the effective bits per pixel of the user's white-black level
        IMG_ATTR_ACQUIRE_TO_SYSTEM_MEMORY = (0x3FF60000 + 0x011B),
        IMG_ATTR_ONBOARD_HOLDING_BUFFER_PTR = (0x3FF60000 + 0x011C), // invalid on 64-bit OS
        IMG_ATTR_SYNCHRONICITY = (0x3FF60000 + 0x011D),
        IMG_ATTR_LAST_ACQUIRED_BUFFER_NUM = (0x3FF60000 + 0x011E),
        IMG_ATTR_LAST_ACQUIRED_BUFFER_INDEX = (0x3FF60000 + 0x011F),
        IMG_ATTR_LAST_TRANSFERRED_BUFFER_NUM = (0x3FF60000 + 0x0120),
        IMG_ATTR_LAST_TRANSFERRED_BUFFER_INDEX = (0x3FF60000 + 0x0121),
        IMG_ATTR_SERIAL_NUM_BYTES_RECEIVED = (0x3FF60000 + 0x012C), // # bytes currently in the internal serial read buffer
        IMG_ATTR_EXPOSURE_TIME_INTERNAL = (0x3FF60000 + 0x013C), // exposure time for 1454 =(internal value - specified in 40MHz clks),
        IMG_ATTR_SERIAL_TERM_STRING = (0x3FF60000 + 0x0150), // The serial termination string
        IMG_ATTR_DETECT_VIDEO = (0x3FF60000 + 0x01A3), // Determines whether to detect a video signal prior to acquiring
        IMG_ATTR_ROI_LEFT = (0x3FF60000 + 0x01A4),
        IMG_ATTR_ROI_TOP = (0x3FF60000 + 0x01A5),
        IMG_ATTR_ROI_WIDTH = (0x3FF60000 + 0x01A6),
        IMG_ATTR_ROI_HEIGHT = (0x3FF60000 + 0x01A7),
        IMG_ATTR_NUM_ISO_IN_LINES = (0x3FF60000 + 0x01A8), // The number of iso in lines the device supports
        IMG_ATTR_NUM_ISO_OUT_LINES = (0x3FF60000 + 0x01A9), // The number of iso out lines the device supports
        IMG_ATTR_NUM_POST_TRIGGER_BUFFERS = (0x3FF60000 + 0x01AA), // The number of buffers that hardware should continue acquire after sensing a stop trigger before it finally does stop
        IMG_ATTR_EXT_TRIG_LINE_FILTER = (0x3FF60000 + 0x01AB), // Whether to use filtering on the TTL trigger lines
        IMG_ATTR_RTSI_LINE_FILTER = (0x3FF60000 + 0x01AC), // Whether to use filtering on the RTSI trigger lines
        IMG_ATTR_NUM_PORTS = (0x3FF60000 + 0x01AD), // Returns the number of ports that this device supports.
        IMG_ATTR_CURRENT_PORT_NUM = (0x3FF60000 + 0x01AE), // Returns the port number that the given interface is using.
        IMG_ATTR_ENCODER_PHASE_A_POLARITY = (0x3FF60000 + 0x01AF), // The polarity of the phase A encoder input
        IMG_ATTR_ENCODER_PHASE_B_POLARITY = (0x3FF60000 + 0x01B0), // The polarity of the phase B encoder input
        IMG_ATTR_ENCODER_FILTER = (0x3FF60000 + 0x01B1), // Specifies whether to use filtering on the encoder input
        IMG_ATTR_ENCODER_DIVIDE_FACTOR = (0x3FF60000 + 0x01B2), // The divide factor for the encoder scaler
        IMG_ATTR_ENCODER_POSITION = (0x3FF60000 + 0x01B3), // Returns the current value of the absolute encoder position as a uInt64
        IMG_ATTR_TEMPERATURE = (0x3FF60000 + 0x01B4), // The device's current temperature, in degrees C
        IMG_ATTR_LED_PASS = (0x3FF60000 + 0x01B5), // The state of the Pass LED
        IMG_ATTR_LED_FAIL = (0x3FF60000 + 0x01B6), // The state of the Fail LED
        IMG_ATTR_SENSOR_PARTIAL_SCAN = (0x3FF60000 + 0x01B7), // The sensor's partial scan mode
        IMG_ATTR_SENSOR_BINNING = (0x3FF60000 + 0x01B8), // The sensor's binning mode
        IMG_ATTR_SENSOR_GAIN = (0x3FF60000 + 0x01B9), // The sensor's gain factor
        IMG_ATTR_LIGHTING_MODE = (0x3FF60000 + 0x01BB), // The internal lighting mode to use
        IMG_ATTR_LIGHTING_CURRENT = (0x3FF60000 + 0x01BC), // The amount of current sourced by the internal lighting controller =(mA),
        IMG_ATTR_LIGHTING_MAX_CURRENT = (0x3FF60000 + 0x01BD), // Returns the maximum current that can be souced by the internal lighting controller given the current configuration
        IMG_ATTR_LIGHTING_EXT_STROBE_5V_TTL = (0x3FF60000 + 0x01BE), // Whether to enable stobing on the external 5V lighting output
        IMG_ATTR_LIGHTING_EXT_STROBE_24V = (0x3FF60000 + 0x01BF), // Whether to enable stobing on the external 24V =(VCC), lighting output
        IMG_ATTR_SENSOR_EXPOSURE = (0x3FF60000 + 0x01C0), // The sensor's exposure time in milliseconds.
        IMG_ATTR_FRAME_RATE = (0x3FF60000 + 0x01C1), // The frame rate.
        IMG_ATTR_MAX_FRAME_RATE = (0x3FF60000 + 0x01C2), // The maximum frame rate.
        IMG_ATTR_SEND_SOFTWARE_TRIGGER = (0x3FF60000 + 0x01C3), // Set to issue a software trigger to the action that was configured to wait for a software trigger.
        IMG_ATTR_FIXED_FRAME_RATE_MODE = (0x3FF60000 + 0x01C4), // Controls whether the sensor operates in fixed-frame-rate mode =(true), or free-run mode =(off),.  When in fixed-frame-rate mode the sensor will run at the frame rate specified by IMG_ATTR_FRAME_RATE.  When in free-run mode, the sensor will run at the maximum frame rate possible.
        IMG_ATTR_UNSIGNED_16BIT_IMAGE = (0x3FF60000 + 0x01C5), // Whether to interpret 16 bits of pixel data as signed or unsigned.
        IMG_ATTR_POCL_STATUS = (0x3FF60000 + 0x01C6), // Returns the status of the PoCL circuitry.
        IMG_ATTR_ROWPIXELS_ALIGNMENT = (0x3FF60000 + 0x01C7), // Alignment constraint of row pixels
        IMG_ATTR_ROWPIXELS_SUGGESTED_ALIGNMENT = (0x3FF60000 + 0x01C8), // Suggested alignment constraint of row pixels
        IMG_ATTR_SUPPORTS_PULSE_UPDATE = (0x3FF60000 + 0x01C9), // Whether pulse updates are supported.
    }

    public enum IMG_OVERWRITE_MODE : uint
    {
        IMG_OVERWRITE_GET_OLDEST         = 0,
        IMG_OVERWRITE_GET_NEXT_ITERATION = 1,
        IMG_OVERWRITE_FAIL               = 2,
        IMG_OVERWRITE_GET_NEWEST         = 3
    }

    public enum Buffer_Location : uint
    {
        IMG_HOST_FRAME = 0, //Buffer in system memory
        IMG_DEVICE_FRAME = 1//Buffer on frame grabber card
    }

    public enum BlItemType : uint
    {
        IMG_BUFF_ADDRESS = (0x3FF60000 + 0x007E),   //Buffer address (IntPtr)
        IMG_BUFF_COMMAND = (0x3FF60000 + 0x007F),   //The buffer command of type BuffCommands
        IMG_BUFF_SKIPCOUNT = (0x3FF60000 + 0x0080),   //Specifies the frame skip count (uint)
        IMG_BUFF_SIZE = (0x3FF60000 + 0x0082),   //Specifies the buffer size (uint)
        IMG_BUFF_TRIGGER = (0x3FF60000 + 0x0083),   //  uInt32
        IMG_BUFF_NUMBUFS = (0x3FF60000 + 0x00B0),   //  uInt32
        IMG_BUFF_CHANNEL = (0x3FF60000 + 0x00Bc),   //The channel to which the image should be acquired (uint)
        IMG_BUFF_ACTUALHEIGHT = (0x3FF60000 + 0x0400),   //  uInt32
    }

    public enum BuffCommands : uint
    {
        IMG_CMD_NEXT = 0x01,  // Proceed to next list entry
        IMG_CMD_LOOP = 0x02,  // Loop back to start of buffer list and continue processing - RING ACQUISITION
        IMG_CMD_PASS = 0x04,  // Do nothing here
        IMG_CMD_STOP = 0x08,  // Stop
        IMG_CMD_INVALID = 0x10  // Reserved for internal use
    }

    public enum IMG_SIGNAL_TYPE : uint
    {
        IMG_SIGNAL_NONE = 0xFFFFFFFF,       //Used to disable the route.
        IMG_SIGNAL_EXTERNAL = 0,            //Specifies the signal type as the external trigger lines.
        IMG_SIGNAL_RTSI = 1,                //Specifies the signal type as the RTSI trigger lines.
        IMG_SIGNAL_ISO_IN = 2,              //Specifies the signal type as the isolated input trigger lines.
        IMG_SIGNAL_ISO_OUT = 3,             //Specifies the signal type as the isolated output trigger lines.
        IMG_SIGNAL_STATUS = 4,              //Specifies the signal type as one of the status signals, IMG_AQ_DONE, IMG_AQ_IN_PROGRESS, IMG_BUF_COMPLETE, IMG_FRAME_DONE, IMG_FRAME_START, IMG_IMMEDIATE.
        IMG_SIGNAL_SCALED_ENCODER = 5,      //Specifies the signal type as the scaled encoder signal
        IMG_SIGNAL_SOFTWARE_TRIGGER = 6     //
    }

    public enum PulseTimebase : uint
    {
        PULSE_TIMEBASE_PIXELCLK = 0x00000001, //Specifies the incoming pixel clock from the camera to use as a timebase for pulse generation.
        PULSE_TIMEBASE_50MHZ = 0x00000002,    //Specifies a 100 kHz timebase to use for pulse generation.
        PULSE_TIMEBASE_100KHZ = 0x00000003,   //Specifies a 50 MHz timebase to use for pulse generation.
        PULSE_TIMEBASE_SCALED_ENCODER = 0x00000004 //Specifies scaled encoder counts as units for pulse generation.
    }

    public enum InternalSignalIdentifier : uint
    {
        IMG_AQ_DONE = 8,     // wait for the entire acquisition to complete
        IMG_FRAME_START = 9,     // wait for the frame to start
        IMG_FRAME_DONE = 10,    // wait for the frame to complete
        IMG_BUF_COMPLETE = 11,    // wait for the buffer to complete 
        IMG_AQ_IN_PROGRESS = 15, //Asserted when the device initiates an acquisition either through a software- or hardware-triggered start.
        IMG_IMMEDIATE = 16,//Causes the function to generate a pulse when the function is executed.
        IMG_FIXED_FREQUENCY = 17,  // used in imgSessionLineTrigSrouce (linescan)
        IMG_LINE_VALID = 18, // wait for line valid signal (HSYNC)
        IMG_FRAME_VALID = 19    // wait for frame valid signal (VSYNC)
    }

    public enum TriggerPolarity : uint
    {
        IMG_TRIG_POLAR_ACTIVEH = 0,
        IMG_TRIG_POLAR_ACTIVEL = 1
    }

    public enum PulsePolarity : uint
    {
        IMG_PULSE_POLAR_ACTIVEH = 0,
        IMG_PULSE_POLAR_ACTIVEL = 1
    }

    public enum PulseMode : uint
    {
        PULSE_MODE_TRAIN = 0x00000001,          //Pulse is generated continuously after the trigger is asserted. Choose this option to generate a continuous pulse train that is inactive for the number of cycles specified in the delay parameter, and active for the number of cycles specified in the width parameter. When the pulse train is started, it continues periodically until you call imgPulseStop, imgPulseDispose, or imgClose.
        PULSE_MODE_SINGLE = 0x00000002,         //Pulse occurs one time when the first trigger occurs. Choose this option to generate a single pulse. On the first occurrence of signal_source, the output line stays inactive for the number of cycles specified in the delay parameter, and becomes active for the number of cycles specified in the width parameter. Future occurrences of signal_source do not affect the output line.
        PULSE_MODE_SINGLE_REARM = 0x00000003    //Pulse occurs one time on each trigger occurrence. Choose this option to generate a rearmed single shot pulse. On every occurrence of signal_source, the output line stays inactive for the number of cycles specified in the delay parameter, and becomes active for the number of cycles specified in the width parameter. When the pulse is started, output toggles for each occurrence of signal_source until you call imgPulseStop, imgPulseDispose, or imgClose. 
    }

    #endregion

    

    /// <summary>
    /// DLL imports from niimaq.dll
    /// </summary>
    unsafe class NIImaq
    {
        /// <summary>
        /// Raises an exception when an imaq function returns
        /// an error condition
        /// </summary>
        /// <param name="retval">The function return value to check</param>
        public static void CheckError(ImaqStatus retval)
        {
            if (retval != 0)
            {
                System.Diagnostics.Debug.WriteLine("IMAQ error. " + retval.ToString());
                throw new NIImaqException("IMAQ error. " + retval.ToString());
            }
        }

        /// <summary>
        /// Opens a camera link interface
        /// </summary>
        /// <param name="interface_name">The name of the camera link interface. Append port number after double colon if required.</param>
        /// <param name="pifid">The id of the interface</param>
        /// <returns>0 on success</returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgInterfaceOpen(string interface_name, out uint pifid);

        /// <summary>
        /// Opens a session on the provided interface
        /// </summary>
        /// <param name="ifid">The interface of the session</param>
        /// <param name="psid">The id of the created session</param>
        /// <returns>0 on success</returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgSessionOpen(uint ifid, out uint psid);

        /// <summary>
        /// Closes a session or interface (if closing interface
        /// all associated sessions will close as well)
        /// </summary>
        /// <param name="void_id">The id of the interface or session</param>
        /// <param name="freeResources">Indicates wheter to free all associated resources</param>
        /// <returns>0 if successful</returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgClose(uint void_id, [MarshalAs(UnmanagedType.Bool)] bool freeResources);

        /// <summary>
        /// Configures and optionally starts a continuous acquisition.
        /// </summary>
        /// <param name="sid">The session on which to start acquisition</param>
        /// <param name="startNow">Indicates whether to immediately start grabbing</param>
        /// <returns>0 on success</returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgGrabSetup(uint sid, [MarshalAs(UnmanagedType.Bool)] bool startNow);

        /// <summary>
        /// Starts an acquisition in the session identified by sid. Use with grab is
        /// startNow was false or acquisition was stopped.
        /// </summary>
        /// <param name="sid">The session on which to start acquisition</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgSessionStartAcquisition(uint sid);

        /// <summary>
        /// Stops the acquisition on the session
        /// </summary>
        /// <param name="sid">The session id</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern int imgSessionStopAcquisition(uint sid);

        /// <summary>
        /// Acquires the most current frame into the specified buffer.
        /// </summary>
        /// <param name="sid">Valid session id</param>
        /// <param name="bufAddr">Pointer to a pointer to an area of memory in which to store the image. If bufAddr points to a NULL pointer
        /// this call allocates an appropriate size buffer and returns the buffer address in the location specified by bufAddr</param>
        /// <param name="syncOnVB">If true waits for vertical blank - performs transfer according to video synchronization</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgGrab(uint sid, ref void* bufAddr, [MarshalAs(UnmanagedType.Bool)] bool syncOnVB);

        /// <summary>
        /// Gets the minimum buffer size needed for frame buffer allocation.
        /// </summary>
        /// <param name="sid">The session id</param>
        /// <param name="sizeNeeded">The buffer size needed for an image</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgSessionGetBufferSize(uint sid, out uint sizeNeeded);

        /// <summary>
        /// Requests the indicated attribute from the camera
        /// </summary>
        /// <param name="void_id">The interface or session id (different attributes require different one...)</param>
        /// <param name="type">The attribute to request</param>
        /// <param name="value">The value - it's type depends on the attribute</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgGetAttribute(uint void_id, ImaqAttribute type, void* value);

        /// <summary>
        /// Requests the indicated attribute from the camera
        /// </summary>
        /// <param name="void_id">The interface or session id (different attributes require different one...)</param>
        /// <param name="type">The attribute to request</param>
        /// <param name="value">The value - this overload only for uint attributes...</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgGetAttribute(uint void_id, ImaqAttribute type, out uint value);

        /// <summary>
        /// Prepares a session for acquiring continously and
        /// looping into a buffer list
        /// </summary>
        /// <param name="sid">The session id</param>
        /// <param name="numberBuffer">The number of buffers in the ring</param>
        /// <param name="bufferList">Array of pointers to the individual buffers</param>
        /// <param name="skipCount">How many frames to skip after each frame</param>
        /// <param name="startnow">If set to true immediately starts acquisition</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgRingSetup(uint sid, uint numberBuffer, [MarshalAs(UnmanagedType.LPArray)] ref IntPtr[] bufferList, uint skipCount, [MarshalAs(UnmanagedType.Bool)] bool startnow);

        /// <summary>
        /// Creates an image buffer for use during acquisition
        /// </summary>
        /// <param name="sid">The session id for bufffer creation</param>
        /// <param name="where">Wheter to create the buffer in system memory or on the card</param>
        /// <param name="bufferSize">The requested buffer size (has to be large enough to fit image)</param>
        /// <param name="bufAddr">Recieves the address of the created buffer</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgCreateBuffer(uint sid, Buffer_Location where, uint bufferSize, out IntPtr bufAddr);

        /// <summary>
        /// Disposes a user created buffer
        /// </summary>
        /// <param name="bufferPtr">The pointer to the buffer to dispose</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgDisposeBuffer(IntPtr bufferPtr);

        /// <summary>
        /// Creates a buffer list. Needs to be initialized and filled before
        /// passing to imgSessionConfigure
        /// </summary>
        /// <param name="numElements">Number of desired entries in the buffer list</param>
        /// <param name="bufListId">The id of the created buffer list</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgCreateBufList(uint numElements, out uint bufListId);

        /// <summary>
        /// Purges image buffers associated with the buffer list. Session needs to be re-
        /// configured after this call to be usable again.
        /// </summary>
        /// <param name="bid">The id of the buffer list to purge</param>
        /// <param name="freeResources">If true buffers will be disposed as well otherwise only list</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgDisposeBufList(uint bid, [MarshalAs(UnmanagedType.Bool)] bool freeResources);

        /// <summary>
        /// Sets the value for a specified itemType for a buffer in a buffer list
        /// </summary>
        /// <param name="bid">The id of the buffer list</param>
        /// <param name="element">The element in the buffer list to set the value for</param>
        /// <param name="itemType">The type of value to set</param>
        /// <param name="itemValue">The value</param>
        /// <returns></returns>
        [DllImport("imaq.dll", CallingConvention=CallingConvention.Cdecl)]
        public static extern ImaqStatus imgSetBufferElement2(uint bid, uint element, BlItemType itemType, uint itemValue);

        /// <summary>
        /// Sets the value for a specified itemType for a buffer in a buffer list
        /// </summary>
        /// <param name="bid">The id of the buffer list</param>
        /// <param name="element">The element in the buffer list to set the value for</param>
        /// <param name="itemType">The type of value to set</param>
        /// <param name="itemValue">The value</param>
        /// <returns></returns>
        [DllImport("imaq.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern ImaqStatus imgSetBufferElement2(uint bid, uint element, BlItemType itemType, IntPtr itemValue);

        /// <summary>
        /// Sets the value for a specified itemType for a buffer in a buffer list
        /// </summary>
        /// <param name="bid">The id of the buffer list</param>
        /// <param name="element">The element in the buffer list to set the value for</param>
        /// <param name="itemType">The type of value to set</param>
        /// <param name="itemValue">The value</param>
        /// <returns></returns>
        [DllImport("imaq.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern ImaqStatus imgSetBufferElement2(uint bid, uint element, BlItemType itemType, BuffCommands itemValue);
        
        /// <summary>
        /// Gets the value for a specified itemType from a buffer in a buffer list
        /// </summary>
        /// <param name="bid">The id of the buffer list</param>
        /// <param name="element">The element in the buffer list to get the value for</param>
        /// <param name="itemType">The type value to retrieve</param>
        /// <param name="itemValue">The value</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgGetBufferElement(uint bid, uint element, uint itemType, out uint itemValue);

        /// <summary>
        /// Gets the value for a specified itemType from a buffer in a buffer list
        /// </summary>
        /// <param name="bid">The id of the buffer list</param>
        /// <param name="element">The element in the buffer list to get the value for</param>
        /// <param name="itemType">The type value to retrieve</param>
        /// <param name="itemValue">The value</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgGetBufferElement(uint bid, uint element, uint itemType, out IntPtr itemValue);

        /// <summary>
        /// Gets the value for a specified itemType from a buffer in a buffer list
        /// </summary>
        /// <param name="bid">The id of the buffer list</param>
        /// <param name="element">The element in the buffer list to get the value for</param>
        /// <param name="itemType">The type value to retrieve</param>
        /// <param name="itemValue">The value</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgGetBufferElement(uint bid, uint element, uint itemType, out BuffCommands itemValue);

        /// <summary>
        /// Specifies the buffer list to use with this session.
        /// Needs a valid buffer list - aftwards imgSessionAcquire can be called.
        /// </summary>
        /// <param name="sid">The session id</param>
        /// <param name="buflist">The buffer list id</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgSessionConfigure(uint sid, uint buflist);

        /// <summary>
        /// Starts an acquisition synchronously or asynchronously to
        /// the frame buffers in the associated session buffer list.
        /// </summary>
        /// <param name="sid">The session id</param>
        /// <param name="async">If true acquisition will be asynchronous</param>
        /// <param name="callback">Gets called when acquisition is completed - can be null</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgSessionAcquire(uint sid, [MarshalAs(UnmanagedType.Bool)] bool async, void* callback);

        /// <summary>
        /// Stops an asynchronous or synchornous continuous acquisition
        /// immediately.
        /// </summary>
        /// <param name="sid">The session id</param>
        /// <param name="bufNum">The last valid buffer number</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgSessionAbort(uint sid, out uint bufNum);

        /// <summary>
        /// Clears a sessions image data to the specified pixel value
        /// </summary>
        /// <param name="sid">The session id</param>
        /// <param name="buf_num">Element number of the buffer to clear</param>
        /// <param name="pixel_value">Pixel value to set the data to</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgSessionClearBuffer(uint sid, uint buf_num, byte pixel_value);

        /// <summary>
        /// Copies an area of a session buffer to a user-specified buffer
        /// </summary>
        /// <param name="sid">The session id</param>
        /// <param name="bufNumber">The cumulative buffer number requested to copy</param>
        /// <param name="userBuffer">The image to copy to</param>
        /// <param name="overwriteMode">Sets the overwrite mode, used to determine acquisition when an image transfer cannot be
        /// completed due to an overwritten internal buffer</param>
        /// <param name="copiedNumber">Actual cumulative number provided?</param>
        /// <param name="copiedIndex">Actual cumulative index provided?</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgSessionCopyBufferByNumber(uint sid, uint bufNumber, IntPtr userBuffer, IMG_OVERWRITE_MODE overwriteMode, out uint copiedNumber, out uint copiedIndex);

        /// <summary>
        /// Locks in memory all image buffers associated with the buffer list. Buffers must
        /// be successfully locked down before acquisition. Function does not do anything
        /// if the buffers exist in onboard memory.
        /// </summary>
        /// <param name="bid">The id of the buffer list</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgMemLock(uint bid);

        /// <summary>
        /// Unlocks the buffers in the buffer list
        /// </summary>
        /// <param name="bid"></param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgMemUnlock(uint bid);

        /// <summary>
        /// Writes a command string to the serial port of the camera link interface
        /// </summary>
        /// <param name="sid">The id of the cameralink session</param>
        /// <param name="buffer">The string to send</param>
        /// <param name="bufSize">The size of the string to send on call and after call contains the number of bytes written</param>
        /// <param name="timeout">The timeout for the write in ms</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgSessionSerialWrite(uint sid, string buffer, ref uint bufSize, uint timeout);

        /// <summary>
        /// Reads a string from the serial port of the camera link interface
        /// </summary>
        /// <param name="sid">The id of the cameralink session</param>
        /// <param name="buffer">The buffer in which to receive the string - pre-initalize to bufSize capacity</param>
        /// <param name="bufSize">The maximum buffer size on call and after call the number of characters read</param>
        /// <param name="timeout">The timeout for the write</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgSessionSerialRead(uint sid, StringBuilder buffer, ref uint bufSize, uint timeout);

        /// <summary>
        /// Flushes the internal serial buffer - call btw. write and read
        /// </summary>
        /// <param name="sid">The id of the current cameralink session</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgSessionSerialFlush(uint sid);

        //public static extern ImaqStatus imgSessionSerialWrite(uint sid, Int8 *buffer, uint *bufSize, uint timeout);
        //public static extern ImaqStatus imgSessionSerialRead(uint sid, Int8 *buffer, uint *bufSize, uint timeout);
        //public static extern ImaqStatus imgSessionSerialFlush(uint sid);

        /// <summary>
        /// Creates a new pulse type that can be generated on the camera link lines
        /// </summary>
        /// <param name="timeBase">The clock timebase of the pulse</param>
        /// <param name="delay">The "delay" or inactive part of the pulse [delay × (timebase resolution)]</param>
        /// <param name="width">The width of the active part of the pulse</param>
        /// <param name="signalType">The signal type that will initiate / trigger pulse generation</param>
        /// <param name="signalIdentifier"> The identifier of the signal that will initiate the pulse generation. If the signal type is one of the triggers, then this value specifies which trigger line.
        /// If the signal type is IMG_SIGNAL_STATUS, signalIdentifier can be one of the enum constants</param>
        /// <param name="signalPolarity">When triggered externally determines if triggered on rising or falling edge</param>
        /// <param name="outputType">Type of trigger line on which the pulse is generated. outputType can be one of the following constants</param>
        /// <param name="outputNumber">Number of the trigger line on which the pulse is generated</param>
        /// <param name="outputPolarity">Polarity of the pulse output </param>
        /// <param name="pulseMode">Indicates if the pulse is generated once or continuously.</param>
        /// <param name="plsID">Receives the pulse ID.</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgPulseCreate2(PulseTimebase timeBase, uint delay, uint width, IMG_SIGNAL_TYPE signalType, uint signalIdentifier, TriggerPolarity signalPolarity, IMG_SIGNAL_TYPE outputType, uint outputNumber, PulsePolarity outputPolarity, PulseMode pulseMode, ref uint plsID);

        /// <summary>
        /// Creates a new pulse type that can be generated on the camera link lines
        /// </summary>
        /// <param name="timeBase">The clock timebase of the pulse</param>
        /// <param name="delay">The "delay" or inactive part of the pulse [delay × (timebase resolution)]</param>
        /// <param name="width">The width of the active part of the pulse</param>
        /// <param name="signalType">The signal type that will initiate / trigger pulse generation</param>
        /// <param name="signalIdentifier"> The identifier of the signal that will initiate the pulse generation. If the signal type is one of the triggers, then this value specifies which trigger line.
        /// If the signal type is IMG_SIGNAL_STATUS, signalIdentifier can be one of the enum constants</param>
        /// <param name="signalPolarity">When triggered externally determines if triggered on rising or falling edge</param>
        /// <param name="outputType">Type of trigger line on which the pulse is generated. outputType can be one of the following constants</param>
        /// <param name="outputNumber">Number of the trigger line on which the pulse is generated</param>
        /// <param name="outputPolarity">Polarity of the pulse output </param>
        /// <param name="pulseMode">Indicates if the pulse is generated once or continuously.</param>
        /// <param name="plsID">Receives the pulse ID.</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgPulseCreate2(PulseTimebase timeBase, uint delay, uint width, IMG_SIGNAL_TYPE signalType, InternalSignalIdentifier signalIdentifier, TriggerPolarity signalPolarity, IMG_SIGNAL_TYPE outputType, uint outputNumber, PulsePolarity outputPolarity, PulseMode pulseMode, ref uint plsID);

        /// <summary>
        /// Stops generation and disposes resources of a given pulse id
        /// </summary>
        /// <param name="plsID">The pulse identifier</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgPulseDispose(uint plsID);

        /// <summary>
        /// Arms a pulse to be triggered (or started immediately)
        /// </summary>
        /// <param name="plsID">The id of the pulse</param>
        /// <param name="sid">The session associated with the pulse</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgPulseStart(uint plsID, uint sid);

        /// <summary>
        /// Stops the given pulse
        /// </summary>
        /// <param name="plsID">The id of the pulse</param>
        /// <returns></returns>
        [DllImport("imaq.dll")]
        public static extern ImaqStatus imgPulseStop(uint plsID);

        //public static extern ImaqStatus imgPulseCreate2(uint timeBase, uint delay, uint width, IMG_SIGNAL_TYPE signalType, uint signalIdentifier, uint signalPolarity, IMG_SIGNAL_TYPE outputType, uint outputNumber, uint outputPolarity, uint pulseMode, PULSE_ID* plsID);
        //public static extern ImaqStatus imgPulseDispose(PULSE_ID plsID);
        //public static extern ImaqStatus imgPulseStart(PULSE_ID pid,uint sid);
        //public static extern ImaqStatus imgPulseStop(PULSE_ID pid);

//public static extern ImaqStatus imgSnap(uint sid, void **bufAddr);
//public static extern ImaqStatus imgSnapArea(uint sid, void **bufAddr,uint top,uint left, uint height, uint width,uint rowBytes);
//public static extern ImaqStatus imgGrabArea(uint sid, void** bufPtr, uint syncOnVB, uint top, uint left, uint height, uint width, uint rowBytes);
//public static extern ImaqStatus imgSequenceSetup(uint sid,  uint numberBuffer,void* bufferList[], uint skipCount[], uint startnow, uint async);
//public static extern ImaqStatus imgSessionStatus(uint sid, ref uint boardStatus, ref uint bufIndex);
//public static extern ImaqStatus imgSessionConfigureROI(uint sid, uint top, uint left, uint height, uint width);
//public static extern ImaqStatus imgSessionGetROI(uint sid, ref uint top, ref uint left, ref uint height, ref uint width);






//public static extern ImaqStatus imgSessionGetLostFramesList(uint sid, ref uint framelist, uint numEntries);
//public static extern ImaqStatus imgSessionReleaseBuffer(uint sid);
//public static extern ImaqStatus imgSessionSetUserLUT8bit(uint sid, uint lutType, uInt8 *lut);
//public static extern ImaqStatus imgSessionSetUserLUT16bit(uint sid, uint lutType, uInt16 *lut);
//public static extern ImaqStatus imgGetCameraAttributeNumeric(uint sid, const Int8* attributeString,double *currentValueNumeric);
//public static extern ImaqStatus imgSetCameraAttributeNumeric(uint sid, const Int8* attributeString,double newValueNumeric);
//public static extern ImaqStatus imgGetCameraAttributeString(uint sid, const Int8* attributeString,Int8* currentValueString,uint sizeofCurrentValueString);
//public static extern ImaqStatus imgSetCameraAttributeString(uint sid, const Int8* attributeString,Int8* newValueString);

//public static extern ImaqStatus imgSessionSerialReadBytes(uint sid, char* buffer, uint *bufferSize, uint timeout);


//public static extern ImaqStatus imgSessionWaitSignal2(uint sid, IMG_SIGNAL_TYPE signalType, uint signalIdentifier, uint signalPolarity, uint timeout);
//public static extern ImaqStatus imgSessionWaitSignalAsync2(uint sid, IMG_SIGNAL_TYPE signalType, uint signalIdentifier, uint signalPolarity, CALL_BACK_PTR2 funcptr, void* callbackData);
//public static extern ImaqStatus imgSessionTriggerDrive2(uint sid, IMG_SIGNAL_TYPE trigType, uint trigNum, uint polarity, uint signal);
//public static extern ImaqStatus imgSessionTriggerRead2(uint sid, IMG_SIGNAL_TYPE trigType, uint trigNum, uint polarity, ref uint status);
//public static extern ImaqStatus imgSessionTriggerRoute2(uint sid, IMG_SIGNAL_TYPE srcTriggerType, uint srcTriggerNumber, IMG_SIGNAL_TYPE dstTriggerType, uint dstTriggerNumber);
//public static extern ImaqStatus imgSessionTriggerClear(uint sid);
//public static extern ImaqStatus imgSessionTriggerConfigure2(uint sid, IMG_SIGNAL_TYPE trigType, uint trigNum, uint polarity, uint timeout, uint action);
//public static extern ImaqStatus imgSessionSaveBufferEx(uint sid, void *buffer,Int8* file_name);
//public static extern ImaqStatus imgShowError(IMG_ERR error, Int8* text);
//public static extern ImaqStatus imgInterfaceReset(uint ifid);
//public static extern ImaqStatus imgInterfaceQueryNames(uint index, Int8* queryName);
//public static extern ImaqStatus imgCalculateBayerColorLUT(double redGain, double greenGain, double blueGain, ref uint redLUT, ref uint greenLUT, ref uint blueLUT, uint bitDepth);
//public static extern ImaqStatus imgBayerColorDecode(void* dst, const void* src, uint rows, uint cols, uint dstRowPixels, uint srcRowPixels, const ref uint redLUT, const ref uint greenLUT, const ref uint blueLUT, uInt8 bayerPattern, uint bitDepth, uint reserved);
//public static extern ImaqStatus imgSessionLineTrigSource2(uint sid, IMG_SIGNAL_TYPE trigType, uint trigNum, uint polarity, uint skip);
//public static extern ImaqStatus imgSessionFitROI(uint sid, IMG_ROI_FIT_MODE fitMode, uint top, uint left, uint height, uint width, ref uint fittedTop, ref uint fittedLeft, ref uint fittedHeight, ref uint fittedWidth);
//public static extern ImaqStatus imgEncoderResetPosition(uint sid);

//public static extern ImaqStatus imgSessionCopyAreaByNumber(uint sid, uint bufNumber, uint top, uint left, uint height, uint width, void* userBuffer, uint rowPixels, IMG_OVERWRITE_MODE overwriteMode, ref uint copiedNumber, ref uint copiedIndex);
//public static extern ImaqStatusC imgSetAttribute2(uint void_id, uint attribute, ...);

//public static extern ImaqStatus imgSessionExamineBuffer2(uint sid, uint whichBuffer, uint *bufferNumber, void** bufferAddr);
//public static extern ImaqStatus imgPlot2(void* hwnd, void* buffer, uint leftBufOffset, uint topBufOffset, uint xsize, uint ysize, uint xpos, uint ypos, uint flags);
//public static extern ImaqStatus imgPlotDC2(void* hdc, void* buffer, uint xbuffoff, uint ybuffoff, uint xsize, uint ysize, uint xscreen, uint yscreen, uint flags);
//public static extern ImaqStatus imgSetAttributeFromVoidPtr(uint void_id, uint attribute, const void* valuePtr);
    }
}
