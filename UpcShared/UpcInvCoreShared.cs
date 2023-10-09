

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using TCore.WebInterop;
using UpcApi.Proxy;

namespace UpcShared
{
    public interface IAlert
    {
        void DoAlert(AlertType at);
    }

    public enum AlertType
    {
        GoodInfo,
        BadInfo,
        Halt,
        Duplicate,
        Drink,
        UPCScanBeep,
        None
    };

    public enum ServiceStatus
    {
        Unknown = -1,
        Running = 0
    };

    public interface IStatusReporting
    {
        void AddMessage(AlertType at, string sMessage, params object[] rgo);
    }

    public enum EventType
    {
        Critical = 0,
        Error = 1,
        Warning = 2,
        Information = 3,
        Verbose = 4
    };

    public interface ILogProvider
    {
        void LogEvent(Guid crids, EventType et, string s, params object[] rgo);
    }

    public class UpcInvCore
    {
        public static int s_workIdNil = -1;

        private WebApi m_api = null;

        private IAlert m_ia;
        private IStatusReporting m_isr;
        private ILogProvider m_lp;

        public enum ADAS
        {
            Generic = 0,
            Book = 1,
            DVD = 2,
            Wine = 3,
            WineRack = 4,
            Max = 5
        }

        public enum WDI // Wine Drink or Inventory
        {
            Drink = 0,
            Inventory = 1,
            Max = 2
        }

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
            if (m_api == null)
                m_api = new WebApi(new WebApiInterop("https://upcinv-api.azurewebsites.net", null));
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetServiceStatusHeartBeat
        	%%Qualified: UpcShared.UpcInvCore.GetServiceStatusHeartBeat
        	
        ----------------------------------------------------------------------------*/
        public async Task<ServiceStatus> GetServiceStatusHeartBeat()
        {
            EnsureServiceConnection();

            USR_DiagnosticResult result = await m_api.GetHeartbeat();

            return (ServiceStatus) (int) result.TheValue;
        }

        /*----------------------------------------------------------------------------
        	%%Function: DvdInfoRetrieve
        	%%Qualified: UniversalUpc.UpcInvCore.DvdInfoRetrieve
        	%%Contact: rlittle
        	
            Get information for this DVD scan code
        ----------------------------------------------------------------------------*/
        public async Task<DvdInfo> DvdInfoRetrieve(string sScanCode)
        {
            EnsureServiceConnection();

            USR_DvdInfo usrd = await m_api.GetDvdScanInfo(sScanCode);
            DvdInfo dvdInfo = usrd.TheValue;

            return dvdInfo;
        }

