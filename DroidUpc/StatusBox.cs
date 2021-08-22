using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AndroidX.AppCompat.App;
using Android.Widget;
using DroidUpc;
using UpcShared;

namespace TCore.StatusBox
{
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

        public void AddMessage(AlertType at, string sMessage, params object[] rgo)
        {
            if (!m_fInit)
                return;

            if (at != AlertType.None && m_ia != null)
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
