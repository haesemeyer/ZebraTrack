/*
Copyright 2019 Martin Haesemeyer
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
using System.IO;
using ZebraTrack.Experiments;

using MHApi.GUI;

namespace ZebraTrack.ViewModels
{
    /// <summary>
    /// View model for laser alignment
    /// </summary>
    class LaserAlignmentViewModel : ViewModelBase, IExperimentVM
    {
        public ExperimentBase CreateExperiment(string name, string fishID)
        {
            string folder = Path.Combine(Properties.Settings.Default.SavePath, "LaserAlignment");
            //TODO: Allow specifying alignment grid spacing [currently 8 pixels], ROI [currently camera FOV]
            //and accuracy requirement [currently 0 pixels]
            return new LaserAlignment(Properties.Settings.Default.FrameRate);
        }
    }
}
