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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;

namespace MHApi.Utilities
{
    /// <summary>
    /// Base class for protocol steps which is aware of its position in the containing collection
    /// and implements the logic to update its representation when its collection changes
    /// </summary>
    public abstract class ProtocolStepBase<T> : INotifyPropertyChanged where T:ProtocolStepBase<T>
    {


        /// <summary>
        /// Text needs to be overwritten in derived classes and a propertychanged event
        /// gets raised when the containing collection gets updated
        /// </summary>
        public abstract string Text { get; }

        /// <summary>
        /// The position of this protocol step in the protocol collection
        /// </summary>
        public int MyPosition
        {
            get
            {
                ObservableCollection<T> temp = MyCollection;
                if (temp == null)
                    return -1;
                else
                {
                    return temp.IndexOf((T)this);
                }
            }
        }

        /// <summary>
        /// The collection should be garbage collectable when its last reference expires
        /// We cannot guarantee this if every item in it holds a reference to the collection itself
        /// Therefore we keep a weak reference only (because of the delegate this is actually mute now)
        /// </summary>
        private WeakReference<ObservableCollection<T>> _myCollection;

        /// <summary>
        /// References the collection to which this item belongs
        /// </summary>
        public ObservableCollection<T> MyCollection
        {
            protected get
            {
                return _myCollection == null ? null : _myCollection.Target;
            }
            set
            {
                if (value == null)
                    _myCollection = null;
                else
                {
                    ObservableCollection<T> old = MyCollection;
                    //unsubsribe from old collections changed event!
                    if (old != null)
                        old.CollectionChanged -= MyListChanged;
                    _myCollection = new WeakReference<ObservableCollection<T>>(value);
                    //subscribe to new collections changed event
                    value.CollectionChanged += MyListChanged;
                }
            }
        }


        #region ListNotifications

        /// <summary>
        /// We respond to changes in the collection by raising property changed since the textual
        /// representation depends on the position in the collection itself
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged("Text");
        }

        #endregion


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected void RaisePropertyChanged(string name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}
