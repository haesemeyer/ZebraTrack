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
//using System.Drawing;
//using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using MHApi.DrewsClasses;

namespace MHApi.Imaging
{
    public unsafe class TiffReader : IDisposable
    {
        /// <summary>
        /// The filenames from which we sequentially read
        /// </summary>
        string[] _fileNames;

        /// <summary>
        /// The index of the file being currently read
        /// </summary>
        int _currentFileIndex;

        /// <summary>
        /// The index of the next file to read
        /// </summary>
        int _currentImageIndex;

        Stream _imageStreamSource;

        TiffBitmapDecoder _tiffDecoder;

        /// <summary>
        /// Constructs a new tiff-reader
        /// </summary>
        /// <param name="filenames">The full filenames including directories. Will be read sequentially</param>
        /// <param name="alphaSort">Determines if the list of filenames should be sorted alphabetically</param>
        public TiffReader(string[] filenames, bool alphaSort)
        {
            if (filenames == null)
                throw new ArgumentNullException("filenames");
            foreach (string file in filenames)
            {
                if (file == null)
                    throw new ArgumentException("Can't pass null as a filename", "filenames");
                if (!File.Exists(file))
                    throw new FileNotFoundException("At least one of the files in the list could not be found", file);
            }
            if (alphaSort)
            {
                var sorted = from s in filenames orderby s select s;
                _fileNames = sorted.ToArray();
            }
            else
            {
                _fileNames = filenames;
            }
            _currentFileIndex = 0;
            _currentImageIndex = 0;
        }

        public bool ReadNextTiff(ref Image8 nextImage)
        {
            BitmapSource bitmapSource;
            if (_imageStreamSource == null)//this is the first read
            {
                //Initialize image stream - open first file
                _imageStreamSource = new FileStream(_fileNames[_currentFileIndex], FileMode.Open, FileAccess.Read, FileShare.Read);
                _tiffDecoder = new TiffBitmapDecoder(_imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
            }
            else//this is not the first read
            {
                //check if the frame we are supposed to read is outside of the current file
                if (_currentImageIndex >= _tiffDecoder.Frames.Count)
                {
                    //close old file
                    _tiffDecoder = null;
                    _imageStreamSource.Close();
                    _imageStreamSource.Dispose();
                    //advance file index
                    _currentFileIndex++;
                    //check if the current file is already the last file - in that case we are done reading
                    if (_currentFileIndex >= _fileNames.Length)
                    {
                        return false;
                    }
                    else
                    {
                        //open next file and create corresponding decoder
                        _imageStreamSource = new FileStream(_fileNames[_currentFileIndex], FileMode.Open, FileAccess.Read, FileShare.Read);
                        _tiffDecoder = new TiffBitmapDecoder(_imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                        _currentImageIndex = 0;
                    }
                }//if _currentImageIndex >= _tiffDecoder.Frames.Count
            }
            //at this point we should have a valid file open, the tiffdecoder set and _currentImageIndex should point to the image we want to return next
            bitmapSource = _tiffDecoder.Frames[_currentImageIndex];
            //check that nextImage is compatible with the images
            if (nextImage == null || nextImage.Width != bitmapSource.PixelWidth || nextImage.Height != bitmapSource.PixelHeight)
            {
                nextImage = new Image8(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
            }
            //copy content from the bitmap source into nextImage
            if (bitmapSource.Format == PixelFormats.Gray8 || bitmapSource.Format == PixelFormats.Indexed8)
            {
                bitmapSource.CopyPixels(System.Windows.Int32Rect.Empty, (IntPtr)nextImage.Image, nextImage.Height * nextImage.Stride, nextImage.Stride);
            }
            else
            {
                throw new NotSupportedException("File pixel formats other than 8-bit indexed or 8-bit grayscale are currently not supported");
            }
            _currentImageIndex++;
            return true;
            //throw new NotImplementedException();
        }

        #region IDisposable

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                if (_imageStreamSource != null)
                {
                    _imageStreamSource.Dispose();
                    _imageStreamSource = null;
                }
                IsDisposed = true;
            }
        }

        #endregion
    }
}
