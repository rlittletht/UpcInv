
using System;
using System.Collections.Generic;
using System.Web.Http;
using TCore;
using TCore.Logging;
using UpcShared;

namespace UpcApi
{
    public class UpcWine
    {
        private static string s_sQueryWine = "$$upc_codes$$.ScanCode, $$upc_codes$$.LastScanDate, $$upc_codes$$.FirstScanDate, $$upc_wines$$.Wine, $$upc_wines$$.Vintage, $$upc_wines$$.Notes " +
                                     " FROM $$#upc_codes$$ " +
                                     " INNER JOIN $$#upc_wines$$ " +
                                     "   ON $$upc_codes$$.ScanCode = $$upc_wines$$.ScanCode";

        private static Dictionary<string, string> s_mpWineAlias = new Dictionary<string, string>
                                                                     {
                                                                         {"upc_codes", "UPCC"},
                                                                         {"upc_wines", "UPCW"}
                                                                     };

        /*----------------------------------------------------------------------------
        	%%Function: ReaderGetBookScanInfoDelegate
        	%%Qualified: UpcSvc.UpcSvc.ReaderGetBookScanInfoDelegate
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        public static void ReaderGetWineScanInfoDelegate(SqlReader sqlr, CorrelationID crid, ref USR_WineInfo usrw)
        {
            WineInfo wni = new WineInfo();

            wni.Code = sqlr.Reader.GetString(0);
            wni.LastScan = sqlr.Reader.GetDateTime(1);
            wni.FirstScan = sqlr.Reader.GetDateTime(2);
            wni.Wine = sqlr.Reader.GetString(3);
            wni.Vintage = sqlr.Reader.GetString(4);
            wni.Notes = sqlr.Reader.IsDBNull(5) ? null : sqlr.Reader.GetString(5);

            usrw = USR_WineInfo.FromTCSR(USR.SuccessCorrelate(crid));
            usrw.TheValue = wni;
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetDvdScanInfo
        	%%Qualified: UpcSvc.UpcSvc.GetDvdScanInfo
        	%%Contact: rlittle
        	
            Get the information for the DVD for the given scancode
        ----------------------------------------------------------------------------*/
        public static USR_WineInfo GetWineScanInfo(string sScanCode)
        {
            SqlWhere sqlw = new SqlWhere();
            sqlw.AddAliases(s_mpWineAlias);
            sqlw.Add(String.Format("$$upc_codes$$.ScanCode='{0}'", Sql.Sqlify(sScanCode)), SqlWhere.Op.And);

            string sFullQuery = String.Format("SELECT {0}", sqlw.GetWhere(s_sQueryWine));

            return Shared.DoGenericQueryDelegateRead(sFullQuery, ReaderGetWineScanInfoDelegate, USR_WineInfo.FromTCSR);
        }

        public static USR DrinkWine(string sScanCode, string sWine, string sVintage, string sNotes)
        {
            string sNow = DateTime.Now.ToString();

            string sCmd = String.Format("sp_drinkwine '{0}', '{1}', '{2}', '{3}', '{4}'", Sql.Sqlify(sScanCode), Sql.Sqlify(sWine), Sql.Sqlify(sVintage), Sql.Sqlify(sNotes), sNow);
            return Shared.DoGenericQueryDelegateRead(sCmd, null, Shared.FromUSR);
        }
    }
}