﻿using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using ZXing.Mobile;
using System.Collections.Generic;
using Android;
using Android.Support.V4.Content;
using Android.Views;
using Android.Views.InputMethods;
using TCore.Logging;
using TCore.StatusBox;

namespace DroidUpc
{
    public partial class MainActivity : AppCompatActivity
    {
        private List<string> m_plsProcessing;
        private UpcInvCore.ADAS m_adasCurrent;

        UpcInvCore.ADAS AdasCurrent()
        {
            Spinner spinner = FindViewById<Spinner>(Resource.Id.spinType);

            return (UpcInvCore.ADAS) spinner.SelectedItemPosition;
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
                m_lp.LogEvent(crid, EventType.Verbose, "Checking processing list, there are {0} items in the list",
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
                m_upca.Play(UpcAlert.AlertType.GoodInfo);
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

            For now, this is just a DVD scan code, but later will handle others...
        ----------------------------------------------------------------------------*/
        private void ScannerControlDispatchScanCode(ZXing.Result result)
        {
            CorrelationID crid = new CorrelationID();

            m_lp.LogEvent(crid, EventType.Information, "Dispatching ScanCode: {0}", result?.Text);

            if (result == null)
            {
                m_upca.Play(UpcAlert.AlertType.BadInfo);
                m_lp.LogEvent(crid, EventType.Error, "result == null");
                RunOnUiThread(() => m_ebScanCode.Text = "");
                return;
            }

            string sResultText = result.Text;

            DispatchScanCode(sResultText, crid);
        }

        /*----------------------------------------------------------------------------
            %%Function: DispatchScanCode
            %%Qualified: UniversalUpc.MainPage.DispatchScanCode
            %%Contact: rlittle

            dispatch the given scan code, regardless of whether or not it was 
            automatically scanned (from the camera) or it was typed in (possibly
            from an attached wand scanner)
        ----------------------------------------------------------------------------*/
        private void DispatchScanCode(string sResultText, CorrelationID crid)
        {
            m_upca.DoAlert(UpcAlert.AlertType.UPCScanBeep);

            if (AdasCurrent() == UpcInvCore.ADAS.DVD)
                DispatchDvdScanCode(sResultText, crid);
            else if (AdasCurrent() == UpcInvCore.ADAS.Book)
                DispatchBookScanCode(sResultText, crid);
            else if (AdasCurrent() == UpcInvCore.ADAS.Wine)
                DispatchWineScanCode(sResultText, crid);
        }

        void SetFocus(EditText eb, bool fWantKeyboard)
        {
            RunOnUiThread(() =>
            {
                eb.RequestFocus();
                eb.SelectAll();

                InputMethodManager imm =
                    (InputMethodManager)GetSystemService(global::Android.Content.Context.InputMethodService);

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

        void DispatchWineScanCode(string sResultText, CorrelationID crid)
        {
            string sScanCode = sResultText;

            // guard against reentrancy on the same scan code.
            m_lp.LogEvent(crid, EventType.Verbose, "About to check for already processing: {0}", sScanCode);
            if (!FAddProcessingCode(sScanCode, crid))
                return;

            // now handle this scan code

            RunOnUiThread(() => m_ebScanCode.Text = sScanCode);

            // The removal of the reentrancy guard will happen asynchronously
            m_upccCore.DoHandleWineScanCode(sScanCode, m_ebNotes.Text, crid, (cridDel, sTitle, fResult) =>
            {
                RunOnUiThread(() =>
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

                    m_lp.LogEvent(cridDel, fResult ? EventType.Information : EventType.Error,
                        "FinalScanCodeCleanup: {0}: {1}", fResult, sTitle);
                    FinishCode(sScanCode, cridDel);
                });
            });
        }

        void DispatchBookScanCode(string sResultText, CorrelationID crid)
        {
            string sIsbn13 = m_upccCore.SEnsureIsbn13(sResultText);

            if (sIsbn13.StartsWith("!!"))
            {
                m_lp.LogEvent(crid, EventType.Error, sIsbn13);
                m_isr.AddMessage(UpcAlert.AlertType.BadInfo, sIsbn13);
                return;
            }

            // guard against reentrancy on the same scan code.
            m_lp.LogEvent(crid, EventType.Verbose, "About to check for already processing: {0}", sIsbn13);
            if (!FAddProcessingCode(sIsbn13, crid))
                return;

            // now handle this scan code

            RunOnUiThread(() => m_ebScanCode.Text = sIsbn13);

            // The removal of the reentrancy guard will happen asynchronously
            m_upccCore.DoHandleBookScanCode(sIsbn13, m_ebLocation.Text, crid, (cridDel, sTitle, fResult) =>
            {
                RunOnUiThread(() =>
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

                m_lp.LogEvent(cridDel, fResult ? EventType.Information : EventType.Error, "FinalScanCodeCleanup: {0}: {1}", fResult, sTitle);
                FinishCode(sIsbn13, cridDel);
            });
        }

        private void DispatchDvdScanCode(string sResultText, CorrelationID crid)
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
                this.RunOnUiThread(() =>
                    {
                        m_isr.AddMessage(UpcAlert.AlertType.Halt, "Exception caught: {0}", exc.Message);
                    });
            }
            // now handle this scan code

            this.RunOnUiThread(() => m_ebScanCode.Text = sCode);


            // The removal of the reentrancy guard will happen asynchronously
            m_upccCore.DoHandleDvdScanCode(sCode, crid, (cridDel, sTitle, fResult) =>
            {
                this.RunOnUiThread(() =>
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

                m_lp.LogEvent(cridDel, fResult ? EventType.Information : EventType.Error, "FinalScanCodeCleanup: {0}: {1}", fResult, sTitle);
                FinishCode(sCode, cridDel);
            });
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoManual
        	%%Qualified: UniversalUpc.MainPage.DoManual
        	%%Contact: rlittle
        	
            take the current title and scan code and create an entry for it.
        ----------------------------------------------------------------------------*/
        private async void DoManual(object sender, EventArgs e)
        {
            string sTitle = m_ebTitle.Text;

            if (m_cbCheckOnly.Checked)
            {
                if (AdasCurrent() == UpcInvCore.ADAS.DVD)
                    m_upccCore.DoCheckDvdTitleInventory(sTitle, new CorrelationID());
                else if (AdasCurrent() == UpcInvCore.ADAS.Book)
                    m_upccCore.DoCheckBookTitleInventory(sTitle, new CorrelationID());
                else if (AdasCurrent() == UpcInvCore.ADAS.Wine)
                    m_isr.AddMessage(UpcAlert.AlertType.BadInfo, "No manual operation available for Wine");

                return;
            }

            if (sTitle.StartsWith("!!"))
            {
                m_isr.AddMessage(UpcAlert.AlertType.BadInfo, "Can't add title with leading error text '!!'");
                return;
            }

            CorrelationID crid = new CorrelationID();

            bool fResult = false;

            if (m_adasCurrent == UpcInvCore.ADAS.DVD)
                fResult = await m_upccCore.DoCreateDvdTitle(m_ebScanCode.Text, sTitle, crid);
            else if (m_adasCurrent == UpcInvCore.ADAS.Book)
                fResult = await m_upccCore.DoCreateBookTitle(m_ebScanCode.Text, sTitle, m_ebLocation.Text, crid);
            else if (m_adasCurrent == UpcInvCore.ADAS.Wine)
                m_isr.AddMessage(UpcAlert.AlertType.BadInfo, "No manual operation available for Wine");

            if (fResult)
                m_isr.AddMessage(UpcAlert.AlertType.GoodInfo, "Added {0} as {1}", m_ebScanCode.Text, sTitle);
            else
                m_isr.AddMessage(UpcAlert.AlertType.Halt, "FAILED  to Added {0} as {1}", m_ebScanCode.Text, sTitle);
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

            RunOnUiThread(() =>
            {
                switch (adas)
                {
                    case UpcInvCore.ADAS.Book:
                        m_txtLocation.Visibility = Android.Views.ViewStates.Visible;
                        m_ebLocation.Visibility = Android.Views.ViewStates.Visible;
                        m_ebNotes.Visibility = Android.Views.ViewStates.Gone;
                        break;
                    case UpcInvCore.ADAS.Wine:
                        m_txtLocation.Visibility = Android.Views.ViewStates.Gone;
                        m_ebLocation.Visibility = Android.Views.ViewStates.Gone;
                        m_ebNotes.Visibility = Android.Views.ViewStates.Visible;
                        break;
                    case UpcInvCore.ADAS.DVD:
                        m_txtLocation.Visibility = Android.Views.ViewStates.Gone;
                        m_ebLocation.Visibility = Android.Views.ViewStates.Gone;
                        m_ebNotes.Visibility = Android.Views.ViewStates.Gone;
                        break;
                    case UpcInvCore.ADAS.Generic:
                        m_txtLocation.Visibility = Android.Views.ViewStates.Gone;
                        m_ebLocation.Visibility = Android.Views.ViewStates.Gone;
                        m_ebNotes.Visibility = Android.Views.ViewStates.Gone;
                        break;
                }
            });
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
}