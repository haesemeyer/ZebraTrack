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

using ZebraTrack.Experiments;

namespace ZebraTrack.ViewModels
{
    /// <summary>
    /// Interface definition for experiment describing
    /// View Models
    /// </summary>
    interface IExperimentVM
    {
        /// <summary>
        /// Use user entered information to create experimental class
        /// </summary>
        /// <param name="name">The name of the experiment</param>
        /// <param name="fishID">The fish id for the experiment</param>
        /// <returns></returns>
        ExperimentBase CreateExperiment(string name, string fishID);
    }
}
