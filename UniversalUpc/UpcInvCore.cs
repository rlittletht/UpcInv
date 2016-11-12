using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using TCore.Logging;
using TCore.StatusBox;
using UpcService = UniversalUpc.UpcSvc;

namespace UniversalUpc
{
    public class UpcInvCore
    {
        private UpcService.UpcSvcClient m_usc = null;
        private IAlert m_ia;
        private IStatusReporting m_isr;
        private ILogProvider m_lp;

        public enum ADAS
        {
            Generic = 0,
            Book = 1,
            DVD = 2,
            Wine = 3,
        }

        ADAS m_adas;

        /*----------------------------------------------------------------------------
        	%%Function: UpcInvCore
        	%%Qualified: UniversalUpc.UpcInvCore.UpcInvCore
        	%%Contact: rlittle
        	
            Createa a new instance of UpcInvCore (provide alerting and status 
            reporting interfaces)
        ----------------------------------------------------------------------------*/
        public UpcInvCore(IAlert ia, IStatusReporting isr, ILogProvider lp)
        {
            m_ia = ia;
            m_isr = isr;
            m_lp = lp;
        }

        #region Service Integration

        /*----------------------------------------------------------------------------
        	%%Function: EnsureServiceConnection
        	%%Qualified: UniversalUpc.UpcInvCore.EnsureServiceConnection
        	%%Contact: rlittle
        	
            Make sure we have a valid connection to our service backend
        ----------------------------------------------------------------------------*/
        private void EnsureServiceConnection()
        {
            if (m_usc == null)
                m_usc = new UpcService.UpcSvcClient();
        }

        public DateTime? DttmGetLastScan(string sScanCode)
        {
            EnsureServiceConnection();

            Task<UpcService.USR_String> tsk = m_usc.GetLastScanDateAsync(sScanCode);

            tsk.Wait();

            if (!tsk.IsCompleted || !tsk.Result.Result)
                return null;

            return DateTime.Parse(tsk.Result.TheValue);
        }

        /*----------------------------------------------------------------------------
        	%%Function: DvdInfoRetrieve
        	%%Qualified: UniversalUpc.UpcInvCore.DvdInfoRetrieve
        	%%Contact: rlittle
        	
            Get information for this DVD scan code
        ----------------------------------------------------------------------------*/
        public async Task<UpcService.DvdInfo> DvdInfoRetrieve(string sScanCode)
        {
            EnsureServiceConnection();
            UpcService.USR_DvdInfo usrd = await m_usc.GetDvdScanInfoAsync(sScanCode);
            UpcService.DvdInfo dvdInfo = usrd.TheValue;

            return dvdInfo;
        }

        public async Task<List<UpcService.DvdInfo>> DvdInfoListRetrieve(string sTitle)
        {
            EnsureServiceConnection();
            UpcService.USR_DvdInfoList usrdl = await m_usc.GetDvdScanInfosFromTitleAsync(sTitle);
            if (usrdl.Result == false || usrdl.TheValue == null)
                return null;

            List<UpcService.DvdInfo> dvdInfoList = new List<UpcService.DvdInfo>(usrdl.TheValue);

            if (dvdInfoList.Count == 0)
                return null;

            return dvdInfoList;
        }

        public delegate void ContinueWithUpdateScanDateDelegate(string sScanCode, bool fSucceeded);

        /*----------------------------------------------------------------------------
        	%%Function: UpdateScanDate
        	%%Qualified: UniversalUpc.UpcInvCore.UpdateScanDate
        	%%Contact: rlittle
        	
            Get scan information for this scan code (this works for any item type)
        ----------------------------------------------------------------------------*/
        public async Task<bool> UpdateScanDate(string sScanCode)
        {
            EnsureServiceConnection();
            UpcService.USR usr = await m_usc.UpdateUpcLastScanDateAsync(sScanCode, "");

            return usr.Result;
        }

        public delegate void ContinueWithFetchTitleDelegate(string sScanCode, string sTitle);

