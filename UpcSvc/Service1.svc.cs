using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace UpcSvc
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "UpcSvc" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select UpcSvc.svc or UpcSvc.svc.cs at the Solution Explorer and start debugging.
    public class UpcSvc : IUpcSvc
    {
        private string _sSqlConnection = null;

        public static string SqlConnectionStatic
        {
            get
            {
#if PRODUCTION
                return ConfigurationManager.AppSettings["Thetasoft.Azure.ConnectionString"];
#else
                return "Server=cantorix;Database=db0902;Trusted_Connection=True;";
#endif
            }
        }

        public string GetSqlConnection()
        {
            if (_sSqlConnection == null)
#if PRODUCTION
                _sSqlConnection = ConfigurationManager.AppSettings["Thetasoft.Azure.ConnectionString"];
#else
                _sSqlConnection = "Server=cantorix;Database=db0902;Trusted_Connection=True;";
#endif
            return _sSqlConnection;
        }


        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }
    }
}
