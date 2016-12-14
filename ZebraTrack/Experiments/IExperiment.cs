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
using System.Threading.Tasks;

using ipp;
using MHApi.DrewsClasses;
using MHApi.Imaging;

namespace ZebraTrack.Experiments
{
    /// <summary>
    /// Generic experimental interface
    /// </summary>
    interface IExperiment
    {
        /// <summary>
        /// Processes the next frame
        /// </summary>
        /// <param name="frameNumber">The index of the current frame</param>
        /// <param name="fish">The tracked fish object</param>
        /// <param name="fishImage">Small ROI image of the fish for further processing</param>
        /// <returns>True if the experiment should continue</returns>
        bool ProcessNext(int frameNumber, BlobWithMoments fish, Image8 fishImage);

        /// <summary>
        /// The number of seconds remaining in the experiment
        /// </summary>
        int SecondsRemaining { get; }

        /// <summary>
        /// A status message about the experiment that can be displayed
        /// </summary>
        string StatusMessage { get; }
    }
}
