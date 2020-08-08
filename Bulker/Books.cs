
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;
using TCore;
using TCore.Scrappy.BarnesAndNoble;


namespace Bulker
{
    class BookUpdater : IQueryResult
    {
        private BulkerConfig m_config;
        private List<BookElementEx> m_plBooks;

        public BookUpdater(BulkerConfig config)
        {
            m_config = config;
            m_plBooks = new List<BookElementEx>();

        }

        [Flags]
        enum UpdateStatus
        {
            Author = 0x0001,
            Summary = 0x0002,
            Series = 0x0004,
            ReleaseDate = 0x0008,
            CoverSrc = 0x0010
        }

        private Dictionary<string, string> s_mpAliases = new Dictionary<string, string>
        {
            {"upc_books", "BK"},
            {"upc_codes", "CD"}
        };

        private static string s_sBaseQuery =
            "SELECT TOP 1000 $$upc_books$$.ID, $$upc_books$$.Title, $$upc_books$$.Author, $$upc_books$$.Series, $$upc_books$$.Summary, $$upc_books$$.ReleaseDate, $$upc_books$$.UpdateStatus, $$upc_codes$$.LastScanDate, $$upc_codes$$.ScanCode, $$upc_books$$.CoverSrc FROM $$#upc_books$$";
        //                        0                    1                    2                     3                      4                       5                        6                              7                         8                         9
        public void DoUpdate(string sConnectionString)
        {
            // get the set of books that we want to update
            TCore.Sql sql;

            SqlSelect sqls = new SqlSelect(s_sBaseQuery, s_mpAliases);
            SqlWhere swInnerJoin = new SqlWhere();

            swInnerJoin.Add("$$upc_books$$.ScanCode = $$upc_codes$$.ScanCode", SqlWhere.Op.And);
            sqls.AddInnerJoin(new SqlInnerJoin("$$#upc_codes$$", swInnerJoin));

            UpdateStatus flagsToUpdate = UpdateStatus.Author | UpdateStatus.Series | UpdateStatus.ReleaseDate | UpdateStatus.CoverSrc;

            if (m_config.ForceUpdateSummary)
                flagsToUpdate |= UpdateStatus.Summary;

            // we want to match any entry that hasn't tried to update ALL of our fields. (if they have tried some,
            // but not others, then try again. after all, we might know more fields now that we want to update.
            sqls.Where.Add($"$$upc_books$$.UpdateStatus & {(int)flagsToUpdate} <> {(int)flagsToUpdate}", SqlWhere.Op.And);
            sqls.Where.StartGroup(SqlWhere.Op.And);
            sqls.Where.Add(String.Format("$$upc_books$$.LastUpdate < '{0}'", DateTime.Now.ToString("d")), SqlWhere.Op.And);
            sqls.Where.Add("$$upc_books$$.LastUpdate IS NULL", SqlWhere.Op.Or);
            sqls.Where.EndGroup();

            // s.Where.Add($"$$upc_books$$.ScanCode = '9780312853952'", SqlWhere.Op.And);

            // and lets update the latest scanned items first
            sqls.AddOrderBy("$$upc_codes$$.LastScanDate DESC"); 

            SR sr = Sql.OpenConnection(out sql, sConnectionString);

            if (!sr.Succeeded)
                throw new Exception(sr.Reason);

            // from this point on, we have to release the sql connection!!
            int cFailed = 0;

            try
            {
                sr = Sql.ExecuteQuery(sql, sqls.ToString(), this, null);

                if (!sr.Succeeded)
                    throw new Exception(sr.Reason);

                foreach (BookElementEx bex in m_plBooks)
                {
                    string sError;

                    Console.WriteLine($"Trying to scrape book {bex.ScanCode}: {bex.Title}...");
                    Book.ScrapeSet scrapedSet = 0;

                    if (m_config.ForceUpdateSummary)
                        bex.Summary = null;

                    if (Book.FScrapeBookSet(bex, out scrapedSet, out sError))
                    {
                        // we might not have scraped everything, but we scraped something...

                        if (scrapedSet.HasFlag(Book.ScrapeSet.CoverSrc) && !DownloadCoverForBook(bex, m_config.LocalCoverRoot, "covers", out sError))
                        {
                            Console.WriteLine($"FAILED: to download cover art: {sError}");
                            LogBookUpdateError(bex, scrapedSet, sError);
                        }
                        else
                        {
                            Console.WriteLine(
                                $"SUCCEEDED: Author: {bex.Author}, ReleaseDate: {bex.ReleaseDate}, Series: {bex.Series}");
                            AddBookToUpdateQueue(bex, scrapedSet);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"FAILED: {sError}");
                        LogBookUpdateError(bex, scrapedSet, sError);
                        cFailed++;
                        if (cFailed > 150)
                            throw new Exception("giving up");
                    }
                }
            }
            finally
            {
                sql.Close();
            }
        }

