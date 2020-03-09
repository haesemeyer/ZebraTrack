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
using ipp;

namespace MHApi.Imaging
{
    /// <summary>
    /// Exception thrown if an Ipp call returns an error
    /// </summary>
    public class IppException : Exception
    {
        /// <summary>
        /// Exception constructor
        /// </summary>
        /// <param name="errorCode">The return code of an ipp function call</param>
        public IppException(IppStatus errorCode) : base(errorCode.ToString()) { }
        
    }

    public static class IppHelper
    {
        /// <summary>
        /// Checks an ipp function return value writes errors to the debug output
        /// </summary>
        /// <param name="retval">The function return value</param>
        /// <exception cref="IppException">Throws IppException if retval != IppStatus.ippStsNoErr</exception>
        public static void IppCheckCall(IppStatus retval)
        {
            if (retval != IppStatus.ippStsNoErr)
            {
                System.Diagnostics.Debug.WriteLine(retval);
            }
        }
    }
}
