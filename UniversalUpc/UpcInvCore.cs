using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using TCore.StatusBox;
using UpcService = UniversalUpc.UpcSvc;

namespace UniversalUpc
{
    public class UpcInvCore
    {
        private UpcService.UpcSvcClient m_usc = null;
        private IAlert m_ia;
        private IStatusReporting m_isr;

        public enum ADAS
        {
            Generic = 0,
            Book = 1,
            DVD = 2,
            Wine = 3,
        }

        ADAS m_adas;

        public UpcInvCore(IAlert ia, IStatusReporting isr)
        {
            m_ia = ia;
            m_isr = isr;
        }

        public void EnsureServiceConnection()
        {
            if (m_usc == null)
                m_usc = new UpcService.UpcSvcClient();
        }

        /*----------------------------------------------------------------------------
        	%%Function: SEnsureEan13
        	%%Qualified: UniversalUpc.UpcInvCore.SEnsureEan13
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        public string SEnsureEan13(string s)
        {
            if (s.Length == 12)
                return "0" + s;

            return s;
        }

        public DateTime? DttmGetLastScan(string sScanCode)
        {
            EnsureServiceConnection();

            Task<UpcService.USR_String> tsk = m_usc.GetLastScanDateAsync(sScanCode);

            tsk.Wait();

            if (!tsk.IsCompleted || !tsk.Result.Result)
                return null;

            return DateTime.Parse(tsk.Result.TheValue);
        }

        public delegate void ContinueWithDvdInfoDelegate(string sScanCode, UpcService.DvdInfo dvdInfo);

        public async void DvdInfoRetrieve(string sScanCode, ContinueWithDvdInfoDelegate del)
        {
            EnsureServiceConnection();
            UpcService.USR_DvdInfo usrd = await m_usc.GetDvdScanInfoAsync(sScanCode);
            UpcService.DvdInfo dvdInfo = usrd.TheValue;

            del(sScanCode, dvdInfo);
        }
    }
}
