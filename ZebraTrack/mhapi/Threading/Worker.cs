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

using System.Windows.Threading;

namespace MHApi.Threading
{
    /// <summary>
    /// Instantiation of WorkerT that allows the thread job
    /// to execute code on the parents thread via a dispatcher object
    /// </summary>
    public class Worker : WorkerT<Dispatcher>
    {
        public Worker(ThreadJob job, bool autostart, int joinTimeout) : base(job, Dispatcher.CurrentDispatcher, autostart, joinTimeout)
        { }

        public Worker(ThreadJob job) : this(job, true, 500)
        { }

        public Worker(ThreadJob job, bool autostart) : this(job, autostart, 500)
        { }
    }
}
