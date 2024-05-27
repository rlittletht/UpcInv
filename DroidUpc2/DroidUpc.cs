using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using DroidUpc;
using TCore.Logging;
using UpcShared;
using EventType = UpcShared.EventType;
using ILogProvider = UpcShared.ILogProvider;

namespace DroidUpc2;

public class DroidUpc(IAppContext m_droidContext, UpcAlert m_upca, IStatusReporting m_isr, ILogProvider m_lp)
{
    private List<string> m_plsProcessing;
    private UpcInvCore.ADAS m_adasCurrent;
    private UpcInvCore m_upccCore = new UpcInvCore(m_upca, m_isr, m_lp);

    #region Public Commands

    /*----------------------------------------------------------------------------
        %%Function: SetNewMediaType
        %%Qualified: DroidUpc2.DroidUpc.SetNewMediaType
    ----------------------------------------------------------------------------*/
    public void SetNewMediaType(object? sender, AdapterView.ItemSelectedEventArgs itemSelectedEventArgs)
    {
        m_adasCurrent = AdasCurrent(); // AdasFromDropdownItem(cbMediaType.SelectedItem as ComboBoxItem);
        m_droidContext.AdjustUIForMediaType(m_adasCurrent);
    }

    #endregion

    public async Task<ServiceStatus> GetServiceStatusHeartBeat()
    {
        return await m_upccCore.GetServiceStatusHeartBeat();
    }

    /*----------------------------------------------------------------------------
        %%Function: AdasCurrent
        %%Qualified: DroidUpc.MainActivity.AdasCurrent
    ----------------------------------------------------------------------------*/
    UpcInvCore.ADAS AdasCurrent()
    {
        return m_droidContext.AdasCurrent();
    }

    /*----------------------------------------------------------------------------
        %%Function: FinishCode
        %%Qualified: DroidUpc.MainActivity.FinishCode

        remove the given code from the processing list
    ----------------------------------------------------------------------------*/
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

