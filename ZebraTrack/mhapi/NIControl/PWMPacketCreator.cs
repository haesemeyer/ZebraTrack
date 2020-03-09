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

namespace MHApi.NIControl
{
    static public class PWMPacketCreator
    {
        //The frequency of the internal sample clock
        private static int sampleClockFrequency = 20000;

        //The number of channels we request PWM packets for
        private static byte nChannels;

        /// <summary>
        /// The SampleClockFrequency that is used on the board to deliver the PWM output on the digital line
        /// </summary>
        public static int SampleClockFrequency
        {
            get
            {
                return sampleClockFrequency;
            }
            set
            {
                if (value < 1 || value > 1000000)
                    throw new System.ArgumentOutOfRangeException("Sample clock frequency has to be between 1Hz and 1Mhz");
                sampleClockFrequency = value;
            }
        }

        /// <summary>
        /// The number of channels which should be affected by the PWM packets
        /// </summary>
        public static byte NChannels
        {
            get
            {
                return nChannels;
            }
            set
            {
                if (value < 1 || value > 32)
                    throw new ArgumentOutOfRangeException("The number of channels has to be between 1 and 32");
                nChannels = value;
            }
        }

        /// <summary>
        /// Gets a data packet representing one cycle of a PWM stream with the desired parameters
        /// </summary>
        /// <param name="desiredPWMFrequency">The frequency of the underlying square wave</param>
        /// <param name="desiredFractionOn">The desired fraction of the square wave for which the output is high</param>
        /// <returns>Returns the packet descibing one cycle of the square wave</returns>
        public static uint[] GetPWMPacket(int[] desiredPWMFrequency, double[] desiredFractionOn)
        {

            //Check that enough parameters are supplied
            if (desiredPWMFrequency.Length != nChannels || desiredFractionOn.Length != nChannels)
            {
                throw new ApplicationException("A desiredPWMFrequency and desiredFractionOn has to be provided once for each channel!");
            }

            //Check that parameters adhere to common standards
            if (desiredFractionOn.Min() < 0 || desiredFractionOn.Max() > 1)
            {
                throw new ArgumentOutOfRangeException("desiredFractionOn cannot be smaller than 0 or larger than 1!");
            }
            if (desiredPWMFrequency.Min() <= 0)
            {
                throw new ArgumentOutOfRangeException("The desired PWM frequency has to be larger than 0Hz");
            }

            int[] cyclesPerPulse = new int[nChannels];
            int[] packetsHigh = new int[nChannels];

            //Check that the sampleClockFrequency can support the desired PWM and compute the cyclesPerPulse as well as packetsHigh for each channel
            for (int i = 0; i < nChannels; i++)
            {
                //in case of continuous high or low we can encode this in a single cycle independent of the desired frequency
                //THIS HOWEVER MEANS THAT: If all channels with the same desiredPWMFrequency transition to a fractionHigh of 0 or 1
                //the number of generated packets suddenly decreases!!!
                if (desiredFractionOn[i] == 0 || desiredFractionOn[i] == 1)
                {
                    cyclesPerPulse[i] = 1;
                }
                else if (desiredPWMFrequency[i] / desiredFractionOn[i] > sampleClockFrequency || desiredPWMFrequency[i] / (1 - desiredFractionOn[i]) > sampleClockFrequency)
                {
                    throw new ApplicationException("The set SampleClockFrequency cannot be used to obtain the desired PWMPacket");
                }
                else
                {
                    cyclesPerPulse[i] = (int)(sampleClockFrequency / desiredPWMFrequency[i]);
                }
                packetsHigh[i] = (int)(cyclesPerPulse[i] * desiredFractionOn[i]);
            }

            //We try to generate one second worth of packets
            //int nPackets = cyclesPerPulse.Max();
            int nPackets = sampleClockFrequency;

            //we need to adjust nPackets so that it is dividable by ALL cyclesPerPulse values
            foreach (int cyc in cyclesPerPulse)
            {
                if (nPackets % cyc != 0)
                    nPackets *= cyc;
            }

            System.Diagnostics.Debug.WriteLineIf(nPackets != sampleClockFrequency, "Generated more than one second of packets.");

            

            uint[] pwmPacket = new uint[nPackets];

            

            //Loop over the channels
            for (int i = 0; i < nChannels; i++)
            {
                //For each channel, loop over the whole payload
                for (int j = 0; j < nPackets; j++)
                {
                    if ((j % cyclesPerPulse[i]) < packetsHigh[i])
                        pwmPacket[j] = pwmPacket[j] | (uint)(1 << i);


                }
            }

            

            return pwmPacket;
        }

        /*public static uint[] GetPWMSecond(uint[] pwmPacket, int second)
        {
            if (second < 0)
            {
                throw new ArgumentOutOfRangeException("second has to be 0 or positive");
            }
            var packetLength = pwmPacket.Length;
            var start = sampleClockFrequency * second;
            start = start % packetLength;
            var end = start + sampleClockFrequency-1;
            uint[] retval = new uint[sampleClockFrequency];
            //if end is still within the packet we have an easy job
            if (end < packetLength)
            {
                for (int i = 0; i < sampleClockFrequency; i++)
                {
                    retval[i] = pwmPacket[start + i];
                }
            }
            else
            {
                //first we loop from start to the end of pwmPacket and then we loop around
                for (int i = 0; i < packetLength - start; i++)
                {
                    retval[i] = pwmPacket[start + i];
                }
                for (int i = packetLength - start; i < sampleClockFrequency; i++)
                {
                    retval[i] = pwmPacket[start + i - packetLength];
                }
            }
            return retval;
        }*/

        
    }
}
