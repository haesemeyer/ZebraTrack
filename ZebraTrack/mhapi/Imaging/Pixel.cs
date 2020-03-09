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

namespace MHApi.Imaging
{
    /// <summary>
    /// Represents an image pixel with a position and its brightness value
    /// </summary>
    public struct Pixel
    {
        /// <summary>
        /// The position of the pixel
        /// </summary>
        public PointF Position;

        /// <summary>
        /// The brightness of the pixel
        /// </summary>
        public byte Value;

        /// <summary>
        /// Constructs a new pixel
        /// </summary>
        /// <param name="position">The position</param>
        /// <param name="value">The brightness</param>
        public Pixel(PointF position, byte value)
        {
            Position = position;
            Value = value;
        }

        /// <summary>
        /// Constructs a new pixel
        /// </summary>
        /// <param name="x">The x-Coordinate of the position</param>
        /// <param name="y">The y-Coordinate of the position</param>
        /// <param name="value">The brightness</param>
        public Pixel(float x, float y, byte value)
        {
            Position = new PointF(x, y);
            Value = value;
        }
    }
}
