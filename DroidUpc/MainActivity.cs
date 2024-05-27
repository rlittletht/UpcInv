using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.AppCompat.App;
using Android.Widget;
using ZXing.Mobile;
using System.Collections.Generic;
using System.Linq;
using Android;
using Android.Media;
using AndroidX.Core.Content;
using Android.Views;
using Com.Microsoft.Appcenter.Ingestion.Models.Json;
using TCore.Logging;
using TCore.StatusBox;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using UpcShared;
using Xamarin.Essentials;
using ActionBar = Android.App.ActionBar;
using EventType = UpcShared.EventType;
using IStatusReporting = UpcShared.IStatusReporting;

namespace DroidUpc
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenLayout)]

    public partial class MainActivity : AppCompatActivity
    {
        private static float s_dipScannerFrameHeight = 250;

        UpcAlert m_upca;
        private UpcInvCore m_upccCore;
        private Scanner m_ups;

        private bool m_fScannerOn;

        private LogProvider m_lpActual;
        private UpcLogProvider m_lp;
        private IStatusReporting m_isr;
        private EditText m_ebTitle;
        private EditText m_ebScanCode;
        private EditText m_ebNotes;
        private EditText m_ebLocation;
        private CheckBox m_cbCheckOnly;
        private TextView m_txtLocation;
        private TextView m_txtBinCode;
        private TextView m_txtRow;
        private TextView m_txtColumn;
        private EditText m_ebBinCode;
        private EditText m_ebRow;
        private EditText m_ebColumn;
        private TextView m_txtStatus;
        private FrameLayout m_frmScanner;
        private Handler m_handlerAlert;

        void SetupMainViewAndEvents()
        {
            SetContentView(Resource.Layout.activity_main);
            Spinner spinner = FindViewById<Spinner>(Resource.Id.spinType);

            spinner.Adapter = new UpcTypeSpinnerAdapter(this);
            spinner.ItemSelected += SetNewMediaType;

            Button buttonScan = FindViewById<Button>(Resource.Id.buttonScan);
            // can't use our convenient m_pbManual and m_ebScancode because we haven't
            // set those up yet...
            FindViewById<Button>(Resource.Id.buttonManual).Click += DoManual;
            FindViewById<EditText>(Resource.Id.ebCode).KeyPress += DoCodeKeyPress;
            
            FindViewById<EditText>(Resource.Id.ebBin).KeyPress += DoBinCodeKeyPress;
            FindViewById<EditText>(Resource.Id.ebColumn).KeyPress += DoColumnCodeKeyPress;
            FindViewById<EditText>(Resource.Id.ebRow).KeyPress += DoRowCodeKeyPress;

            buttonScan.Click += OnScanClick;
        }

        // we need to remember the last resolution we requested for the preview -- we
        // will use this later to resize the scanner control so we can get the
        // right aspect ration on the control
        private CameraResolution lastResolutionSet;

        public int convertDipToPixels(float dips)
        {
            return (int)(dips * DeviceDisplay.MainDisplayInfo.Density  + 0.5f);
        }

        public CameraResolution SelectLowestResolutionMatchingDisplayAspectRatio(
	        List<CameraResolution> availableResolutions)
        {
            frameWidth = DeviceDisplay.MainDisplayInfo.Width;
            frameHeight = convertDipToPixels(s_dipScannerFrameHeight);

	        CameraResolution result = null;
	        //a tolerance of 0.1 should not be visible to the user
	        double aspectTolerance = 0.1;

#if UseFullScreen
            double mainDisplayWidth = DeviceDisplay.MainDisplayInfo.Width;
            double mainDisplayHeight = DeviceDisplay.MainDisplayInfo.Height;
            double displayOrientationHeight = DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait
		        ? mainDisplayHeight
                : mainDisplayWidth;
	        double displayOrientationWidth = DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait
		        ? mainDisplayWidth
                : mainDisplayHeight;

            Android.Util.Log.Debug(MobileBarcodeScanner.TAG, $"frameWidth: {frameWidth}, frameHeight: {frameHeight}");
#else
            double displayOrientationWidth = frameWidth;
            double displayOrientationHeight = frameHeight;
#endif

            double targetRatio = displayOrientationHeight / displayOrientationWidth;
	        double targetHeight = displayOrientationHeight;
	        double minDiff = double.MaxValue;

#if DEBUGDISPLAY
            Android.Util.Log.Debug(MobileBarcodeScanner.TAG, $"width: {mainDisplayWidth}, height: {mainDisplayHeight}, orientation: {DeviceDisplay.MainDisplayInfo.Orientation}, targetRatio: {targetRatio}");
            
            foreach (CameraResolution r in availableResolutions)
            {
                Android.Util.Log.Debug(MobileBarcodeScanner.TAG, $"available resolution: {r.Width} x {r.Height}");
            }
#endif
            //camera API lists all available resolutions from highest to lowest, perfect for us
            //making use of this sorting, following code runs some comparisons to select the lowest
            //resolution that matches the screen aspect ratio and lies within tolerance
            //selecting the lowest makes Qr detection actual faster most of the time
            // (make sure we at least get 600 pixels on the width)
            foreach (CameraResolution rActual in availableResolutions)
            {
                CameraResolution r = new CameraResolution();

                // compensate for phone resolutions being flipped...
                if (rActual.Width > rActual.Height)
                {
                    r.Width = rActual.Height;
                    r.Height = rActual.Width;
                }
                else
                {
                    r.Height = rActual.Height;
                    r.Width = rActual.Width;
                }

                double ratio = ((double)r.Height / r.Width);

#if DEBUGDISPLAY
                Android.Util.Log.Debug(MobileBarcodeScanner.TAG, $"checking {r.Width} x {r.Height} ({ratio})");
#endif
                if ((Math.Abs(ratio - targetRatio) < aspectTolerance) && r.Width > 600)
                {

                    //slowly going down the list to the lowest matching solution with the correct aspect ratio
                    if (Math.Abs(r.Height - targetHeight) < minDiff)
                        minDiff = Math.Abs(r.Height - targetHeight);
                    result = rActual;
                }
            }

#if DEBUGDISPLAY
            Android.Util.Log.Debug(MobileBarcodeScanner.TAG, $"result: {result}");
#endif
            if (result == null)
	        {
		        var smallestDiff = availableResolutions.OrderBy(s =>
		        {
			        var ratio = DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait
				        ? (double) s.Width / s.Height
				        : (double) s.Height / s.Width;
			        return Math.Abs(
				        ratio - targetRatio);

		        }).FirstOrDefault();

#if DEBUGDISPLAY
                Android.Util.Log.Debug(MobileBarcodeScanner.TAG, $"result null, smallestDiff: {smallestDiff.Width} x {smallestDiff.Height}");
#endif
                result = new CameraResolution()
		        {
			        Width = smallestDiff.Width,
			        Height = smallestDiff.Height
		        };
	        }

	        lastResolutionSet = new CameraResolution();
	        lastResolutionSet.Width = result.Width;
	        lastResolutionSet.Height = result.Height;

			return result;
        }

        private double frameWidth;
        private double frameHeight;

        void SetupScannerFragment()
        {
            m_ups = new Scanner(Application);

            MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();

            options.PossibleFormats = new List<ZXing.BarcodeFormat> {ZXing.BarcodeFormat.All_1D};

            options.CameraResolutionSelector = new MobileBarcodeScanningOptions.CameraResolutionSelectorDelegate(SelectLowestResolutionMatchingDisplayAspectRatio);

            m_frmScanner = FindViewById<FrameLayout>(Resource.Id.frameScanner);
            options.ScanningArea = ScanningArea.From(0f, 0.49f, 1f, 0.51f);

            m_ups.SetupScanner(options, true);

            SupportFragmentManager.BeginTransaction().Add(Resource.Id.frameScanner, m_ups.Fragment).Commit();

            m_frmScanner.Visibility = ViewStates.Gone;
        }

        void InitializeApplication()
        {
            m_handlerAlert = new Handler();
            m_upca = new UpcAlert(this, m_handlerAlert);

            StatusBox sb = new StatusBox();
            TextView tv = FindViewById<TextView>(Resource.Id.tvLog);
            sb.Initialize(tv, m_upca, this);

            m_isr = sb;
            m_lpActual = new LogProvider(null);
            m_lp = new UpcLogProvider(m_lpActual);
            m_upccCore = new UpcInvCore(m_upca, m_isr, m_lp);
            m_plsProcessing = new List<string>();

            m_txtLocation = FindViewById<TextView>(Resource.Id.tvLocationLabel);
            m_ebNotes = FindViewById<EditText>(Resource.Id.ebTastingNotes);
            m_ebTitle = FindViewById<EditText>(Resource.Id.ebTitle);
            m_ebScanCode = FindViewById<EditText>(Resource.Id.ebCode);
            m_ebLocation = FindViewById<EditText>(Resource.Id.ebLocation);
            m_txtStatus = FindViewById<TextView>(Resource.Id.tvStatus);
            m_cbCheckOnly = FindViewById<CheckBox>(Resource.Id.cbCheckOnly);
            m_txtBinCode = FindViewById<TextView>(Resource.Id.tvBinLabel);
            m_txtRow = FindViewById<TextView>(Resource.Id.tvRowLabel);
            m_txtColumn = FindViewById<TextView>(Resource.Id.tvColumnLabel);
            m_ebBinCode = FindViewById<EditText>(Resource.Id.ebBin);
            m_ebRow = FindViewById<EditText>(Resource.Id.ebRow);
            m_ebColumn = FindViewById<EditText>(Resource.Id.ebColumn);
        }

        /*----------------------------------------------------------------------------
        	%%Function: OnCreate
        	%%Qualified: DroidUpc.MainActivity.OnCreate
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            AppCenter.Start("cb8b70c7-63a1-4124-b62c-ea09f6271367",
                typeof(Analytics), typeof(Crashes));

            SetupMainViewAndEvents();

            VolumeControlStream = Stream.Music;

            SetupScannerFragment();
            InitializeApplication();
            DoServiceHeartbeat();

            // SetFocus(m_ebScanCode, false);
        }

        protected override void OnResume()
        {
            base.OnResume();
            // HockeyApp.Android.CrashManager.Register(this, "cb8b70c763a14124b62cea09f6271367");
        }

        private int iAlert = 0;
        
        void TestAlert(object sender, EventArgs e)
        {
            AlertType[] rgat =
            {
                AlertType.GoodInfo,
                AlertType.BadInfo,
                AlertType.Halt,
                AlertType.Duplicate,
                AlertType.Drink,
                AlertType.UPCScanBeep
            };

            if (++iAlert >= rgat.Length)
                iAlert = 0;

            m_upca.Play(rgat[iAlert]);
        }

        async void DoServiceHeartbeat()
        {
            m_isr.AddMessage(AlertType.None, "Checking server for heartbeat...");

            ServiceStatus status = await m_upccCore.GetServiceStatusHeartBeat();

            if (status == ServiceStatus.Running)
                m_isr.AddMessage(AlertType.None, "Server is running.");
            else
                m_isr.AddMessage(AlertType.Halt, "Server is not running!");
        }

        void OnScanClick(object sender, EventArgs e)
        {
            if (!m_fScannerOn)
            {
                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != Permission.Granted)
                    RequestPermissions(new string[] { Manifest.Permission.Camera}, 0);
                else
                {
                    // start scanner here so we can get lastResolutionSet populated
                    m_ups.StartScanner(ScannerControlDispatchScanCode);

	                if (lastResolutionSet != null)
	                {
		                float ratio = (float)lastResolutionSet.Width / (float)lastResolutionSet.Height;
		                float newWidth = (float)m_frmScanner.LayoutParameters.Height * ratio;

		                m_frmScanner.LayoutParameters =
			                new LinearLayout.LayoutParams((int)newWidth, m_frmScanner.LayoutParameters.Height);
	                }

                    m_frmScanner.Visibility = ViewStates.Visible;
                    m_isr.AddMessage(AlertType.None, "Turning Scanner on");
                    m_fScannerOn = true;
                }
            }
            else
            {
	            m_ups.StopScanner();
	            m_isr.AddMessage(AlertType.None, "Turning Scanner off");
	            m_frmScanner.Visibility = ViewStates.Gone;
	            m_fScannerOn = false;
            }
        }
    }
}