﻿using System;
using System.Threading.Tasks;
using TCore.WebInterop;
using UpcShared;

namespace UpcApi.Proxy
{
    public class WebApi
    {
        private WebApiInterop m_apiInterop;

        public WebApi(WebApiInterop apiInterop)
        {
            m_apiInterop = apiInterop;
        }

        public async Task<T> Generic<T>(string sApiTemplate, params object[] rgo)
        {
            if (rgo != null && rgo.Length > 0)
            {
                object[] rgoNew = new object[rgo.Length];
                for (int i = 0; i < rgo.Length; i++)
                {
                    if (rgo[i] is string)
                        rgoNew[i] = Uri.EscapeDataString((string)rgo[i]);
                    else
                        rgoNew[i] = rgo[i];
                }

                rgo = rgoNew;
            }

            string sQuery = String.Format(sApiTemplate, rgo);
            return await m_apiInterop.CallService<T>(sQuery, false);
        }

        #region Books
        public async Task<USR_BookInfoList> GetBookScanInfosFromTitle(string Title)
        {
            return await Generic<USR_BookInfoList>("api/book/GetBookScanInfosFromTitle?Title={0}", Title ?? "");
        }

        public async Task<USR_BookInfo> GetBookScanInfo(string ScanCode)
        {
            return await Generic<USR_BookInfo>("api/book/GetBookScanInfo?ScanCode={0}", ScanCode ?? "");
        }

        public async Task<USR> CreateBook(string ScanCode, string Title, string Location)
        {
            return await Generic<USR>("api/book/CreateBook?ScanCode={0}&Title={1}&Location={2}", ScanCode ?? "", Title ?? "", Location ?? "");
        }

        public async Task<USR> UpdateBookScan(string ScanCode, string Title, string Location)
        {
            return await Generic<USR>("api/book/UpdateBookScan?ScanCode={0}&Title={1}&Location={2}", ScanCode ?? "", Title ?? "", Location ?? "");
        }

        #endregion

        #region Upc

        public async Task<USR_String> GetLastScanDate(string ScanCode)
        {
            return await Generic<USR_String>("api/Upc/GetLastScanDate?ScanCode?ScanCode={0}", ScanCode ?? "");
        }

        public async Task<USR> UpdateUpcLastScanDate(string ScanCode, string Title)
        {
            return await Generic<USR>("api/Upc/UpdateUpcLastScanDate?ScanCode={0}&Title={1}", ScanCode ?? "", Title ?? "");
        }

        public async Task<string> FetchTitleFromGenericUPC(string Code)
        {
            return await Generic<string>("api/Upc/FetchTitleFromGenericUPC?Code={0}", Code ?? "");
        }

        public async Task<string> FetchTitleFromISBN13(string Code)
        {
            return await Generic<string>("api/Upc/FetchTitleFromISBN13?ScanCode={0}", Code ?? "");
        }

        #endregion

        #region DVD

        public async Task<USR_DvdInfo> GetDvdScanInfo(string ScanCode)
        {
            return await Generic<USR_DvdInfo>("api/dvd/GetDvdScanInfo?ScanCode={0}", ScanCode ?? "");
        }

        public async Task<USR_DvdInfoList> GetDvdScanInfosFromTitle(string Title)
        {
            return await Generic<USR_DvdInfoList>("api/dvd/GetDvdScanInfosFromTitle?Title={0}", Title ?? "");
        }

        public async Task<USR> CreateDvd(string ScanCode, string Title)
        {
            return await Generic<USR>("api/dvd/CreateDvd?ScanCode={0}&Title={1}", ScanCode ?? "", Title ?? "");
        }

        #endregion

        #region Wine
        public async Task<USR_WineInfo> GetWineScanInfo(string ScanCode)
        {
            return await Generic<USR_WineInfo>("api/wine/GetWineScanInfo?ScanCode={0}", ScanCode ?? "");
        }

        public async Task<USR> DrinkWine(string ScanCode, string Wine, string Vintage, string Notes)
        {
            return await Generic<USR>
                ("api/wine/DrinkWine?ScanCode={0}&Wine={1}&Vintage={2}&Notes={3}",
                ScanCode ?? "",
                Wine ?? "",
                Vintage ?? "",
                Notes ?? "");
        }

        public async Task<USR> UpdateWineInventory(string ScanCode, string Wine, string sBinCode)
        {
            return await Generic<USR>(
                "api/wine/UpdateInventory?ScanCode={0}&Wine={1}&BinCode={2}",
                ScanCode ?? "",
                Wine ?? "",
                sBinCode ?? "");
        }

        #endregion

        #region Diagnostics

        public async Task<USR_DiagnosticResult> GetHeartbeat()
        {
            return await Generic<USR_DiagnosticResult>("api/diagnostics/Heartbeat");
        }

        #endregion
#if no
        public async Task<TTTT> APINAME(args)
        {
            return await Generic<TTTT>("apipath?", );
        }
#endif

        void ThrowNYI()
        {
#if !DEBUG
            throw new Exception("NYI");
#endif
        }
    }
}