
using System;
using TCore.CmdLine;
using System.Configuration;
using System.Threading.Tasks;
using TCore.KeyVault;
using TCore.Scrappy.BarnesAndNoble;

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
                new CmdLineSwitch("C", false, false, "Cover source full path to root", "Cover source full path to root", null), 
                new CmdLineSwitch("B", true, false, "Bulk update books", "Bulk update books", null), 
                new CmdLineSwitch("Bs", true, false, "Update book summary (force)", "Summary", null),
                new CmdLineSwitch("D", true, false, "Bulk update dvds", "Bulk update dvds", null),
                new CmdLineSwitch("Ds", true, false, "Update dvd summary (force)", "Summary", null),
                new CmdLineSwitch("PW", true, false, "Show password", "show password for debugging purposes", null), 
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

        private Client m_clientKeyVault;
        private string s_sAppID = "bfbaffd7-2217-4deb-a85a-4f697e6bdf94";
        private string m_sAppTenant = "b90f9ef3-5e11-43e0-a75c-1f45e6b223fb";
        private string s_sConnectionStringSecretID = "Thetasoft-Azure-ConnectionString/324deaac388a480ab992ccef03072b61";
        public string ConnectionString { get; set; }

        public async Task Run(string[] args)
        {
            ParseCmdLine(args);

            m_clientKeyVault = new Client(m_sAppTenant, s_sAppID);
            ConnectionString = await m_clientKeyVault.GetSecret(s_sConnectionStringSecretID);
            if (m_config.ShowPassword)
            {
                Console.WriteLine($"Secret: {ConnectionString}");
            }

            if (m_config.Action != BulkerConfig.RequestedAction.Books
                && m_config.Action != BulkerConfig.RequestedAction.Dvds)
            {
                Console.WriteLine("No action specified. Terminating.");
                return;
            }

            switch (m_config.Action)
            {
                case BulkerConfig.RequestedAction.Books:
                {
                    BookUpdater books = new BookUpdater(m_config);

                    books.DoUpdate(ConnectionString);
                    break;
                }
                case BulkerConfig.RequestedAction.Dvds:
                {
                    DvdUpdater dvd = new DvdUpdater(m_config);

                    dvd.DoUpdate(ConnectionString);
                    break;
                }
            }

        }
    }

}