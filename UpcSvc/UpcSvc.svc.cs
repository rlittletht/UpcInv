using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
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
        delegate T DelegateFromTCSR<T>(USR usr);

        T DoGenericQueryDelegateRead<T>(string sQuery, DelegateReader<T> delegateReader, DelegateFromTCSR<T> delegateFromTcsr)
        {
            LocalSqlHolder lsh = null;
            CorrelationID crid = new CorrelationID();

            try
                {
                lsh = new LocalSqlHolder(null, crid, SqlConnectionStatic);
                string sCmd = sQuery;

                SqlReader sqlr = new SqlReader(lsh);
                try
                    {
                    if (sqlr.FExecuteQuery(sCmd, SqlConnectionStatic))
                        {
                        if (!sqlr.Reader.Read())
                            return delegateFromTcsr(USR.FromSR(SR.FailedCorrelate("scan code not found", crid)));

                        return delegateReader(sqlr, crid);
                        }
                    }
                catch (Exception e)
                    {
                    sqlr.Close();
                    return delegateFromTcsr(USR.FromSR(SR.FailedCorrelate(e, crid)));
                    }
                }
            catch (Exception e)
                {
                return delegateFromTcsr(USR.FromSR(SR.FailedCorrelate(e, crid)));
                }
            finally
                {
                lsh?.Close();
                }
            return delegateFromTcsr(USR.FromSR(SR.FailedCorrelate("unknown", crid)));
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
            dvdi.FirstScan = sqlr.Reader.GetDateTime(1);
            dvdi.LastScan = sqlr.Reader.GetDateTime(2);
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
    }
}
