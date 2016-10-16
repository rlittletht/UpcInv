using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using ZXing;
using ZXing.Common;
using ZXing.Mobile;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UniversalUpc
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Scanner m_ups;
        private UpcAlert m_upca;

        public MainPage()
        {
            this.InitializeComponent();

            m_ups = new Scanner(this.Dispatcher, scannerControl);
            m_upca = new UpcAlert();
        }

        private void DisplayResult(Result result)
        {
            if (result != null)
                {
                txtStatus.Text = "result != null, format = " + result.BarcodeFormat + ", text = " + result.Text;
                m_upca.Play(UpcAlert.AlertType.GoodInfo);
                }
            else
                {
                txtStatus.Text = "result = null";
                }
        }

        private async void ToggleScan(object sender, RoutedEventArgs e)
        {
            m_ups.SetupScanner(null, true);
            m_ups.StartScanner(DisplayResult);
        }

        private void txtStatus_ContextCanceled(UIElement sender, RoutedEventArgs args)
        {

        }
    }
}