        /*----------------------------------------------------------------------------
        	%%Function: FetchTitleFromGenericUPC
        	%%Qualified: UniversalUpc.UpcInvCore.FetchTitleFromGenericUPC
        	%%Contact: rlittle
        	
            Call the service to see if it can get us a product title from the given
            UPC code (NOT EAN number)
        ----------------------------------------------------------------------------*/
        public async Task<string> FetchTitleFromGenericUPC(string sScanCode)
        {
            EnsureServiceConnection();
            string sTitle = await m_usc.FetchTitleFromGenericUPCAsync(sScanCode);

            return sTitle;
        }

        public async Task<string> FetchTitleFromISBN13(string sScanCode)
        {
            EnsureServiceConnection();
            string sTitle = await m_usc.FetchTitleFromISBN13Async(sScanCode);

            return sTitle;
        }

        public delegate void ContinueWithCreateDvdDelegate(string sScanCode, string sTitle, bool fResult);

        /*----------------------------------------------------------------------------
        	%%Function: CreateDvd
        	%%Qualified: UniversalUpc.UpcInvCore.CreateDvd
        	%%Contact: rlittle
        	
            Create a DVD with the given scan code and title.
        ----------------------------------------------------------------------------*/
        public async Task<bool> CreateDvd(string sScanCode, string sTitle, CorrelationID crid)
        {
            EnsureServiceConnection();
            UpcService.USR usr = await m_usc.CreateDvdAsync(sScanCode, sTitle);

            if (usr.Result)
                m_lp.LogEvent(crid, EventType.Verbose, "Successfully added title for {0}", sScanCode);
            else
                m_lp.LogEvent(crid, EventType.Error, "Failed to add title for {0}", sScanCode);

            return usr.Result;
        }

        public async Task<UpcService.BookInfo> BookInfoRetrieve(string sScanCode)
        {
            EnsureServiceConnection();
            UpcService.USR_BookInfo usrd = await m_usc.GetBookScanInfoAsync(sScanCode);
            UpcService.BookInfo bki = usrd.TheValue;

            return bki;
        }

        public async Task<List<UpcService.BookInfo>> BookInfoListRetrieve(string sTitle)
        {
            EnsureServiceConnection();
            UpcService.USR_BookInfoList usrdl = await m_usc.GetBookScanInfosFromTitleAsync(sTitle);
            if (usrdl.Result == false || usrdl.TheValue == null)
                return null;

            List<UpcService.BookInfo> bkiList = new List<UpcService.BookInfo>(usrdl.TheValue);

            if (bkiList.Count == 0)
                return null;

            return bkiList;
        }

        /*----------------------------------------------------------------------------
        	%%Function: CreateBook
        	%%Qualified: UniversalUpc.UpcInvCore.CreateBook
        	%%Contact: rlittle
        	
            Create a Book with the given scan code and title.
        ----------------------------------------------------------------------------*/
        public async Task<bool> UpdateBookScan(string sScanCode, string sTitle, string sLocation, CorrelationID crid)
        {
            EnsureServiceConnection();
            UpcService.USR usr = await m_usc.UpdateBookScanAsync(sScanCode, sTitle, sLocation);

            if (usr.Result)
                m_lp.LogEvent(crid, EventType.Verbose, "Successfully update title for {0}", sScanCode);
            else
                m_lp.LogEvent(crid, EventType.Error, "Failed to update title for {0}", sScanCode);

            return usr.Result;
        }
        /*----------------------------------------------------------------------------
        	%%Function: CreateBook
        	%%Qualified: UniversalUpc.UpcInvCore.CreateBook
        	%%Contact: rlittle
        	
            Create a Book with the given scan code and title.
        ----------------------------------------------------------------------------*/
        public async Task<bool> CreateBook(string sScanCode, string sTitle, string sLocation, CorrelationID crid)
        {
            EnsureServiceConnection();
            UpcService.USR usr = await m_usc.CreateBookAsync(sScanCode, sTitle, sLocation);

            if (usr.Result)
                m_lp.LogEvent(crid, EventType.Verbose, "Successfully added title for {0}", sScanCode);
            else
                m_lp.LogEvent(crid, EventType.Error, "Failed to add title for {0}", sScanCode);

            return usr.Result;
        }

        public async Task<UpcService.WineInfo> WineInfoRetrieve(string sScanCode)
        {
            EnsureServiceConnection();
            UpcService.USR_WineInfo usrd = await m_usc.GetWineScanInfoAsync(sScanCode);
            UpcService.WineInfo wni = usrd.TheValue;

            return wni;
        }

