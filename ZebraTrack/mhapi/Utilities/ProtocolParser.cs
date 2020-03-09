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
    /// Base class for protocol parsers requiring validation and unfolding of goto-steps
    /// </summary>
    /// <typeparam name="ProtocolType">The protocol type used; must derive from IList(IGotoStep)</typeparam>
    /// <typeparam name="StepType">The steptype used; must derive from IGotoStep</typeparam>
    public class ProtocolParser<ProtocolType,StepType> where ProtocolType:IList<StepType> where StepType:IStep
    {
        protected ProtocolParser() { }

        /// <summary>
        /// Uses a dictionary relating steps with repeat counts to compute the next step
        /// after a goto instruction has been encountered
        /// </summary>
        /// <param name="stepNumber">The step at which a goto instruction was encountered</param>
        /// <param name="gotoMap">The dictionary relating steps and repeat counts</param>
        /// <returns>The next step to process</returns>
        protected static int ProcessGoto(StepType step, int stepNumber, Dictionary<int, uint> gotoMap)
        {
            //have we been here before?
            if (gotoMap.ContainsKey(stepNumber))//yes we have been here before
            {
                //do we have any repeats left?
                if (gotoMap[stepNumber] > 0)//yes we have repeats left
                {

                    //We have to re-enable potential in-between goto steps, giving them their original repeat count back
                    //The easiest way to accomplish this is to remove those steps (since in that case we don't have to keep track of what their original value was)
                    //To do this we use LinQ to find all goto steps that have a stepNumber smaller than the current step - this will ensure that potential in-between
                    //gotos are removed!
                    IEnumerable<int> query = from k in gotoMap.Keys where k < stepNumber select k;
                    query = query.ToArray();
                    foreach (int key in query)
                        gotoMap.Remove(key);
                    gotoMap[stepNumber]--;//decrement goto repeat counter
                    stepNumber = step.GotoStep;//go back to the indicated step
                }
                else //no repeats left, lets proceed through the loop
                    stepNumber++;
            }
            else//no we haven't been here before
            {
                gotoMap.Add(stepNumber, step.RepeatCount);
                //For the following code, see comment above
                IEnumerable<int> query = from k in gotoMap where k.Key < stepNumber select k.Key;
                query = query.ToArray();
                foreach (int key in query)
                    gotoMap.Remove(key);
                gotoMap[stepNumber]--;//decrement goto repeat counter
                stepNumber = step.GotoStep;//go back to the indicated step
            }
            return stepNumber;
        }

        public static bool ValidateProtocolGoto(ProtocolType protocol)
        {
            IEnumerable<int> queryGotoSteps = from step in protocol where step.StepType == ProtocolStepType.Goto select step.GotoStep;
            IEnumerable<int> queryInvalid = from gStep in queryGotoSteps where gStep > protocol.Count || protocol[gStep].StepType == ProtocolStepType.Goto select gStep;
            if (queryInvalid.ToArray().Length > 0)
                return false;
            else
                return true;
        }
    }
}
