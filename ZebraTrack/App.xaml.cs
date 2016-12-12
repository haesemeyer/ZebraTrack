using System.Windows;

using MHApi.Utilities;
using ipp;

namespace ZebraTrack
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            DispatcherHelper.Initialize();
            core.ippInit();
        }
    }
}
