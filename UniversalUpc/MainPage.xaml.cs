using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Perception.Spatial;
using Windows.System;
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
using TCore.Logging;


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
        private bool m_fScannerOn;
        private bool m_fScannerSetup;
        private bool m_fCheckOnly;

        private UpcInvCore.ADAS m_adasCurrent;

        private LogProvider m_lp;
        public MainPage()
        {
            m_lp = ((App) Application.Current).LpCurrent();

            this.InitializeComponent();
            
            m_ups = new Scanner(this.Dispatcher, scannerControl);
            m_upca = new UpcAlert();
            m_sb = new StatusBox();
            m_upccCore = new UpcInvCore(m_upca, m_sb, m_lp);
            m_plsProcessing = new List<string>();

            m_sb.Initialize(recStatus, m_upca);
            AdjustUIForMediaType(m_adasCurrent);
            AdjustUIForAvailableHardware();
        }

        async Task AdjustUIForAvailableHardware()
        {
            DeviceInformationCollection devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            if (devices.Count > 0)
                return;

            scannerControl.Visibility = Visibility.Collapsed;
            pbScan.Visibility = Visibility.Collapsed;
        }

        private List<string> m_plsProcessing;

        UpcInvCore.ADAS AdasFromDropdownItem(ComboBoxItem cbi)
        {
            if (String.Compare((string)cbi.Tag, "dvd") == 0)
                return UpcInvCore.ADAS.DVD;
            else if (String.Compare((string)cbi.Tag, "book") == 0)
                return UpcInvCore.ADAS.Book;
            else if (String.Compare((string)cbi.Tag, "wine") == 0)
                return UpcInvCore.ADAS.Wine;
            else if (String.Compare((string)cbi.Tag, "upc") == 0)
                return UpcInvCore.ADAS.Generic;

            throw new Exception("illegal ADAS combobox item");
        }

        void FinishCode(string sCode, CorrelationID crid)
        {
            lock (m_plsProcessing)
                {
                int i;

                for (i = m_plsProcessing.Count - 1; i >= 0; i--)
                    {
                    if (m_plsProcessing[i] == sCode)
                        {
                        m_lp.LogEvent(crid, EventType.Verbose, "Removing code {0} from processing list", sCode);
                        m_plsProcessing.RemoveAt(i);
                        return;
                        }
                    }
                }
            m_lp.LogEvent(crid, EventType.Error, "FAILED TO FIND {0} in processing list during remove", sCode);
        }

        bool FAddProcessingCode(string sCode, CorrelationID crid)
        {
            lock (m_plsProcessing)
                {
                m_lp.LogEvent(crid, EventType.Verbose, "Checking processing list, there are {0} items in the list", m_plsProcessing.Count);
                for (int i = m_plsProcessing.Count - 1; i >= 0; i--)
                    if (m_plsProcessing[i] == sCode)
                        {
                        m_lp.LogEvent(crid, EventType.Verbose, "!!Code already in processing list {0}", sCode);
                        return false;
                        }

                m_plsProcessing.Add(sCode);
                m_lp.LogEvent(crid, EventType.Verbose, "!!Code not present in processing list {0}", sCode);
                return true;
                }
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
        
        /*----------------------------------------------------------------------------
        	%%Function: DispatchScanCode
        	%%Qualified: UniversalUpc.MainPage.DispatchScanCode
        	%%Contact: rlittle
        	
            Handle the scanner dispatching a scan code to us.

            For now, this is just a DVD scan code, but later will handle others...
        ----------------------------------------------------------------------------*/
        private void DispatchScanCode(Result result)
        {
            CorrelationID crid = new CorrelationID();

            m_lp.LogEvent(crid, EventType.Information, "Dispatching ScanCode: {0}", result?.Text);

            if (result == null)
                {
                m_upca.Play(UpcAlert.AlertType.BadInfo);
                m_lp.LogEvent(crid, EventType.Error, "result == null");
                ebScanCode.Text = "";
                return;
                }

            m_upca.DoAlert(UpcAlert.AlertType.UPCScanBeep);

            if (m_adasCurrent == UpcInvCore.ADAS.DVD)
                DispatchDvdScanCode(result, crid);
            else if (m_adasCurrent == UpcInvCore.ADAS.Book)
                DispatchBookScanCode(result, crid);
            else if (m_adasCurrent == UpcInvCore.ADAS.Wine)
                DispatchWineScanCode(result, crid);
        }

        void DispatchWineScanCode(Result result, CorrelationID crid)
        {
            string sScanCode = result.Text;

            // guard against reentrancy on the same scan code.
            m_lp.LogEvent(crid, EventType.Verbose, "About to check for already processing: {0}", sScanCode);
            if (!FAddProcessingCode(sScanCode, crid))
                return;

            // now handle this scan code

            ebScanCode.Text = sScanCode;

            // The removal of the reentrancy guard will happen asynchronously
            m_upccCore.DoHandleWineScanCode(ebScanCode.Text, ebNotes.Text, crid, (cridDel, sTitle, fResult) =>
                {
                ebTitle.Text = sTitle ?? "!!TITLE NOT FOUND";
                m_lp.LogEvent(cridDel, fResult ? EventType.Information : EventType.Error, "FinalScanCodeCleanup: {0}: {1}", fResult, sTitle);
                FinishCode(sScanCode, cridDel);
                });
        }
        void DispatchBookScanCode(Result result, CorrelationID crid)
        {
            string sIsbn13 = m_upccCore.SEnsureIsbn13(result.Text);

            if (sIsbn13.StartsWith("!!"))
                {
                m_lp.LogEvent(crid, EventType.Error, sIsbn13);
                m_sb.AddMessage(UpcAlert.AlertType.BadInfo, sIsbn13);
                return;
                }

            // guard against reentrancy on the same scan code.
            m_lp.LogEvent(crid, EventType.Verbose, "About to check for already processing: {0}", sIsbn13);
            if (!FAddProcessingCode(sIsbn13, crid))
                return;

            // now handle this scan code

            ebScanCode.Text = sIsbn13;

            // The removal of the reentrancy guard will happen asynchronously
            m_upccCore.DoHandleBookScanCode(ebScanCode.Text, ebLocation.Text, crid, (cridDel, sTitle, fResult) =>
                {
                ebTitle.Text = sTitle ?? "!!TITLE NOT FOUND";
                m_lp.LogEvent(cridDel, fResult ? EventType.Information : EventType.Error, "FinalScanCodeCleanup: {0}: {1}", fResult, sTitle);
                FinishCode(sIsbn13, cridDel);
                });
        }

        private void DispatchDvdScanCode(Result result, CorrelationID crid)
        {
            string sCode = m_upccCore.SEnsureEan13(result.Text);

            // guard against reentrancy on the same scan code.
            m_lp.LogEvent(crid, EventType.Verbose, "About to check for already processing: {0}", sCode);
            if (!FAddProcessingCode(sCode, crid))
                return;

            // now handle this scan code

            ebScanCode.Text = sCode;

            // The removal of the reentrancy guard will happen asynchronously
            m_upccCore.DoHandleDvdScanCode(sCode, crid, (cridDel, sTitle, fResult) =>
                {
                ebTitle.Text = sTitle ?? "!!TITLE NOT FOUND";
                m_lp.LogEvent(cridDel, fResult ? EventType.Information : EventType.Error, "FinalScanCodeCleanup: {0}: {1}", fResult, sTitle);
                FinishCode(sCode, cridDel);
                });
        }

        private async void ToggleScan(object sender, RoutedEventArgs e)
        {
            if (m_fScannerOn == false)
                {
                if (!m_fScannerSetup)
                    {
                    m_fScannerSetup = true;
                    m_ups.SetupScanner(null, true);
                    }
                m_ups.StartScanner(DispatchScanCode);
                m_fScannerOn = true;
                }
            else
                {
                m_ups.StopScanner();
                m_fScannerOn = false;
                }
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoManual
        	%%Qualified: UniversalUpc.MainPage.DoManual
        	%%Contact: rlittle
        	
            take the current title and scan code and create an entry for it.
        ----------------------------------------------------------------------------*/
        private async void DoManual(object sender, RoutedEventArgs e)
        {
            string sTitle = ebTitle.Text;

            if (m_fCheckOnly)
                {
                m_upccCore.DoCheckDvdTitleInventory(sTitle, new CorrelationID());
                return;
                }
            if (sTitle.StartsWith("!!"))
                {
                m_sb.AddMessage(UpcAlert.AlertType.BadInfo, "Can't add title with leading error text '!!'");
                return;
                }

            CorrelationID crid = new CorrelationID();

            bool fResult = await m_upccCore.DoCreateDvdTitle(ebScanCode.Text, sTitle, crid);

            if (fResult)
                m_sb.AddMessage(UpcAlert.AlertType.GoodInfo, "Added {0} as {1}", ebScanCode.Text, sTitle);
            else
                m_sb.AddMessage(UpcAlert.AlertType.Halt, "FAILED  to Added {0} as {1}", ebScanCode.Text, sTitle);
        }

        private void SetNewMediaType(object sender, RoutedEventArgs e)
        {
            m_adasCurrent = AdasFromDropdownItem(cbMediaType.SelectedItem as ComboBoxItem);
            AdjustUIForMediaType(m_adasCurrent);
        }

        void AdjustUIForMediaType(UpcInvCore.ADAS adas)
        {
            if (txtLocation == null || ebLocation == null || ebNotes == null)
                return;

            switch (adas)
                {
                case UpcInvCore.ADAS.Book:
                    txtLocation.Visibility = Visibility.Visible;
                    ebLocation.Visibility = Visibility.Visible;
                    ebNotes.Visibility = Visibility.Collapsed;
                    break;
                case UpcInvCore.ADAS.Wine:
                    txtLocation.Visibility = Visibility.Collapsed;
                    ebLocation.Visibility = Visibility.Collapsed;
                    ebNotes.Visibility = Visibility.Visible;
                    break;
                case UpcInvCore.ADAS.DVD:
                    txtLocation.Visibility = Visibility.Collapsed;
                    ebLocation.Visibility = Visibility.Collapsed;
                    ebNotes.Visibility = Visibility.Collapsed;
                    break;
                case UpcInvCore.ADAS.Generic:
                    txtLocation.Visibility = Visibility.Collapsed;
                    ebLocation.Visibility = Visibility.Collapsed;
                    ebNotes.Visibility = Visibility.Collapsed;
                    break;
                }
        }
        private void DoCheckChange(object sender, RoutedEventArgs e)
        {
            m_fCheckOnly = cbCheckOnly.IsChecked ?? false;
        }

        private void OnEnterSelectAll(object sender, RoutedEventArgs e)
        {
            TextBox eb = (TextBox) sender;
            eb.Select(0, eb.Text.Length);
        }

        private void OnCodeKeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = false;
        }
    }
}
