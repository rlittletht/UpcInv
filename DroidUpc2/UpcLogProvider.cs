using TCore.Logging;
using EventType = UpcShared.EventType;

namespace DroidUpc2;

public class UpcLogProvider(TCore.Logging.ILogProvider m_logProvider) : UpcShared.ILogProvider
{
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

        m_logProvider.LogEvent(CorrelationID.FromCrids(crids), etActual, s, rgo);
    }
}
