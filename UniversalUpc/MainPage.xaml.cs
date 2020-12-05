using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using TCore.Logging;
using TCore.Pipeline;
using TCore.StatusBox;
using UpcInv;
using UpcShared;
using ZXing;
using EventType = UpcShared.EventType;


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
        private StatusBox m_sb;
        private bool m_fScannerOn;
        private bool m_fScannerSetup;
        private bool m_fCheckOnly;
        private bool m_fErrorSoundsOnly;

        private UpcInvCore.ADAS m_adasCurrent;
        private UpcInvCore.WDI m_wdiCurrent;

        private LogProvider m_lpActual;
        private UpcLogProvider m_lp;

        public MainPage()
        {
            m_lpActual = ((App) Application.Current).LpCurrent();
            m_lp = new UpcLogProvider(m_lpActual);

            InitializeComponent();

            m_ups = new Scanner(Dispatcher, scannerControl);
            m_upca = new UpcAlert();
            m_sb = new StatusBox();
            m_upccCore = new UpcInvCore(m_upca, m_sb, m_lp);
            m_plsProcessing = new List<string>();

            m_sb.Initialize(recStatus, m_upca);
            AdjustUIForMediaType(m_adasCurrent, m_wdiCurrent);
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
        /// <param name="crids"></param>
        /// <param name="sTitle"></param>
        /// <param name="fResult"></param>
        void ReportAndRemoveReentrancyEntry(int workId, string scanCode, Guid crids, string sTitle, bool fResult)
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
                crids,
                fResult ? EventType.Information : EventType.Error,
                "FinalScanCodeCleanup: {0}: {1}",
                fResult,
                sTitle);

            m_board.UpdateWork(workId, fResult, sDescription);
            FinishCode(scanCode, CorrelationID.FromCrids(crids));
        }
        #endregion

        #region UI Update
        private void DisplayResult(Result result)
        {
            if (result != null)
            {
                txtStatus.Text = "result != null, format = " + result.BarcodeFormat + ", text = " + result.Text;
                if (!m_fErrorSoundsOnly)
                    m_upca.Play(AlertType.GoodInfo);
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
        ----------------------------------------------------------------------------*/
        private void ScannerControlDispatchScanCode(Result result)
        {
            CorrelationID crid = new CorrelationID();

            m_lp.LogEvent(crid, EventType.Information, "Dispatching ScanCode: {0}", result?.Text);

            if (result == null)
            {
                m_upca.Play(AlertType.BadInfo);
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
                m_upca.DoAlert(AlertType.UPCScanBeep);

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
                scanToAdjust => sResultText,
                m_upccCore.DoHandleWineScanCode,
                sResultText,
                ebNotes.Text,
                m_wdiCurrent == UpcInvCore.WDI.Drink ? null : ebBinCode.Text,
                fCheckOnly,
                fErrorSoundsOnly,
                crid
            );
        }

        void DispatchBookScanCode(string sResultText, bool fCheckOnly, bool fErrorSoundsOnly, CorrelationID crid)
        {
            DispatchScanCodeCore(
                scanToAdjust => m_upccCore.SEnsureIsbn13(sResultText),
                m_upccCore.DoHandleBookScanCode,
                sResultText,
                ebLocation.Text,
                null,
                fCheckOnly,
                fErrorSoundsOnly,
                crid
            );
        }

        private void DispatchDvdScanCode(string sResultText, bool fCheckOnly, bool fErrorSoundsOnly, CorrelationID crid)
        {
            DispatchScanCodeCore(
                scanToAdjust => m_upccCore.SEnsureEan13(scanToAdjust),
                m_upccCore.DoHandleDvdScanCode,
                sResultText,
                null, // sExtra
                null, // sExtra2
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
            string sExtra2, // binCode for win, null for book or dvd
            bool fCheckOnly,
            bool fErrorSoundsOnly,
            Guid crids,
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
        /// <param name="crids"></param>
        void DispatchScanCodeCore(
            AdjustScanCode delAdjust,
            DoHandleDispatchScanCodeDelegate delDispatch,
            string scanCode,
            string sExtra,
            string sExtra2,
            bool fCheckOnly,
            bool fErrorSoundsOnly,
            Guid crids)
        {
            string scanCodeAdjusted = delAdjust(scanCode);

            if (scanCodeAdjusted.StartsWith("!!"))
            {
                m_lp.LogEvent(crids, EventType.Error, scanCodeAdjusted);
                SetFocus(ebScanCode, false);
                m_sb.AddMessage(m_fErrorSoundsOnly ? AlertType.None : AlertType.BadInfo, scanCodeAdjusted);
                return;
            }

            // guard against reentrancy on the same scan code.
            m_lp.LogEvent(crids, EventType.Verbose, "About to check for already processing: {0}", scanCodeAdjusted);
            if (!FAddProcessingCode(scanCodeAdjusted, CorrelationID.FromCrids(crids)))
            {
                // even if we bail out...set the focus
                SetFocus(ebScanCode, false);
                return;
            }

            // The removal of the reentrancy guard will happen asynchronously

            int workId = m_board.CreateWork(scanCodeAdjusted, null);

            WorkBoard.WorkItemDispatch del = async () =>
            {
                await delDispatch(
                    workId,
                    scanCodeAdjusted,
                    sExtra,
                    sExtra2,
                    fCheckOnly,
                    fErrorSoundsOnly,
                    crids,
                    ReportAndRemoveReentrancyEntry);
            };

            m_board.SetWorkDelegate(workId, del);

            WorkItemView view = m_board.GetWorkItemView(workId);

            lstWorkBoard.Items.Insert(0, view);
            m_pipeline.Producer.QueueRecord(new Transaction(workId));
            SetFocus(ebScanCode, false);
            ResetWineInventoryControls();
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
                await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            scannerControl.Visibility = Visibility.Collapsed; // always collapsed until they press the scan button

            if (devices.Count > 0)
                return;

            pbScan.Visibility = Visibility.Collapsed;
        }

        UpcInvCore.WDI WdiFromDropdownItem(ComboBoxItem cbi)
        {
            if (String.Compare((string)cbi.Content, "drink", true) == 0)
                return UpcInvCore.WDI.Drink;
            if (String.Compare((string)cbi.Content, "inventory", true) == 0)
                return UpcInvCore.WDI.Inventory;

            throw new Exception("Illegal WDI combobox item");
        }

        UpcInvCore.ADAS AdasFromDropdownItem(ComboBoxItem cbi)
        {
            if (String.Compare((string)cbi.Tag, "dvd") == 0)
                return UpcInvCore.ADAS.DVD;
            if (String.Compare((string)cbi.Tag, "book") == 0)
                return UpcInvCore.ADAS.Book;
            if (String.Compare((string)cbi.Tag, "wine") == 0)
                return UpcInvCore.ADAS.Wine;
            if (String.Compare((string)cbi.Tag, "upc") == 0)
                return UpcInvCore.ADAS.Generic;

            throw new Exception("illegal ADAS combobox item");
        }

        private void ToggleScan(object sender, RoutedEventArgs e)
        {
            if (m_fScannerOn == false)
            {
                scannerControl.Visibility = Visibility.Visible;
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
                scannerControl.Visibility = Visibility.Collapsed;
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
                    m_sb.AddMessage(AlertType.BadInfo, "No manual operation available for Wine");

                return;
            }

            if (sTitle.StartsWith("!!"))
            {
                m_sb.AddMessage(AlertType.BadInfo, "Can't add title with leading error text '!!'");
                return;
            }

            CorrelationID crid = new CorrelationID();

            bool fResult = false;

            if (m_adasCurrent == UpcInvCore.ADAS.DVD)
                fResult = await m_upccCore.DoCreateDvdTitle(ebScanCode.Text, sTitle, m_fCheckOnly, m_fErrorSoundsOnly, crid);
            else if (m_adasCurrent == UpcInvCore.ADAS.Book)
                fResult = await m_upccCore.DoCreateBookTitle(ebScanCode.Text, sTitle, ebLocation.Text, m_fCheckOnly, m_fErrorSoundsOnly, crid);
            else if (m_adasCurrent == UpcInvCore.ADAS.Wine)
                m_sb.AddMessage(AlertType.BadInfo, "No manual operation available for Wine");

            if (fResult)
            {
                m_sb.AddMessage(
                    m_fErrorSoundsOnly ? AlertType.None : AlertType.GoodInfo,
                    "Added {0} as {1}",
                    ebScanCode.Text,
                    sTitle);
            }
            else
            {
                m_sb.AddMessage(AlertType.Halt, "FAILED  to Added {0} as {1}", ebScanCode.Text, sTitle);
            }
        }

        private void SetNewMediaType(object sender, RoutedEventArgs e)
        {
            m_adasCurrent = AdasFromDropdownItem(cbMediaType.SelectedItem as ComboBoxItem);
            AdjustUIForMediaType(m_adasCurrent, m_wdiCurrent);
            SetFocus(ebScanCode, false);
        }


        private void DoIsbnify(object sender, RoutedEventArgs e)
        {
            // take a 9 digit ISBN number and calculate the check digit and create
            // a 13 digit ISBN number
            if (ebScanCode.Text.Length != 9
                && ebScanCode.Text.Length != 10
                && ebScanCode.Text.Length != 12
                && ebScanCode.Text.Length != 13)
            {
                m_sb.AddMessage(AlertType.BadInfo, "Scancode is not 9 or 12 digits");
                return;
            }

            SetTextBoxText(ebScanCode, m_upccCore.SCreateIsbn13FromIsbn(ebScanCode.Text));
        }

        void AdjustUIForMediaType(UpcInvCore.ADAS adas, UpcInvCore.WDI wdi)
        {
            if (txtLocation == null || ebLocation == null || ebNotes == null)
                return;

            Visibility visWineInventory = Visibility.Collapsed;
            Visibility visLocation = Visibility.Collapsed;
            Visibility visNotes = Visibility.Collapsed;
            Visibility visIsbnify = Visibility.Collapsed;
            Visibility visWine = Visibility.Collapsed;

            switch (adas)
            {
                case UpcInvCore.ADAS.Book:
                    visLocation = Visibility.Visible;
                    visIsbnify = Visibility.Visible;
                    break;
                case UpcInvCore.ADAS.Wine:
                    if (wdi == UpcInvCore.WDI.Inventory)
                        visWineInventory = Visibility.Visible;
                    else
                        visNotes = Visibility.Visible;

                    visWine = Visibility.Visible;
                    break;
                case UpcInvCore.ADAS.DVD:
                    break;
                case UpcInvCore.ADAS.Generic:
                    break;
            }

            if (cbDrinkInventory != null)
                cbDrinkInventory.Visibility = visWine;

            txtBinCode.Visibility = visWineInventory;
            ebBinCode.Visibility = visWineInventory;
            txtBinRow.Visibility = visWineInventory;
            ebBinRow.Visibility = visWineInventory;
            ebWineCode.Visibility = visWineInventory;
            txtBinColumn.Visibility = visWineInventory;
            ebBinColumn.Visibility = visWineInventory;

            txtLocation.Visibility = visLocation;
            ebLocation.Visibility = visLocation;

            ebNotes.Visibility = visNotes;
            pbIsbnify.Visibility = visIsbnify;
        }

        private void DoCheckChange(object sender, RoutedEventArgs e)
        {
            m_fCheckOnly = cbCheckOnly.IsChecked ?? false;
        }

        private void DoDrinkInventoryChange(object sender, RoutedEventArgs e)
        {
            m_wdiCurrent = WdiFromDropdownItem(cbDrinkInventory.SelectedItem as ComboBoxItem);
            AdjustUIForMediaType(m_adasCurrent, m_wdiCurrent);
            SetFocus(ebScanCode, false);
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

        private void DispatchScanCodeFromEnter(string sScanCode)
        {
            CorrelationID crid = new CorrelationID();

            m_lp.LogEvent(crid, EventType.Information, "Dispatching ScanCode: {0}", sScanCode);

            DispatchScanCode(sScanCode, m_fCheckOnly, crid);
        }

        void UpdateBinCode()
        {
            string sBinCode = UpcInvCore.BinCodeFromRowColumn(ebBinRow.Text, ebBinColumn.Text);

            if (sBinCode != null)
                ebBinCode.Text = sBinCode;
        }

        // every wine inventory scan is a composite. reset the items we need to have 
        // scanned in every time (row and scan code)
        void ResetWineInventoryControls()
        {
            if (m_adasCurrent != UpcInvCore.ADAS.Wine || m_wdiCurrent != UpcInvCore.WDI.Inventory)
                return;

            ebBinRow.Text = "";
            ebBinCode.Text = "";
            ebWineCode.Text = "";
        }

        void RouteWineScanOnEnter(string sScanCode)
        {
            if (sScanCode.ToUpper().StartsWith("C_"))
            {
                // this is a column code
                ebBinColumn.Text = sScanCode.ToUpper();
            }
            if (sScanCode.ToUpper().StartsWith("R_"))
            {
                // this is a column code
                ebBinRow.Text = sScanCode.ToUpper();
            }

            // if the row and column codes are complete, calculate the bin code
            UpdateBinCode();

            // if its all numbers, then its a wine scan code
            if (Char.IsDigit(sScanCode[0]))
            {
                ebWineCode.Text = sScanCode;
            }

            // now, if all the bin code is done, and the scan code is done
            // we can dispatch. otherwise, set focus back to ourselves (and select
            // all waiting for the next input)
            if (!string.IsNullOrEmpty(ebWineCode.Text)
                && !string.IsNullOrEmpty(ebBinCode.Text))
            {
                DispatchScanCodeFromEnter(ebWineCode.Text);
                return;
            }

            SetFocus(ebScanCode, false);
        }

        private void OnCodeKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                if (m_adasCurrent == UpcInvCore.ADAS.Wine && m_wdiCurrent == UpcInvCore.WDI.Inventory)
                {
                    // in wine inventory mode, several scans must compose together to make a
                    // complete scan. they all come into the same control
                    RouteWineScanOnEnter(ebScanCode.Text);
                    return;
                }

                DispatchScanCodeFromEnter(ebScanCode.Text);
                return;
            }

            e.Handled = false;
        }
        #endregion

    }
}