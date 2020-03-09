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

using ipp;

namespace MHApi.Imaging
{
    /// <summary>
    /// Coordinate conversions
    /// </summary>
    public static class Coordinates
    {
        public enum Rotation { None, Clock90, Clock180, CounterClock90 };

        /// <summary>
        /// Rotates a coordinate within a given reference frame
        /// </summary>
        /// <param name="original">The original coordinate</param>
        /// <param name="size">The dimensions of the reference frame</param>
        /// <param name="rot">The desired rotation</param>
        /// <returns>The rotated coordinate</returns>
        public static IppiPoint Rotate(IppiPoint original, IppiSize size, Rotation rot)
        {
            switch (rot)
            {
                case Rotation.None:
                    return original;
                case Rotation.Clock90:
                    return new IppiPoint(size.height - original.y, original.x);
                case Rotation.Clock180:
                    return new IppiPoint(size.width - original.x, size.height - original.y);
                default:
                    return new IppiPoint(original.y, size.width - original.x);
            }
        }

        /// <summary>
        /// Inverses a coordinate rotation within a given reference frame
        /// </summary>
        /// <param name="original">The rotated coordinate</param>
        /// <param name="size">The dimensions of the reference frame</param>
        /// <param name="rot">The original rotation</param>
        /// <returns>The original point</returns>
        public static IppiPoint RotateInverse(IppiPoint rotated, IppiSize size, Rotation rot)
        {
            switch (rot)
            {
                case Rotation.None:
                    return rotated;
                case Rotation.Clock90:
                    return new IppiPoint(rotated.x, size.height-rotated.y);
                case Rotation.Clock180:
                    return new IppiPoint(size.width-rotated.x, size.height - rotated.y);
                default:
                    return new IppiPoint(size.width-rotated.y, rotated.x);
            }
        }
    }
}
