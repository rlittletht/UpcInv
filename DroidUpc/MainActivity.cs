using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using ZXing.Mobile;
using System.Collections.Generic;
using Android;
using Android.Media;
using Android.Support.V4.Content;
using Android.Views;
using TCore.Logging;
using TCore.StatusBox;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using UpcShared;
using IStatusReporting = UpcShared.IStatusReporting;

namespace DroidUpc
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenLayout)]

    public partial class MainActivity : AppCompatActivity
    {
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
//            FindViewById<Button>(Resource.Id.buttonManual).Click += TestAlert;

            buttonScan.Click += OnScanClick;
        }

        void SetupScannerFragment()
        {
            m_ups = new Scanner(Application);

            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.frameScanner, m_ups.Fragment)
                .Commit();

            MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();

            options.PossibleFormats = new List<ZXing.BarcodeFormat> {ZXing.BarcodeFormat.All_1D};
            options.DelayBetweenContinuousScans = 750;

            options.CameraResolutionSelector = (availableResolutions) =>
            {
                CameraResolution arRet = null;

                foreach (var ar in availableResolutions)
                {
                    Console.WriteLine("Resolution: " + ar.Width + "x" + ar.Height);
                }

                return arRet;
            };
            

            m_ups.SetupScanner(options, true);
            m_frmScanner = FindViewById<FrameLayout>(Resource.Id.frameScanner);
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
            SetFocus(m_ebScanCode, false);
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
                    m_frmScanner.Visibility = ViewStates.Visible;
                    m_ups.StartScanner(ScannerControlDispatchScanCode);
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