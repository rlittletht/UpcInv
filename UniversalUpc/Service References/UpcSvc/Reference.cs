﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This code was auto-generated by Microsoft.VisualStudio.ServiceReference.Platforms, version 14.0.23107.0
// 
namespace UniversalUpc.UpcSvc {
    using System.Runtime.Serialization;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="TCSRBase", Namespace="http://schemas.datacontract.org/2004/07/UpcSvc")]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(UniversalUpc.UpcSvc.TUSROfDvdInfo9_SjqeMlk))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(UniversalUpc.UpcSvc.USR_DvdInfo))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(UniversalUpc.UpcSvc.USR))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(UniversalUpc.UpcSvc.TUSROfstring))]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(UniversalUpc.UpcSvc.USR_String))]
    public partial class TCSRBase : object, System.ComponentModel.INotifyPropertyChanged {
        
        private System.Guid CorrelationIDField;
        
        private string ReasonField;
        
        private bool ResultField;
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.Guid CorrelationID {
            get {
                return this.CorrelationIDField;
            }
            set {
                if ((this.CorrelationIDField.Equals(value) != true)) {
                    this.CorrelationIDField = value;
                    this.RaisePropertyChanged("CorrelationID");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Reason {
            get {
                return this.ReasonField;
            }
            set {
                if ((object.ReferenceEquals(this.ReasonField, value) != true)) {
                    this.ReasonField = value;
                    this.RaisePropertyChanged("Reason");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public bool Result {
            get {
                return this.ResultField;
            }
            set {
                if ((this.ResultField.Equals(value) != true)) {
                    this.ResultField = value;
                    this.RaisePropertyChanged("Result");
                }
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="TUSROfDvdInfo9_SjqeMlk", Namespace="http://schemas.datacontract.org/2004/07/UpcSvc")]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(UniversalUpc.UpcSvc.USR_DvdInfo))]
    public partial class TUSROfDvdInfo9_SjqeMlk : UniversalUpc.UpcSvc.TCSRBase {
        
        private UniversalUpc.UpcSvc.DvdInfo TheValueField;
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public UniversalUpc.UpcSvc.DvdInfo TheValue {
            get {
                return this.TheValueField;
            }
            set {
                if ((object.ReferenceEquals(this.TheValueField, value) != true)) {
                    this.TheValueField = value;
                    this.RaisePropertyChanged("TheValue");
                }
            }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="USR_DvdInfo", Namespace="http://schemas.datacontract.org/2004/07/UpcSvc")]
    public partial class USR_DvdInfo : UniversalUpc.UpcSvc.TUSROfDvdInfo9_SjqeMlk {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="USR", Namespace="http://schemas.datacontract.org/2004/07/UpcSvc")]
    public partial class USR : UniversalUpc.UpcSvc.TCSRBase {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="TUSROfstring", Namespace="http://schemas.datacontract.org/2004/07/UpcSvc")]
    [System.Runtime.Serialization.KnownTypeAttribute(typeof(UniversalUpc.UpcSvc.USR_String))]
    public partial class TUSROfstring : UniversalUpc.UpcSvc.TCSRBase {
        
        private string TheValueField;
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string TheValue {
            get {
                return this.TheValueField;
            }
            set {
                if ((object.ReferenceEquals(this.TheValueField, value) != true)) {
                    this.TheValueField = value;
                    this.RaisePropertyChanged("TheValue");
                }
            }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="USR_String", Namespace="http://schemas.datacontract.org/2004/07/UpcSvc")]
    public partial class USR_String : UniversalUpc.UpcSvc.TUSROfstring {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="DvdInfo", Namespace="http://schemas.datacontract.org/2004/07/UpcSvc")]
    public partial class DvdInfo : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string CodeField;
        
        private System.DateTime FirstScanField;
        
        private System.DateTime LastScanField;
        
        private string LocationField;
        
        private string TitleField;
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Code {
            get {
                return this.CodeField;
            }
            set {
                if ((object.ReferenceEquals(this.CodeField, value) != true)) {
                    this.CodeField = value;
                    this.RaisePropertyChanged("Code");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.DateTime FirstScan {
            get {
                return this.FirstScanField;
            }
            set {
                if ((this.FirstScanField.Equals(value) != true)) {
                    this.FirstScanField = value;
                    this.RaisePropertyChanged("FirstScan");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.DateTime LastScan {
            get {
                return this.LastScanField;
            }
            set {
                if ((this.LastScanField.Equals(value) != true)) {
                    this.LastScanField = value;
                    this.RaisePropertyChanged("LastScan");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Location {
            get {
                return this.LocationField;
            }
            set {
                if ((object.ReferenceEquals(this.LocationField, value) != true)) {
                    this.LocationField = value;
                    this.RaisePropertyChanged("Location");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Title {
            get {
                return this.TitleField;
            }
            set {
                if ((object.ReferenceEquals(this.TitleField, value) != true)) {
                    this.TitleField = value;
                    this.RaisePropertyChanged("Title");
                }
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="UpcSvc.IUpcSvc")]
    public interface IUpcSvc {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IUpcSvc/GetLastScanDate", ReplyAction="http://tempuri.org/IUpcSvc/GetLastScanDateResponse")]
        System.Threading.Tasks.Task<UniversalUpc.UpcSvc.USR_String> GetLastScanDateAsync(string sScanCode);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IUpcSvc/GetDvdScanInfo", ReplyAction="http://tempuri.org/IUpcSvc/GetDvdScanInfoResponse")]
        System.Threading.Tasks.Task<UniversalUpc.UpcSvc.USR_DvdInfo> GetDvdScanInfoAsync(string sScanCode);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IUpcSvc/CreateDvd", ReplyAction="http://tempuri.org/IUpcSvc/CreateDvdResponse")]
        System.Threading.Tasks.Task<UniversalUpc.UpcSvc.USR> CreateDvdAsync(string sScanCode, string sTitle);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IUpcSvc/UpdateUpcLastScanDate", ReplyAction="http://tempuri.org/IUpcSvc/UpdateUpcLastScanDateResponse")]
        System.Threading.Tasks.Task<UniversalUpc.UpcSvc.USR> UpdateUpcLastScanDateAsync(string sScanCode, string sTitle);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IUpcSvc/FetchTitleFromGenericUPC", ReplyAction="http://tempuri.org/IUpcSvc/FetchTitleFromGenericUPCResponse")]
        System.Threading.Tasks.Task<string> FetchTitleFromGenericUPCAsync(string sCode);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IUpcSvcChannel : UniversalUpc.UpcSvc.IUpcSvc, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class UpcSvcClient : System.ServiceModel.ClientBase<UniversalUpc.UpcSvc.IUpcSvc>, UniversalUpc.UpcSvc.IUpcSvc {
        
        /// <summary>
        /// Implement this partial method to configure the service endpoint.
        /// </summary>
        /// <param name="serviceEndpoint">The endpoint to configure</param>
        /// <param name="clientCredentials">The client credentials</param>
        static partial void ConfigureEndpoint(System.ServiceModel.Description.ServiceEndpoint serviceEndpoint, System.ServiceModel.Description.ClientCredentials clientCredentials);
        
        public UpcSvcClient() : 
                base(UpcSvcClient.GetDefaultBinding(), UpcSvcClient.GetDefaultEndpointAddress()) {
            this.Endpoint.Name = EndpointConfiguration.BasicHttpBinding_IUpcSvc.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public UpcSvcClient(EndpointConfiguration endpointConfiguration) : 
                base(UpcSvcClient.GetBindingForEndpoint(endpointConfiguration), UpcSvcClient.GetEndpointAddress(endpointConfiguration)) {
            this.Endpoint.Name = endpointConfiguration.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public UpcSvcClient(EndpointConfiguration endpointConfiguration, string remoteAddress) : 
                base(UpcSvcClient.GetBindingForEndpoint(endpointConfiguration), new System.ServiceModel.EndpointAddress(remoteAddress)) {
            this.Endpoint.Name = endpointConfiguration.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public UpcSvcClient(EndpointConfiguration endpointConfiguration, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(UpcSvcClient.GetBindingForEndpoint(endpointConfiguration), remoteAddress) {
            this.Endpoint.Name = endpointConfiguration.ToString();
            ConfigureEndpoint(this.Endpoint, this.ClientCredentials);
        }
        
        public UpcSvcClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public System.Threading.Tasks.Task<UniversalUpc.UpcSvc.USR_String> GetLastScanDateAsync(string sScanCode) {
            return base.Channel.GetLastScanDateAsync(sScanCode);
        }
        
        public System.Threading.Tasks.Task<UniversalUpc.UpcSvc.USR_DvdInfo> GetDvdScanInfoAsync(string sScanCode) {
            return base.Channel.GetDvdScanInfoAsync(sScanCode);
        }
        
        public System.Threading.Tasks.Task<UniversalUpc.UpcSvc.USR> CreateDvdAsync(string sScanCode, string sTitle) {
            return base.Channel.CreateDvdAsync(sScanCode, sTitle);
        }
        
        public System.Threading.Tasks.Task<UniversalUpc.UpcSvc.USR> UpdateUpcLastScanDateAsync(string sScanCode, string sTitle) {
            return base.Channel.UpdateUpcLastScanDateAsync(sScanCode, sTitle);
        }
        
        public System.Threading.Tasks.Task<string> FetchTitleFromGenericUPCAsync(string sCode) {
            return base.Channel.FetchTitleFromGenericUPCAsync(sCode);
        }
        
        public virtual System.Threading.Tasks.Task OpenAsync() {
            return System.Threading.Tasks.Task.Factory.FromAsync(((System.ServiceModel.ICommunicationObject)(this)).BeginOpen(null, null), new System.Action<System.IAsyncResult>(((System.ServiceModel.ICommunicationObject)(this)).EndOpen));
        }
        
        public virtual System.Threading.Tasks.Task CloseAsync() {
            return System.Threading.Tasks.Task.Factory.FromAsync(((System.ServiceModel.ICommunicationObject)(this)).BeginClose(null, null), new System.Action<System.IAsyncResult>(((System.ServiceModel.ICommunicationObject)(this)).EndClose));
        }
        
        private static System.ServiceModel.Channels.Binding GetBindingForEndpoint(EndpointConfiguration endpointConfiguration) {
            if ((endpointConfiguration == EndpointConfiguration.BasicHttpBinding_IUpcSvc)) {
                System.ServiceModel.BasicHttpBinding result = new System.ServiceModel.BasicHttpBinding();
                result.MaxBufferSize = int.MaxValue;
                result.ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max;
                result.MaxReceivedMessageSize = int.MaxValue;
                result.AllowCookies = true;
                return result;
            }
            throw new System.InvalidOperationException(string.Format("Could not find endpoint with name \'{0}\'.", endpointConfiguration));
        }
        
        private static System.ServiceModel.EndpointAddress GetEndpointAddress(EndpointConfiguration endpointConfiguration) {
            if ((endpointConfiguration == EndpointConfiguration.BasicHttpBinding_IUpcSvc)) {
                return new System.ServiceModel.EndpointAddress("http://thetasoft2.azurewebsites.net/UpcSvc/UpcSvc.svc/soap");
            }
            throw new System.InvalidOperationException(string.Format("Could not find endpoint with name \'{0}\'.", endpointConfiguration));
        }
        
        private static System.ServiceModel.Channels.Binding GetDefaultBinding() {
            return UpcSvcClient.GetBindingForEndpoint(EndpointConfiguration.BasicHttpBinding_IUpcSvc);
        }
        
        private static System.ServiceModel.EndpointAddress GetDefaultEndpointAddress() {
            return UpcSvcClient.GetEndpointAddress(EndpointConfiguration.BasicHttpBinding_IUpcSvc);
        }
        
        public enum EndpointConfiguration {
            
            BasicHttpBinding_IUpcSvc,
        }
    }
}
