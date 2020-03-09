/*
Copyright 2011 Drew Robson
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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace MHApi.DrewsClasses {
    public sealed class TiffWriter : IDisposable {
        public bool IsDisposed { get; private set; }
        string baseName;

        string CurrentFileNameWithExtension { get { return baseName + "_" + fileIndex.ToString("D4") + ".tif"; } }

        static ImageCodecInfo tiffCodecInfo;

        static TiffWriter() {
            foreach (ImageCodecInfo i in ImageCodecInfo.GetImageEncoders())
                if (i.FormatDescription == "TIFF") {
                    tiffCodecInfo = i;
                    break;
                }
        }

        /// <summary>
        /// Constructs a new TiffWriter
        /// </summary>
        /// <param name="fileName">The name of the file we want to save to</param>
        public TiffWriter(string fileName) : this(fileName,false) {}

        /// <summary>
        /// Constructs a new TiffWriter making the filename unique if required
        /// </summary>
        /// <param name="fileName">The name of the file we want to save to</param>
        public TiffWriter(string fileName, bool renameIfExists)
        {
            baseName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName));
            if (File.Exists(CurrentFileNameWithExtension))
            {
                if (!renameIfExists)
                    throw new IOException("TiffWriter error: File " + CurrentFileNameWithExtension + " already exists");
                else
                {
                    string fWoExtension = Path.GetFileNameWithoutExtension(fileName);
                    string dir = Path.GetDirectoryName(fileName);
                    fWoExtension += '_' + DateTime.Now.Ticks.ToString();
                    baseName = Path.Combine(dir, fWoExtension);
                }

            }
            if (!Directory.Exists(Path.GetDirectoryName(CurrentFileNameWithExtension)))
                Directory.CreateDirectory(Path.GetDirectoryName(CurrentFileNameWithExtension));
        }

        bool firstFrame = true;
        IntPtr pFirstIm;
        Bitmap firstBmp;
        int fileIndex;


        public unsafe void WriteFrame(byte[,] frame) {
            int width = frame.GetLength(1);
            int height = frame.GetLength(0);
            if (firstFrame) {
                pFirstIm = Marshal.AllocHGlobal(width * height);
                byte* pImRaw = (byte*)pFirstIm;
                for (int i = 0; i < height; i++)
                    for (int j = 0; j < width; j++)
                        *pImRaw++ = frame[i, j];
                firstBmp = new Bitmap(width, height, width, PixelFormat.Format8bppIndexed, pFirstIm);
                ColorPalette palette = firstBmp.Palette;
                int length = palette.Entries.Length;
                for (int i = 0; i < length; i++)
                    palette.Entries[i] = Color.FromArgb((int)(255 * (double)i / length), (int)(255 * (double)i / length),
                                                        (int)(255 * (double)i / length));
                firstBmp.Palette = palette;

                var EncoderParams = new EncoderParameters(2);
                EncoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionNone);
                EncoderParams.Param[1] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.MultiFrame);
                firstBmp.Save(CurrentFileNameWithExtension, tiffCodecInfo, EncoderParams);
                firstFrame = false;
            } else {
                if (new FileInfo(CurrentFileNameWithExtension).Length > 2040109466) { // If we're above 1.9 GB, start a new file to avoid 2GB limitation in some tiff readers
                    fileIndex++;
                    firstFrame = true;
                    WriteFrame(frame);
                    return;
                }
                IntPtr pIm = Marshal.AllocHGlobal(width * height);
                byte* pImRaw = (byte*)pIm;
                for (int i = 0; i < height; i++)
                    for (int j = 0; j < width; j++)
                        *pImRaw++ = frame[i, j];
                Bitmap bmp = new Bitmap(width, height, width, PixelFormat.Format8bppIndexed, pIm);
                ColorPalette palette = bmp.Palette;
                int length = palette.Entries.Length;
                for (int i = 0; i < length; i++)
                    palette.Entries[i] = Color.FromArgb((int)(255 * (double)i / length), (int)(255 * (double)i / length),
                                                        (int)(255 * (double)i / length));
                bmp.Palette = palette;

                var EncoderParams = new EncoderParameters(2);
                EncoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionNone);
                EncoderParams.Param[1] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.FrameDimensionPage);
                firstBmp.SaveAdd(bmp, EncoderParams);

                bmp.Dispose();
                bmp = null;
                Marshal.FreeHGlobal(pIm);
                pIm = IntPtr.Zero;
            }
        }

        public unsafe void WriteFrame(Image8 image) {
            int width = image.Width;
            int height = image.Height;
            int stride = image.Stride;
            if (firstFrame) {
                firstBmp = new Bitmap(width, height, stride, PixelFormat.Format8bppIndexed, (IntPtr) image.Image);
                var palette = firstBmp.Palette;
                int length = palette.Entries.Length;
                for (int i = 0; i < length; i++)
                    palette.Entries[i] = Color.FromArgb((int)(255 * (double)i / length), (int)(255 * (double)i / length),
                                                        (int)(255 * (double)i / length));
                firstBmp.Palette = palette;

                var EncoderParams = new EncoderParameters(2);
                EncoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionNone);
                EncoderParams.Param[1] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.MultiFrame);
                firstBmp.Save(CurrentFileNameWithExtension, tiffCodecInfo, EncoderParams);
                firstFrame = false;
            } else {
                if (new FileInfo(CurrentFileNameWithExtension).Length > 2040109466) { // If we're above 1.9 GB, start a new file to avoid 2GB limitation in some tiff readers
                    fileIndex++;
                    firstFrame = true;
                    WriteFrame(image);
                    return;
                }
                var bmp = new Bitmap(width, height, stride, PixelFormat.Format8bppIndexed, (IntPtr) image.Image);
                var palette = bmp.Palette;
                int length = palette.Entries.Length;
                for (int i = 0; i < length; i++)
                    palette.Entries[i] = Color.FromArgb((int)(255 * (double)i / length), (int)(255 * (double)i / length),
                                                        (int)(255 * (double)i / length));
                bmp.Palette = palette;

                var EncoderParams = new EncoderParameters(2);
                EncoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionNone);
                EncoderParams.Param[1] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.FrameDimensionPage);
                firstBmp.SaveAdd(bmp, EncoderParams);

                bmp.Dispose();
            }
        }

        #region IDisposable Members

        public void Dispose() {
            if (IsDisposed) return;
            if (!firstFrame) {
                var EncoderParams = new EncoderParameters(1);
                EncoderParams.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.Flush);
                firstBmp.SaveAdd(EncoderParams);
                firstBmp.Dispose();
                firstBmp = null;
                Marshal.FreeHGlobal(pFirstIm);
                pFirstIm = IntPtr.Zero;
            }
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }

        ~TiffWriter() {
            System.Diagnostics.Debug.WriteLine("IDisposable class " + ToString() + " finalized without being disposed first. Always call Dispose() to allow class to clean up");
            Dispose();
        }

        #endregion
    }
}