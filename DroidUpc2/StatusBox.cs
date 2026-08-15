using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Widget;
using DroidUpc2;
using UpcShared;

namespace TCore.StatusBox
{
    class StatusBox(TextView textView, IAlert? m_ia, Activity m_act) : IStatusReporting
    {
        private readonly bool m_fInit = true;

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
                s2 = textView.Text ?? "";

                s = s + "\n" + s2;

                textView.Text = s;
            });
        }
    }
}
