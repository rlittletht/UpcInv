using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.SpeechRecognition;
using Windows.Perception.Spatial;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using TCore.StatusBox;
using ZXing;
using ZXing.Common;
using ZXing.Mobile;
using TCore.Logging;
using TCore.Pipeline;


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
        private bool m_fErrorSoundsOnly;

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

            m_board = new WorkBoard(UpdateWorkBoard);

            m_pipeline = new ProducerConsumer<Transaction>(null, TransactionDispatcher);
            m_pipeline.Start();
        }

        #region Processing Reentrancy Protection

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

        /// <summary>
        /// generic remove the given code from the reentrancy protection. This code MUST match the code that
        /// that was added to the queue
        /// </summary>
        /// <param name="crid"></param>
        /// <param name="sTitle"></param>
        /// <param name="fResult"></param>
        void ReportAndRemoveReentrancyEntry(int workId, string scanCode, CorrelationID crid, string sTitle, bool fResult)
        {
            string sDescription = sTitle;

            if (sTitle == null)
            {
                //                SetTextBoxText(ebScanCode, "");
                SetTextBoxText(ebTitle, "!!TITLE NOT FOUND");
                sDescription = "";
                // SetFocus(ebTitle, true);
            }
            else
            {
                SetTextBoxText(ebTitle, sTitle);
                //SetFocus(ebScanCode, false);
            }

            m_lp.LogEvent(
                crid,
                fResult ? EventType.Information : EventType.Error,
                "FinalScanCodeCleanup: {0}: {1}",
                fResult,
                sTitle);

            m_board.UpdateWork(workId, fResult, sDescription);
            FinishCode(scanCode, crid);
        }
        #endregion

        #region UI Update
        private void DisplayResult(Result result)
        {
            if (result != null)
            {
                txtStatus.Text = "result != null, format = " + result.BarcodeFormat + ", text = " + result.Text;
                if (!m_fErrorSoundsOnly)
                    m_upca.Play(UpcAlert.AlertType.GoodInfo);
            }
            else
            {
                txtStatus.Text = "result = null";
            }
        }

        void SetFocus(TextBox eb, bool fWantKeyboard)
        {
            eb.Focus(fWantKeyboard ? FocusState.Pointer : FocusState.Programmatic);
            eb.Select(0, eb.Text.Length);
        }

        async void SetTextBoxText(TextBox eb, string text)
        {
            if (eb.Dispatcher.HasThreadAccess)
                eb.Text = text;
            else
                await eb.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { eb.Text = text; });
        }

        #endregion

        #region UI Dispatch

        /*----------------------------------------------------------------------------
        	%%Function: ScannerControlDispatchScanCode
        	%%Qualified: UniversalUpc.MainPage.ScannerControlDispatchScanCode
        	%%Contact: rlittle
        	
            Handle the scanner dispatching a scan code to us.

            For now, this is just a DVD scan code, but later will handle others...
        ----------------------------------------------------------------------------*/
        private void ScannerControlDispatchScanCode(Result result)
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

            string sResultText = result.Text;

            DispatchScanCode(sResultText, m_fCheckOnly, crid);
        }

        /*----------------------------------------------------------------------------
        	%%Function: DispatchScanCode
        	%%Qualified: UniversalUpc.MainPage.DispatchScanCode
        	%%Contact: rlittle
        	
            dispatch the given scan code, regardless of whether or not it was 
            automatically scanned (from the camera) or it was typed in (possibly
            from an attached wand scanner)
        ----------------------------------------------------------------------------*/
        private void DispatchScanCode(string sResultText, bool fCheckOnly, CorrelationID crid)
        {
            if (!m_fErrorSoundsOnly)
                m_upca.DoAlert(UpcAlert.AlertType.UPCScanBeep);

            if (m_adasCurrent == UpcInvCore.ADAS.DVD)
                DispatchDvdScanCode(sResultText, fCheckOnly, m_fErrorSoundsOnly, crid);
            else if (m_adasCurrent == UpcInvCore.ADAS.Book)
                DispatchBookScanCode(sResultText, fCheckOnly, m_fErrorSoundsOnly, crid);
            else if (m_adasCurrent == UpcInvCore.ADAS.Wine)
                DispatchWineScanCode(sResultText, fCheckOnly, m_fErrorSoundsOnly, crid);
        }

        void DispatchWineScanCode(string sResultText, bool fCheckOnly, bool fErrorSoundsOnly, CorrelationID crid)
        {
            DispatchScanCodeCore(
                (scanToAdjust) => sResultText,
                m_upccCore.DoHandleWineScanCode,
                sResultText,
                ebNotes.Text,
                fCheckOnly,
                fErrorSoundsOnly,
                crid
            );
        }

        void DispatchBookScanCode(string sResultText, bool fCheckOnly, bool fErrorSoundsOnly, CorrelationID crid)
        {
            DispatchScanCodeCore(
                (scanToAdjust) => m_upccCore.SEnsureIsbn13(sResultText),
                m_upccCore.DoHandleBookScanCode,
                sResultText,
                ebLocation.Text,
                fCheckOnly,
                fErrorSoundsOnly,
                crid
            );
        }

        private void DispatchDvdScanCode(string sResultText, bool fCheckOnly, bool fErrorSoundsOnly, CorrelationID crid)
        {
            DispatchScanCodeCore(
                (scanToAdjust) => m_upccCore.SEnsureEan13(scanToAdjust),
                m_upccCore.DoHandleDvdScanCode,
                sResultText,
                null, // sExtra
                fCheckOnly,
                fErrorSoundsOnly,
                crid
            );
        }

        delegate string AdjustScanCode(string scanCode);

        delegate Task DoHandleDispatchScanCodeDelegate(
            int workId,
            string sCode,
            string sExtra, // location for book, notes for wine, null for dvd
            bool fCheckOnly,
            bool fErrorSoundsOnly,
            CorrelationID crid,
            UpcInvCore.FinalScanCodeReportAndCleanupDelegate delReportAndCleanup);


        /// <summary>
        /// Dispatch the scancode with the given workitem delegate.
        /// The scanCode must already be normalized. The delegate should take care of dispatching
        /// to the correct upcc handler AND must include the delegate to remove the reentrancy item
        /// </summary>
        /// <param name="delDispatch"></param>
        /// <param name="delAdjust"></param>
        /// <param name="scanCode"></param>
        /// <param name="fErrorSoundsOnly"></param>
        /// <param name="crid"></param>
        void DispatchScanCodeCore(
            AdjustScanCode delAdjust,
            DoHandleDispatchScanCodeDelegate delDispatch,
            string scanCode,
            string sExtra,
            bool fCheckOnly,
            bool fErrorSoundsOnly,
            CorrelationID crid)
        {
            string scanCodeAdjusted = delAdjust(scanCode);

            if (scanCodeAdjusted.StartsWith("!!"))
            {
                m_lp.LogEvent(crid, EventType.Error, scanCodeAdjusted);
                SetFocus(ebScanCode, false);
                m_sb.AddMessage(m_fErrorSoundsOnly ? UpcAlert.AlertType.None : UpcAlert.AlertType.BadInfo, scanCodeAdjusted);
                return;
            }

            // guard against reentrancy on the same scan code.
            m_lp.LogEvent(crid, EventType.Verbose, "About to check for already processing: {0}", scanCodeAdjusted);
            if (!FAddProcessingCode(scanCodeAdjusted, crid))
            {
                // even if we bail out...set the focus
                SetFocus(ebScanCode, false);
                return;
            }

            // The removal of the reentrancy guard will happen asynchronously

            int workId = m_board.CreateWork(scanCodeAdjusted, null);

            WorkBoard.WorkItemDispatch del = new WorkBoard.WorkItemDispatch(
                async () =>
                {
                    await delDispatch(
                        workId,
                        scanCodeAdjusted,
                        sExtra,
                        fCheckOnly,
                        fErrorSoundsOnly,
                        crid,
                        ReportAndRemoveReentrancyEntry);
                });

            m_board.SetWorkDelegate(workId, del);

            WorkItemView view = m_board.GetWorkItemView(workId);

            lstWorkBoard.Items.Insert(0, view);
            m_pipeline.Producer.QueueRecord(new Transaction(workId));
            SetFocus(ebScanCode, false);
        }

        #endregion

        #region Work Board
        private WorkBoard m_board;

        void AddToWorkBoard(WorkItemView view)
        {
            lstWorkBoard.Items.Insert(0, view);
        }

        void UpdateWorkBoardCore(WorkItemView view)
        {
            // find the item in the view and update it
            for (int i = 0; i < lstWorkBoard.Items.Count; i++)
            {
                if (((WorkItemView) lstWorkBoard.Items[i]).WorkId == view.WorkId)
                {
                    lstWorkBoard.Items[i] = view;
                    return;
                }
            }

            throw new Exception("Can't find work item to update");
        }

        async void UpdateWorkBoard(WorkItemView view)
        {
            if (lstWorkBoard.Dispatcher.HasThreadAccess)
                UpdateWorkBoardCore(view);
            else
                await lstWorkBoard.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { UpdateWorkBoardCore(view); });
        }
        #endregion

        #region Pipeline
        public class Transaction : IPipelineBase<Transaction>
        {
            public int WorkId { get; set; }

            public Transaction()
            {
            }

            public Transaction(int workId)
            {
                WorkId = workId;
            }

            public void InitFrom(Transaction from)
            {
                WorkId = from.WorkId;
            }
        }

        private ProducerConsumer<Transaction> m_pipeline;

        public void TransactionDispatcher(IEnumerable<Transaction> items)
        {
            foreach (Transaction item in items)
            {
                m_board.DoWorkItem(item.WorkId).Wait();
            }
        }
        #endregion

        #region UI Controls / Manual Scanning
        
        async Task AdjustUIForAvailableHardware()
        {
            DeviceInformationCollection devices =
                await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            if (devices.Count > 0)
                return;

            scannerControl.Visibility = Visibility.Collapsed;
            pbScan.Visibility = Visibility.Collapsed;
        }

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

        private void ToggleScan(object sender, RoutedEventArgs e)
        {
            if (m_fScannerOn == false)
            {
                if (!m_fScannerSetup)
                {
                    m_fScannerSetup = true;
                    m_ups.SetupScanner(null, true);
                }

                m_ups.StartScanner(ScannerControlDispatchScanCode);
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
                if (m_adasCurrent == UpcInvCore.ADAS.DVD)
                    await m_upccCore.DoCheckDvdTitleInventory(sTitle, new CorrelationID());
                else if (m_adasCurrent == UpcInvCore.ADAS.Book)
                    await m_upccCore.DoCheckBookTitleInventory(sTitle, new CorrelationID());
                else if (m_adasCurrent == UpcInvCore.ADAS.Wine)
                    m_sb.AddMessage(UpcAlert.AlertType.BadInfo, "No manual operation available for Wine");

                return;
            }

            if (sTitle.StartsWith("!!"))
            {
                m_sb.AddMessage(UpcAlert.AlertType.BadInfo, "Can't add title with leading error text '!!'");
                return;
            }

            CorrelationID crid = new CorrelationID();

            bool fResult = false;

            if (m_adasCurrent == UpcInvCore.ADAS.DVD)
                fResult = await m_upccCore.DoCreateDvdTitle(ebScanCode.Text, sTitle, m_fCheckOnly, m_fErrorSoundsOnly, crid);
            else if (m_adasCurrent == UpcInvCore.ADAS.Book)
                fResult = await m_upccCore.DoCreateBookTitle(ebScanCode.Text, sTitle, ebLocation.Text, m_fCheckOnly, m_fErrorSoundsOnly, crid);
            else if (m_adasCurrent == UpcInvCore.ADAS.Wine)
                m_sb.AddMessage(UpcAlert.AlertType.BadInfo, "No manual operation available for Wine");

            if (fResult)
            {
                m_sb.AddMessage(
                    m_fErrorSoundsOnly ? UpcAlert.AlertType.None : UpcAlert.AlertType.GoodInfo,
                    "Added {0} as {1}",
                    ebScanCode.Text,
                    sTitle);
            }
            else
            {
                m_sb.AddMessage(UpcAlert.AlertType.Halt, "FAILED  to Added {0} as {1}", ebScanCode.Text, sTitle);
            }
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

        private void DoErrorSoundsChange(object sender, RoutedEventArgs e)
        {
            m_fErrorSoundsOnly = cbErrorSoundsOnly.IsChecked ?? false;
        }

        private void OnEnterSelectAll(object sender, RoutedEventArgs e)
        {
            TextBox eb = (TextBox) sender;
            eb.Select(0, eb.Text.Length);
        }

        private void OnCodeKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                CorrelationID crid = new CorrelationID();
                string sResultText = ebScanCode.Text;

                m_lp.LogEvent(crid, EventType.Information, "Dispatching ScanCode: {0}", sResultText);

                DispatchScanCode(sResultText, m_fCheckOnly, crid);
                e.Handled = true;
                return;
            }

            e.Handled = false;
        }
        #endregion

    }
}