    /*----------------------------------------------------------------------------
        %%Function: FAddProcessingCode
        %%Qualified: DroidUpc.MainActivity.FAddProcessingCode

        add the given code to the processing list (so we don't process multiple
        times if scanner dispatches multiple times)
    ----------------------------------------------------------------------------*/
    bool FAddProcessingCode(string sCode, CorrelationID crid)
    {
        lock (m_plsProcessing)
        {
            m_lp.LogEvent(
                crid,
                EventType.Verbose,
                "Checking processing list, there are {0} items in the list",
                m_plsProcessing.Count);
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

#if NOTYET
        private void DisplayResult(Result result)
        {
            if (result != null)
            {
                m_txtStatus.Text = "result != null, format = " + result.BarcodeFormat + ", text = " + result.Text;
                m_upca.Play(AlertType.GoodInfo);
            }
            else
            {
                m_txtStatus.Text = "result = null";
            }
        }
#endif
    /*----------------------------------------------------------------------------
        %%Function: ScannerControlDispatchScanCode
        %%Qualified: UniversalUpc.MainPage.ScannerControlDispatchScanCode
        %%Contact: rlittle

        Handle the scanner dispatching a scan code to us.
    ----------------------------------------------------------------------------*/
    public void ScannerControlDispatchScanCode(ZXing.Result result)
    {
        CorrelationID crid = new CorrelationID();

        m_lp.LogEvent(crid, EventType.Information, "Dispatching ScanCode: {0}", result?.Text);

        if (result == null)
        {
            m_upca.Play(AlertType.BadInfo);
            m_lp.LogEvent(crid, EventType.Error, "result == null");
            m_droidContext.SetTextElementValue(DroidUITextElement.ScanCode, "", ThreadPolicy.Sync);
            return;
        }

        string sResultText = result.Text;

        // we don't bother to await this call -- it wouldn't mean much anyway since the next
        // control dispatch would just come in and execute before we were done anyway...
        _ = DispatchScanCode(sResultText, m_droidContext.GetCheckboxElementValue(DroidUICheckboxElement.CheckOnly), crid);
    }

    void UpdateBinCode()
    {
        string sBinCode = UpcInvCore.BinCodeFromRowColumn(
            m_droidContext.GetTextElementValue(DroidUITextElement.Row),
            m_droidContext.GetTextElementValue(DroidUITextElement.Column));

        if (sBinCode != null)
            m_droidContext.SetTextElementValue(DroidUITextElement.BinCode, sBinCode, ThreadPolicy.Sync);
    }

    // every wine inventory scan is a composite. reset the items we need to have 
    // scanned in every time (row and scan code)
    void ResetWineInventoryControls()
    {
        if (m_adasCurrent != UpcInvCore.ADAS.WineRack)
            return;

        m_droidContext.RunOnUiThread(
            () =>
            {
                m_droidContext.SetTextElementValue(DroidUITextElement.Row, "", ThreadPolicy.Sync);
                m_droidContext.SetTextElementValue(DroidUITextElement.BinCode, "", ThreadPolicy.Sync);
                m_droidContext.SetTextElementValue(DroidUITextElement.ScanCode, "", ThreadPolicy.Sync);
            });
    }

    /*----------------------------------------------------------------------------
        %%Function: RouteWineScanOnEnter
        %%Qualified: DroidUpc.MainActivity.RouteWineScanOnEnter

        When doing wine inventory (in winerack mode), multiple scans compose
        together to accrue a scan
    ----------------------------------------------------------------------------*/
    async Task RouteWineRackScanCode(string sScanCode, bool fCheckOnly, CorrelationID crid)
    {
        if (sScanCode.ToUpper().StartsWith("C_"))
        {
            // this is a column code
            await m_droidContext.SetTextElementValue(DroidUITextElement.Column, sScanCode.ToUpper(), ThreadPolicy.Async);
        }
        if (sScanCode.ToUpper().StartsWith("R_"))
        {
            // this is a column code
            await m_droidContext.SetTextElementValue(DroidUITextElement.Row, sScanCode.ToUpper(), ThreadPolicy.Async);
        }

        // FUTURE: All of these RunOnUiThread calls run ASYNC, which means we have 
        // to wait for them to finish. they need to signal us that they are done so we can continue. 
        // MAKE a generic run on Ui thread that is async so we can await them.
        // if the row and column codes are complete, calculate the bin code
        UpdateBinCode();

        if (string.IsNullOrEmpty(sScanCode))
        {
            m_droidContext.SetFocusOnTextElement(DroidUITextElement.ScanCode, false);
            return;
        }

        // if its all numbers, then its a wine scan code
        if (Char.IsDigit(sScanCode[0]))
        {
            await m_droidContext.SetTextElementValue(DroidUITextElement.ScanCode, sScanCode, ThreadPolicy.Async);
        }
        else
        {
            await m_droidContext.SetTextElementValue(DroidUITextElement.ScanCode, "", ThreadPolicy.Async);
        }

        string scanCode = m_droidContext.GetTextElementValue(DroidUITextElement.ScanCode);
        string binCode = m_droidContext.GetTextElementValue(DroidUITextElement.BinCode);

        // now, if all the bin code is done, and the scan code is done
        // we can dispatch. otherwise, set focus back to ourselves (and select
        // all waiting for the next input)
        if (!string.IsNullOrEmpty(scanCode)
            && !string.IsNullOrEmpty(binCode))
        {
            if (binCode.Length != 8)
            {
                m_isr.AddMessage(AlertType.BadInfo, $"Bincode {binCode} is not 8 digits. Skipping partial bincode");
                m_droidContext.SetFocusOnTextElement(DroidUITextElement.BinCode, false);
                return;
            }

            DispatchWineScanCode(scanCode, binCode, fCheckOnly, crid);
            return;
        }

        m_droidContext.SetFocusOnTextElement(DroidUITextElement.ScanCode, false);
    }

    /*----------------------------------------------------------------------------
        %%Function: DispatchScanCode
        %%Qualified: UniversalUpc.MainPage.DispatchScanCode
        %%Contact: rlittle

        dispatch the given scan code, regardless of whether or not it was 
        automatically scanned (from the camera) or it was typed in (possibly
        from an attached wand scanner)
    ----------------------------------------------------------------------------*/
    public async Task DispatchScanCode(string sResultText, bool fCheckOnly, CorrelationID crid)
    {
        m_isr.AddMessage(AlertType.UPCScanBeep, $"Scan received: {sResultText}");

        if (AdasCurrent() == UpcInvCore.ADAS.DVD)
            DispatchDvdScanCode(sResultText, fCheckOnly, crid);
        else if (AdasCurrent() == UpcInvCore.ADAS.Book)
            DispatchBookScanCode(sResultText, fCheckOnly, crid);
        else if (AdasCurrent() == UpcInvCore.ADAS.Wine)
            DispatchWineScanCode(sResultText, null /*sBinCode*/, fCheckOnly, crid);
        else if (AdasCurrent() == UpcInvCore.ADAS.WineRack)
            await RouteWineRackScanCode(sResultText, fCheckOnly, crid);
    }


    /*----------------------------------------------------------------------------
        %%Function: DispatchWineScanCode
        %%Qualified: DroidUpc.MainActivity.DispatchWineScanCode

        dispatch the wine code (drink the wine, or look it up)
    ----------------------------------------------------------------------------*/
    async void DispatchWineScanCode(string sResultText, string sBinCode, bool fCheckOnly, CorrelationID crid)
    {
        string sScanCode = sResultText;

        // guard against reentrancy on the same scan code.
        m_lp.LogEvent(crid, EventType.Verbose, "About to check for already processing: {0}", sScanCode);
        if (!FAddProcessingCode(sScanCode, crid))
            return;

        // now handle this scan code

        m_droidContext.SetTextElementValue(DroidUITextElement.ScanCode, sScanCode, ThreadPolicy.Sync);

        // The removal of the reentrancy guard will happen asynchronously
        await m_upccCore.DoHandleWineScanCode(
            UpcInvCore.s_workIdNil,
            sScanCode,
            m_droidContext.GetTextElementValue(DroidUITextElement.Notes),
            sBinCode,
            fCheckOnly,
            false /* fErrorSoundsOnly*/,
            crid,
            async (workId, scanCode, crids, sTitle, fResult) =>
            {
                await m_droidContext.RunOnUiThreadAsync(
                    () =>
                    {
                        if (sTitle == null)
                        {
                            m_droidContext.SetTextElementValue(DroidUITextElement.Title, "!!TITLE NOT FOUND", ThreadPolicy.Sync);
                            m_droidContext.SetFocusOnTextElement(DroidUITextElement.Title, true);
                        }
                        else
                        {
                            m_droidContext.SetTextElementValue(DroidUITextElement.Title, sTitle, ThreadPolicy.Sync);
                            m_droidContext.SetFocusOnTextElement(DroidUITextElement.ScanCode, false);
                        }

                        m_lp.LogEvent(
                            crids,
                            fResult ? EventType.Information : EventType.Error,
                            "FinalScanCodeCleanup: {0}: {1}",
                            fResult,
                            sTitle);
                        FinishCode(sScanCode, CorrelationID.FromCrids(crids));
                    });
            });

        ResetWineInventoryControls();
    }

    async void DispatchBookScanCode(string sResultText, bool fCheckOnly, CorrelationID crid)
    {
        string sIsbn13 = m_upccCore.SEnsureIsbn13(sResultText);

        if (sIsbn13.StartsWith("!!"))
        {
            m_lp.LogEvent(crid, EventType.Error, sIsbn13);
            m_isr.AddMessage(AlertType.BadInfo, sIsbn13);
            return;
        }

        string location = m_droidContext.GetTextElementValue(DroidUITextElement.Location);

        if (String.IsNullOrEmpty(location))
        {
            m_isr.AddMessage(AlertType.Halt, "Cannot scan book without location!");
            return;
        }

        // guard against reentrancy on the same scan code.
        m_lp.LogEvent(crid, EventType.Verbose, "About to check for already processing: {0}", sIsbn13);
        if (!FAddProcessingCode(sIsbn13, crid))
            return;

        // now handle this scan code

        m_droidContext.SetTextElementValue(DroidUITextElement.ScanCode, sIsbn13, ThreadPolicy.Sync);

        // The removal of the reentrancy guard will happen asynchronously
        await m_upccCore.DoHandleBookScanCode(
            UpcInvCore.s_workIdNil,
            sIsbn13,
            location,
            null,
            fCheckOnly,
            false /*fErrorSoundsOnly*/,
            crid,
            async (workId, scanCode, crids, sTitle, fResult) =>
            {
                await m_droidContext.RunOnUiThreadAsync(
                    () =>
                    {
                        if (sTitle == null)
                        {
                            m_droidContext.SetTextElementValue(DroidUITextElement.Title, "!!TITLE NOT FOUND", ThreadPolicy.Async);
                            m_droidContext.SetFocusOnTextElement(DroidUITextElement.Title, true);
                        }
                        else
                        {
                            m_droidContext.SetTextElementValue(DroidUITextElement.Title, sTitle, ThreadPolicy.Async);
                            m_droidContext.SetFocusOnTextElement(DroidUITextElement.ScanCode, true);
                        }
                    });

                m_lp.LogEvent(
                    crids,
                    fResult ? EventType.Information : EventType.Error,
                    "FinalScanCodeCleanup: {0}: {1}",
                    fResult,
                    sTitle);
                FinishCode(sIsbn13, CorrelationID.FromCrids(crids));
            });
    }

    private async void DispatchDvdScanCode(string sResultText, bool fCheckOnly, CorrelationID crid)
    {
        string sCode = m_upccCore.SEnsureEan13(sResultText);

        // guard against reentrancy on the same scan code.
        m_lp.LogEvent(crid, EventType.Verbose, "About to check for already processing: {0}", sCode);
        try
        {
            if (!FAddProcessingCode(sCode, crid))
                return;
        }
        catch (Exception exc)
        {
            m_droidContext.RunOnUiThread(() => { m_isr.AddMessage(AlertType.Halt, "Exception caught: {0}", exc.Message); });
        }
        // now handle this scan code

        m_droidContext.SetTextElementValue(DroidUITextElement.ScanCode, sCode, ThreadPolicy.Sync);

        // The removal of the reentrancy guard will happen asynchronously
        await m_upccCore.DoHandleDvdScanCode(
            UpcInvCore.s_workIdNil,
            sCode,
            null /*sExtra*/,
            null /*sExtra2*/,
            fCheckOnly,
            false /*fErrorSoundsOnly*/,
            crid,
            async (workId, scanCode, crids, sTitle, fResult) =>
            {
                await m_droidContext.RunOnUiThreadAsync(
                    () =>
                    {
                        if (sTitle == null)
                        {
                            m_droidContext.SetTextElementValue(DroidUITextElement.Title, "!!TITLE NOT FOUND", ThreadPolicy.Async);
                            m_droidContext.SetFocusOnTextElement(DroidUITextElement.Title, true);
                        }
                        else
                        {
                            m_droidContext.SetTextElementValue(DroidUITextElement.Title, sTitle, ThreadPolicy.Async);
                            m_droidContext.SetFocusOnTextElement(DroidUITextElement.ScanCode, true);
                        }
                    });

                m_lp.LogEvent(
                    crids,
                    fResult ? EventType.Information : EventType.Error,
                    "FinalScanCodeCleanup: {0}: {1}",
                    fResult,
                    sTitle);
                FinishCode(sCode, CorrelationID.FromCrids(crids));
            });
    }

    public string ZeroPrefixString(string sOriginal)
    {
        return sOriginal.PadLeft(4, '0');
    }

    public delegate string AdjustTextDelegate(string s);
    public delegate void AfterOnEnterDelegate();
    public delegate Task DispatchDelegate(string sResultText, bool fCheckOnly, CorrelationID crid);

    /*----------------------------------------------------------------------------
        %%Function: DoManual
        %%Qualified: UniversalUpc.MainPage.DoManual
        %%Contact: rlittle

        take the current title and scan code and create an entry for it.

        if SimScan is checked, then this will perform a simulated scan by
        directly invoking the code that the frame preview would have called
    ----------------------------------------------------------------------------*/
    public async void DoManual(object? sender, EventArgs e)
    {
        string sTitle = m_droidContext.GetTextElementValue(DroidUITextElement.Title);
        bool fCheckOnly = m_droidContext.GetCheckboxElementValue(DroidUICheckboxElement.CheckOnly);

        if (fCheckOnly)
        {
            if (AdasCurrent() == UpcInvCore.ADAS.DVD)
                await m_upccCore.DoCheckDvdTitleInventory(sTitle, new CorrelationID());
            else if (AdasCurrent() == UpcInvCore.ADAS.Book)
                await m_upccCore.DoCheckBookTitleInventory(sTitle, new CorrelationID());
            else if (AdasCurrent() == UpcInvCore.ADAS.Wine)
                m_isr.AddMessage(AlertType.BadInfo, "No manual operation available for Wine");

            return;
        }

        if (sTitle.StartsWith("!!"))
        {
            m_isr.AddMessage(AlertType.BadInfo, "Can't add title with leading error text '!!'");
            return;
        }

        CorrelationID crid = new CorrelationID();

        bool fResult = false;

        string scanCode = m_droidContext.GetTextElementValue(DroidUITextElement.ScanCode);
        string location = m_droidContext.GetTextElementValue(DroidUITextElement.Location);

        if (m_adasCurrent == UpcInvCore.ADAS.DVD)
            fResult = await m_upccCore.DoCreateDvdTitle(scanCode, sTitle, fCheckOnly, false, crid);
        else if (m_adasCurrent == UpcInvCore.ADAS.Book)
            fResult = await m_upccCore.DoCreateBookTitle(scanCode, sTitle, location, fCheckOnly, false, crid);
        else if (m_adasCurrent == UpcInvCore.ADAS.Wine)
            m_isr.AddMessage(AlertType.BadInfo, "No manual operation available for Wine");

        // refresh the value
        scanCode = m_droidContext.GetTextElementValue(DroidUITextElement.ScanCode);

        if (fResult)
            m_isr.AddMessage(AlertType.GoodInfo, "Added {0} as {1}", scanCode, sTitle);
        else
            m_isr.AddMessage(AlertType.Halt, "FAILED  to Added {0} as {1}", scanCode, sTitle);
    }




    private void DoCheckChange(object sender, EventArgs e)
    {
        // TODO: m_fCheckOnly = cbCheckOnly.IsChecked ?? false;
    }

    private void OnEnterSelectAll(object sender, View.FocusChangeEventArgs e)
    {
        EditText eb = (EditText)sender;
        if (e.HasFocus)
            eb.SelectAll();
    }

#if NOTYET
        private void OnCodeKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                CorrelationID crid = new CorrelationID();
                string sResultText = ebScanCode.Text;

                m_lp.LogEvent(crid, EventType.Information, "Dispatching ScanCode: {0}", sResultText);

                DispatchScanCode(sResultText, crid);
                e.Handled = true;
                return;
            }

            e.Handled = false;
        }
#endif
}
