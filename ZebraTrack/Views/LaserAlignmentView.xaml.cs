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
    /// Interaction logic for LaserAlignmentView.xaml
    /// </summary>
    public partial class LaserAlignmentView : WindowAwareView
    {
        LaserAlignmentViewModel _viewModel;

        public LaserAlignmentView()
        {
            InitializeComponent();
            _viewModel = ViewModel.Source as LaserAlignmentViewModel;
        }

        private void WindowAwareView_Loaded(object sender, RoutedEventArgs e)
        {
            //Make the MainViewModel aware of the fact that this experiment type has been selected
            MainViewModel.ExperimentViewModel = _viewModel;
        }
    }
}
