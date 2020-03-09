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
    /// Static class providing several standard
    /// distance measures
    /// </summary>
    public static class Distance
    {
        /// <summary>
        /// Computes the Euclidian distance between two points
        /// </summary>
        /// <param name="p1">Point1</param>
        /// <param name="p2">Point2</param>
        /// <returns>The euclidian distance</returns>
        public static double Euclidian(IppiPoint p1, IppiPoint p2)
        {
            return Math.Sqrt((p1.x - p2.x) * (p1.x - p2.x) + (p1.y - p2.y) * (p1.y - p2.y));
        }

        /// <summary>
        /// Computes the Euclidian distance between two points
        /// </summary>
        /// <param name="p1">Point1</param>
        /// <param name="p2">Point2</param>
        /// <returns>The euclidian distance</returns>
        public static double Euclidian(IppiPoint_32f p1, IppiPoint_32f p2)
        {
            return Math.Sqrt((p1.x - p2.x) * (p1.x - p2.x) + (p1.y - p2.y) * (p1.y - p2.y));
        }

        /// <summary>
        /// Computes the 4-connected path distance between two points
        /// </summary>
        /// <param name="p1">Point1</param>
        /// <param name="p2">Point2</param>
        /// <returns>The minimum length 4-connected path between the two points</returns>
        public static int D4(IppiPoint p1, IppiPoint p2)
        {
            return Math.Abs(p1.x - p2.x) + Math.Abs(p1.y - p2.y);
        }

        /// <summary>
        /// Computes the 4-connected path distance between two points
        /// </summary>
        /// <param name="p1">Point1</param>
        /// <param name="p2">Point2</param>
        /// <returns>The minimum length 4-connected path between the two points</returns>
        public static int CityBlock(IppiPoint p1, IppiPoint p2){
            return D4(p1,p2);
        }

        /// <summary>
        /// Computes the 8-connected path distance between two points
        /// </summary>
        /// <param name="p1">Point1</param>
        /// <param name="p2">Point2</param>
        /// <returns>The minimum length 8-connected path between the two points</returns>
        public static int D8(IppiPoint p1, IppiPoint p2)
        {
            return Math.Max(Math.Abs(p1.x - p2.x), Math.Abs(p1.y - p2.y));
        }

        /// <summary>
        /// Computes the 8-connected path distance between two points
        /// </summary>
        /// <param name="p1">Point1</param>
        /// <param name="p2">Point2</param>
        /// <returns>The minimum length 8-connected path between the two points</returns>
        public static int Chessboard(IppiPoint p1, IppiPoint p2)
        {
            return D8(p1, p2);
        }
    }
}
