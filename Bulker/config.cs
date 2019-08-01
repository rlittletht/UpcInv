﻿
using System;
using TCore.CmdLine;

namespace Bulker
{
    // to hold parsed parameters for all features
    class BulkerConfig : ICmdLineDispatch
    {
        private string m_sLogFile; // where to log errors
        private string m_sSqlFile; // this is the sql file to use when recording and not just directly updating the database
        private string m_sLocalCoverRoot;

        private RequestedAction m_action;

        public RequestedAction Action => m_action;
        public string LogFile => m_sLogFile;
        public string SqlFile => m_sSqlFile;
        public string LocalCoverRoot => m_sLocalCoverRoot;

        public enum RequestedAction
        {
            Books
        }
       
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
            else if (cls.Switch == "C")
            {
                m_sLocalCoverRoot = sParam;
            }
            else if (cls.Switch == "B")
            {
                m_action = RequestedAction.Books;
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
