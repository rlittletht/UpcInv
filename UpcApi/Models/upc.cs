using System;
using TCore;
using TCore.Scrappy.BarnesAndNoble;
using UpcShared;

namespace UpcApi
{
    public class UpcUpc
    {
        /*----------------------------------------------------------------------------
        	%%Function: GetLastScanDate
        	%%Qualified: UpcSvc.UpcSvc.GetLastScanDate
        	%%Contact: rlittle
        	
            get the last scan date for the given UPC code -- this doesn't care 
            what type of item it is
        ----------------------------------------------------------------------------*/
        public static USR_String GetLastScanDate(string sScanCode)
        {
            string sCmd = String.Format("select ScanCode, LastScanDate from upc_Codes where ScanCode='{0}'", Sql.Sqlify(sScanCode));

            return Shared.DoGenericQueryDelegateRead<USR_String>(sCmd, Shared.ReaderLastScanDateDelegate, USR_String.FromTCSR);
        }

        public static USR UpdateUpcLastScanDate(string sScanCode, string sTitle)
        {
            if (String.IsNullOrEmpty(sTitle))
                return USR.Failed("title cannot be null or empty!");

            string sCmd = String.Format("sp_updatescan '{0}', '{1}', '{2}'", Sql.Sqlify(sScanCode), Sql.Sqlify(sTitle), DateTime.Now.ToString());
            return Shared.DoGenericQueryDelegateRead(sCmd, null, Shared.FromUSR);
        }


        public static string FetchTitleFromGenericUPC(string sCode)
        {
            return TCore.Scrappy.GenericUPC.FetchTitleFromUPC(sCode);
        }

        public static string FetchTitleFromISBN13(string sCode)
        {
            string sTitle = TCore.Scrappy.GenericISBN.FetchTitleFromISBN13(sCode, Shared.GetIsbnDbAccessKey());

            if (!sTitle.StartsWith("!!NO TITLE FOUND"))
                return sTitle;

            // fallback to BN
            Book.BookElement book = new Book.BookElement(sCode);
            string sError;

            if (!Book.FScrapeBook(book, out sError))
                return "!!NO TITLE FOUND: " + sError.Substring(0, 50);

            return book.Title;
        }
    }
}