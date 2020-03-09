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

namespace MHApi.Utilities
{
    /// <summary>
    /// Represents the strength of illumination in R,G,B and IR channels
    /// </summary>
    public struct IlluminationInfo
    {
        private double r;

        private double g;

        private double b;

        private double ir;

        /// <summary>
        /// Checks if a value can serve as a fraction
        /// </summary>
        /// <param name="val">The value to be checked</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if value not btw. 0 and 1</exception>
        private static void CheckFraction(double val)
        {
            if (val < 0 || val > 1)
            {
                throw new ArgumentOutOfRangeException("Illumination fractions have to be between 0 and 1");
            }
        }

        /// <summary>
        /// The fraction of red light
        /// </summary>
        public double R
        {
            get
            {
                return r;
            }
            set
            {
                CheckFraction(value);
                r = value;
            }
        }

        /// <summary>
        /// The fraction of green light
        /// </summary>
        public double G
        {
            get
            {
                return g;
            }
            set
            {
                CheckFraction(value);
                g = value;
            }
        }

        /// <summary>
        /// The fraction of blue light
        /// </summary>
        public double B
        {
            get
            {
                return b;
            }
            set
            {
                CheckFraction(value);
                b = value;
            }
        }

        /// <summary>
        /// The fraction of IR light
        /// </summary>
        public double IR
        {
            get
            {
                return ir;
            }
            set
            {
                CheckFraction(value);
                ir = value;
            }
        }

        /// <summary>
        /// Constructor initializing the elements
        /// </summary>
        /// <param name="red">The fraction of red light</param>
        /// <param name="green">The fraction of green light</param>
        /// <param name="blue">The fraction of blue light</param>
        /// <param name="infrared">The fraction infrared light</param>
        public IlluminationInfo(double red, double green, double blue, double infrared)
        {
            CheckFraction(red);
            CheckFraction(green);
            CheckFraction(blue);
            CheckFraction(infrared);
            r = red;
            g = green;
            b = blue;
            ir = infrared;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="info">The illumination info to be copied</param>
        public IlluminationInfo(IlluminationInfo info)
        {
            r = info.R;
            g = info.G;
            b = info.B;
            ir = info.IR;
        }
    }
}
