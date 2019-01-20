
using System;
using TCore.CmdLine;

namespace Bulker
{
    // This will eventually bulk update all types in the inventory database.

    // First implemented is books (to scrape series, publish date, author, and synopsis
    class Bulker
    {
        private BulkerConfig m_config;

        public Bulker(){}

        public void ParseCmdLine(string[] args)
        {
            m_config = new BulkerConfig();

            CmdLineConfig cfg = new CmdLineConfig(new CmdLineSwitch[]
            {
                new CmdLineSwitch("R", false, false, "Record file", "Record file", null),
                new CmdLineSwitch("L", false, false, "Log file", "Log file", null),
            });

            CmdLine cmdLine = new CmdLine(cfg);

            string sError;

            if (!cmdLine.FParse(args, m_config, null, out sError))
            {
                cmdLine.Usage(ConsoleWriteDelegate);
            }
        }

        void ConsoleWriteDelegate(string s)
        {
            Console.WriteLine(s);
        }

        public void Run(string[] args)
        {
            ParseCmdLine(args);


        }
    }

}