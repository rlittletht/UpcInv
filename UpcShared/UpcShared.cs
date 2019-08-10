

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UpcShared
{
    public enum ADAS
    {
        Generic = 0,
        Book = 1,
        DVD = 2,
        Wine = 3,
    }
    public class TCSRBase
    {
        private string m_sReason;
        private bool m_fResult;
        private Guid m_crids;

        public bool Result
        {
            get { return m_fResult; }
            set { m_fResult = value; }
        }

        public string Reason
        {
            get { return m_sReason; }
            set { m_sReason = value; }
        }

        public Guid CorrelationID
        {
            get { return m_crids; }
            set { m_crids = value; }
        }

        public bool Succeeded
        {
            get { return m_fResult; }
        }

        #if no
        public void Log(LogProvider lp, string s, params object[] rgo)
        {
            if (lp != null)
            {
                if (!Result)
                {
                    string sT = String.Format("{0} FAILED: {1}", s, Reason);
                    lp.LogEvent(TCore.Logging.CorrelationID.FromCrids(CorrelationID), EventType.Error, sT, rgo);
                }
                else
                    lp.LogEvent(TCore.Logging.CorrelationID.FromCrids(CorrelationID), EventType.Information, s, rgo);
            }
        }
#endif
    }

    public class USR : TCSRBase
    {
        public static USR FromSR(TCore.SR sr)
        {
            USR usr = new USR();

            usr.Result = sr.Result;
            usr.Reason = sr.Reason;
            usr.CorrelationID = sr.CorrelationID;

            return usr;
        }

        public static USR FromSRCorrelate(TCore.SR sr, Guid crids)
        {
            USR usr = new USR();

            usr.Result = sr.Result;
            usr.Reason = sr.Reason;
            usr.CorrelationID = crids;

            return usr;
        }

        public static USR Success()
        {
            USR sr = new USR();
            sr.Result = true;
            sr.Reason = null;
            sr.CorrelationID = Guid.Empty;

            return sr;
        }

        public static USR SuccessCorrelate(Guid crids)
        {
            USR sr = new USR();
            sr.Result = true;
            sr.Reason = null;
            sr.CorrelationID = crids;

            return sr;
        }

        public static USR Failed(Exception e)
        {
            USR sr = new USR();
            sr.Result = false;
            sr.Reason = e.Message;
            sr.CorrelationID = Guid.Empty;
            return sr;
        }

        public static USR Failed(string sReason)
        {
            USR sr = new USR();
            sr.Result = false;
            sr.Reason = sReason;
            sr.CorrelationID = Guid.Empty;

            return sr;
        }

        public static USR FailedCorrelate(Exception e, Guid crids)
        {
            USR sr = new USR();
            sr.Result = false;
            sr.Reason = e.Message;
            sr.CorrelationID = crids;
            return sr;
        }

        public static USR FailedCorrelate(string sReason, Guid crids)
        {
            USR sr = new USR();
            sr.Result = false;
            sr.Reason = sReason;
            sr.CorrelationID = crids;

            return sr;
        }

    }

    public class TUSR<T> : TCSRBase
    {
        private T m_t;

        public T TheValue
        {
            get { return m_t; }
            set { m_t = value; }
        }

        public static USR ToTcsr(TUSR<T> sr)
        {
            USR usr = new USR();

            usr.Reason = sr.Reason;
            usr.Result = sr.Result;
            usr.CorrelationID = sr.CorrelationID;

            return usr;
        }

        public static TUSR<T> FromSR(TCore.SR sr)
        {
            TUSR<T> vsr = new TUSR<T>();

            vsr.Result = sr.Result;
            vsr.Reason = sr.Reason;
            vsr.CorrelationID = sr.CorrelationID;

            return vsr;
        }

        public static TUSR<T> Success()
        {
            TUSR<T> sr = new TUSR<T>();
            sr.Result = true;
            sr.Reason = null;
            sr.CorrelationID = Guid.Empty;

            return sr;
        }

        public static TUSR<T> Failed(Exception e)
        {
            TUSR<T> sr = new TUSR<T>();
            sr.Result = false;
            sr.Reason = e.Message;
            sr.CorrelationID = Guid.Empty;

            return sr;
        }

        public static TUSR<T> Failed(string sReason)
        {
            TUSR<T> sr = new TUSR<T>();
            sr.Result = false;
            sr.Reason = sReason;
            sr.CorrelationID = Guid.Empty;
            return sr;
        }

        public static TUSR<T> SuccessCorrelate(Guid crids)
        {
            TUSR<T> sr = new TUSR<T>();
            sr.Result = true;
            sr.Reason = null;
            sr.CorrelationID = crids;

            return sr;
        }

        public static TUSR<T> FailedCorrelate(Exception e, Guid crids)
        {
            TUSR<T> sr = new TUSR<T>();
            sr.Result = false;
            sr.Reason = e.Message;
            sr.CorrelationID = crids;

            return sr;
        }

        public static TUSR<T> FailedCorrelate(string sReason, Guid crids)
        {
            TUSR<T> sr = new TUSR<T>();
            sr.Result = false;
            sr.Reason = sReason;
            sr.CorrelationID = crids;
            return sr;
        }
    }

    public class USR_String : TUSR<String>
    {
        public static USR_String FromTCSR(USR usr)
        {
            USR_String usrs = new USR_String();
            usrs.Reason = usr.Reason;
            usrs.Result = usr.Result;
            usrs.CorrelationID = usr.CorrelationID;

            return usrs;
        }
    }

    public class DvdInfo
    {
        public string Code { get; set; }
        public string Title { get; set; }
        public DateTime FirstScan { get; set; }
        public DateTime LastScan { get; set; }
    }

    public class USR_DvdInfo : TUSR<DvdInfo>
    {
        public static USR_DvdInfo FromTCSR(USR usr)
        {
            USR_DvdInfo usrs = new USR_DvdInfo();
            usrs.Reason = usr.Reason;
            usrs.Result = usr.Result;
            usrs.CorrelationID = usr.CorrelationID;

            return usrs;
        }
    }

    public class USR_DvdInfoList : TUSR<List<DvdInfo>>
    {
        public static USR_DvdInfoList FromTCSR(USR usr)
        {
            USR_DvdInfoList usrs = new USR_DvdInfoList();
            usrs.Reason = usr.Reason;
            usrs.Result = usr.Result;
            usrs.CorrelationID = usr.CorrelationID;

            return usrs;
        }
    }

    public class BookInfo
    {
        public string Code { get; set; }
        public string Title { get; set; }
        public string Location { get; set; }
        public DateTime FirstScan { get; set; }
        public DateTime LastScan { get; set; }
    }

    public class USR_BookInfo : TUSR<BookInfo>
    {
        public static USR_BookInfo FromTCSR(USR usr)
        {
            USR_BookInfo usrs = new USR_BookInfo();
            usrs.Reason = usr.Reason;
            usrs.Result = usr.Result;
            usrs.CorrelationID = usr.CorrelationID;

            return usrs;
        }
    }

    public class USR_BookInfoList : TUSR<List<BookInfo>>
    {
        public static USR_BookInfoList FromTCSR(USR usr)
        {
            USR_BookInfoList usrs = new USR_BookInfoList();
            usrs.Reason = usr.Reason;
            usrs.Result = usr.Result;
            usrs.CorrelationID = usr.CorrelationID;

            return usrs;
        }
    }

    public class WineInfo
    {
        public string Code { get; set; }
        public string Wine { get; set; }
        public string Notes { get; set; }
        public string Vintage { get; set; }
        public DateTime FirstScan { get; set; }
        public DateTime LastScan { get; set; }
    }

    public class USR_WineInfo : TUSR<WineInfo>
    {
        public static USR_WineInfo FromTCSR(USR usr)
        {
            USR_WineInfo usrw = new USR_WineInfo();
            usrw.Reason = usr.Reason;
            usrw.Result = usr.Result;
            usrw.CorrelationID = usr.CorrelationID;

            return usrw;
        }
    }
}
