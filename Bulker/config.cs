
using TCore.CmdLine;

namespace Bulker
{
    // to hold parsed parameters for all features
    class BulkerConfig : ICmdLineDispatch
    {
        private string m_sLogFile; // where to log errors
        private string m_sSqlFile; // this is the sql file to use when recording and not just directly updating the database

        public bool FDispatchCmdLineSwitch(TCore.CmdLine.CmdLineSwitch cls, string sParam, object oClient, out string sError)
        {
            sError = "";

            if (cls.Switch == "R")
            {
                m_sSqlFile = sParam;
            }
            else if (cls.Switch == "L")

            {
                m_sLogFile = sParam;
            }
            else
            {
                sError = $"Invalid switch {cls.Switch}";
                return false;
            }

            return true;
        }
    }

}
