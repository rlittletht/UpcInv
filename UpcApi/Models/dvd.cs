
using System;
using System.Collections.Generic;
using TCore;
using TCore.Logging;

namespace UpcApi
{
    public class UpcDvd
    {

        private static string s_sQueryDvd = "$$upc_codes$$.ScanCode, $$upc_codes$$.LastScanDate, $$upc_codes$$.FirstScanDate, $$upc_dvd$$.Title " +
                                     " FROM $$#upc_codes$$ " +
                                     " INNER JOIN $$#upc_dvd$$ " +
                                     "   ON $$upc_codes$$.ScanCode = $$upc_dvd$$.ScanCode";

        private static Dictionary<string, string> s_mpDvdAlias = new Dictionary<string, string>
                                                                     {
                                                                         {"upc_codes", "UPCC"},
                                                                         {"upc_dvd", "UPCD"}
                                                                     };

        /*----------------------------------------------------------------------------
        	%%Function: ReaderGetDvdScanInfoDelegate
        	%%Qualified: UpcSvc.UpcSvc.ReaderGetDvdScanInfoDelegate
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        public static void ReaderGetDvdScanInfoDelegate(SqlReader sqlr, CorrelationID crid, ref USR_DvdInfo usrd)
        {
            DvdInfo dvdi = new DvdInfo();

            dvdi.Code = sqlr.Reader.GetString(0);
            dvdi.LastScan = sqlr.Reader.GetDateTime(1);
            dvdi.FirstScan = sqlr.Reader.GetDateTime(2);
            dvdi.Title = sqlr.Reader.GetString(3);

            usrd = USR_DvdInfo.FromTCSR(USR.SuccessCorrelate(crid));
            usrd.TheValue = dvdi;
        }

        /*----------------------------------------------------------------------------
        	%%Function: ReaderGetDvdScanInfoListDelegate
        	%%Qualified: UpcSvc.UpcSvc.ReaderGetDvdScanInfoListDelegate
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        public static void ReaderGetDvdScanInfoListDelegate(SqlReader sqlr, CorrelationID crid, ref USR_DvdInfoList usrds)
        {
            DvdInfo dvdi = new DvdInfo();

            dvdi.Code = sqlr.Reader.GetString(0);
            dvdi.LastScan = sqlr.Reader.GetDateTime(1);
            dvdi.FirstScan = sqlr.Reader.GetDateTime(2);
            dvdi.Title = sqlr.Reader.GetString(3);

            if (usrds == null)
            {
                usrds = USR_DvdInfoList.FromTCSR(USR.SuccessCorrelate(crid));
                usrds.TheValue = new List<DvdInfo>();
            }
            usrds.TheValue.Add(dvdi);
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetDvdScanInfo
        	%%Qualified: UpcSvc.UpcSvc.GetDvdScanInfo
        	%%Contact: rlittle
        	
            Get the information for the DVD for the given scancode
        ----------------------------------------------------------------------------*/
        public static USR_DvdInfo GetDvdScanInfo(string sScanCode)
        {
            SqlWhere sqlw = new SqlWhere();
            sqlw.AddAliases(s_mpDvdAlias);
            sqlw.Add(String.Format("$$upc_codes$$.ScanCode='{0}'", Sql.Sqlify(sScanCode)), SqlWhere.Op.And);

            string sFullQuery = String.Format("SELECT {0}", sqlw.GetWhere(s_sQueryDvd));

            return Shared.DoGenericQueryDelegateRead(sFullQuery, ReaderGetDvdScanInfoDelegate, USR_DvdInfo.FromTCSR);
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetDvdScanInfosFromTitle
        	%%Qualified: UpcSvc.UpcSvc.GetDvdScanInfosFromTitle
        	%%Contact: rlittle
        	
            Get the list of matching dvdInfo items for the given title substring
        ----------------------------------------------------------------------------*/
        public static USR_DvdInfoList GetDvdScanInfosFromTitle(string sTitleSubstring)
        {
            SqlWhere sqlw = new SqlWhere();
            sqlw.AddAliases(s_mpDvdAlias);
            sqlw.Add(String.Format("$$upc_dvd$$.Title like '%{0}%'", Sql.Sqlify(sTitleSubstring)), SqlWhere.Op.And);

            string sFullQuery = String.Format("SELECT {0}", sqlw.GetWhere(s_sQueryDvd));

            return Shared.DoGenericQueryDelegateRead(sFullQuery, ReaderGetDvdScanInfoListDelegate, USR_DvdInfoList.FromTCSR);
        }

        public static USR CreateDvd(string sScanCode, string sTitle)
        {
            string sNow = DateTime.Now.ToString();

            string sCmd = String.Format("sp_createdvd '{0}', '{1}', '{2}', '{3}', 'D'", Sql.Sqlify(sScanCode), Sql.Sqlify(sTitle), sNow, sNow);
            return Shared.DoGenericQueryDelegateRead(sCmd, null, Shared.FromUSR);
        }
    }
}