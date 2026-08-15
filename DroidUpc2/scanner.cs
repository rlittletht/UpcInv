
using Android.Util;
using Android.Views;
using ZXing.Mobile;
using Exception = Java.Lang.Exception;

namespace DroidUpc2;

public class Scanner // UPS
{
    private readonly ZXingScannerFragment m_scannerControl;
    private ScanCompleteDelegate? m_onScanComplete;
    public ScanCompleteDelegate OnScanComplete => m_onScanComplete ?? new ScanCompleteDelegate((_) => { });
    private MobileBarcodeScanningOptions _Options => m_options ?? throw new Exception("options not set");
    private MobileBarcodeScanningOptions? m_options;

    public Fragment Fragment => m_scannerControl;
    public ZXingScannerFragment ScannerFragment => m_scannerControl;

    public Scanner(Android.App.Application app)
    {
        m_scannerControl = new ZXingScannerFragment();

    }

    public void StartScanner(ScanCompleteDelegate scd)
    {
        m_onScanComplete = scd;

        m_scannerControl.StartScanning(async (result) =>
                                       {
                                           string msg = "Found Barcode: " + result.Text;

                                           await Task.Run(() => OnScanComplete(result));
                                       }, _Options);
    }

    public void StopScanner()
    {
        m_scannerControl.StopScanning();
        m_onScanComplete = null;
    }

    public delegate void ScanCompleteDelegate(ZXing.Result result);

    public void SetupScanner(MobileBarcodeScanningOptions options, bool fContinuous)
    {
        m_scannerControl.UseCustomOverlayView = false;
        m_scannerControl.TopText = null;
        m_scannerControl.BottomText = null;
        m_scannerControl.ScanningOptions = options ?? new MobileBarcodeScanningOptions();

        m_options = options;
    }
}