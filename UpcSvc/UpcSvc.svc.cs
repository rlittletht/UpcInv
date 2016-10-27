using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Web.UI;
using HtmlAgilityPack;
using ScrapySharp.Network;
using TCore;
using TCore.Logging;

namespace UpcSvc
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "UpcSvc" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select UpcSvc.svc or UpcSvc.svc.cs at the Solution Explorer and start debugging.
    public class UpcSvc : IUpcSvc
    {
        private string _sSqlConnection = null;

        public static string SqlConnectionStatic
        {
            get
            {
#if PRODUCTION
                return ConfigurationManager.AppSettings["Thetasoft.Azure.ConnectionString"];
#else
                return "Server=cantorix;Database=db0902;Trusted_Connection=True;";
#endif
            }
        }

        public string GetSqlConnection()
        {
            if (_sSqlConnection == null)
#if PRODUCTION
                _sSqlConnection = ConfigurationManager.AppSettings["Thetasoft.Azure.ConnectionString"];
#else
                _sSqlConnection = "Server=cantorix;Database=db0902;Trusted_Connection=True;";
#endif
            return _sSqlConnection;
        }

        delegate T DelegateReader<T>(SqlReader sqlr, CorrelationID crid);
        delegate T DelegateFromUSR<T>(USR usr);

        T DoGenericQueryDelegateRead<T>(string sQuery, DelegateReader<T> delegateReader, DelegateFromUSR<T> delegateFromUsr)
        {
            LocalSqlHolder lsh = null;
            CorrelationID crid = new CorrelationID();

            try
                {
                lsh = new LocalSqlHolder(null, crid, SqlConnectionStatic);
                string sCmd = sQuery;

                if (delegateReader == null)
                    {
                    // just execute as a command
                    return delegateFromUsr(USR.FromSR(TCore.Sql.ExecuteNonQuery(lsh, sCmd, SqlConnectionStatic)));
                    }
                else
                    {
                    SqlReader sqlr = new SqlReader(lsh);
                    try
                        {
                        if (sqlr.FExecuteQuery(sCmd, SqlConnectionStatic))
                            {
                            if (!sqlr.Reader.Read())
                                return delegateFromUsr(USR.FromSR(SR.FailedCorrelate("scan code not found", crid)));

                            return delegateReader(sqlr, crid);
                            }
                        }
                    catch (Exception e)
                        {
                        sqlr.Close();
                        return delegateFromUsr(USR.FromSR(SR.FailedCorrelate(e, crid)));
                        }
                    }
                }
            catch (Exception e)
                {
                return delegateFromUsr(USR.FromSR(SR.FailedCorrelate(e, crid)));
                }
            finally
                {
                lsh?.Close();
                }
            return delegateFromUsr(USR.FromSR(SR.FailedCorrelate("unknown", crid)));
        } 

        USR_String ReaderLastScanDateDelegate(SqlReader sqlr, CorrelationID crid)
        {
            DateTime dttm = sqlr.Reader.GetDateTime(1);

            USR_String usrs = USR_String.FromTCSR(USR.SuccessCorrelate(crid));
            usrs.TheValue = dttm.ToString();

            return usrs;
        }

        public USR_String GetLastScanDate(string sScanCode)
        {
            string sCmd = String.Format("select ScanCode, LastScanDate from upc_COdes where ScanCode='{0}'", Sql.Sqlify(sScanCode));

            return DoGenericQueryDelegateRead<USR_String>(sCmd, ReaderLastScanDateDelegate, USR_String.FromTCSR);
        }

        public enum ADAS
        {
            Generic = 0,
            Book = 1,
            DVD = 2,
            Wine = 3,
        }

        private string s_sQueryDvd = "$$upc_codes$$.ScanCode, $$upc_codes$$.LastScanDate, $$upc_codes$$.FirstScanDate, $$upc_dvd$$.Title " +
                                     " FROM $$#upc_codes$$ " +
                                     " INNER JOIN $$#upc_dvd$$ " +
                                     "   ON $$upc_codes$$.ScanCode = $$upc_dvd$$.ScanCode";

        private static Dictionary<string, string> s_mpDvdAlias = new Dictionary<string, string>
                                                                     {
                                                                         {"upc_codes", "UPCC"},
                                                                         {"upc_dvd", "UPCD"}
                                                                     };

        public USR_DvdInfo ReaderGetDvdScanInfoDelegate(SqlReader sqlr, CorrelationID crid)
        {
            DvdInfo dvdi = new DvdInfo();

            dvdi.Code = sqlr.Reader.GetString(0);
            dvdi.LastScan = sqlr.Reader.GetDateTime(1);
            dvdi.FirstScan = sqlr.Reader.GetDateTime(2);
            dvdi.Title = sqlr.Reader.GetString(3);

            USR_DvdInfo usrd = USR_DvdInfo.FromTCSR(USR.SuccessCorrelate(crid));
            usrd.TheValue = dvdi;

            return usrd;
        }

        public USR_DvdInfo GetDvdScanInfo(string sScanCode)
        {
            SqlWhere sqlw = new SqlWhere();
            sqlw.AddAliases(s_mpDvdAlias);
            sqlw.Add(String.Format("$$upc_codes$$.ScanCode='{0}'", Sql.Sqlify(sScanCode)), SqlWhere.Op.And);

            string sFullQuery = String.Format("SELECT {0}", sqlw.GetWhere(s_sQueryDvd));

            return DoGenericQueryDelegateRead(sFullQuery, ReaderGetDvdScanInfoDelegate, USR_DvdInfo.FromTCSR);
        }

        USR FromUSR(USR usr)
        {
            return usr;
        }

        public string FetchTitleFromGenericUPC(string sCode)
        {
            if (sCode.Length == 13)
                sCode = sCode.Substring(1);

            try
                {
                ScrapingBrowser sbr = new ScrapingBrowser();
                sbr.AllowAutoRedirect = false;
                sbr.AllowMetaRedirect = false;

                WebPage wp = sbr.NavigateToPage(new Uri("http://www.searchupc.com/upc/" + sCode));

                HtmlNodeCollection nodes = wp.Html.SelectNodes("//table[@id='searchresultdata']");
                HtmlNodeCollection nodesTr = nodes[0].SelectNodes("tr");

                if (nodesTr == null || nodesTr.Count != 2)
                    {
                    return "!!NO TITLE FOUND";
                    }
                return nodesTr[1].ChildNodes[1].InnerText;

                }
            catch
                {
                return "!!NO TITLE FOUND";
                }
        }

        public USR UpdateUpcLastScanDate(string sScanCode, string sTitle)
        {
            string sCmd = String.Format("sp_updatescan '{0}', '{1}', '{2}'", Sql.Sqlify(sScanCode), Sql.Sqlify(sTitle), DateTime.Now.ToString());
            return DoGenericQueryDelegateRead(sCmd, null, FromUSR);
        }

        public USR CreateDvd(string sScanCode, string sTitle)
        {
            string sNow = DateTime.Now.ToString();

            string sCmd = String.Format("sp_createdvd '{0}', '{1}', '{2}', '{3}', 'D'", Sql.Sqlify(sScanCode), Sql.Sqlify(sTitle), sNow, sNow);
            return DoGenericQueryDelegateRead(sCmd, null, FromUSR);
        }

        public USR TestLog()
        {
            LogProvider lp = new LogProvider("UpcSvc");
            CorrelationID crid = new CorrelationID();

            lp.LogEvent(crid, EventType.Verbose, "TestLoggingEnter");

            USR sr = USR.FromSRCorrelate(SR.Failed("test failure"), crid);
            lp.LogEvent(crid, EventType.Critical, "Critical test");
            lp.LogEvent(crid, EventType.Warning, "Warning test");
            lp.LogEvent(crid, EventType.Information, "Information test");
            lp.LogEvent(crid, EventType.Error, "Error test");
            lp.LogEvent(crid, EventType.Verbose, "Verbose test");
            lp.LogEvent(crid, EventType.Verbose, "TestLoggingEnter2");
            

            sr.Log(lp, "sr.Log test");
            sr.Log(lp, "sr.Log My paramaterized test {0} after param", 911);
            if (lp.FShouldLog(EventType.Verbose))
                sr.Reason = "Verbose is logging";

            return sr;
        }
    }
}
