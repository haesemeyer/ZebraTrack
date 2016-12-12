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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using MHApi.GUI;
using ZebraTrack.ViewModels;

namespace ZebraTrack.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : WindowAwareView
    {
        MainViewModel _viewModel;

        public MainView()
        {
            InitializeComponent();
            _viewModel = ViewModel.Source as MainViewModel;
        }

        #region Cleanup

        protected override void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel != null)
                _viewModel.Dispose();
            base.WindowClosing(sender, e);
        }

        #endregion
    }
}
