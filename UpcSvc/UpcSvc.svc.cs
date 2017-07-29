using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Web.UI;
using System.Xml;
using NUnit.Framework;
using TCore;
using TCore.Logging;

namespace UpcSvc
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "UpcSvc" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select UpcSvc.svc or UpcSvc.svc.cs at the Solution Explorer and start debugging.
    public class UpcSvc : IUpcSvc
    {
        private string _sSqlConnection = null;
        private string _sIsbnDbAccessKey = null;

        public static string IsbnDbAccessKeyStatic
        {
            get
            {
                return ConfigurationManager.AppSettings["IsbnDB.AccessKey"];
            }
        }

        public string GetIsbnDbAccessKey()
        {
            return _sIsbnDbAccessKey ?? (_sIsbnDbAccessKey = IsbnDbAccessKeyStatic);
        }

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

        #region All Items
        delegate void DelegateReader<T>(SqlReader sqlr, CorrelationID crid, ref T t);
        delegate T DelegateFromUSR<T>(USR usr);

        /*----------------------------------------------------------------------------
        	%%Function: DoGenericQueryDelegateRead
        	%%Qualified: UpcSvc.UpcSvc.DoGenericQueryDelegateRead<T>
        	%%Contact: rlittle
        	
            Do a generic query and return the result for type T
        ----------------------------------------------------------------------------*/
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
                            T t = default(T);
                            bool fOnce = false;

                            while (sqlr.Reader.Read())
                                {
                                delegateReader(sqlr, crid, ref t);
                                fOnce = true;
                                }

                            if (!fOnce)
                                return delegateFromUsr(USR.FromSR(SR.FailedCorrelate("scan code not found", crid)));

                            return t;
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

        /*----------------------------------------------------------------------------
        	%%Function: ReaderLastScanDateDelegate
        	%%Qualified: UpcSvc.UpcSvc.ReaderLastScanDateDelegate
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        void ReaderLastScanDateDelegate(SqlReader sqlr, CorrelationID crid, ref USR_String usrs)
        {
            DateTime dttm = sqlr.Reader.GetDateTime(1);

            usrs = USR_String.FromTCSR(USR.SuccessCorrelate(crid));
            usrs.TheValue = dttm.ToString();
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetLastScanDate
        	%%Qualified: UpcSvc.UpcSvc.GetLastScanDate
        	%%Contact: rlittle
        	
            get the last scan date for the given UPC code -- this doesn't care 
            what type of item it is
        ----------------------------------------------------------------------------*/
        public USR_String GetLastScanDate(string sScanCode)
        {
            string sCmd = String.Format("select ScanCode, LastScanDate from upc_Codes where ScanCode='{0}'", Sql.Sqlify(sScanCode));

            return DoGenericQueryDelegateRead<USR_String>(sCmd, ReaderLastScanDateDelegate, USR_String.FromTCSR);
        }

        public USR UpdateUpcLastScanDate(string sScanCode, string sTitle)
        {
            string sCmd = String.Format("sp_updatescan '{0}', '{1}', '{2}'", Sql.Sqlify(sScanCode), Sql.Sqlify(sTitle), DateTime.Now.ToString());
            return DoGenericQueryDelegateRead(sCmd, null, FromUSR);
        }

        #endregion

        public enum ADAS
        {
            Generic = 0,
            Book = 1,
            DVD = 2,
            Wine = 3,
        }

        #region Books
        private string s_sQueryBook = "$$upc_codes$$.ScanCode, $$upc_codes$$.LastScanDate, $$upc_codes$$.FirstScanDate, $$upc_books$$.Title, $$upc_books$$.Note " +
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
        public void ReaderGetBookScanInfoDelegate(SqlReader sqlr, CorrelationID crid, ref USR_BookInfo usrb)
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
        public void ReaderGetBookScanInfoListDelegate(SqlReader sqlr, CorrelationID crid, ref USR_BookInfoList usrbs)
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
        public USR_BookInfo GetBookScanInfo(string sScanCode)
        {
            SqlWhere sqlw = new SqlWhere();
            sqlw.AddAliases(s_mpBookAlias);
            sqlw.Add(String.Format("$$upc_codes$$.ScanCode='{0}'", Sql.Sqlify(sScanCode)), SqlWhere.Op.And);

            string sFullQuery = String.Format("SELECT {0}", sqlw.GetWhere(s_sQueryBook));

            return DoGenericQueryDelegateRead(sFullQuery, ReaderGetBookScanInfoDelegate, USR_BookInfo.FromTCSR);
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetDvdScanInfosFromTitle
        	%%Qualified: UpcSvc.UpcSvc.GetDvdScanInfosFromTitle
        	%%Contact: rlittle
        	
            Get the list of matching dvdInfo items for the given title substring
        ----------------------------------------------------------------------------*/
        public USR_BookInfoList GetBookScanInfosFromTitle(string sTitleSubstring)
        {
            SqlWhere sqlw = new SqlWhere();
            sqlw.AddAliases(s_mpDvdAlias);
            sqlw.Add(String.Format("$$upc_book$$.Title like '%{0}%'", Sql.Sqlify(sTitleSubstring)), SqlWhere.Op.And);

            string sFullQuery = String.Format("SELECT {0}", sqlw.GetWhere(s_sQueryBook));

            return DoGenericQueryDelegateRead(sFullQuery, ReaderGetBookScanInfoListDelegate, USR_BookInfoList.FromTCSR);
        }

        public USR CreateBook(string sScanCode, string sTitle, string sLocation)
        {
            string sNow = DateTime.Now.ToString();

            string sCmd = String.Format("sp_createbook '{0}', '{1}', '{2}', '{3}', '{4}'", Sql.Sqlify(sScanCode), Sql.Sqlify(sTitle), sNow, sNow, Sql.Sqlify(sLocation));
            return DoGenericQueryDelegateRead(sCmd, null, FromUSR);
        }


        public USR UpdateBookScan(string sScanCode, string sTitle, string sLocation)
        {
            string sCmd = String.Format("sp_updatebookscanlocation '{0}', '{1}', '{2}', '{3}'", Sql.Sqlify(sScanCode), Sql.Sqlify(sTitle), DateTime.Now.ToString(), Sql.Sqlify(sLocation));
            return DoGenericQueryDelegateRead(sCmd, null, FromUSR);
        }

        #endregion

        #region DVD

        private string s_sQueryDvd = "$$upc_codes$$.ScanCode, $$upc_codes$$.LastScanDate, $$upc_codes$$.FirstScanDate, $$upc_dvd$$.Title " +
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
        public void ReaderGetDvdScanInfoDelegate(SqlReader sqlr, CorrelationID crid, ref USR_DvdInfo usrd)
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
        public void ReaderGetDvdScanInfoListDelegate(SqlReader sqlr, CorrelationID crid, ref USR_DvdInfoList usrds)
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
        public USR_DvdInfo GetDvdScanInfo(string sScanCode)
        {
            SqlWhere sqlw = new SqlWhere();
            sqlw.AddAliases(s_mpDvdAlias);
            sqlw.Add(String.Format("$$upc_codes$$.ScanCode='{0}'", Sql.Sqlify(sScanCode)), SqlWhere.Op.And);

            string sFullQuery = String.Format("SELECT {0}", sqlw.GetWhere(s_sQueryDvd));

            return DoGenericQueryDelegateRead(sFullQuery, ReaderGetDvdScanInfoDelegate, USR_DvdInfo.FromTCSR);
        }

        /*----------------------------------------------------------------------------
        	%%Function: GetDvdScanInfosFromTitle
        	%%Qualified: UpcSvc.UpcSvc.GetDvdScanInfosFromTitle
        	%%Contact: rlittle
        	
            Get the list of matching dvdInfo items for the given title substring
        ----------------------------------------------------------------------------*/
        public USR_DvdInfoList GetDvdScanInfosFromTitle(string sTitleSubstring)
        {
            SqlWhere sqlw = new SqlWhere();
            sqlw.AddAliases(s_mpDvdAlias);
            sqlw.Add(String.Format("$$upc_dvd$$.Title like '%{0}%'", Sql.Sqlify(sTitleSubstring)), SqlWhere.Op.And);

            string sFullQuery = String.Format("SELECT {0}", sqlw.GetWhere(s_sQueryDvd));

            return DoGenericQueryDelegateRead(sFullQuery, ReaderGetDvdScanInfoListDelegate, USR_DvdInfoList.FromTCSR);
        }

        public USR CreateDvd(string sScanCode, string sTitle)
        {
            string sNow = DateTime.Now.ToString();

            string sCmd = String.Format("sp_createdvd '{0}', '{1}', '{2}', '{3}', 'D'", Sql.Sqlify(sScanCode), Sql.Sqlify(sTitle), sNow, sNow);
            return DoGenericQueryDelegateRead(sCmd, null, FromUSR);
        }
        #endregion

        #region Wine
        private string s_sQueryWine = "$$upc_codes$$.ScanCode, $$upc_codes$$.LastScanDate, $$upc_codes$$.FirstScanDate, $$upc_wines$$.Wine, $$upc_wines$$.Vintage, $$upc_wines$$.Notes " +
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
        public void ReaderGetWineScanInfoDelegate(SqlReader sqlr, CorrelationID crid, ref USR_WineInfo usrw)
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
        public USR_WineInfo GetWineScanInfo(string sScanCode)
        {
            SqlWhere sqlw = new SqlWhere();
            sqlw.AddAliases(s_mpWineAlias);
            sqlw.Add(String.Format("$$upc_codes$$.ScanCode='{0}'", Sql.Sqlify(sScanCode)), SqlWhere.Op.And);

            string sFullQuery = String.Format("SELECT {0}", sqlw.GetWhere(s_sQueryWine));

            return DoGenericQueryDelegateRead(sFullQuery, ReaderGetWineScanInfoDelegate, USR_WineInfo.FromTCSR);
        }

        public USR DrinkWine(string sScanCode, string sWine, string sVintage, string sNotes)
        {
            string sNow = DateTime.Now.ToString();

            string sCmd = String.Format("sp_drinkwine '{0}', '{1}', '{2}', '{3}', '{4}'", Sql.Sqlify(sScanCode), Sql.Sqlify(sWine), Sql.Sqlify(sVintage), Sql.Sqlify(sNotes), sNow);
            return DoGenericQueryDelegateRead(sCmd, null, FromUSR);
        }
        #endregion

        USR FromUSR(USR usr)
        {
            return usr;
        }

        #region UPC Lookup

        public string FetchTitleFromGenericUPC(string sCode)
        {
            return TCore.Scrappy.GenericUPC.FetchTitleFromUPC(sCode);
        }
#if no
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
#endif
        const string sRequestTemplate = "http://isbndb.com/api/books.xml?access_key={0}&index1=isbn&value1={1}";

        public string FetchTitleFromISBN13(string sCode)
        {
            string sTitle = "!!NO TITLE FOUND";
            string sReq = String.Format(sRequestTemplate, GetIsbnDbAccessKey(), sCode);

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(sReq);
            if (req != null)
                {
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                if (resp != null)
                    {
                    Stream stm = resp.GetResponseStream();
                    if (stm != null)
                        {
                        System.Xml.XmlDocument dom = new System.Xml.XmlDocument();

                        try
                            {
                            dom.Load(stm);

                            XmlNode node = dom.SelectSingleNode("/ISBNdb/BookList/BookData/Title");
                            if (node == null)
                                {
                                // try again scraping from bn.com...this is notoriously fragile, so its our last resort.
                                sTitle = "!!NO TITLE FOUND" +sReq + dom.InnerXml; // SScrapeISBN(sIsbn);
                                }
                            else
                                {
                                sTitle = node.InnerText;
                                }
                            }
                        catch (Exception exc)
                            {
                            sTitle = "!!NO TITLE FOUND: (" + exc.Message + ")";
                            }
                        }
                    }
                }

            return sTitle;
            
        }
#endregion


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
