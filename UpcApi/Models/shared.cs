
using System;
using System.Configuration;
using TCore;
using TCore.Logging;

namespace UpcApi
{
    public class Shared
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
#if PRODDATA
                return ConfigurationManager.AppSettings["Thetasoft.Azure.ConnectionString"];
#else
                return "Server=cantorix;Database=db0902;Trusted_Connection=True;";
#endif
            }
        }

        #region All Items
        public delegate void DelegateReader<T>(SqlReader sqlr, CorrelationID crid, ref T t);
        public delegate T DelegateFromUSR<T>(USR usr);

        /*----------------------------------------------------------------------------
        	%%Function: DoGenericQueryDelegateRead
        	%%Qualified: UpcSvc.UpcSvc.DoGenericQueryDelegateRead<T>
        	%%Contact: rlittle
        	
            Do a generic query and return the result for type T
        ----------------------------------------------------------------------------*/
        public static T DoGenericQueryDelegateRead<T>(string sQuery, DelegateReader<T> delegateReader, DelegateFromUSR<T> delegateFromUsr)
        {
            LocalSqlHolder lsh = null;
            CorrelationID crid = new CorrelationID();
            SR sr = SR.Failed("unknown");

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
                        sr = sqlr.ExecuteQuery(sQuery, SqlConnectionStatic);
                        sr.CorrelationID = crid;

                        if (sr.Succeeded)
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

            {
                USR usr = USR.FromSR(sr);

                usr.Reason += $"( fExecuteQuery returned false) (static={SqlConnectionStatic})";

                return delegateFromUsr(usr);
            }
        }

        /*----------------------------------------------------------------------------
        	%%Function: ReaderLastScanDateDelegate
        	%%Qualified: UpcSvc.UpcSvc.ReaderLastScanDateDelegate
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        static void ReaderLastScanDateDelegate(SqlReader sqlr, CorrelationID crid, ref USR_String usrs)
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
        public static USR_String GetLastScanDate(string sScanCode)
        {
            string sCmd = String.Format("select ScanCode, LastScanDate from upc_Codes where ScanCode='{0}'", Sql.Sqlify(sScanCode));

            return DoGenericQueryDelegateRead<USR_String>(sCmd, ReaderLastScanDateDelegate, USR_String.FromTCSR);
        }

        public static USR UpdateUpcLastScanDate(string sScanCode, string sTitle)
        {
            string sCmd = String.Format("sp_updatescan '{0}', '{1}', '{2}'", Sql.Sqlify(sScanCode), Sql.Sqlify(sTitle), DateTime.Now.ToString());
            return DoGenericQueryDelegateRead(sCmd, null, FromUSR);
        }

        public static USR FromUSR(USR usr)
        {
            return usr;
        }

        #endregion

    }
}