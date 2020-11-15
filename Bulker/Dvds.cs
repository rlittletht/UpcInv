
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
    class DvdUpdater : IQueryResult
    {
        private BulkerConfig m_config;
        private List<DvdElementEx> m_plDvd;

        public DvdUpdater(BulkerConfig config)
        {
            m_config = config;
            m_plDvd = new List<DvdElementEx>();

        }

        [Flags]
        enum UpdateStatus
        {
            Summary = 0x0001,
            CoverSrc = 0x0002,
            Title = 0x0004,
            MediaType= 0x0008,
            Categories = 0x0010
        }

        private Dictionary<string, string> s_mpAliases = new Dictionary<string, string>
        {
            {"upc_dvd", "DV"},
            {"upc_codes", "CD"}
        };

        private static string s_sBaseQuery =
            "SELECT TOP 1000 $$upc_dvd$$.ID, $$upc_dvd$$.Title, $$upc_dvd$$.Classification, $$upc_dvd$$.MediaType, $$upc_dvd$$.Summary, $$upc_dvd$$.UpdateStatus, $$upc_codes$$.LastScanDate, $$upc_codes$$.ScanCode, $$upc_dvd$$.CoverSrc FROM $$#upc_dvd$$";
        //                        0                    1                    2                     3                      4                       5                        6                              7                         8
        public void DoUpdate(string sConnectionString)
        {
            // get the set of books that we want to update
            TCore.Sql sql;

            SqlSelect sqls = new SqlSelect(s_sBaseQuery, s_mpAliases);
            SqlWhere swInnerJoin = new SqlWhere();

            swInnerJoin.Add("$$upc_dvd$$.ScanCode = $$upc_codes$$.ScanCode", SqlWhere.Op.And);
            sqls.AddInnerJoin(new SqlInnerJoin("$$#upc_codes$$", swInnerJoin));

            UpdateStatus flagsToUpdate = UpdateStatus.MediaType | UpdateStatus.Title | UpdateStatus.Categories| UpdateStatus.CoverSrc | UpdateStatus.Summary;

            if (m_config.ForceUpdateSummary)
                flagsToUpdate |= UpdateStatus.Summary;

            // we want to match any entry that hasn't tried to update ALL of our fields. (if they have tried some,
            // but not others, then try again. after all, we might know more fields now that we want to update.
            sqls.Where.Add($"$$upc_dvd$$.UpdateStatus & {(int)flagsToUpdate} <> {(int)flagsToUpdate}", SqlWhere.Op.And);
            sqls.Where.StartGroup(SqlWhere.Op.And);
            sqls.Where.Add(String.Format("$$upc_dvd$$.LastUpdate < '{0}'", DateTime.Now.ToString("d")), SqlWhere.Op.And);
            sqls.Where.Add("$$upc_dvd$$.LastUpdate IS NULL", SqlWhere.Op.Or);
            sqls.Where.EndGroup();

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

                foreach (DvdElementEx dvdex in m_plDvd)
                {
                    string sError;

                    Console.WriteLine($"Trying to scrape dvd {dvdex.ScanCode}: {dvdex.Title}...");
                    DVD.ScrapeSet scrapedSet = 0;

                    if (m_config.ForceUpdateSummary)
                        dvdex.Summary = null;

                    if (DVD.FScrapeDvd(dvdex, out scrapedSet, out sError))
                    {
                        // we might not have scraped everything, but we scraped something...

                        if (scrapedSet.HasFlag(DVD.ScrapeSet.CoverSrc) && !DownloadCoverForDvd(dvdex, m_config.LocalCoverRoot, "covers", out sError))
                        {
                            Console.WriteLine($"FAILED: to download cover art: {sError}");
                            LogDvdUpdateError(dvdex, scrapedSet, sError);
                        }
                        else
                        {
                            Console.WriteLine(
                                $"SUCCEEDED: MediaType: {dvdex.MediaType}, Classification: {dvdex.Classification}");
                            AddDVDToUpdateQueue(dvdex, scrapedSet);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"FAILED: {sError}");
                        LogDvdUpdateError(dvdex, scrapedSet, sError);
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

        private bool DownloadCoverForDvd(DvdElementEx dvdex, string sLocalRoot, string sLocalPath, out string sError)
        {
            sError = null;
            try
            {
                string sFullPath = Path.Combine(sLocalRoot, sLocalPath);

                if (!Directory.Exists(sFullPath))
                {
                    Directory.CreateDirectory(sFullPath);
                }

                Uri uri = new Uri($"http://{dvdex.CoverSrc}");
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

                dvdex.CoverSrc = sOutPath;
                return true;
            }
            catch (Exception exc)
            {
                dvdex.CoverSrc = null;
                sError = $"failed to download cover art: {dvdex.CoverSrc}: {exc.Message}";
                return false;
            }
        }

        void AddDVDToUpdateQueue(DvdElementEx dvdex, DVD.ScrapeSet scrapedSet)
        {
            StreamWriter sw = new StreamWriter(m_config.SqlFile, true /*fAppend*/, System.Text.Encoding.Default);

            sw.WriteLine(BuildSetString(dvdex, null, scrapedSet));
            sw.Close();
        }

        string BuildSetString(DvdElementEx dvdex, string sComment, DVD.ScrapeSet set)
        {
            List<string> plsSet = new List<string>();

            UpdateStatus status = (UpdateStatus)dvdex.UpdateStatus;

            plsSet.Add(String.Format(" LastUpdate='{0}' ", DateTime.Now.ToString("d")));

            if (!String.IsNullOrEmpty(dvdex.MediaType) && set.HasFlag(DVD.ScrapeSet.MediaType))
            {
                plsSet.Add(String.Format(" MediaType='{0}' ", dvdex.MediaType));
                status |= UpdateStatus.MediaType;
            }

            if (!String.IsNullOrEmpty(dvdex.Classification) && set.HasFlag(DVD.ScrapeSet.Categories))
            {
                plsSet.Add($" Classification='{Sql.Sqlify(dvdex.Classification)}' ");
                status |= UpdateStatus.Categories;
            }

            if (!String.IsNullOrEmpty(dvdex.CoverSrc) && set.HasFlag(DVD.ScrapeSet.CoverSrc))
            {
                plsSet.Add($" CoverSrc='{Sql.Sqlify(dvdex.CoverSrc)}' ");
                status |= UpdateStatus.CoverSrc;
            }

            if (!String.IsNullOrEmpty(dvdex.Summary) && set.HasFlag(DVD.ScrapeSet.Summary))
            {
                string s = Sql.Sqlify(dvdex.Summary);

                s = s.Replace("\n", "' + CHAR(13) + CHAR(10) + '");
                plsSet.Add($" Summary='{s}' ");
                status |= UpdateStatus.Summary;
            }

            StringBuilder sb = new StringBuilder(256);
            sb.Append($"UPDATE upc_DVD SET UpdateStatus={(int)status} ");

            foreach (string s in plsSet)
            {
                sb.Append(", ");
                sb.Append(s);
            }

            sb.Append($"WHERE ID='{dvdex.DvdID}'");
            if (!String.IsNullOrEmpty(sComment))
                sb.Append(sComment);

            return sb.ToString();
        }

        void LogDvdUpdateError(DvdElementEx dvdex, DVD.ScrapeSet scrapedSet, string sReason)
        {
            StreamWriter swSql = new StreamWriter(m_config.SqlFile, true /*fAppend*/, System.Text.Encoding.Default);
            StreamWriter swLog = new StreamWriter(m_config.LogFile, true /*fAppend*/, System.Text.Encoding.Default);

            swLog.WriteLine($"{dvdex.ScanCode}: {dvdex.Title}: FAILED: {sReason}");
            swLog.Close();

            swSql.WriteLine(BuildSetString(dvdex, " -- FAILED QUERY!", scrapedSet));

            swSql.Close();
        }

        class DvdElementEx : DVD.DvdElement
        {
            private string m_sDvdID;
            private int m_nUpdateStatus;

            public string DvdID { get => m_sDvdID; set => m_sDvdID = value; }
            public int UpdateStatus { get => m_nUpdateStatus; set => m_nUpdateStatus = value; }
        }

        //        "SELECT TOP 1000 $$upc_dvd$$.ID, $$upc_dvd$$.Title, $$upc_dvd$$.Classification, $$upc_dvd$$.MediaType, 
        //                        0                    1                    2                     3                      

        //      $$upc_dvd$$.Summary, $$upc_dvd$$.UpdateStatus, $$upc_codes$$.LastScanDate, $$upc_codes$$.ScanCode, $$upc_dvd$$.CoverSrc FROM $$#upc_dvd$$";
        //              4                       5                        6                              7                         8
        bool IQueryResult.FAddResultRow(SqlReader sqlr, int iRecordSet)
        {
            DvdElementEx dvd = new DvdElementEx();

            dvd.ScanCode = sqlr.Reader.GetString(7);
            dvd.DvdID = sqlr.Reader.GetGuid(0).ToString();
            dvd.Title = sqlr.Reader.GetString(1);
            dvd.Classification = sqlr.Reader.IsDBNull(2) ? null : sqlr.Reader.GetString(2);
            dvd.MediaType = sqlr.Reader.IsDBNull(3) ? null : sqlr.Reader.GetString(3);
            dvd.Summary = sqlr.Reader.IsDBNull(4) ? null : sqlr.Reader.GetString(4);
            dvd.UpdateStatus = sqlr.Reader.GetInt32(5);
            dvd.CoverSrc = sqlr.Reader.IsDBNull(8) ? null : sqlr.Reader.GetString(8);

            m_plDvd.Add(dvd);
            return true;
        }

    }
}