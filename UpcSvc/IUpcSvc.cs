using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace UpcSvc
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IUpcSvc" in both code and config file together.
    [ServiceContract]
    public interface IUpcSvc
    {
        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped)]
        USR_String GetLastScanDate(string sScanCode);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped)]
        USR_DvdInfo GetDvdScanInfo(string sScanCode);       

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped)]
        USR_DvdInfoList GetDvdScanInfosFromTitle(string sTitleSubstring);       

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped)]
        USR CreateDvd(string sScanCode, string sTitle);       

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped)]
        USR_BookInfo GetBookScanInfo(string sScanCode);       

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped)]
        USR_BookInfoList GetBookScanInfosFromTitle(string sTitleSubstring);       

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped)]
        USR CreateBook(string sScanCode, string sTitle, string sLocation);       

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped)]
        USR UpdateBookScan(string sScanCode, string sTitle, string sLocation);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped)]
        USR_WineInfo GetWineScanInfo(string sScanCode);       

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped)]
        USR DrinkWine(string sScanCode, string sWine, string sVintage, string sNotes);       

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped)]
        USR UpdateUpcLastScanDate(string sScanCode, string sTitle);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped)]
        string FetchTitleFromGenericUPC(string sCode);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped)]
        string FetchTitleFromISBN13(string sCode);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped)]
        USR TestLog();

    }
}
