/*
Copyright 2017 Martin Haesemeyer
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
using System.Threading.Tasks;

using MHApi.Scanning;
using MHApi.Laser;
using ipp;
using MHApi.DrewsClasses;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace ZebraTrack.Experiments
{
    /// <summary>
    /// "Experimental" class to perform stimulus laser
    /// alignment to camera coordinates
    /// </summary>
    class LaserAlignment : ExperimentBase
    {

        /// <summary>
        /// Creates a new LaserAlignment object
        /// </summary>
        /// <param name="frameRate">The frameRate of the experiment</param>
        public LaserAlignment(int frameRate) : base("", "", "", 0, frameRate)
        {
        }

        #region Properties

        public override int SecondsRemaining
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string StatusMessage
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Computes an average angle for a set of angles
        /// </summary>
        /// <param name="angles">The angles which should be averaged in radians</param>
        /// <returns>The average angle in radians</returns>
        public static double AverageAngles(double[] angles)
        {
            double sinsum = 0;
            double cossum = 0;
            foreach(var angle in angles)
            {
                sinsum += Math.Sin(angle);
                cossum += Math.Cos(angle);
            }
            return Math.Atan2(sinsum, cossum);
        }

        /// <summary>
        /// Computes the augmented rotation matrix for an augmented 2D point around the origin
        /// </summary>
        /// <param name="theta">The clockwise rotation angle in radians</param>
        /// <returns>The augmented (3x3) 2D rotation matrix</returns>
        public static Matrix<double> RotationMatrix(double theta)
        {
            return DenseMatrix.OfArray(new double[,]
            {
                {Math.Cos(theta), Math.Sin(theta), 0 },
                {-1*Math.Sin(theta), Math.Cos(theta), 0 },
                {0, 0, 1 }
            });
        }

        /// <summary>
        /// Computes the augmented translation matrix for an augmented 2D point
        /// </summary>
        /// <param name="dx">Translation amount in x direction</param>
        /// <param name="dy">Translation amount in y direction</param>
        /// <returns>The augmented (3x3) 2D translation matrix</returns>
        public static Matrix<double> TranslationMatrix(double dx, double dy)
        {
            return DenseMatrix.OfArray(new double[,]
            {
                {1, 0, dx },
                {0, 1, dy },
                {0, 0, 1 }
            });
        }

        /// <summary>
        /// Augments a 2D coordinate point
        /// </summary>
        /// <param name="px">The x-coordinate</param>
        /// <param name="py">The y-coordinate</param>
        /// <returns>A 3 element vector comprising the point</returns>
        public static Vector<double> Augment2DCoordinate(double px, double py)
        {
            return DenseVector.OfArray(new double[] { px, py, 1 });
        }

        /// <summary>
        /// Composes (multiplies) a list of transformation matrices in order
        /// NOTE: List should be ordered in order of application which needs to be reversed for the composition!
        /// </summary>
        /// <param name="matrices">List of transformations to compose</param>
        /// <returns>The composed matrix</returns>
        public Matrix<double> Compose(List<Matrix<double>> matrices)
        {
            Matrix<double> m = DenseMatrix.CreateIdentity(3);
            for(int i = matrices.Count-1; i>=0; i--)
                m *= matrices[i];
            return m;
        }

        /// <summary>
        /// Applies transformation to given point
        /// </summary>
        /// <param name="p"></param>
        /// <param name="T"></param>
        /// <returns></returns>
        public static IppiPoint TransformPoint(IppiPoint p, Matrix<double> T)
        {
            Vector<double> result = Augment2DCoordinate(p.x, p.y) * T;
            return new IppiPoint((int)result[0], (int)result[1]);
        }

        /// <summary>
        /// Applies transformation to given point
        /// </summary>
        /// <param name="p"></param>
        /// <param name="T"></param>
        /// <returns></returns>
        public static IppiPoint_32f TransformPoint(IppiPoint_32f p, Matrix<double> T)
        {
            Vector<double> result =  T * Augment2DCoordinate(p.x, p.y);
            return new IppiPoint_32f((float)result[0], (float)result[1]);
        }

        /// <summary>
        /// Translates and reflects a camera coordinate point such that the origin
        /// is placed in the center of the image and y-coordinates increase going up
        /// </summary>
        /// <param name="coordinate">The camera coordinate</param>
        /// <param name="imageSize">The size of the camera image</param>
        /// <returns>Transformed coordinate</returns>
        private static IppiPoint CamCenterTransform(IppiPoint coordinate, IppiSize imageSize)
        {
            return new IppiPoint(coordinate.x - imageSize.width / 2, -1 * (coordinate.y - imageSize.height / 2));
        }

        /// <summary>
        /// Translates mirror voltages in relation to preset roughly centered resting voltages
        /// </summary>
        /// <param name="voltages"></param>
        /// <returns></returns>
        private static IppiPoint_32f MirrorCenterTransform(IppiPoint_32f voltages)
        {
            return new IppiPoint_32f(voltages.x + Properties.Settings.Default.MirrorXVC, voltages.y + Properties.Settings.Default.MirrorYVC);
        }

        public override bool ProcessNext(int frameNumber, Image8 camImage, out IppiPoint? poi)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
