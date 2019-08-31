using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using UniversalUpc;
using UpcShared;

namespace TCore.StatusBox
{
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

        public void AddMessageCore(AlertType at, string sMessage, params object[] rgo)
        {
            ITextDocument iDoc = m_rec.Document;

            string s2;

            string s = String.Format(sMessage, rgo);
            iDoc.GetText(TextGetOptions.None, out s2);
            s = s + "\r" + s2;

            iDoc.SetText(TextSetOptions.None, s);
            if (at != AlertType.None)
                m_ia.DoAlert(at);
        }

        public async void AddMessage(AlertType at, string sMessage, params object[] rgo)
        {
            if (!m_fInit)
                return;

            if (!m_rec.Dispatcher.HasThreadAccess)
            {
                await m_rec.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { AddMessageCore(at, sMessage, rgo); });
            }
            else
            {
                AddMessageCore(at, sMessage, rgo);
            }
        }
    }
}
