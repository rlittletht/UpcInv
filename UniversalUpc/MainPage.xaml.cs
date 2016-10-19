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
using TCore.StatusBox;
using UniversalUpc.UpcSvc;
using ZXing;
using ZXing.Common;
using ZXing.Mobile;
using UpcService = UniversalUpc.UpcSvc;


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
        private UpcInvCore m_upccCore;
        private TCore.StatusBox.StatusBox m_sb;

        public MainPage()
        {
            this.InitializeComponent();

            m_ups = new Scanner(this.Dispatcher, scannerControl);
            m_upca = new UpcAlert();
            m_sb = new StatusBox();
            m_upccCore = new UpcInvCore(m_upca, m_sb);
            m_sb.Initialize(recStatus, m_upca);
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

        private void DispatchScanCode(Result result)
        {
            if (result == null)
                {
                m_upca.Play(UpcAlert.AlertType.BadInfo);
                ebScanCode.Text = "";
                }
            else
                {
                string sCode = m_upccCore.SEnsureEan13(result.Text);
                ebScanCode.Text = sCode;
                m_upccCore.DvdInfoRetrieve(sCode, (sScanCode, dvdi) =>
                    {
                    if (dvdi != null)
                        {
                        ebTitle.Text = dvdi.Title;
                        // check for a dupe/too soon last scan (within 1 hour)
                        if (dvdi.LastScan > DateTime.Now.AddHours(-1))
                            {
                            m_sb.AddMessage(String.Format("{0}: Duplicate?! LastScan was {1}", dvdi.Title, dvdi.LastScan.ToString()), UpcAlert.AlertType.Duplicate);
                            return;
                            }

                        // now update the last scan date
                        m_upccCore.UpdateScanDate(sScanCode, (_sScanCode, fSucceeded) =>
                            {
                            if (fSucceeded)
                                m_sb.AddMessage(String.Format("{0}: Updated LastScan (was {1})", dvdi.Title, dvdi.LastScan.ToString()), UpcAlert.AlertType.GoodInfo);
                            else
                                m_sb.AddMessage(String.Format("{0}: Failed to update last scan!", dvdi.Title), UpcAlert.AlertType.BadInfo);
                            });
                        }
                    else
                        {
                        ebTitle.Text = "";
                        m_sb.AddMessage("code not found", UpcAlert.AlertType.BadInfo);
                        }
                    });
                }
        }

        private async void ToggleScan(object sender, RoutedEventArgs e)
        {
            m_ups.SetupScanner(null, true);
            m_ups.StartScanner(DispatchScanCode);
        }

        private async void CmdTest1(object sender, RoutedEventArgs e)
        {
            m_sb.AddMessage("testing", UpcAlert.AlertType.None);
        }


        private void txtStatus_ContextCanceled(UIElement sender, RoutedEventArgs args)
        {

        }
    }
}
