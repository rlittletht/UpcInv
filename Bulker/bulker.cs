
using System;
using TCore.CmdLine;
using System.Configuration;
using TCore.Scrappy.BarnesAndNoble;

namespace Bulker
{
    // This will eventually bulk update all types in the inventory database.

    // First implemented is books (to scrape series, publish date, author, and synopsis
    class Bulker
    {
        private BulkerConfig m_config;

        public static string s_sConnectionString =
            ConfigurationManager.AppSettings["Thetasoft.Azure.ConnectionString"];

        public Bulker(){}

        public void ParseCmdLine(string[] args)
        {
            m_config = new BulkerConfig();

            CmdLineConfig cfg = new CmdLineConfig(new CmdLineSwitch[]
            {
                new CmdLineSwitch("R", false, false, "Record file", "Record file", null),
                new CmdLineSwitch("L", false, false, "Log file", "Log file", null),
                new CmdLineSwitch("C", false, false, "Cover source full path to root", "Cover source full path to root", null), 
                new CmdLineSwitch("B", true, false, "Bulk update books", "Bulk update books", null), 
                new CmdLineSwitch("Bs", true, false, "Update book summary (force)", "Summary", null), 
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

            if (m_config.Action != BulkerConfig.RequestedAction.Books)
                throw new Exception("no action specified");

            switch (m_config.Action)
            {
                case BulkerConfig.RequestedAction.Books:
                {
                    BookUpdater books = new BookUpdater(m_config);

                    books.DoUpdate();
                    break;
                }
            }

        }
    }

}