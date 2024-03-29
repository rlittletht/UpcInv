﻿using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.AppCompat.App;
using Android.Runtime;
using Android.Widget;
using ZXing.Mobile;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using ApxLabs.FastAndroidCamera;
using TCore.Logging;
using TCore.StatusBox;
using UpcShared;
using EventType = UpcShared.EventType;

namespace DroidUpc
{
    public partial class MainActivity : AppCompatActivity
    {
        private List<string> m_plsProcessing;
        private UpcInvCore.ADAS m_adasCurrent;

        delegate void ActivityResultDelegate(Intent? intent, Result resultCode);

        private Dictionary<int, ActivityResultDelegate> m_activityDelegates =
            new Dictionary<int, ActivityResultDelegate>();

        void RegisterActivityDelegate(int requestCode, ActivityResultDelegate del)
        {
            if (!m_activityDelegates.ContainsKey(requestCode))
                m_activityDelegates.Add(requestCode, null);

            m_activityDelegates[requestCode] = del;
        }

        /*----------------------------------------------------------------------------
        	%%Function: AdasCurrent
        	%%Qualified: DroidUpc.MainActivity.AdasCurrent
        ----------------------------------------------------------------------------*/
        UpcInvCore.ADAS AdasCurrent()
        {
            Spinner spinner = FindViewById<Spinner>(Resource.Id.spinType);

            return (UpcInvCore.ADAS) spinner.SelectedItemPosition;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            if (m_activityDelegates.ContainsKey(requestCode))
                m_activityDelegates[requestCode](data, resultCode);

            base.OnActivityResult(requestCode, resultCode, data);
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
        private void ScannerControlDispatchScanCode(ZXing.Result result)
        {
            CorrelationID crid = new CorrelationID();

            m_lp.LogEvent(crid, EventType.Information, "Dispatching ScanCode: {0}", result?.Text);

            if (result == null)
            {
                m_upca.Play(AlertType.BadInfo);
                m_lp.LogEvent(crid, EventType.Error, "result == null");
                RunOnUiThread(() => m_ebScanCode.Text = "");
                return;
            }

            string sResultText = result.Text;

            // we don't bother to await this call -- it wouldn't mean much anyway since the next
            // control dispatch would just come in and execute before we were done anyway...
            _ = DispatchScanCode(sResultText, m_cbCheckOnly.Checked, crid);
        }

        void UpdateBinCode()
        {
            string sBinCode = UpcInvCore.BinCodeFromRowColumn(m_ebRow.Text, m_ebColumn.Text);

            if (sBinCode != null)
                RunOnUiThread(() => m_ebBinCode.Text = sBinCode);
        }

        // every wine inventory scan is a composite. reset the items we need to have 
        // scanned in every time (row and scan code)
        void ResetWineInventoryControls()
        {
            if (m_adasCurrent != UpcInvCore.ADAS.WineRack)
                return;

            RunOnUiThread(
                () =>
                {
                    m_ebRow.Text = "";
                    m_ebBinCode.Text = "";
                    m_ebScanCode.Text = "";
                });
        }

        delegate void UiThreadDelegate();

        async Task RunOnUiThreadAsync(UiThreadDelegate del)
        {
            ManualResetEventSlim mres = new ManualResetEventSlim(false);

            RunOnUiThread(
                () =>
                {
                    del();
                    mres.Set();
                });

            // wait on a background thread. this way caller can choose to await it,
            // or just let it run...
            await Task.Run(() => mres.Wait());
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
                await RunOnUiThreadAsync(() => m_ebColumn.Text = sScanCode.ToUpper());
            }
            if (sScanCode.ToUpper().StartsWith("R_"))
            {
                // this is a column code
                await RunOnUiThreadAsync(() => m_ebRow.Text = sScanCode.ToUpper());
            }

            // FUTURE: All of these RunOnUiThread calls run ASYNC, which means we have 
            // to wait for them to finish. they need to signal us that they are done so we can continue. 
            // MAKE a generic run on Ui thread that is async so we can await them.
            // if the row and column codes are complete, calculate the bin code
            UpdateBinCode();

            if (string.IsNullOrEmpty(sScanCode))
            {
	            SetFocus(m_ebScanCode, false);
	            return;
            }
            
            // if its all numbers, then its a wine scan code
            if (Char.IsDigit(sScanCode[0]))
            {
                await RunOnUiThreadAsync(() => m_ebScanCode.Text = sScanCode);
            }
            else
            {
                await RunOnUiThreadAsync(() => m_ebScanCode.Text = "");
            }

            // now, if all the bin code is done, and the scan code is done
            // we can dispatch. otherwise, set focus back to ourselves (and select
            // all waiting for the next input)
            if (!string.IsNullOrEmpty(m_ebScanCode.Text)
                && !string.IsNullOrEmpty(m_ebBinCode.Text))
            {
	            if (m_ebBinCode.Text.Length != 8)
	            {
		            m_isr.AddMessage(AlertType.BadInfo, $"Bincode {m_ebBinCode.Text} is not 8 digits. Skipping partial bincode");
		            SetFocus(m_ebBinCode, false);
                    return;
	            }
	            
                DispatchWineScanCode(m_ebScanCode.Text, m_ebBinCode.Text, fCheckOnly, crid);
                return;
            }

            SetFocus(m_ebScanCode, false);
        }

