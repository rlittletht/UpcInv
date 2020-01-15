
using System;
using System.Collections.Generic;
using TCore;
using TCore.Logging;
using UpcShared;

namespace UpcApi
{
    public class UpcDvd
    {

        private static string s_sQueryDvd = 
            "$$upc_codes$$.ScanCode, "
            + "$$upc_codes$$.LastScanDate, "
            + "$$upc_codes$$.FirstScanDate, "
            + "$$upc_dvd$$.Title "
            + "FROM $$#upc_codes$$ "
            + " INNER JOIN $$#upc_dvd$$ " 
            + "   ON $$upc_codes$$.ScanCode = $$upc_dvd$$.ScanCode";

        private static string s_sQueryDvdEx =
            "$$upc_codes$$.ScanCode, "
            + "$$upc_codes$$.LastScanDate, "
            + "$$upc_codes$$.FirstScanDate, "
            + "$$upc_dvd$$.Title, "
            + "$$upc_dvd$$.Summary, "
            + "$$upc_dvd$$.Classification, "
            + "$$upc_dvd$$.MediaType, "
            + "$$upc_dvd$$.CoverSrc "
            + "FROM $$#upc_codes$$ "
            + " INNER JOIN $$#upc_dvd$$ "
            + "   ON $$upc_codes$$.ScanCode = $$upc_dvd$$.ScanCode";

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
        	%%Function: ReaderGetDvdScanInfoExDelegate
        	%%Qualified: UpcSvc.UpcSvc.ReaderGetDvdScanInfoExDelegate
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        public static void ReaderGetDvdScanInfoExDelegate(SqlReader sqlr, CorrelationID crid, ref USR_DvdInfoEx usrd)
        {
            USR_DvdInfo dvdInfo = null;
            ReaderGetDvdScanInfoDelegate(sqlr, crid, ref dvdInfo);

            DvdInfoEx dvdix = DvdInfoEx.FromDvdInfo(dvdInfo.TheValue);

            dvdix.Summary = sqlr.Reader.IsDBNull(4) ? null : sqlr.Reader.GetString(4);
            dvdix.Classification = sqlr.Reader.IsDBNull(5) ? null : sqlr.Reader.GetString(5);
            dvdix.MediaType = sqlr.Reader.IsDBNull(6) ? null : sqlr.Reader.GetString(6);
            dvdix.CoverSrc = sqlr.Reader.IsDBNull(7) ? null : sqlr.Reader.GetString(7);

            usrd = USR_DvdInfoEx.FromTCSR(USR.SuccessCorrelate(crid));
            usrd.TheValue = dvdix;
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
            %%Function: GetFullDvdScanInfo
            %%Qualified: UpcSvc.UpcSvc.GetFullDvdScanInfo
    
            Get the information for the DVD for the given scancode
        ----------------------------------------------------------------------------*/
        public static USR_DvdInfoEx GetFullDvdScanInfo(string sScanCode)
        {
            SqlWhere sqlw = new SqlWhere();
            sqlw.AddAliases(s_mpDvdAlias);
            sqlw.Add(String.Format("$$upc_codes$$.ScanCode='{0}'", Sql.Sqlify(sScanCode)), SqlWhere.Op.And);

            string sFullQuery = String.Format("SELECT {0}", sqlw.GetWhere(s_sQueryDvdEx));

            return Shared.DoGenericQueryDelegateRead(sFullQuery, ReaderGetDvdScanInfoExDelegate, USR_DvdInfoEx.FromTCSR);
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

        public static USR_DvdInfoList QueryDvdScanInfos(
            string sTitleSubstring,
            string sSummarySubstring,
            DateTime? dttmSince)
        {
            SqlWhere sqlw = new SqlWhere();
            sqlw.AddAliases(s_mpDvdAlias);
            if (!string.IsNullOrEmpty(sTitleSubstring))
                sqlw.Add(String.Format("$$upc_dvd$$.Title like '%{0}%'", Sql.Sqlify(sTitleSubstring)), SqlWhere.Op.And);
            if (!string.IsNullOrEmpty(sSummarySubstring))
                sqlw.Add(String.Format("$$upc_dvd$$.Summary like '%{0}%'", Sql.Sqlify(sSummarySubstring)), SqlWhere.Op.And);
            if (dttmSince != null)
                sqlw.Add(String.Format("$$upc_codes$$.FirstScanDate > '{0}'", UpcBook.ToSqlDateTime(dttmSince.Value)), SqlWhere.Op.And);

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