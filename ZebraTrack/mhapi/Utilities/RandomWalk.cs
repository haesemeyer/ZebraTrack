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

using MHApi.Imaging;

namespace MHApi.Utilities
{
    /// <summary>
    /// Simple class for uniform random walk
    /// in an 8-connected neighborhood of a point
    /// </summary>
    public static class RandomWalk
    {
        /// <summary>
        /// The random number generator used to decide
        /// the next walking direction
        /// </summary>
        private static Random _decider = new Random();

        /// <summary>
        /// We use recursion to obtain valid coordinates within limits
        /// since this is a random process in theory every possible
        /// recursion depth will be reached at some point - so better limit this
        /// </summary>
        private static int _recurseDepth = 0;

        /// <summary>
        /// The maximum recursion depth we allow before simply returning
        /// the current point as the next point
        /// </summary>
        const int MaxRecurseDepth = 100;

        /// <summary>
        /// Based on the current position computes the next
        /// position in the 8-connected neighborhood
        /// </summary>
        /// <param name="current">The current position</param>
        /// <returns>The next position</returns>
        public static IppiPoint Next(IppiPoint current)
        {
            var decider = _decider.Next(0, 8);
            IppiPoint retval = new IppiPoint(current.x, current.y);
            switch (decider)
            {
                case 0:
                    retval.y++;
                    break;
                case 1:
                    retval.x++;
                    retval.y++;
                    break;
                case 2:
                    retval.x++;
                    break;
                case 3:
                    retval.x++;
                    retval.y--;
                    break;
                case 4:
                    retval.y--;
                    break;
                case 5:
                    retval.x--;
                    retval.y--;
                    break;
                case 6:
                    retval.x--;
                    break;
                case 7:
                    retval.x--;
                    retval.y++;
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false, "Unrecognized decider in random walk");
                    break;
            }
            return retval;
        }

        /// <summary>
        /// Based on the current position computes the next
        /// position in the 8-connected neighborhood only returning
        /// points within the given rectangular limits
        /// </summary>
        /// <param name="current">The current position</param>
        /// <param name="xmin">The minimal allowed x-coordinate (inclusive)</param>
        /// <param name="xmax">The maximal allowed x-coordinate (inclusive)</param>
        /// <param name="ymin">The minimal allowed y-coordinate (inclusive)</param>
        /// <param name="ymax">The maximal allowed y-coordinate (inclusive)</param>
        /// <returns>The new point within the limits</returns>
        public static IppiPoint Next(IppiPoint current, int xmin, int xmax, int ymin, int ymax)
        {
            if (xmin >= xmax || ymin >= ymax)
                throw new ArgumentException("Minimal limit must be less than maximal limit");
            if (current.x < xmin || current.x > xmax || current.y < ymin || current.y > ymax)
                throw new ArgumentException("Current point can't be outside the set limits");

            //increment our recursion counter
            _recurseDepth++;

            IppiPoint retval = Next(current);
            //if the returned point is outside of the limits, retry recursively
            if (retval.x < xmin || retval.x > xmax || retval.y < ymin || retval.y > ymax)
            {
                if (_recurseDepth > MaxRecurseDepth)
                {
                    //have reached recursion limit - don't walk
                    _recurseDepth--;
                    return current;
                }
                retval = Next(current, xmin, xmax, ymin, ymax);
                //we are about to return, decrement recursion counter
                _recurseDepth--;
                return retval;
            }
            else
            {
                //return, decrement recursion counter
                _recurseDepth--;
                return retval;
            }
        }

        /// <summary>
        /// Based on the current position computes the next
        /// position in the 8-connected neighborhood only returning
        /// points within the given circle
        /// </summary>
        /// <param name="current">The current position</param>
        /// <param name="center">The center of the circle of valid positions</param>
        /// <param name="radius">The radius of the valid area</param>
        /// <returns>The new point within the limits</returns>
        public static IppiPoint Next(IppiPoint current, IppiPoint center, float radius)
        {
            if (radius < 2)
                throw new ArgumentOutOfRangeException("radius", "The radius has to be two or more pixels");
            if (Distance.Euclidian(current, center) > radius)
                throw new ArgumentException("The current point can't be outside of the valid circle.");

            //increment our recursion counter
            _recurseDepth++;

            IppiPoint retval = Next(current);
            //if the returned point is outside of the limits, retry recursively
            if (Distance.Euclidian(retval,center)>radius)
            {
                if (_recurseDepth > MaxRecurseDepth)
                {
                    //have reached recursion limit - don't walk
                    _recurseDepth--;
                    return current;
                }
                retval = Next(current, center, radius);
                //we are about to return, decrement recursion counter
                _recurseDepth--;
                return retval;
            }
            else
            {
                //return, decrement recursion counter
                _recurseDepth--;
                return retval;
            }
        }

    }
}
