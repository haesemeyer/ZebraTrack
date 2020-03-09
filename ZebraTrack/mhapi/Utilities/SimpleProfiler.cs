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

using System.Diagnostics;

namespace MHApi.Utilities
{
    /// <summary>
    /// Helper class to time (repeating)
    /// sections of code by name
    /// </summary>
    public static class SimpleProfiler
    {
        private struct TimeData
        {
            /// <summary>
            /// Number of total elapsed ticks
            /// </summary>
            public double Elapsed;

            /// <summary>
            /// Number of measurement iterations
            /// </summary>
            public double Iterations;

            /// <summary>
            /// The time elapsed per iteration in
            /// milliseconds
            /// </summary>
            public double AverageInMilliseconds
            {
                get
                {
                    var avg = Elapsed / Iterations;
                    return avg / Stopwatch.Frequency * 1000;
                }
            }

            /// <summary>
            /// The total time elapsed in seconds
            /// </summary>
            public double TotalInSeconds
            {
                get
                {
                    return Elapsed / Stopwatch.Frequency;
                }
            }
        }

        /// <summary>
        /// Dictionary relating measurement names to measurements
        /// </summary>
        private static Dictionary<string, TimeData> _timings = new Dictionary<string, TimeData>();

        /// <summary>
        /// Dictionary to keep track of tick-counts at the start of a new measurement
        /// </summary>
        private static Dictionary<string, long> _startTicks = new Dictionary<string, long>();

        private static object _dictLocker = new object();

        /// <summary>
        /// Starts a new named measurement
        /// </summary>
        /// <param name="name">The name of the measurement</param>
        [Conditional("DEBUG")]
        public static void StartNextMeasurement(string name)
        {
            lock (_dictLocker)
            {
                _startTicks[name] = Stopwatch.GetTimestamp();
            }
        }

        /// <summary>
        /// End a previously started measurement and add
        /// the elapsed time
        /// </summary>
        /// <param name="name">The name of the measurement</param>
        [Conditional("DEBUG")]
        public static void EndNextMeasurement(string name)
        {
            var end = Stopwatch.GetTimestamp();
            lock (_dictLocker)
            {
                if (_startTicks.ContainsKey(name))
                {
                    if (_timings.ContainsKey(name))
                    {
                        var t = _timings[name];
                        t.Iterations++;
                        t.Elapsed += end - _startTicks[name];
                        _timings[name] = t;
                    }
                    else
                    {
                        var t = new TimeData();
                        t.Iterations = 1;
                        t.Elapsed = end - _startTicks[name];
                        _timings[name] = t;
                    }
                }
                else
                    Debug.WriteLine("Can't end measurement that hasn't been started. Ignored call.", name);
            }
        }

        /// <summary>
        /// Writes the number elapsed milliseconds per iteration
        /// to the debug console
        /// </summary>
        /// <param name="name">The name of the measurement</param>
        [Conditional("DEBUG")]
        public static void ReportAverageMilliseconds(string name)
        {
            lock (_dictLocker)
            {
                if (_timings.ContainsKey(name))
                {
                    Debug.WriteLine("Each iteration of {0} took {1} ms.", name, _timings[name].AverageInMilliseconds);
                }
                else
                    Debug.WriteLine("No measurement with this name exists.", name);
            }
        }

        /// <summary>
        /// Report all per iteration averaged
        /// performance measures
        /// </summary>
        public static void ReportAllAverageMilliseconds()
        {
            lock (_dictLocker)
            {
                foreach (string k in _timings.Keys)
                {
                    ReportAverageMilliseconds(k);
                }
            }
        }

        /// <summary>
        /// Writes the total number of elapsed seconds
        /// to the debug console
        /// </summary>
        /// <param name="name">The name of the measurement</param>
        [Conditional("DEBUG")]
        public static void ReportTotalSeconds(string name)
        {
            lock (_dictLocker)
            {
                if (_timings.ContainsKey(name))
                {
                    Debug.WriteLine("Total time taken by {0} was {1} seconds.", name, _timings[name].TotalInSeconds);
                }
                else
                    Debug.WriteLine("No measurement with this name exists.", name);
            }
        }

        /// <summary>
        /// Resets the named measurement
        /// </summary>
        /// <param name="name">The name of the measurement</param>
        [Conditional("DEBUG")]
        public static void Reset(string name)
        {
            lock (_dictLocker)
            {
                if (_timings.ContainsKey(name))
                {
                    _timings[name] = new TimeData();
                }
                else
                    Debug.WriteLine("No measurement with this name exists.", name);
            }
        }

    }
}