        public async Task<bool> DrinkWine(string sScanCode, string sWine, string sVintage, string sNotes, CorrelationID crid)
        {
            EnsureServiceConnection();
            UpcService.USR usr = await m_usc.DrinkWineAsync(sScanCode, sWine, sVintage, sNotes);

            if (usr.Result)
                m_lp.LogEvent(crid, EventType.Verbose, "Successfully added title for {0}", sScanCode);
            else
                m_lp.LogEvent(crid, EventType.Error, "Failed to add title for {0}", sScanCode);

            return usr.Result;
        }
        #endregion

        #region UPC/EAN Support
        /*----------------------------------------------------------------------------
        	%%Function: SEnsureEan13
        	%%Qualified: UniversalUpc.UpcInvCore.SEnsureEan13
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        public string SEnsureEan13(string s)
        {
            if (s.Length == 12)
                return "0" + s;

            return s;
        }

        public string SEnsureIsbn13(string s)
        {
            string sIsbn13 = null;

            if (s.Length == 10)
                {
                if (s.Substring(9, 1) != SCheckCalcIsbn10(s.Substring(0, 9)))
                    return String.Format("!!ISBN check value incorret: {0} != {1}", s.Substring(9, 1), SCheckCalcIsbn10(s.Substring(0, 9)));

                sIsbn13 = "978" + s.Substring(0, 9);
                sIsbn13 += SCheckCalcIsbn13(sIsbn13);
                return sIsbn13;
                }

            if (s.Length == 13)
                {
                if (s.Substring(12, 1) != SCheckCalcIsbn13(s.Substring(0,12)))
                    return String.Format("!!ISBN check value incorret: {0} != {1}", s.Substring(12, 1), SCheckCalcIsbn10(s.Substring(0, 12)));

                return s;
                }

            return "!!ISBN UPC must be 10 or 13 digits";
        }

        string SCheckCalcIsbn10(string sIsbn10)
        {
            if (sIsbn10.Length != 9)
                return null;
                
            Int32 n = 0;
            Int32 i;
            
            for (i = 0; i < 9; i++)
                {
                n += ((i + 1) * Int32.Parse(sIsbn10.Substring(i, 1)));
            }
            n = n % 11;
            if (n == 10)
                return "X";
            else
                return String.Format("{0}", n);
        
        }

        string SCheckCalcIsbn13(string sIsbn13)
        {
            if (sIsbn13.Length != 12)
                return null;

            Int32 n = 0;
            Int32 i;

            for (i = 0; i < 12; i++)
                {   
                int nDigit = Int32.Parse(sIsbn13.Substring(i, 1));
                if (i % 2 == 0)
                    { // get a weight of 1
                    n += nDigit;
                    }
                else
                    {
                    n += nDigit * 3;
                    }
                }
            n = n % 10;
            n = 10 - n;
            n = n % 10;
            return String.Format("{0}", n);
        }

        #endregion

        #region Client Business Logic

        #region DVD Client
        public delegate void FinalScanCodeCleanupDelegate(CorrelationID crid, string sFinalTitle, bool fResult);

        /*----------------------------------------------------------------------------
        	%%Function: DoHandleDvdScanCode
        	%%Qualified: UniversalUpc.UpcInvCore.DoHandleDvdScanCode
        	%%Contact: rlittle
        	
            Handle the dispatch of a DVD scan code.  Update the scan date, 
            lookup a title, and create a title if necessary.
        ----------------------------------------------------------------------------*/
        public async void DoHandleDvdScanCode(string sCode, CorrelationID crid, FinalScanCodeCleanupDelegate del)
        {
            string sTitle = null;
            bool fResult = false;
            m_lp.LogEvent(crid, EventType.Verbose, "Continuing with processing for {0}...Checking for DvdInfo from service", sCode);
            UpcService.DvdInfo dvdi = await DvdInfoRetrieve(sCode);

            if (dvdi != null)
                {
                DoUpdateDvdScanDate(sCode, dvdi, crid, del);
                }
            else
                {
                sTitle = await DoLookupDvdTitle(sCode, crid);

                if (sTitle != null)
                    fResult = await DoCreateDvdTitle(sCode, sTitle, crid);

                del(crid, sTitle, fResult);
                }
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoLookupDvdTitle
        	%%Qualified: UniversalUpc.UpcInvCore.DoLookupDvdTitle
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        private async Task<string> DoLookupDvdTitle(string sCode, CorrelationID crid)
        {
            m_lp.LogEvent(crid, EventType.Verbose, "No DVD info for scan code {0}, looking up...", sCode);

            m_isr.AddMessage(UpcAlert.AlertType.None, "looking up code {0}", sCode);
            string sTitle = await FetchTitleFromGenericUPC(sCode);

            if (sTitle == "" || sTitle.Substring(0, 2) == "!!")
                {
                m_lp.LogEvent(crid, EventType.Verbose, "Service did not return a title for {0}", sCode);

                m_isr.AddMessage(UpcAlert.AlertType.BadInfo, "Couldn't find title for {0}: {1}", sCode, sTitle);
                return null;
                }
            return sTitle;
        }

        public async Task<bool> DoCheckDvdTitleInventory(string sTitle, CorrelationID crid)
        {
            m_lp.LogEvent(crid, EventType.Verbose, "Checking inventory for dvd title {0}", sTitle);

            List<UpcService.DvdInfo> dvdis = await DvdInfoListRetrieve(sTitle);
            if (dvdis == null)
                {
                m_isr.AddMessage(UpcAlert.AlertType.BadInfo, "No inventory found for titles like {0}", sTitle);
                m_lp.LogEvent(crid, EventType.Verbose, "No inventory found for titles like {0}", sTitle);
                return false;
                }
            else
                {
                m_lp.LogEvent(crid, EventType.Verbose, "Found {0} matching titles for {1}", dvdis.Count, sTitle);
                m_isr.AddMessage(UpcAlert.AlertType.GoodInfo, "Found the following {0} matching titles in inventory:", dvdis.Count);
                foreach (UpcService.DvdInfo dvdi in dvdis)
                    {
                    m_isr.AddMessage(UpcAlert.AlertType.None, "{0}: {1} ({2})", dvdi.Code, dvdi.Title, dvdi.LastScan);
                    }
                return true;
                }
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoCreateDvdTitle
        	%%Qualified: UniversalUpc.UpcInvCore.DoCreateDvdTitle
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        public async Task<bool> DoCreateDvdTitle(string sCode, string sTitle, CorrelationID crid)
        {
            m_lp.LogEvent(crid, EventType.Verbose, "Service returned title {0} for code {1}. Adding title.", sTitle, sCode);

            bool fResult = await CreateDvd(sCode, sTitle, crid);
            if (fResult)
                m_isr.AddMessage(UpcAlert.AlertType.GoodInfo, "Added title for {0}: {1}", sCode, sTitle);
            else
                m_isr.AddMessage(UpcAlert.AlertType.BadInfo, "Couldn't create DVD title for {0}: {1}", sCode, sTitle);

            return fResult;
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoUpdateDvdScanDate
        	%%Qualified: UniversalUpc.UpcInvCore.DoUpdateDvdScanDate
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        private async void DoUpdateDvdScanDate(string sCode, UpcService.DvdInfo dvdi, CorrelationID crid, FinalScanCodeCleanupDelegate del)
        {
            m_lp.LogEvent(crid, EventType.Verbose, "Service returned info for {0}", sCode);

            // check for a dupe/too soon last scan (within 1 hour)
            if (dvdi.LastScan > DateTime.Now.AddHours(-1))
                {
                m_lp.LogEvent(crid, EventType.Verbose, "Avoiding duplicate scan for {0}", sCode);
                m_isr.AddMessage(UpcAlert.AlertType.Duplicate, "{0}: Duplicate?! LastScan was {1}", dvdi.Title, dvdi.LastScan.ToString());
                del(crid, dvdi.Title, true);
                return;
                }

            m_lp.LogEvent(crid, EventType.Verbose, "Calling service to update scan date for {0}", sCode);

            // now update the last scan date
            bool fResult = await UpdateScanDate(sCode);

            if (fResult)
                {
                m_lp.LogEvent(crid, EventType.Verbose, "Successfully updated last scan for {0}", sCode);
                m_isr.AddMessage(UpcAlert.AlertType.GoodInfo, "{0}: Updated LastScan (was {1})", dvdi.Title, dvdi.LastScan.ToString());
                }
            else
                {
                m_lp.LogEvent(crid, EventType.Error, "Failed to update last scan for {0}", sCode);
                m_isr.AddMessage(UpcAlert.AlertType.BadInfo, "{0}: Failed to update last scan!", dvdi.Title);
                }

            del(crid, dvdi.Title, true);
        }
        #endregion

        #region Wine Client
        public async void DoHandleWineScanCode(string sCode, string sNotes, CorrelationID crid, FinalScanCodeCleanupDelegate del)
        {
            if (sNotes.StartsWith("!!"))
                {
                m_isr.AddMessage(UpcAlert.AlertType.BadInfo, "Notes not set: {0}", sNotes);
                del(crid, null, false);
                return;
                }
            string sTitle = null;
            bool fResult = false;
            m_lp.LogEvent(crid, EventType.Verbose, "Continuing with processing for {0}...Checking for WineInfo from service", sCode);
            UpcService.WineInfo wni = await WineInfoRetrieve(sCode);

            if (wni != null)
                {
                DoDrinkWine(sCode, sNotes, wni, crid, del);
                }
            else
                {
                m_isr.AddMessage(UpcAlert.AlertType.BadInfo, "Could not find wine for {0}", sCode);

                sTitle = "!!WINE NOTE FOUND";

                del(crid, sTitle, false);
                }
        }

        private async void DoDrinkWine(string sCode, string sNotes, UpcService.WineInfo wni, CorrelationID crid, FinalScanCodeCleanupDelegate del)
        {
            m_lp.LogEvent(crid, EventType.Verbose, "Service returned info for {0}", sCode);

            m_lp.LogEvent(crid, EventType.Verbose, "Drinking wine {0}", sCode);

            // now update the last scan date
            bool fResult = await DrinkWine(sCode, wni.Wine, wni.Vintage, sNotes, crid);

            if (fResult)
                {
                m_lp.LogEvent(crid, EventType.Verbose, "Successfully drank wine for {0}", sCode);
                m_isr.AddMessage(UpcAlert.AlertType.Drink, "{0}: Drank wine!", wni.Wine);
                }
            else
                {
                m_lp.LogEvent(crid, EventType.Error, "Failed to drink wine {0}", sCode);
                m_isr.AddMessage(UpcAlert.AlertType.BadInfo, "{0}: Failed to drink wine!", wni.Wine);
                }

            del(crid, wni.Wine, true);
        }
        #endregion


        #region Book Client
        /*----------------------------------------------------------------------------
        	%%Function: DoHandleBookScanCode
        	%%Qualified: UniversalUpc.UpcInvCore.DoHandleBookScanCode
        	%%Contact: rlittle
        	
            Handle the dispatch of a DVD scan code.  Update the scan date, 
            lookup a title, and create a title if necessary.
        ----------------------------------------------------------------------------*/
        public async void DoHandleBookScanCode(string sCode, string sLocation, CorrelationID crid, FinalScanCodeCleanupDelegate del)
        {
            if (sLocation.StartsWith("!!"))
                {
                m_isr.AddMessage(UpcAlert.AlertType.BadInfo, "Location not set: {0}", sLocation);
                del(crid, null, false);
                return;
                }
            string sTitle = null;
            bool fResult = false;
            m_lp.LogEvent(crid, EventType.Verbose, "Continuing with processing for {0}...Checking for BookInfo from service", sCode);
            UpcService.BookInfo bki = await BookInfoRetrieve(sCode);

            if (bki != null)
                {
                DoUpdateBookScanDate(sCode, sLocation, bki, crid, del);
                }
            else
                {
                sTitle = await DoLookupBookTitle(sCode, crid);

                if (sTitle != null)
                    fResult = await DoCreateBookTitle(sCode, sTitle, sLocation, crid);

                del(crid, sTitle, fResult);
                }
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoLookupDvdTitle
        	%%Qualified: UniversalUpc.UpcInvCore.DoLookupDvdTitle
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        private async Task<string> DoLookupBookTitle(string sCode, CorrelationID crid)
        {
            m_lp.LogEvent(crid, EventType.Verbose, "No Book info for scan code {0}, looking up...", sCode);

            m_isr.AddMessage(UpcAlert.AlertType.None, "looking up code {0}", sCode);
            string sTitle = await FetchTitleFromISBN13(sCode);

            if (sTitle == "" || sTitle.Substring(0, 2) == "!!")
                {
                m_lp.LogEvent(crid, EventType.Verbose, "Service did not return a title for {0}", sCode);

                m_isr.AddMessage(UpcAlert.AlertType.BadInfo, "Couldn't find title for {0}: {1}", sCode, sTitle);
                return null;
                }
            return sTitle;
        }

        public async Task<bool> DoCheckBookTitleInventory(string sTitle, CorrelationID crid)
        {
            m_lp.LogEvent(crid, EventType.Verbose, "Checking inventory for dvd title {0}", sTitle);

            List<UpcService.BookInfo> bkis = await BookInfoListRetrieve(sTitle);
            if (bkis == null)
                {
                m_isr.AddMessage(UpcAlert.AlertType.BadInfo, "No inventory found for titles like {0}", sTitle);
                m_lp.LogEvent(crid, EventType.Verbose, "No inventory found for titles like {0}", sTitle);
                return false;
                }
            else
                {
                m_lp.LogEvent(crid, EventType.Verbose, "Found {0} matching titles for {1}", bkis.Count, sTitle);
                m_isr.AddMessage(UpcAlert.AlertType.GoodInfo, "Found the following {0} matching titles in inventory:", bkis.Count);
                foreach (UpcService.BookInfo bki in bkis)
                    {
                    m_isr.AddMessage(UpcAlert.AlertType.None, "{0}: {1} ({2})", bki.Code, bki.Title, bki.LastScan);
                    }
                return true;
                }
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoCreateDvdTitle
        	%%Qualified: UniversalUpc.UpcInvCore.DoCreateDvdTitle
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        public async Task<bool> DoCreateBookTitle(string sCode, string sTitle, string sLocation, CorrelationID crid)
        {
            m_lp.LogEvent(crid, EventType.Verbose, "Service returned title {0} for code {1}. Adding title.", sTitle, sCode);

            bool fResult = await CreateBook(sCode, sTitle, sLocation, crid);
            if (fResult)
                m_isr.AddMessage(UpcAlert.AlertType.GoodInfo, "Added title for {0}: {1}", sCode, sTitle);
            else
                m_isr.AddMessage(UpcAlert.AlertType.BadInfo, "Couldn't create Book title for {0}: {1}", sCode, sTitle);

            return fResult;
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoUpdateDvdScanDate
        	%%Qualified: UniversalUpc.UpcInvCore.DoUpdateDvdScanDate
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        private async void DoUpdateBookScanDate(string sCode, string sLocation, UpcService.BookInfo bki, CorrelationID crid, FinalScanCodeCleanupDelegate del)
        {
            m_lp.LogEvent(crid, EventType.Verbose, "Service returned info for {0}", sCode);

            // check for a dupe/too soon last scan (within 1 hour)
            if (bki.LastScan > DateTime.Now.AddHours(-1))
                {
                m_lp.LogEvent(crid, EventType.Verbose, "Avoiding duplicate scan for {0}", sCode);
                m_isr.AddMessage(UpcAlert.AlertType.Duplicate, "{0}: Duplicate?! LastScan was {1}", bki.Title, bki.LastScan.ToString());
                del(crid, bki.Title, true);
                return;
                }

            m_lp.LogEvent(crid, EventType.Verbose, "Calling service to update scan date for {0}", sCode);

            // now update the last scan date
            bool fResult = await UpdateBookScan(sCode, bki.Title, sLocation, crid);

            if (fResult)
                {
                m_lp.LogEvent(crid, EventType.Verbose, "Successfully updated last scan for {0}", sCode);
                m_isr.AddMessage(UpcAlert.AlertType.GoodInfo, "{0}: Updated LastScan (was {1})", bki.Title, bki.LastScan.ToString());
                }
            else
                {
                m_lp.LogEvent(crid, EventType.Error, "Failed to update last scan for {0}", sCode);
                m_isr.AddMessage(UpcAlert.AlertType.BadInfo, "{0}: Failed to update last scan!", bki.Title);
                }

            del(crid, bki.Title, true);
        }

        #endregion

        #endregion

    }
}
