using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Support.V7.App;
using Android.Widget;
using DroidUpc;

namespace TCore.StatusBox
{
    public interface IStatusReporting
    {
        void AddMessage(UpcAlert.AlertType at, string sMessage, params object[] rgo);
    }

    class StatusBox : IStatusReporting
    {
        private TextView m_tv;
        private bool m_fInit;
        private IAlert m_ia;
        private AppCompatActivity m_act;

        public StatusBox() { }

        public void Initialize(TextView tv, IAlert ia, AppCompatActivity act)
        {
            m_tv = tv;
            m_ia = ia;
            m_fInit = true;
            m_act = act;
        }

        public void AddMessage(UpcAlert.AlertType at, string sMessage, params object[] rgo)
        {
            if (!m_fInit)
                return;

            if (at != UpcAlert.AlertType.None && m_ia != null)
                m_ia.DoAlert(at);

            string s2;

            string s = String.Format(sMessage, rgo);
            m_act.RunOnUiThread(() =>
            {
                s2 = m_tv.Text;

                s = s + "\n" + s2;

                m_tv.Text = s;
            });
        }
    }
}
