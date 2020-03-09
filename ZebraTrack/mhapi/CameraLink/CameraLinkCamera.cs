/*
Copyright 2016 Martin Haesemeyer
   Licensed under the MIT License, see License.txt.
   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
   SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MHApi.CameraLink.Imports;

using MHApi.DrewsClasses;
using MHApi.Imaging;

using ipp;


namespace MHApi.CameraLink
{
	/// <summary>
	/// Represents a camera running off a camera
	/// link interface.
	/// </summary>
	public unsafe class CameraLinkCamera : IDisposable
	{
		#region Fields

		/// <summary>
		/// The session id
		/// </summary>
		uint _sid;

		/// <summary>
		/// The interface id
		/// </summary>
		uint _ifid;

		/// <summary>
		/// The buffer list id
		/// </summary>
		uint _bufId;

		/// <summary>
		/// The width of the image (ROI)
		/// </summary>
		uint _width;

		/// <summary>
		/// The height of the image (ROI)
		/// </summary>
		uint _height;

		/// <summary>
		/// The bytes per pixel delivered by the camera
		/// </summary>
		uint _bytesPerPixel;

		/// <summary>
		/// The bits per pixel delivered by the camera
		/// </summary>
		uint _bitsPerPixel;

		/// <summary>
		/// This factor is used if we need
		/// to stretch the image to fill 16 bits
		/// </summary>
		ushort _scaleFactor;

		/// <summary>
		/// Indicates whether the session has already been
		/// configured for acquisition
		/// </summary>
		bool _configured;

		/// <summary>
		/// Indicates whether we are grabbing frames or not
		/// </summary>
		bool _captureRunning;

		/// <summary>
		/// The frame obtained from the buffer
		/// </summary>
		uint _acquiredFrames;

		/// <summary>
		/// The internal buffers used for acquisition
		/// </summary>
		IntPtr[] _ringBuffer;

		/// <summary>
		/// Image to allow downsizing of pixel depth to 8 bit
		/// </summary>
		Image16 _imgDownsize;

		#endregion

		#region Constructor

		/// <summary>
		/// Constructs a new IMAQ camera on the
		/// specified camera link interface
		/// </summary>
		/// <param name="interfaceId">The interface name. Optionally followed by ::# identifying the port - 0 based</param>
		public CameraLinkCamera(string interfaceId)
		{
			//Attach to interface
			NIImaq.CheckError(NIImaq.imgInterfaceOpen(interfaceId, out _ifid));
			//Open session
            NIImaq.CheckError(NIImaq.imgSessionOpen(_ifid, out _sid));
			//Get image dimensions
            NIImaq.CheckError(NIImaq.imgGetAttribute(_sid, ImaqAttribute.IMG_ATTR_ROI_WIDTH, out _width));
            NIImaq.CheckError(NIImaq.imgGetAttribute(_sid, ImaqAttribute.IMG_ATTR_ROI_HEIGHT, out _height));
            NIImaq.CheckError(NIImaq.imgGetAttribute(_sid, ImaqAttribute.IMG_ATTR_BYTESPERPIXEL, out _bytesPerPixel));
			System.Diagnostics.Debug.WriteLine("Bytes per pixel {0}", _bytesPerPixel);
            NIImaq.CheckError(NIImaq.imgGetAttribute(_sid, ImaqAttribute.IMG_ATTR_BITSPERPIXEL, out _bitsPerPixel));
			if (_bytesPerPixel == 2 && _bitsPerPixel < 16)
			{
				_scaleFactor = 1;
				_scaleFactor <<= (16-(int)_bitsPerPixel);
			}
			else
				_scaleFactor = 1;
			System.Diagnostics.Debug.WriteLine("Bits per pixel {0}", _bitsPerPixel);
			System.Diagnostics.Debug.WriteLine("Frame width: {0}", _width);
			System.Diagnostics.Debug.WriteLine("Frame height: {0}", _height);
			//Unfortunately there is now way (?) to nicely align the camera buffers on 4-byte boundary for ipp
			//The user generated image wrappers will expect this however - so for now do NOT allow to
			//start acquisition with a width which is non-dividable by 4!!
			System.Diagnostics.Debug.Assert(Width % 4 == 0, "At the moment for memory alignment, the class requires an image width which is dividable by 4!!");
			//We also only allow images with either 1 or 2 bytes per pixel!
			if (BytesPerPixel < 1 || BytesPerPixel > 2)
				System.Diagnostics.Debug.Assert(false, "Unknown pixel depth", "Currently a pixel depth of {0} bytes is not supported", BytesPerPixel);
		}

		#endregion

		#region Properties

		/// <summary>
		/// The image width
		/// </summary>
		public int Width
		{
			get
			{
				return (int)_width;
			}
		}

		/// <summary>
		/// The image height
		/// </summary>
		public int Height
		{
			get
			{
				return (int)_height;
			}
		}

		/// <summary>
		/// Indicates whether we are currently grabbing frames
		/// </summary>
		public bool CaptureRunning
		{
			get
			{
				return _captureRunning;
			}
		}

		/// <summary>
		/// The bits per pixel delivered by the camera
		/// </summary>
		public uint BytesPerPixel
		{
			get
			{
				return _bytesPerPixel;
			}
		}

		/// <summary>
		/// Indicates whether the session has already
		/// been configured for acquisition
		/// </summary>
		public bool Configured
		{
			get
			{
				return _configured;
			}
		}

		/// <summary>
		/// The total frames acquired on this camera
		/// </summary>
		public uint AcquiredFrames
		{
			get
			{
				return _acquiredFrames;
			}
		}

		#endregion

		#region Methods

		

		/// <summary>
		/// Configures the camera setting up required buffers.
		/// Allows separation of this time-intensive task from
		/// acquisition start.
		/// </summary>
		/// <param name="bufferCount">The number of frames requested as buffer</param>
		public void Configure(uint bufferCount)
		{
			//set up ring buffer pointers
			_ringBuffer = new IntPtr[bufferCount];
			//create buffer list
            NIImaq.CheckError(NIImaq.imgCreateBufList(bufferCount, out _bufId));
			//compute required buffer size            
			uint bufSize = _width * _height * BytesPerPixel;
			//if this is a 16bit acquisition we pre-allocate an internal buffer
			//to allow us handing out 8bit images instead
			_imgDownsize = new Image16((int)Width, (int)Height);
			//Create our camera buffers and set up the buffer list
			//We set the list up as a ring buffer which means that the command
			//is NEXT for each buffer except the last one, where it will be loop
			for (uint i = 0; i < bufferCount; i++)
			{
				//Let imaq obtain the buffer memory
                NIImaq.CheckError(NIImaq.imgCreateBuffer(_sid, Buffer_Location.IMG_HOST_FRAME, bufSize, out _ringBuffer[i]));
				//assign buffer to our buffer list
                NIImaq.CheckError(NIImaq.imgSetBufferElement2(_bufId, i, BlItemType.IMG_BUFF_ADDRESS, _ringBuffer[i]));
				//tell the list about our buffer size
                NIImaq.CheckError(NIImaq.imgSetBufferElement2(_bufId, i, BlItemType.IMG_BUFF_SIZE, bufSize));
				//Set the appropriate buffer command
				var bufCmd = i < (bufferCount - 1) ? BuffCommands.IMG_CMD_NEXT : BuffCommands.IMG_CMD_LOOP;
                NIImaq.CheckError(NIImaq.imgSetBufferElement2(_bufId, i, BlItemType.IMG_BUFF_COMMAND, bufCmd));
			}
			//lock buffer list (supposed to be obsolote but example still does it...)
            NIImaq.CheckError(NIImaq.imgMemLock(_bufId));
			//configure the session to use this buffer list
            NIImaq.CheckError(NIImaq.imgSessionConfigure(_sid, _bufId));
			_configured = true;
		}

		/// <summary>
		/// Starts acquisition on the camera and configures
		/// the session if necessary
		/// </summary>
		/// <param name="bufferCount">The number of frames requested as buffer</param>
		public void Start(int bufferCount)
		{
			if (_captureRunning)
			{
				System.Diagnostics.Debug.WriteLine("Tried to start already running capture");
				return;
			}
			if (!_configured)
				Configure((uint)bufferCount);
			//Start asynchronous acquisition - after this call the ring-buffer
			//will get filled with images!
            NIImaq.CheckError(NIImaq.imgSessionAcquire(_sid, true, null));
			_captureRunning = true;
			_acquiredFrames = 0;
		}

		/// <summary>
		/// Ends acquisition and de-configures the session
		/// </summary>
		public void Stop()
		{
			if (!_captureRunning)
			{
				System.Diagnostics.Debug.WriteLine("Tried to stop non-running capture");
				return;
			}
			uint lastBuffNum;
			NIImaq.imgSessionAbort(_sid, out lastBuffNum);
			
			System.Threading.Thread.Sleep(500);
			if (_bufId != 0)
				NIImaq.imgMemUnlock(_bufId);
			//free buffers
			for (int i = 0; i < (uint)_ringBuffer.Length; i++)
				if (_ringBuffer[i] != IntPtr.Zero)
					NIImaq.imgDisposeBuffer(_ringBuffer[i]);
			//close buffer list
			if (_bufId != 0)
				NIImaq.imgDisposeBufList(_bufId, false);
			_captureRunning = false;
			_configured = false;
		}

		/// <summary>
		/// Extracts an 8-bit image reducing bit depth if necessary
		/// </summary>
		/// <param name="imageOut">The container to recieve the image</param>
		/// <param name="requestedFrame">The framenumber to request</param>
		/// <returns>The frame number actually retrieved from the buffer</returns>
		public uint Extract(Image8 imageOut, uint requestedFrame)
		{
			uint  frameActual, indexActual;
			if (_bytesPerPixel == 2)
			{
				//this requires acquisition into our internal buffer first
				//followed by handing out a downsized copy
				frameActual = Extract(_imgDownsize, requestedFrame);
				//if our bit-depth is not 16 we have a problem - we need
				//to scale our image first to really fill the 16bits otherwise we run into
				//problems later...
				if (_bitsPerPixel < 16)
				{
					ip.ippiMulC_16u_C1IRSfs(_scaleFactor, _imgDownsize.Image, _imgDownsize.Stride, _imgDownsize.Size, 0);
				}
				//we got handed 8 bits
				_imgDownsize.ReduceTo8U(imageOut);
			}
			else
			{
                NIImaq.CheckError(NIImaq.imgSessionCopyBufferByNumber(_sid, requestedFrame, (IntPtr)imageOut.Image, IMG_OVERWRITE_MODE.IMG_OVERWRITE_GET_NEWEST, out frameActual, out indexActual));
				if (frameActual != requestedFrame)
				{
					System.Diagnostics.Debug.WriteLine("Requested frame {0}; obtained frame {1}", requestedFrame, frameActual);
				}
			}
			_acquiredFrames++;
			return frameActual;
		}

		/// <summary>
		/// Extracts a 16-bit image
		/// </summary>
		/// <param name="imageOut">The container to recieve the image</param>
		/// <param name="requestedFrame">The framenumber to request</param>
		/// <returns>The frame number actually retrieved from the buffer</returns>
		public uint Extract(Image16 imageOut, uint requestedFrame)
		{
			uint frameActual, indexActual;
			if (_bytesPerPixel == 1)
				System.Diagnostics.Debug.WriteLine("Acquired 8 bit image into 16bit structure");
            NIImaq.CheckError(NIImaq.imgSessionCopyBufferByNumber(_sid, requestedFrame, (IntPtr)imageOut.Image, IMG_OVERWRITE_MODE.IMG_OVERWRITE_GET_NEWEST, out frameActual, out indexActual));
			if (frameActual != requestedFrame)
			{
				System.Diagnostics.Debug.WriteLine("Requested frame {0}; obtained frame {1}", requestedFrame, frameActual);
			}
			_acquiredFrames++;
			return frameActual;
		}

		/// <summary>
		/// Writes a serial command to the camera and returns the number of characters written
		/// </summary>
		/// <param name="command">The serial command to write including the appropriate termination char</param>
		/// <returns>The number of characters written to the interface</returns>
		public uint SerialWrite(string command)
		{
			uint written = (uint)command.Length;
            NIImaq.CheckError(NIImaq.imgSessionSerialWrite(_sid, command, ref written, 1000));
			return written;
		}

		/// <summary>
		/// Reads up to the specified number of characters from the serial buffer
		/// </summary>
		/// <param name="charsToRead">The (maximum) number of characters to read on call and the number
		/// of characters actually read after the function call</param>
		/// <returns>The read string from the serial port</returns>
		public string SerialRead(ref uint charsToRead)
		{
			StringBuilder sb = new StringBuilder((int)charsToRead);
            NIImaq.CheckError(NIImaq.imgSessionSerialRead(_sid, sb, ref charsToRead, 1000));
			return sb.ToString();
		}

		/// <summary>
		/// Flushes the serial buffer. Call btw. consecutive writes and reads
		/// </summary>
		public void SerialFlush()
		{
            NIImaq.CheckError(NIImaq.imgSessionSerialFlush(_sid));
		}

		#endregion

		#region Cleanup

		public bool IsDisposed { get; private set; }

		protected virtual void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;
			if (_captureRunning)
				Stop();
			//Close interface and session
			NIImaq.imgClose(_sid, true);
			NIImaq.imgClose(_ifid, true);
			if (_imgDownsize != null)
			{
				_imgDownsize.Dispose();
				_imgDownsize = null;
			}
		}

		public void Dispose()
		{
			Dispose(true);
            IsDisposed = true;
		}

		~CameraLinkCamera()
		{
			if (!IsDisposed)
			{
				System.Diagnostics.Debug.WriteLine("Forgot to dispose camera!");
                Dispose(false);
			}
		}

		#endregion
	}
}
