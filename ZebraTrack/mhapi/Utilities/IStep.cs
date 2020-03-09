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
    /// Determines if a protocol step describes a goto command or experimental condition
    /// </summary>
    public enum ProtocolStepType { Action = 0, Goto = 1 };

    /// <summary>
    /// Provides properties describing a step-type and goto descriptors in a protocol
    /// </summary>
    public interface IStep
    {
        /// <summary>
        /// The type of the protocol step
        /// </summary>
        ProtocolStepType StepType { get; set; }

        /// <summary>
        /// The step in the protocol to which the goto points
        /// </summary>
        int GotoStep { get; set; }

        /// <summary>
        /// The number of times the goto should be repeated
        /// </summary>
        uint RepeatCount { get; set; }
 
    }
}
