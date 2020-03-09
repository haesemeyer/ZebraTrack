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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace MHApi.Utilities
{
    /// <summary>
    /// List type with a ToString overrride that concatenates all items intermitting a newline
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class StringConvertibleList<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Gives a string representation of all items, each on a separate line
        /// </summary>
        /// <returns>A string representation of all items</returns>
        public override string ToString()
        {
            if (this.Count == 0)
                return "";
            else
            {
                //Lets assume that on average each item is represented by 40 characters
                //The stringbuilder gives us better performance than a regular string
                //since the string type is immutable
                StringBuilder retval = new StringBuilder(Count * 40);
                foreach (T item in this)
                {
                    retval.AppendLine(item.ToString());
                }
                return retval.ToString();
            }
        }

    }
}
