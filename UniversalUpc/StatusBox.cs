using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using UniversalUpc;

namespace TCore.StatusBox
{
    public interface IStatusReporting
    {
        void AddMessage(string s, UpcAlert.AlertType at);
    }

    class StatusBox : IStatusReporting
    {
        private RichEditBox m_rec;
        private bool m_fInit;
        private IAlert m_ia;

        public StatusBox() {}

        public void Initialize(RichEditBox rec, IAlert ia)
        {
            m_rec = rec;
            m_ia = ia;
            m_fInit = true;
        }

        public void AddMessage(string s, UpcAlert.AlertType at)
        {
            if (!m_fInit)
                return;

            ITextDocument iDoc = m_rec.Document;

            string s2;

            iDoc.GetText(TextGetOptions.None, out s2);
            s = s + "\r" + s2;

            iDoc.SetText(TextSetOptions.None, s);
            if (at != UpcAlert.AlertType.None)
                m_ia.DoAlert(at);
        }
    }
}
