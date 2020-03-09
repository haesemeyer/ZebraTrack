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

namespace MHApi.RND
{
    /// <summary>
    /// Class to generate exponentially distributed
    /// random numbers by transform of uniform
    /// distribution
    /// </summary>
    public sealed class RandExp
    {
        /// <summary>
        /// Uniform random number generator used
        /// to generate the exponentially distributed numbers
        /// </summary>
        readonly Random _random;

        public RandExp(Random random = null)
        {
            _random = random ?? ThreadSafeRandom.ThisThreadsRandom;
        }

        /// <summary>
        /// Generates an exponentially distributed random number from
        /// an exponential distribution with the indicated scale beta
        /// mean = beta
        /// var = beta^2
        /// </summary>
        /// <param name="beta">The scale (=mean) of the distribution</param>
        /// <returns>Random number from the distribution</returns>
        public double NextExponential(double beta)
        {
            var u = _random.NextDouble();
            return -1 * beta * Math.Log(u);
        }
    }
}
