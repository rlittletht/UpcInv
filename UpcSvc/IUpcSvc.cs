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
        USR UpdateUpcLastScanDate(string sScanCode, string sTitle);

        [OperationContract]
        [WebInvoke(Method = "GET", BodyStyle = WebMessageBodyStyle.Wrapped)]
        string FetchTitleFromGenericUPC(string sCode);

    }
}
