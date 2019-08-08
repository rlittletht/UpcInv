
using System;
using System.Collections.Generic;
using TCore;
using TCore.Logging;
using UpcShared;

namespace UpcApi
{
    public class UpcBook
    {
        private static string s_sQueryBook =
            "$$upc_codes$$.ScanCode, $$upc_codes$$.LastScanDate, $$upc_codes$$.FirstScanDate, $$upc_books$$.Title, $$upc_books$$.Note " +
            " FROM $$#upc_codes$$ " +
            " INNER JOIN $$#upc_books$$ " +
            "   ON $$upc_codes$$.ScanCode = $$upc_books$$.ScanCode";

        private static Dictionary<string, string> s_mpBookAlias = new Dictionary<string, string>
        {
            {"upc_codes", "UPCC"},
            {"upc_books", "UPCB"}
        };

        /*----------------------------------------------------------------------------
            %%Function: ReaderGetBookScanInfoDelegate
            %%Qualified: UpcSvc.UpcSvc.ReaderGetBookScanInfoDelegate
            %%Contact: rlittle
    
        ----------------------------------------------------------------------------*/
        public static void ReaderGetBookScanInfoDelegate(SqlReader sqlr, CorrelationID crid, ref USR_BookInfo usrb)
        {
            BookInfo bki = new BookInfo();

            bki.Code = sqlr.Reader.GetString(0);
            bki.LastScan = sqlr.Reader.GetDateTime(1);
            bki.FirstScan = sqlr.Reader.GetDateTime(2);
            bki.Title = sqlr.Reader.GetString(3);
            bki.Location = sqlr.Reader.GetString(4);

            usrb = USR_BookInfo.FromTCSR(USR.SuccessCorrelate(crid));
            usrb.TheValue = bki;
        }

        /*----------------------------------------------------------------------------
            %%Function: ReaderGetDvdScanInfoListDelegate
            %%Qualified: UpcSvc.UpcSvc.ReaderGetDvdScanInfoListDelegate
            %%Contact: rlittle
    
        ----------------------------------------------------------------------------*/
        public static void ReaderGetBookScanInfoListDelegate(SqlReader sqlr, CorrelationID crid, ref USR_BookInfoList usrbs)
        {
            BookInfo bki = new BookInfo();

            bki.Code = sqlr.Reader.GetString(0);
            bki.LastScan = sqlr.Reader.GetDateTime(1);
            bki.FirstScan = sqlr.Reader.GetDateTime(2);
            bki.Title = sqlr.Reader.GetString(3);

            if (usrbs == null)
            {
                usrbs = USR_BookInfoList.FromTCSR(USR.SuccessCorrelate(crid));
                usrbs.TheValue = new List<BookInfo>();
            }

            usrbs.TheValue.Add(bki);
        }

        /*----------------------------------------------------------------------------
            %%Function: GetBookScanInfo
            %%Qualified: UpcSvc.UpcSvc.GetBookScanInfo
            %%Contact: rlittle
    
            Get the information for the DVD for the given scancode
        ----------------------------------------------------------------------------*/
        public static USR_BookInfo GetBookScanInfo(string sScanCode)
        {
#if no
            WebOperationContext.Current.OutgoingResponse.Headers
                .Add("Access-Control-Allow-Origin", "*");
            //identify preflight request and add extra headers
            if (WebOperationContext.Current.IncomingRequest.Method == "OPTIONS")
            {
                WebOperationContext.Current.OutgoingResponse.Headers
                    .Add("Access-Control-Allow-Methods", "POST, OPTIONS, GET");
                WebOperationContext.Current.OutgoingResponse.Headers
                    .Add("Access-Control-Allow-Headers",
                        "Content-Type, Accept, Authorization, x-requested-with");
                return null;
            }
#endif
            SqlWhere sqlw = new SqlWhere();
            sqlw.AddAliases(s_mpBookAlias);
            sqlw.Add(String.Format("$$upc_codes$$.ScanCode='{0}'", Sql.Sqlify(sScanCode)), SqlWhere.Op.And);

            string sFullQuery = String.Format("SELECT {0}", sqlw.GetWhere(s_sQueryBook));

            return Shared.DoGenericQueryDelegateRead(sFullQuery, ReaderGetBookScanInfoDelegate, USR_BookInfo.FromTCSR);
        }

        /*----------------------------------------------------------------------------
            %%Function: GetDvdScanInfosFromTitle
            %%Qualified: UpcSvc.UpcSvc.GetDvdScanInfosFromTitle
            %%Contact: rlittle
    
            Get the list of matching dvdInfo items for the given title substring
        ----------------------------------------------------------------------------*/
        public static USR_BookInfoList GetBookScanInfosFromTitle(string sTitleSubstring)
        {
            SqlWhere sqlw = new SqlWhere();
            sqlw.AddAliases(s_mpBookAlias);
            sqlw.Add(String.Format("$$upc_books$$.Title like '%{0}%'", Sql.Sqlify(sTitleSubstring)), SqlWhere.Op.And);

            string sFullQuery = String.Format("SELECT {0}", sqlw.GetWhere(s_sQueryBook));

            return Shared.DoGenericQueryDelegateRead(sFullQuery, ReaderGetBookScanInfoListDelegate, USR_BookInfoList.FromTCSR);
        }

        public static USR CreateBook(string sScanCode, string sTitle, string sLocation)
        {
            string sNow = DateTime.Now.ToString();

            string sCmd = String.Format("sp_createbook '{0}', '{1}', '{2}', '{3}', '{4}'", Sql.Sqlify(sScanCode),
                Sql.Sqlify(sTitle), sNow, sNow, Sql.Sqlify(sLocation));
            return Shared.DoGenericQueryDelegateRead(sCmd, null, Shared.FromUSR);
        }

        public static USR UpdateBookScan(string sScanCode, string sTitle, string sLocation)
        {
            string sCmd = String.Format("sp_updatebookscanlocation '{0}', '{1}', '{2}', '{3}'", Sql.Sqlify(sScanCode),
                Sql.Sqlify(sTitle), DateTime.Now.ToString(), Sql.Sqlify(sLocation));
            return Shared.DoGenericQueryDelegateRead(sCmd, null, Shared.FromUSR);
        }
    }
}