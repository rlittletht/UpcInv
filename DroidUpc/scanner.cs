﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using Xamarin.Essentials;
using ZXing.Mobile;

namespace DroidUpc
{
    public class Scanner // UPS
    {
        private ZXingScannerFragment m_scannerControl;

        public Fragment Fragment => m_scannerControl;
        public ZXingScannerFragment ScannerFragment => m_scannerControl;

        public Scanner(Android.App.Application app)
        {
//            MobileBarcodeScanner.Initialize(app);
            // m_scanner = new MobileBarcodeScanner();
//            m_scanner.Dispatcher = dispatcher; // this is what they did in the sample -- don't know why its necessary...
            m_scannerControl = new ZXingScannerFragment();
        }

        public void StartScanner(ScanCompleteDelegate scd)
        {
            m_scdNotify = scd;

            m_scannerControl.StartScanning(
                async (result) =>
                {
                    var msg = "Found Barcode: " + result.Text;

                    await Task.Run(() => m_scdNotify(result));
                },
                m_options);
        }

        public void StopScanner()
        {
            m_scannerControl.StopScanning();
        }

        public ScanCompleteDelegate m_scdNotify;

        public delegate void ScanCompleteDelegate(ZXing.Result result);

        public void SetupScanner(MobileBarcodeScanningOptions options, bool fContinuous)
        {
            m_scannerControl.UseCustomOverlayView = false;
            m_scannerControl.TopText = null;
            m_scannerControl.BottomText = null;
            m_scannerControl.ScanningOptions = options ?? new MobileBarcodeScanningOptions();
            // m_scannerControl.ContinuousScanning = fContinuous;
            m_options = options;
        }

        private MobileBarcodeScanningOptions m_options;
    }
}
