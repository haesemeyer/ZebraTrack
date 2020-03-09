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
using System.ComponentModel;
using System.Windows;
using System.Diagnostics;

using MHApi.Utilities;

namespace MHApi.GUI
{
    /// <summary>
    /// Abstract base class for view models providing Designtime support,
    /// Support for property change notificiations and implementation of a dispose
    /// pattern as well as an associated IsDisposing event.
    /// </summary>
    public abstract class ViewModelBase : PropertyChangeNotification, IDisposable
    {

        #region DesignModeHelper
        static bool? isInDesignMode;
        public static bool IsInDesignMode
        {
            get
            {
                if (!isInDesignMode.HasValue)
                {
                    var prop = DesignerProperties.IsInDesignModeProperty;
                    isInDesignMode = (bool)DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement)).Metadata.DefaultValue;
                    if (!isInDesignMode.Value && Process.GetCurrentProcess().ProcessName.StartsWith("devenv", StringComparison.Ordinal))
                        isInDesignMode = true;
                }
                return isInDesignMode.Value;
            }
        }

        #endregion


        //INotifyPropertyChanged is now implemented in PropertyChangeNotification baseclass
      /*  #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected void RaisePropertyChanged(string name)
        {
            if (name != "")
            {
                VerifyPropertyName(name);
            }
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        protected void VerifyPropertyName(string propertyName)
        {
            var myType = GetType();
            if (myType.GetProperty(propertyName) == null)
                throw new ArgumentException("Property not found", propertyName);
        }
        #endregion*/

        #region IDisposable
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// This event gets raised when the viewmodel gets disposed
        /// </summary>
        public EventHandler<EventArgs> IsDisposing = delegate { };

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    //Since by default we assign the empty delegate no need to check for null!!
                    IsDisposing(this, EventArgs.Empty);
                }
                IsDisposed = true;
            }
        }

        ~ViewModelBase()
        {
            Dispose(false);
        }

        #endregion
    }
}