        /*----------------------------------------------------------------------------
            %%Function: DispatchScanCode
            %%Qualified: UniversalUpc.MainPage.DispatchScanCode
            %%Contact: rlittle

            dispatch the given scan code, regardless of whether or not it was 
            automatically scanned (from the camera) or it was typed in (possibly
            from an attached wand scanner)
        ----------------------------------------------------------------------------*/
        private async Task DispatchScanCode(string sResultText, bool fCheckOnly, CorrelationID crid)
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
        	%%Function: SetFocus
        	%%Qualified: DroidUpc.MainActivity.SetFocus
        	
            set focus to the given editbox
        ----------------------------------------------------------------------------*/
        void SetFocus(EditText eb, bool fWantKeyboard)
        {
            RunOnUiThread(
                () =>
                {
                    eb.RequestFocus();
                    eb.SelectAll();

                    InputMethodManager imm =
                        (InputMethodManager) GetSystemService(global::Android.Content.Context.InputMethodService);

                    if (fWantKeyboard)
                    {
                        imm.ShowSoftInput(eb, ShowFlags.Implicit);
                    }
                    else
                    {
                        imm.HideSoftInputFromWindow(eb.WindowToken, 0);
                    }
                });
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

            RunOnUiThread(() => m_ebScanCode.Text = sScanCode);

            // The removal of the reentrancy guard will happen asynchronously
            await m_upccCore.DoHandleWineScanCode(
                UpcInvCore.s_workIdNil,
                sScanCode,
                m_ebNotes.Text,
                sBinCode,
                fCheckOnly,
                false /* fErrorSoundsOnly*/,
                crid,
                async (workId, scanCode, crids, sTitle, fResult) =>
                {
                    await RunOnUiThreadAsync(
                        () =>
                        {
                            if (sTitle == null)
                            {
                                m_ebTitle.Text = "!!TITLE NOT FOUND";
                                SetFocus(m_ebTitle, true);
                            }
                            else
                            {
                                m_ebTitle.Text = sTitle;
                                SetFocus(m_ebScanCode, false);
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

            if (String.IsNullOrEmpty(m_ebLocation.Text))
            {
                m_isr.AddMessage(AlertType.Halt, "Cannot scan book without location!");
                return;
            }

            // guard against reentrancy on the same scan code.
            m_lp.LogEvent(crid, EventType.Verbose, "About to check for already processing: {0}", sIsbn13);
            if (!FAddProcessingCode(sIsbn13, crid))
                return;

            // now handle this scan code

            RunOnUiThread(() => m_ebScanCode.Text = sIsbn13);

            // The removal of the reentrancy guard will happen asynchronously
            await m_upccCore.DoHandleBookScanCode(
                UpcInvCore.s_workIdNil,
                sIsbn13,
                m_ebLocation.Text,
                null,
                fCheckOnly,
                false /*fErrorSoundsOnly*/,
                crid,
                async (workId, scanCode, crids, sTitle, fResult) =>
                {
                    await RunOnUiThreadAsync(
                        () =>
                        {
                            if (sTitle == null)
                            {
                                m_ebTitle.Text = "!!TITLE NOT FOUND";
                                SetFocus(m_ebTitle, true);
                            }
                            else
                            {
                                m_ebTitle.Text = sTitle;
                                SetFocus(m_ebScanCode, false);
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

        async private void DispatchDvdScanCode(string sResultText, bool fCheckOnly, CorrelationID crid)
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
                this.RunOnUiThread(() => { m_isr.AddMessage(AlertType.Halt, "Exception caught: {0}", exc.Message); });
            }
            // now handle this scan code

            this.RunOnUiThread(() => m_ebScanCode.Text = sCode);

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
                    await RunOnUiThreadAsync(
                        () =>
                        {
                            if (sTitle == null)
                            {
                                m_ebTitle.Text = "!!TITLE NOT FOUND";
                                SetFocus(m_ebTitle, true);
                            }
                            else
                            {
                                m_ebTitle.Text = sTitle;
                                SetFocus(m_ebScanCode, false);
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

        string ZeroPrefixString(string sOriginal)
        {
	        return sOriginal.PadLeft(4, '0');
        }
        
        delegate string AdjustTextDelegate(string s);
        delegate void AfterOnEnterDelegate();
        
        private async Task DoKeyPressCommon(string sPrefix, object sender, View.KeyEventArgs eventArgs, AdjustTextDelegate adjustText, AfterOnEnterDelegate afterOnEnter)
		{
			eventArgs.Handled = false;

			if (eventArgs.KeyCode == Keycode.Enter)
			{
				if (eventArgs.Event.Action == KeyEventActions.Up)
				{
					EditText eb = (EditText)sender;
					string sResultText = eb.Text;
					if (adjustText != null)
						sResultText = adjustText(sResultText);
					
					CorrelationID crid = new CorrelationID();

					m_lp.LogEvent(crid, EventType.Information, "Dispatching ScanCode: {0}", sResultText);

					await RunOnUiThreadAsync(() => eb.Text = $"{sPrefix}{sResultText}");

                    await DispatchScanCode(eb.Text, m_cbCheckOnly.Checked, crid);
                    afterOnEnter?.Invoke();
				}

                // handle both down and up so we don't get the default behavior...
                eventArgs.Handled = true;
			}
        }

        private async void DoColumnCodeKeyPress(object sender, View.KeyEventArgs eventArgs)
        {
	        await DoKeyPressCommon("C_", sender, eventArgs, ZeroPrefixString, () => SetFocus(m_ebRow, true));
        }

        private async void DoRowCodeKeyPress(object sender, View.KeyEventArgs eventArgs)
        {
	        await DoKeyPressCommon("R_", sender, eventArgs, ZeroPrefixString, () => SetFocus(m_ebBinCode, true));
        }

        /*----------------------------------------------------------------------------
			%%Function:DoBinCodeKeyPress
			%%Qualified:DroidUpc.MainActivity.DoBinCodeKeyPress

			We're going to cleverly lie here and pass in the scancode control since
			enter here means we might be ready to complete the scancode...
        ----------------------------------------------------------------------------*/
        private async void DoBinCodeKeyPress(object sender, View.KeyEventArgs eventArgs)
        {
	        await DoKeyPressCommon("", m_ebScanCode, eventArgs, null, null);
        }

        private async void DoCodeKeyPress(object sender, View.KeyEventArgs eventArgs)
        {
	        await DoKeyPressCommon("", sender, eventArgs, null, null);
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoManual
        	%%Qualified: UniversalUpc.MainPage.DoManual
        	%%Contact: rlittle
        	
            take the current title and scan code and create an entry for it.

            if SimScan is checked, then this will perform a simulated scan by
            directly invoking the code that the frame preview would have called
        ----------------------------------------------------------------------------*/
        private async void DoManual(object sender, EventArgs e)
        {
            string sTitle = m_ebTitle.Text;

            if (m_cbCheckOnly.Checked)
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

            if (m_adasCurrent == UpcInvCore.ADAS.DVD)
                fResult = await m_upccCore.DoCreateDvdTitle(m_ebScanCode.Text, sTitle, m_cbCheckOnly.Checked, false, crid);
            else if (m_adasCurrent == UpcInvCore.ADAS.Book)
                fResult = await m_upccCore.DoCreateBookTitle(m_ebScanCode.Text, sTitle, m_ebLocation.Text, m_cbCheckOnly.Checked, false, crid);
            else if (m_adasCurrent == UpcInvCore.ADAS.Wine)
                m_isr.AddMessage(AlertType.BadInfo, "No manual operation available for Wine");

            if (fResult)
                m_isr.AddMessage(AlertType.GoodInfo, "Added {0} as {1}", m_ebScanCode.Text, sTitle);
            else
                m_isr.AddMessage(AlertType.Halt, "FAILED  to Added {0} as {1}", m_ebScanCode.Text, sTitle);
        }

        private void SetNewMediaType(object sender, EventArgs e)
        {
            m_adasCurrent = AdasCurrent(); // AdasFromDropdownItem(cbMediaType.SelectedItem as ComboBoxItem);
            AdjustUIForMediaType(m_adasCurrent);
        }

        void AdjustUIForMediaType(UpcInvCore.ADAS adas)
        {
            if (m_txtLocation == null || m_ebLocation == null || m_ebNotes == null)
                return;

            RunOnUiThread(
                () =>
                {
                    ViewStates visLocation = ViewStates.Gone;
                    ViewStates visNotes = ViewStates.Gone;
                    ViewStates visBin = ViewStates.Gone;

                    switch (adas)
                    {
                        case UpcInvCore.ADAS.Book:
                            visLocation = ViewStates.Visible;
                            break;
                        case UpcInvCore.ADAS.Wine:
                            visNotes = ViewStates.Visible;
                            break;
                        case UpcInvCore.ADAS.WineRack:
                            visBin = ViewStates.Visible;
                            break;
                        case UpcInvCore.ADAS.DVD:
                            break;
                        case UpcInvCore.ADAS.Generic:
                            break;
                    }

                    m_txtLocation.Visibility = visLocation;
                    m_ebLocation.Visibility = visLocation;
                    m_ebNotes.Visibility = visNotes;
                    m_txtBinCode.Visibility = visBin;
                    m_txtColumn.Visibility = visBin;
                    m_txtRow.Visibility = visBin;
                    m_ebBinCode.Visibility = visBin;
                    m_ebColumn.Visibility = visBin;
                    m_ebRow.Visibility = visBin;

                });
        }

        private void DoCheckChange(object sender, EventArgs e)
        {
            // TODO: m_fCheckOnly = cbCheckOnly.IsChecked ?? false;
        }

        private void OnEnterSelectAll(object sender, View.FocusChangeEventArgs e)
        {
            EditText eb = (EditText) sender;
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
}