        public async Task<List<DvdInfo>> DvdInfoListRetrieve(string sTitle)
        {
            EnsureServiceConnection();

            USR_DvdInfoList usrdl = await m_api.GetDvdScanInfosFromTitle(sTitle);
            if (usrdl.Result == false || usrdl.TheValue == null)
                return null;

            List<DvdInfo> dvdInfoList = new List<DvdInfo>(usrdl.TheValue);

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
        public async Task<bool> UpdateScanDate(string sScanCode, string sTitle)
        {
            EnsureServiceConnection();
            USR usr = await m_api.UpdateUpcLastScanDate(sScanCode, sTitle);

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
            string sTitle = await m_api.FetchTitleFromGenericUPC(sScanCode);

            return sTitle;
        }

        public async Task<string> FetchTitleFromISBN13(string sScanCode)
        {
            EnsureServiceConnection();
            string sTitle = await m_api.FetchTitleFromISBN13(sScanCode);

            return sTitle;
        }

        public delegate void ContinueWithCreateDvdDelegate(string sScanCode, string sTitle, bool fResult);

        /*----------------------------------------------------------------------------
        	%%Function: CreateDvd
        	%%Qualified: UniversalUpc.UpcInvCore.CreateDvd
        	%%Contact: rlittle
        	
            Create a DVD with the given scan code and title.
        ----------------------------------------------------------------------------*/
        public async Task<bool> CreateDvd(string sScanCode, string sTitle, Guid crids)
        {
            EnsureServiceConnection();
            USR usr = await m_api.CreateDvd(sScanCode, sTitle);

            if (usr.Result)
                m_lp.LogEvent(crids, EventType.Verbose, "Successfully added title for {0}", sScanCode);
            else
                m_lp.LogEvent(crids, EventType.Error, "Failed to add title for {0}", sScanCode);

            return usr.Result;
        }

        public async Task<BookInfo> BookInfoRetrieve(string sScanCode)
        {
            EnsureServiceConnection();
            USR_BookInfo usrd = await m_api.GetBookScanInfo(sScanCode);
            BookInfo bki = usrd.TheValue;

            return bki;
        }

        public async Task<List<BookInfo>> BookInfoListRetrieve(string sTitle)
        {
            EnsureServiceConnection();
            USR_BookInfoList usrdl = await m_api.GetBookScanInfosFromTitle(sTitle);

            if (usrdl.Result == false || usrdl.TheValue == null)
                return null;

            List<BookInfo> bkiList = new List<BookInfo>(usrdl.TheValue);

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
        public async Task<bool> UpdateBookScan(string sScanCode, string sTitle, string sLocation, Guid crids)
        {
            EnsureServiceConnection();
            USR usr = await m_api.UpdateBookScan(sScanCode, sTitle, sLocation);

            if (usr.Result)
                m_lp.LogEvent(crids, EventType.Verbose, "Successfully update title for {0}", sScanCode);
            else
                m_lp.LogEvent(crids, EventType.Error, "Failed to update title for {0}", sScanCode);

            return usr.Result;
        }

        /*----------------------------------------------------------------------------
        	%%Function: CreateBook
        	%%Qualified: UniversalUpc.UpcInvCore.CreateBook
        	%%Contact: rlittle
        	
            Create a Book with the given scan code and title.
        ----------------------------------------------------------------------------*/
        public async Task<bool> CreateBook(string sScanCode, string sTitle, string sLocation, Guid crids)
        {
            EnsureServiceConnection();
            USR usr;

            try
            {
                usr = await m_api.CreateBook(sScanCode, sTitle, sLocation);
            }
            catch (Exception exc)
            {
                usr = USR.Failed(exc);
            }

            if (usr.Result)
                m_lp.LogEvent(crids, EventType.Verbose, "Successfully added title for {0}", sScanCode);
            else
                m_lp.LogEvent(crids, EventType.Error, "Failed to add title for {0}", sScanCode);

            return usr.Result;
        }

        public async Task<WineInfo> WineInfoRetrieve(string sScanCode)
        {
            EnsureServiceConnection();
            // we will try this a few times, trying to correct for leading zeros in the scan code
            int cZerosLeft = 2;

            USR_WineInfo usrd;
            WineInfo wni;

            while (true)
            {
                usrd = await m_api.GetWineScanInfo(sScanCode);
                wni = usrd.TheValue;

                if (usrd.Succeeded || cZerosLeft-- <= 0)
                    break;

                sScanCode = $"0{sScanCode}";
            }

            return wni;
        }

        public async Task<bool> DrinkWine(string sScanCode, string sWine, string sVintage, string sNotes, Guid crids)
        {
            EnsureServiceConnection();
            USR usr;

            usr = await m_api.DrinkWine(sScanCode, sWine, sVintage, sNotes);

            if (usr.Result)
                m_lp.LogEvent(crids, EventType.Verbose, "Successfully added title for {0}", sScanCode);
            else
                m_lp.LogEvent(crids, EventType.Error, "Failed to add title for {0}", sScanCode);

            return usr.Result;
        }

        public async Task<bool> UpdateWineInventory(string sScanCode, string sWine, string sBinCode, Guid crids)
        {
            EnsureServiceConnection();
            USR usr;

            usr = await m_api.UpdateWineInventory(sScanCode, sWine, sBinCode);

            if (usr.Result)
                m_lp.LogEvent(crids, EventType.Verbose, "Successfully added title for {0}", sScanCode);
            else
                m_lp.LogEvent(crids, EventType.Error, "Failed to add title for {0}", sScanCode);

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

        public string SCreateIsbn13FromIsbn(string s)
        {
            if (s.Length == 9)
            {
                s = $"{s}{SCheckCalcIsbn10(s)}";
            }
            else if (s.Length == 12)
            {
                s = $"{s}{SCheckCalcIsbn13(s)}";
            }

            if (s.Length != 10 && s.Length != 13)
                return null;

            return SEnsureIsbn13(s);
        }

        public string SEnsureIsbn13(string s)
        {
            string sIsbn13 = null;

            // check for internal scan codes as well. right now all of our manual scan codes are 030###
            if (s.Length == 6 && s.StartsWith("030"))
                return s;

            if (s.Length == 10)
            {
                if (s.Substring(9, 1) != SCheckCalcIsbn10(s.Substring(0, 9)))
                    return String.Format("!!ISBN check value incorret: {0} != {1}", s.Substring(9, 1),
                        SCheckCalcIsbn10(s.Substring(0, 9)));

                sIsbn13 = "978" + s.Substring(0, 9);
                sIsbn13 += SCheckCalcIsbn13(sIsbn13);
                return sIsbn13;
            }

            if (s.Length == 13)
            {
                if (s.Substring(12, 1) != SCheckCalcIsbn13(s.Substring(0, 12)))
                    return String.Format("!!ISBN check value incorret: {0} != {1}", s.Substring(12, 1),
                        SCheckCalcIsbn10(s.Substring(0, 12)));

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
                {
                    // get a weight of 1
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

        public delegate Task FinalScanCodeReportAndCleanupDelegate(int workId, string scanCode, Guid crids, string sFinalTitle, bool fResult);

        /*----------------------------------------------------------------------------
        	%%Function: DoHandleDvdScanCode
        	%%Qualified: UniversalUpc.UpcInvCore.DoHandleDvdScanCode
        	%%Contact: rlittle
        	
            Handle the dispatch of a DVD scan code.  Update the scan date, 
            lookup a title, and create a title if necessary.
        ----------------------------------------------------------------------------*/
        public async Task DoHandleDvdScanCode(
            int workId,
            string sCode,
            string sUnused,
            string sUnused2,
            bool fCheckOnly,
            bool fErrorSoundsOnly,
            Guid crids,
            FinalScanCodeReportAndCleanupDelegate del)
        {
            string sTitle = null;
            bool fResult = false;
            m_lp.LogEvent(crids, EventType.Verbose,
                "Continuing with processing for {0}...Checking for DvdInfo from service", sCode);

            m_isr.AddMessage(AlertType.None, $"Checking inventory for code: {sCode}...");

            DvdInfo dvdi = await DvdInfoRetrieve(sCode);

            if (dvdi != null)
            {
                DoUpdateDvdScanDate(workId, sCode, dvdi, fCheckOnly, fErrorSoundsOnly, crids, del);
            }
            else
            {
                try
                {
                    sTitle = await DoLookupDvdTitle(sCode, crids);

                    if (sTitle != null)
                        fResult = await DoCreateDvdTitle(sCode, sTitle, fCheckOnly, fErrorSoundsOnly, crids);
                }
                catch (Exception exc)
                {
                    m_isr.AddMessage(AlertType.Halt, "Exception Caught: {0}", exc.Message);

                    sTitle = "!!Exception";
                    fResult = false;
                }
                finally
                {
                    await del(workId, sCode, crids, sTitle, fResult);
                }
            }
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoLookupDvdTitle
        	%%Qualified: UniversalUpc.UpcInvCore.DoLookupDvdTitle
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        private async Task<string> DoLookupDvdTitle(string sCode, Guid crids)
        {
            m_lp.LogEvent(crids, EventType.Verbose, "No DVD info for scan code {0}, looking up...", sCode);

            m_isr.AddMessage(AlertType.None, "No inventory found, doing lookup for code: {0}", sCode);

            // if the scan length is > 12, then chop off the beginning (likely padding)
            if (sCode.Length > 12)
                sCode = sCode.Substring(sCode.Length - 12);

            if (sCode.Length != 12
                && !(sCode.Length == 6 && sCode.StartsWith("030")))
            {
                m_isr.AddMessage(AlertType.BadInfo, $"Scancode {sCode} not a valid UPC code.");
                return null;
            }

            string sTitle = await FetchTitleFromGenericUPC(sCode);

            if (sTitle == "" || sTitle.Substring(0, 2) == "!!")
            {
                m_lp.LogEvent(crids, EventType.Verbose, "Service did not return a title for {0}", sCode);

                m_isr.AddMessage(AlertType.BadInfo, "Couldn't find title for {0}: {1}", sCode, sTitle);
                return null;
            }

            return sTitle;
        }

        public async Task<bool> DoCheckDvdTitleInventory(string sTitle, Guid crids)
        {
            m_lp.LogEvent(crids, EventType.Verbose, "Checking inventory for dvd title {0}", sTitle);

            List<DvdInfo> dvdis = await DvdInfoListRetrieve(sTitle);
            if (dvdis == null)
            {
                m_isr.AddMessage(AlertType.BadInfo, "No inventory found for titles like {0}", sTitle);
                m_lp.LogEvent(crids, EventType.Verbose, "No inventory found for titles like {0}", sTitle);
                return false;
            }
            else
            {
                m_lp.LogEvent(crids, EventType.Verbose, "Found {0} matching titles for {1}", dvdis.Count, sTitle);
                m_isr.AddMessage(AlertType.GoodInfo, "Found the following {0} matching titles in inventory:",
                    dvdis.Count);
                foreach (DvdInfo dvdi in dvdis)
                {
                    m_isr.AddMessage(AlertType.None, "{0}: {1} ({2})", dvdi.Code, dvdi.Title, dvdi.LastScan);
                }

                return true;
            }
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoCreateDvdTitle
        	%%Qualified: UniversalUpc.UpcInvCore.DoCreateDvdTitle
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        public async Task<bool> DoCreateDvdTitle(string sCode, string sTitle, bool fCheckOnly, bool fErrorSoundsOnly, Guid crids)
        {
            string sCheck = fCheckOnly ? "[CheckOnly] " : "";
            m_lp.LogEvent(crids, EventType.Verbose, "Service returned title {0} for code {1}. Adding title.", sTitle, sCode);

            bool fResult = fCheckOnly || await CreateDvd(sCode, sTitle, crids);

            if (fResult)
                m_isr.AddMessage(fErrorSoundsOnly ? AlertType.None : AlertType.GoodInfo, "{2}Added title for {0}: {1}", sCode, sTitle, sCheck);
            else
                m_isr.AddMessage(AlertType.BadInfo, "Couldn't create DVD title for {0}: {1}", sCode, sTitle);

            return fResult;
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoUpdateDvdScanDate
        	%%Qualified: UniversalUpc.UpcInvCore.DoUpdateDvdScanDate
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        private async void DoUpdateDvdScanDate(
            int workId,
            string sCode,
            DvdInfo dvdi,
            bool fCheckOnly,
            bool fErrorSoundsOnly,
            Guid crids,
            FinalScanCodeReportAndCleanupDelegate del)
        {
            string sCheck = fCheckOnly ? "[CheckOnly] " : "";

            m_lp.LogEvent(crids, EventType.Verbose, "Service returned info for {0}", sCode);

            // check for a dupe/too soon last scan (within 1 hour)
            if (dvdi.LastScan > DateTime.Now.AddHours(-1))
            {
                m_lp.LogEvent(crids, EventType.Verbose, "{1}Avoiding duplicate scan for {0}", sCode, sCheck);
                m_isr.AddMessage(AlertType.Duplicate, "{2}{0}: Duplicate?! LastScan was {1}", dvdi.Title,
                    dvdi.LastScan.ToString(), sCheck);
                await del(workId, sCode, crids, dvdi.Title, true);
                return;
            }

            m_lp.LogEvent(crids, EventType.Verbose, "{1}Calling service to update scan date for {0}", sCode, sCheck);

            // now update the last scan date
            bool fResult = fCheckOnly || await UpdateScanDate(sCode, dvdi.Title);

            if (fResult)
            {
                m_lp.LogEvent(crids, EventType.Verbose, "{1}Successfully updated last scan for {0}", sCode, sCheck);
                m_isr.AddMessage(fErrorSoundsOnly ? AlertType.None : AlertType.GoodInfo, "{2}{0}: Updated LastScan (was {1})", dvdi.Title,
                    dvdi.LastScan.ToString(), sCheck);
            }
            else
            {
                m_lp.LogEvent(crids, EventType.Error, "{1}Failed to update last scan for {0}", sCode, sCheck);
                m_isr.AddMessage(AlertType.BadInfo, "{1}{0}: Failed to update last scan!", dvdi.Title, sCheck);
            }

            await del(workId, sCode, crids, dvdi.Title, true);
        }

        #endregion

        #region Wine Client

        public async Task DoHandleWineScanCode(
            int workId,
            string sCode,
            string sNotes,
            string sBinCode,
            bool fCheckOnly,
            bool fErrorSoundsOnly,
            Guid crids,
            FinalScanCodeReportAndCleanupDelegate del)
        {
            bool fInventory = sBinCode != null;

            if (sNotes.StartsWith("!!") && !fInventory)
            {
                m_isr.AddMessage(AlertType.BadInfo, "Notes not set: {0}", sNotes);
                await del(workId, sCode, crids, null, false);
                return;
            }

            string sTitle = null;
            m_lp.LogEvent(crids, EventType.Verbose,
                "Continuing with processing for {0}...Checking for WineInfo from service", sCode);
            WineInfo wni = await WineInfoRetrieve(sCode);

            if (wni != null)
            {
                string sOriginalCode = sCode;

                // we want to refresh the code to what we just retrieved, but first we have to capture the 
                // original scancode so it can be sent to the delgate correctly
                FinalScanCodeReportAndCleanupDelegate delWrapper =
                    async (int workIdDel, string scanCodeDel, Guid cridsDel, string sFinalTitleDel, bool fResultDel) =>
                    {
                        await del(workIdDel, sOriginalCode, cridsDel, sFinalTitleDel, fResultDel);
                    };

                sCode = wni.Code;

                if (fInventory)
                    await DoUpdateWineInventory(workId, sCode, sBinCode, wni, fCheckOnly, fErrorSoundsOnly, crids, delWrapper);
                else
                    await DoDrinkWine(workId, sCode, sNotes, wni, fCheckOnly, fErrorSoundsOnly, crids, delWrapper);
            }
            else
            {
                m_isr.AddMessage(AlertType.BadInfo, "Could not find wine for {0}", sCode);

                sTitle = "!!WINE NOTE FOUND";

                await del(workId, sCode, crids, sTitle, false);
            }
        }

        private async Task DoDrinkWine(
            int workId,
            string sCode,
            string sNotes,
            WineInfo wni,
            bool fCheckOnly,
            bool fErrorSoundsOnly, // we ignore this for wines -- we don't do bulk wine scanning
            Guid crids,
            FinalScanCodeReportAndCleanupDelegate del)
        {
            string sCheck = fCheckOnly ? "[CheckOnly] " : "";
            m_lp.LogEvent(crids, EventType.Verbose, "Service returned info for {0}", sCode);

            m_lp.LogEvent(crids, EventType.Verbose, "{1}Drinking wine {0}", sCode, sCheck);

            // now update the last scan date
            bool fResult = fCheckOnly || await DrinkWine(sCode, wni.Wine, wni.Vintage, sNotes, crids);

            if (fResult)
            {
                m_lp.LogEvent(crids, EventType.Verbose, "{1}Successfully drank wine for {0}", sCode, sCheck);
                m_isr.AddMessage(AlertType.Drink, "{1}{0}: Drank wine!", wni.Wine, sCheck);
            }
            else
            {
                m_lp.LogEvent(crids, EventType.Error, "Failed to drink wine {0}", sCode);
                m_isr.AddMessage(AlertType.BadInfo, "{0}: Failed to drink wine!", wni.Wine);
            }

            await del(workId, sCode, crids, wni.Wine, true);
        }

        private async Task DoUpdateWineInventory(
            int workId,
            string sCode,
            string sBinCode,
            WineInfo wni,
            bool fCheckOnly,
            bool fErrorSoundsOnly, // we ignore this for wines -- we don't do bulk wine scanning
            Guid crids,
            FinalScanCodeReportAndCleanupDelegate del)
        {
            string sCheck = fCheckOnly ? "[CheckOnly] " : "";
            m_lp.LogEvent(crids, EventType.Verbose, "Service returned info for {0}", sCode);

            m_lp.LogEvent(crids, EventType.Verbose, "{1}Updating inventory for wine {0} ({2})", sCode, sCheck, sBinCode);

            // now update the last scan date
            bool fResult = fCheckOnly || await UpdateWineInventory(sCode, wni.Wine, sBinCode, crids);

            if (fResult)
            {
                m_lp.LogEvent(crids, EventType.Verbose, "{1}Successfully updated inventory for wine for {0}", sCode, sCheck);
                m_isr.AddMessage(fErrorSoundsOnly ? AlertType.None : AlertType.GoodInfo, "{1}{0}: Updated inventory for wine!", wni.Wine, sCheck);
            }
            else
            {
                m_lp.LogEvent(crids, EventType.Error, "Failed to update inventory for wine {0}", sCode);
                m_isr.AddMessage(AlertType.BadInfo, "{0}: Failed to update inventory for wine!", wni.Wine);
            }

            await del(workId, sCode, crids, wni.Wine, true);
        }


        public static string BinCodeFromRowColumn(string sRow, string sColumn)
        {
            sRow = sRow.Trim();
            sColumn = sColumn.Trim();

            if (!sColumn.StartsWith("C_"))
	            return null;

            foreach (char ch in sColumn.Substring(2))
                if (!char.IsDigit(ch))
                    return null;

            sColumn = sColumn.Substring(2);

            // we want a 3 digit column and a 3 digit row
            if (sColumn.Length > 3)
            {
                // confirm all leading digits are 0
                foreach (char ch in sColumn.Substring(0, sColumn.Length - 3))
                    if (ch != '0')
                        return null;
            }

            string sBinCode = sColumn.Substring(0, Math.Max(3, sColumn.Length)).PadLeft(3, '0');
            
            if (!sRow.StartsWith("R_"))
	            return sBinCode; // return the partial code...

            foreach (char ch in sRow.Substring(2))
	            if (!char.IsDigit(ch))
		            return sBinCode; // return the partial code...

            sRow = sRow.Substring(2);

            if (sRow.Length > 3)
            {
                // confirm all leading digits are 0
                foreach(char ch in sRow.Substring(0, sRow.Length - 3))
                    if (ch != '0')
                        return sBinCode;
            }

            sBinCode += sRow.Substring(0, Math.Max(3, sRow.Length)).PadLeft(3, '0');

            return sBinCode;
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
        public async Task DoHandleBookScanCode(
            int workId,
            string sCode,
            string sLocation,
            string sUnused2,
            bool fCheckOnly,
            bool fErrorSoundsOnly,
            Guid crids,
            FinalScanCodeReportAndCleanupDelegate del)
        {
            if (sLocation.StartsWith("!!"))
            {
                m_isr.AddMessage(AlertType.BadInfo, "Location not set: {0}", sLocation);
                await del(workId, sCode, crids, null, false);
                return;
            }

            string sTitle = null;
            bool fResult = false;
            m_lp.LogEvent(crids, EventType.Verbose, "Continuing with processing for {0}...Checking for BookInfo from service", sCode);
            BookInfo bki = await BookInfoRetrieve(sCode);

            if (bki != null)
            {
                DoUpdateBookScanDate(workId, sCode, sLocation, bki, fCheckOnly, fErrorSoundsOnly, crids, del);
            }
            else
            {
                try
                {
                    sTitle = await DoLookupBookTitle(sCode, crids);

                    if (sTitle != null)
                        fResult = await DoCreateBookTitle(sCode, sTitle, sLocation, fCheckOnly, fErrorSoundsOnly, crids);
                }
                catch (Exception exc)
                {
                    m_isr.AddMessage(AlertType.Halt, "Exception Caught: {0}", exc.Message);
                    sTitle = "!!Exception";
                    fResult = false;
                }
                finally
                {
                    await del(workId, sCode, crids, sTitle, fResult);
                }

            }
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoLookupDvdTitle
        	%%Qualified: UniversalUpc.UpcInvCore.DoLookupDvdTitle
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        private async Task<string> DoLookupBookTitle(string sCode, Guid crids)
        {
            m_lp.LogEvent(crids, EventType.Verbose, "No Book info for scan code {0}, looking up...", sCode);

            m_isr.AddMessage(AlertType.None, "looking up code {0}", sCode);

            string sTitle = await FetchTitleFromISBN13(sCode);

            if (sTitle == "" || sTitle.Substring(0, 2) == "!!")
            {
                m_lp.LogEvent(crids, EventType.Verbose, "Service did not return a title for {0}", sCode);

                m_isr.AddMessage(AlertType.BadInfo, "Couldn't find title for {0}: {1}", sCode, sTitle);
                return null;
            }

            return sTitle;
        }

        public async Task<bool> DoCheckBookTitleInventory(string sTitle, Guid crids)
        {
            m_lp.LogEvent(crids, EventType.Verbose, "Checking inventory for dvd title {0}", sTitle);

            List<BookInfo> bkis = await BookInfoListRetrieve(sTitle);
            if (bkis == null)
            {
                m_isr.AddMessage(AlertType.BadInfo, "No inventory found for titles like {0}", sTitle);
                m_lp.LogEvent(crids, EventType.Verbose, "No inventory found for titles like {0}", sTitle);
                return false;
            }
            else
            {
                m_lp.LogEvent(crids, EventType.Verbose, "Found {0} matching titles for {1}", bkis.Count, sTitle);
                m_isr.AddMessage(AlertType.GoodInfo, "Found the following {0} matching titles in inventory:",
                    bkis.Count);
                foreach (BookInfo bki in bkis)
                {
                    m_isr.AddMessage(AlertType.None, "{0}: {1} ({2})", bki.Code, bki.Title, bki.LastScan);
                }

                return true;
            }
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoCreateDvdTitle
        	%%Qualified: UniversalUpc.UpcInvCore.DoCreateDvdTitle
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        public async Task<bool> DoCreateBookTitle(string sCode, string sTitle, string sLocation, bool fCheckOnly, bool fErrorSoundsOnly, Guid crids)
        {
            string sCheck = fCheckOnly ? "[CheckOnly] " : "";
            m_lp.LogEvent(crids, EventType.Verbose, "Service returned title {0} for code {1}. Adding title.", sTitle,
                sCode);

            sCode = SEnsureIsbn13(sCode);
            bool fResult = fCheckOnly || await CreateBook(sCode, sTitle, sLocation, crids);
            if (fResult)
                m_isr.AddMessage(fErrorSoundsOnly ? AlertType.None : AlertType.GoodInfo, "{2}Added title for {0}: {1}", sCode, sTitle, sCheck);
            else
                m_isr.AddMessage(AlertType.BadInfo, "Couldn't create Book title for {0}: {1}", sCode, sTitle);

            return fResult;
        }

        /*----------------------------------------------------------------------------
        	%%Function: DoUpdateDvdScanDate
        	%%Qualified: UniversalUpc.UpcInvCore.DoUpdateDvdScanDate
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        private async void DoUpdateBookScanDate(
            int workId,
            string sCode,
            string sLocation,
            BookInfo bki,
            bool fCheckOnly,
            bool fErrorSoundsOnly,
            Guid crids,
            FinalScanCodeReportAndCleanupDelegate del)
        {
            string sCheck = fCheckOnly ? "[CheckOnly] " : "";
            m_lp.LogEvent(crids, EventType.Verbose, "Service returned info for {0}", sCode);

            // check for a dupe/too soon last scan (within 1 hour)
            if (bki.LastScan > DateTime.Now.AddHours(-1))
            {
                m_lp.LogEvent(crids, EventType.Verbose, "{1}Avoiding duplicate scan for {0}", sCode, sCheck);
                m_isr.AddMessage(AlertType.Duplicate, "{2}{0}: Duplicate?! LastScan was {1}", bki.Title,
                    bki.LastScan.ToString(), sCheck);
                await del(workId, sCode, crids, bki.Title, true);
                return;
            }

            m_lp.LogEvent(crids, EventType.Verbose, "Calling service to update scan date for {0}", sCode);

            // now update the last scan date
            bool fResult = fCheckOnly || await UpdateBookScan(sCode, bki.Title, sLocation, crids);

            if (fResult)
            {
                m_lp.LogEvent(crids, EventType.Verbose, "{1}Successfully updated last scan for {0}", sCode, sCheck);
                m_isr.AddMessage(fErrorSoundsOnly ? AlertType.None : AlertType.GoodInfo, "{2}{0}: Updated LastScan (was {1})", bki.Title,
                    bki.LastScan.ToString(), sCheck);
            }
            else
            {
                m_lp.LogEvent(crids, EventType.Error, "Failed to update last scan for {0}", sCode);
                m_isr.AddMessage(AlertType.BadInfo, "{0}: Failed to update last scan!", bki.Title);
            }

            await del(workId, sCode, crids, bki.Title, true);
        }

        #endregion

        #endregion
    }
}
