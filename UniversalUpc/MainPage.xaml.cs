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

        private LogProvider m_lp;
        public MainPage()
        {
            m_lp = ((App) Application.Current).LpCurrent();

            this.InitializeComponent();
            
            m_ups = new Scanner(this.Dispatcher, scannerControl);
            m_upca = new UpcAlert();
            m_sb = new StatusBox();
            m_upccCore = new UpcInvCore(m_upca, m_sb);
            m_plsProcessing = new List<string>();

            m_sb.Initialize(recStatus, m_upca);
        }

        private List<string> m_plsProcessing;

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

        private void DispatchScanCode(Result result)
        {
            CorrelationID crid = new CorrelationID();

            m_lp.LogEvent(crid, EventType.Information, "Dispatching ScanCode: {0}", result?.Text);
            if (result == null)
                {
                m_upca.Play(UpcAlert.AlertType.BadInfo);
                ebScanCode.Text = "";
                }
            else
                {
                string sCode = m_upccCore.SEnsureEan13(result.Text);

                m_lp.LogEvent(crid, EventType.Verbose, "About to check for already processing: {0}", result.Text);
                if (!FAddProcessingCode(sCode, crid))
                    return; // already processing this code...

                m_lp.LogEvent(crid, EventType.Verbose, "Continuing with processing for {0}...Checking for DvdInfo from service", result.Text);
                ebScanCode.Text = sCode;
                m_upccCore.DvdInfoRetrieve(sCode, (sScanCode, dvdi) =>
                    {
                    if (dvdi != null)
                        {
                        m_lp.LogEvent(crid, EventType.Verbose, "Service returned info for {0}", sScanCode);
                        ebTitle.Text = dvdi.Title;
                        // check for a dupe/too soon last scan (within 1 hour)
                        if (dvdi.LastScan > DateTime.Now.AddHours(-1))
                            {
                            m_lp.LogEvent(crid, EventType.Verbose, "Avoiding duplicate scan for {0}", sScanCode);
                            m_sb.AddMessage(String.Format("{0}: Duplicate?! LastScan was {1}", dvdi.Title, dvdi.LastScan.ToString()), UpcAlert.AlertType.Duplicate);
                            FinishCode(sScanCode, crid);
                            return;
                            }

                        m_lp.LogEvent(crid, EventType.Verbose, "Calling service to update scan date for {0}", sScanCode);

                        // now update the last scan date
                        m_upccCore.UpdateScanDate(sScanCode, (_sScanCode, fSucceeded) =>
                            {
                            if (fSucceeded)
                                {
                                m_lp.LogEvent(crid, EventType.Verbose, "Successfully updated last scan for {0}", _sScanCode);
                                m_sb.AddMessage(String.Format("{0}: Updated LastScan (was {1})", dvdi.Title, dvdi.LastScan.ToString()), UpcAlert.AlertType.GoodInfo);
                                }
                            else
                                {
                                m_lp.LogEvent(crid, EventType.Error, "Failed to update last scan for {0}", _sScanCode);
                                m_sb.AddMessage(String.Format("{0}: Failed to update last scan!", dvdi.Title), UpcAlert.AlertType.BadInfo);
                                }

                            FinishCode(sScanCode, crid);
                            });
                        }
                    else
                        {
                        // not found, so lookup
                        ebScanCode.Text = sCode;
                        m_lp.LogEvent(crid, EventType.Verbose, "No DVD info for scan code {0}, looking up...", sScanCode);

                        m_sb.AddMessage(String.Format("looking up code {0}", sCode), UpcAlert.AlertType.None);
                        m_upccCore.FetchTitleFromGenericUPC(sCode, (_sTitleScan, sTitle) =>
                            {
                            if (sTitle == "" || sTitle.Substring(0, 2) == "!!")
                                {
                                m_lp.LogEvent(crid, EventType.Verbose, "Service did not return a title for {0}", sScanCode);

                                m_sb.AddMessage(String.Format("Couldn't find title for {0}: {1}", _sTitleScan, sTitle), UpcAlert.AlertType.BadInfo);
                                FinishCode(sScanCode, crid);
                                return;
                                }

                            m_lp.LogEvent(crid, EventType.Verbose, "Service returned title {0} for code {1}. Adding title.", sTitle, sScanCode);

                            m_upccCore.CreateDvd(_sTitleScan, sTitle, (_sCreateScan, _sCreateTitle, fResult) =>
                                {
                                if (fResult)
                                    {
                                    m_lp.LogEvent(crid, EventType.Verbose, "Successfully added title for {0}", sScanCode);

                                    m_sb.AddMessage(String.Format("Added title for {0}: {1}", _sTitleScan, sTitle), UpcAlert.AlertType.GoodInfo);
                                    }
                                else
                                    {
                                    m_lp.LogEvent(crid, EventType.Error, "Failed to add title for {0}", sScanCode);

                                    m_sb.AddMessage(String.Format("Couldn't create DVD title for {0}: {1}", _sTitleScan, sTitle), UpcAlert.AlertType.BadInfo);
                                    }
                                FinishCode(sScanCode, crid);
                                });

                            });
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
