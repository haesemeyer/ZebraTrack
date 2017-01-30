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

        public override bool ProcessNext(int frameNumber, Image8 camImage, out IppiPoint? poi)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
