using System;
using TCore.Logging;
using EventType = UpcShared.EventType;

namespace DroidUpc
{
    class UpcLogProvider : UpcShared.ILogProvider
    {
        private TCore.Logging.ILogProvider m_ilpActual;

        public UpcLogProvider(TCore.Logging.ILogProvider ilpActual)
        {
            m_ilpActual = ilpActual;
        }

        public void LogEvent(Guid crids, EventType et, string s, params object[] rgo)
        {
            TCore.Logging.EventType etActual;

            switch (et)
            {
                case EventType.Critical:
                    etActual = TCore.Logging.EventType.Critical;
                    break;
                case EventType.Error:
                    etActual = TCore.Logging.EventType.Error;
                    break;
                case EventType.Information:
                    etActual = TCore.Logging.EventType.Information;
                    break;
                case EventType.Verbose:
                    etActual = TCore.Logging.EventType.Verbose;
                    break;
                case EventType.Warning:
                    etActual = TCore.Logging.EventType.Warning;
                    break;
                default:
                    etActual = TCore.Logging.EventType.Critical;
                    break;
            }

            m_ilpActual.LogEvent(CorrelationID.FromCrids(crids), etActual, s, rgo);
        }
    }
}