        private bool DownloadCoverForBook(BookElementEx bex, string sLocalRoot, string sLocalPath, out string sError)
        {
            sError = null;
            try
            {
                string sFullPath = Path.Combine(sLocalRoot, sLocalPath);

                if (!Directory.Exists(sFullPath))
                {
                    Directory.CreateDirectory(sFullPath);
                }

                Uri uri = new Uri($"http://{bex.RawCoverUrl}");
                WebRequest req = WebRequest.Create(uri);

                string sPathname = uri.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);
                string sFilename = Path.GetFileName(sPathname);
                string sFullOutPath = Path.Combine(sFullPath, sFilename);
                string sOutPath = $"{sLocalPath}\\{sFilename}";

                WebResponse response = req.GetResponse();

                Stream stream = response.GetResponseStream();
                Stream stmOut = new FileStream(sFullOutPath, FileMode.Create);

                stream.CopyTo(stmOut);
                stmOut.Close();
                stmOut.Dispose();
                stream.Close();
                stream.Dispose();
                response.Close();
                response.Dispose();

                bex.RawCoverUrl = sOutPath;
                return true;
            }
            catch (Exception exc)
            {
                bex.RawCoverUrl = null;
                sError = $"failed to download cover art: {bex.RawCoverUrl}: {exc.Message}";
                return false;
            }
        }

        void AddBookToUpdateQueue(BookElementEx bex, Book.ScrapeSet scrapedSet)
        {
            StreamWriter sw = new StreamWriter(m_config.SqlFile, true /*fAppend*/, System.Text.Encoding.Default);

            sw.WriteLine(BuildSetString(bex, null, scrapedSet));
            sw.Close();
        }

        string BuildSetString(BookElementEx bex, string sComment, Book.ScrapeSet set)
        {
            List<string> plsSet = new List<string>();

            UpdateStatus status = (UpdateStatus)bex.UpdateStatus;

            plsSet.Add(String.Format(" LastUpdate='{0}' ", DateTime.Now.ToString("d")));

            if (!String.IsNullOrEmpty(bex.ReleaseDate) && set.HasFlag(Book.ScrapeSet.ReleaseDate))
            {
                DateTime dttm = DateTime.Parse(bex.ReleaseDate);
                
                plsSet.Add(String.Format(" ReleaseDate='{0}' ", dttm.ToString("yyyy/MM/dd")));
                status |= UpdateStatus.ReleaseDate;
            }

            if (!String.IsNullOrEmpty(bex.Author) && set.HasFlag(Book.ScrapeSet.Author))
            {
                plsSet.Add($" Author='{Sql.Sqlify(bex.Author)}' ");
                status |= UpdateStatus.Author;
            }

            if (!String.IsNullOrEmpty(bex.RawCoverUrl) && set.HasFlag(Book.ScrapeSet.CoverSrc))
            {
                plsSet.Add($" CoverSrc='{Sql.Sqlify(bex.RawCoverUrl)}' ");
                status |= UpdateStatus.CoverSrc;
            }

            if (!String.IsNullOrEmpty(bex.Series) && set.HasFlag(Book.ScrapeSet.Series))
            {
                plsSet.Add($" Series='{Sql.Sqlify(bex.Series)}' ");
                status |= UpdateStatus.Series;
            }

            if (!String.IsNullOrEmpty(bex.Summary) && set.HasFlag(Book.ScrapeSet.Summary))
            {
                string s = Sql.Sqlify(bex.Summary);

                s = s.Replace("\n", "' + CHAR(13) + CHAR(10) + '");
                plsSet.Add($" Summary='{s}' ");
                status |= UpdateStatus.Summary;
            }

            StringBuilder sb = new StringBuilder(256);
            sb.Append($"UPDATE upc_Books SET UpdateStatus={(int)status} ");

            foreach (string s in plsSet)
            {
                sb.Append(", ");
                sb.Append(s);
            }

            sb.Append($"WHERE ID='{bex.BookID}'");
            if (!String.IsNullOrEmpty(sComment))
                sb.Append(sComment);

            return sb.ToString();
        }

        void LogBookUpdateError(BookElementEx bex, Book.ScrapeSet scrapedSet, string sReason)
        {
            StreamWriter swSql = new StreamWriter(m_config.SqlFile, true /*fAppend*/, System.Text.Encoding.Default);
            StreamWriter swLog = new StreamWriter(m_config.LogFile, true /*fAppend*/, System.Text.Encoding.Default);

            swLog.WriteLine($"{bex.ScanCode}: {bex.Title}: FAILED: {sReason}");
            swLog.Close();

            swSql.WriteLine(BuildSetString(bex, " -- FAILED QUERY!", scrapedSet));

            swSql.Close();
        }

        class BookElementEx : Book.BookElement
        {
            private string m_sBookID;
            private int m_nUpdateStatus;

            public string BookID { get => m_sBookID; set => m_sBookID = value; }
            public int UpdateStatus { get => m_nUpdateStatus; set => m_nUpdateStatus = value; }
        }

        bool IQueryResult.FAddResultRow(SqlReader sqlr, int iRecordSet)
        {
            BookElementEx book = new BookElementEx();

            book.ScanCode = sqlr.Reader.GetString(8);
            book.BookID = sqlr.Reader.GetGuid(0).ToString();
            book.Title = sqlr.Reader.GetString(1);
            book.Author = sqlr.Reader.IsDBNull(2) ? null : sqlr.Reader.GetString(2);
            book.Series = sqlr.Reader.IsDBNull(3) ? null : sqlr.Reader.GetString(3);
            book.Summary = sqlr.Reader.IsDBNull(4) ? null : sqlr.Reader.GetString(4);
            book.ReleaseDate = sqlr.Reader.IsDBNull(5) ? null : sqlr.Reader.GetDateTime(5).ToString("d");
            book.UpdateStatus = sqlr.Reader.GetInt32(6);
            book.RawCoverUrl = sqlr.Reader.IsDBNull(9) ? null : sqlr.Reader.GetString(9);

            m_plBooks.Add(book);
            return true;
        }

    }
}