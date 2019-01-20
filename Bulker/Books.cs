
using System.Data.SqlClient;


namespace Bulker
{
    class BookUpdater
    {
        private BulkerConfig m_config;

        public BookUpdater(BulkerConfig config)
        {
            m_config = config;
        }

        public void DoUpdate()
        {
            // get the set of books that we want to update
            TCore.Sql sql;

            



        }
    }
}