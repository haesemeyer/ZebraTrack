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
using System.Drawing;

using ipp;

namespace MHApi.Imaging
{
    /// <summary>
    /// Describes an ROI in an image
    /// </summary>
    public class IppiROI
    {
        private IppiSize _size;

        private IppiPoint _topleft;

        /// <summary>
        /// The size of the ROI
        /// </summary>
        public IppiSize Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
            }
        }

        /// <summary>
        /// The width of the ROI
        /// </summary>
        public int Width
        {
            get
            {
                return _size.width;
            }
            set
            {
                _size.width = value;
            }
        }

        /// <summary>
        /// The height of the ROI
        /// </summary>
        public int Height
        {
            get
            {
                return _size.height;
            }
            set
            {
                _size.height = value;
            }
        }

        /// <summary>
        /// The topleft corner of the ROI
        /// </summary>
        public IppiPoint TopLeft
        {
            get
            {
                return _topleft;
            }
            set
            {
                _topleft = value;
            }
        }

        /// <summary>
        /// The X-coordinate of the topleft corner of the ROI
        /// </summary>
        public int X
        {
            get
            {
                return _topleft.x;
            }
            set
            {
                _topleft.x = value;
            }
        }

        /// <summary>
        /// The y-coordinate of the topleft corner of the ROI
        /// </summary>
        public int Y
        {
            get
            {
                return _topleft.y;
            }
            set
            {
                _topleft.y = value;
            }
        }

        public IppiROI(int x, int y, int width, int height)
        {
            _size = new IppiSize(width, height);
            _topleft = new IppiPoint(x, y);
        }

        public IppiROI(IppiPoint topleft, IppiSize roiSize)
        {
            _size = roiSize;
            _topleft = topleft;
        }

        public IppiROI(Rectangle rect)
        {
            _size = new IppiSize(rect.Width, rect.Height);
            _topleft = new IppiPoint(rect.X, rect.Y);
        }

        /// <summary>
        /// Converts the well ROI into a string representation following the convention:
        /// topleft.x topleft.y width height
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0}\t{1}\t{2}\t{3}", TopLeft.x, TopLeft.y, Width, Height);
        }
    }
}
