using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using ZXing.Mobile;

namespace UniversalUpc
{
    public class Scanner // UPS
    {
        MobileBarcodeScanner m_scanner;
        private ZXingScannerControl m_scannerControl;

        public Scanner(CoreDispatcher dispatcher, ZXingScannerControl scannerControl)
        {
            m_scanner = new MobileBarcodeScanner(dispatcher);
            m_scanner.Dispatcher = dispatcher; // this is what they did in the sample -- don't know why its necessary...
            m_scannerControl = scannerControl;
        }

        public void StartScanner(ScanCompleteDelegate scd)
        {
            m_scdNotify = scd;
            m_scannerControl.StartScanning(async (result) =>
                {
                var msg = "Found Barcode: " + result.Text;

                await m_scanner.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                    m_scdNotify(result);
                    });
                });
        }

        public void StopScanner()
        {
            m_scannerControl.StopScanning();
        }

        public ScanCompleteDelegate m_scdNotify;

        public delegate void ScanCompleteDelegate(ZXing.Result result);

        public void SetupScanner(MobileBarcodeScanningOptions options, bool fContinuous)
        {
            m_scannerControl.UseCustomOverlay = false;
            m_scannerControl.TopText = "top text";
            m_scannerControl.BottomText = "bottom text";
            m_scannerControl.ScanningOptions = options ?? new MobileBarcodeScanningOptions();
            m_scannerControl.ContinuousScanning = fContinuous;            
        }
    }
}
