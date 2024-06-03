using Android.Content.PM;
using System.Runtime.CompilerServices;
using Android.OS;
using Android.Views;
using DroidUpc;
using TCore.Logging;
using TCore.StatusBox;
using Exception = Java.Lang.Exception;
using EventType = UpcShared.EventType;
using UpcShared;
using Android.Views.InputMethods;
using System.Data.Common;
using Android.Content;
using Android.Runtime;
using Android.Util;
using ZXing.Mobile;

namespace DroidUpc2;

/*----------------------------------------------------------------------------
    %%Class: MainActivity
    %%Qualified: DroidUpc2.MainActivity

    Our main activity
----------------------------------------------------------------------------*/
[Activity(
    Label = "@string/app_name",
    MainLauncher = true,
    Theme = "@style/AppTheme",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenLayout)]
public class MainActivity : Activity, IAppContext
{
    private bool m_fScannerOn = false;
    private Scanner Scanner => m_scanner ?? throw new Exception("Scanner not created");
    private Scanner? m_scanner;

    private DroidUpc _Droid => m_droid ?? throw new Exception("_Droid used before initialized");
    private DroidUpc? m_droid;

    private UpcLogProvider _LogProvider => m_logProvider ?? throw new Exception("_LogProvider used before initialized");
    private readonly UpcLogProvider? m_logProvider;

    private IStatusReporting _StatusReporting => m_statusReporting ?? throw new Exception("_StatusReporting used before initialized");
    private IStatusReporting? m_statusReporting;

    private EditText? m_ebTitle;
    private EditText? m_ebScanCode;
    private EditText? m_ebNotes;
    private EditText? m_ebLocation;
    private CheckBox? m_cbCheckOnly;
    private TextView? m_txtLocation;
    private TextView? m_txtBinCode;
    private TextView? m_txtRow;
    private TextView? m_txtColumn;
    private EditText? m_ebBinCode;
    private EditText? m_ebRow;
    private EditText? m_ebColumn;
    private TextView? m_txtStatus;

    private Handler _AlertHandler => m_alertHandler ?? throw new Exception("_AlertHandler used before initialized");
    private Handler? m_alertHandler;

    private UpcAlert _UpcAlert => m_upcAlert ?? throw new Exception("_UpcAlert used before initialized");
    private UpcAlert? m_upcAlert;

    private Dictionary<DroidUITextElement, int> m_textElementIds = new Dictionary<DroidUITextElement, int>();
    private Dictionary<DroidUILabelElement, int> m_labelElementIds = new Dictionary<DroidUILabelElement, int>();
    private Dictionary<DroidUICheckboxElement, int> m_checkboxElementIds = new Dictionary<DroidUICheckboxElement, int>();

    private LowestResolutionMatchingAspectRatioSelector? m_resolutionSelector;

    public MainActivity()
    {
        LogProvider? mLogProviderActual = new(null);
        m_logProvider = new UpcLogProvider(mLogProviderActual);
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Set our view from the "main" layout resource
        VolumeControlStream = Android.Media.Stream.Music;
        SetContentView(Resource.Layout.activity_main);

        InitializeOnCreate();
        SetupMainViewAndEvents();

        DoServiceHeartbeat();
    }

    #region Initialization

    public void InitializeOnCreate()
    {
        SetupElementMaps();
        SetupScannerFragment();

        m_txtLocation = GetResource<TextView>(Resource.Id.tvLocationLabel);
        m_ebNotes = GetResource<EditText>(Resource.Id.ebTastingNotes);
        m_ebTitle = GetResource<EditText>(Resource.Id.ebTitle);
        m_ebScanCode = GetResource<EditText>(Resource.Id.ebCode);
        m_ebLocation = GetResource<EditText>(Resource.Id.ebLocation);
        m_txtStatus = GetResource<TextView>(Resource.Id.tvStatus);
        m_cbCheckOnly = GetResource<CheckBox>(Resource.Id.cbCheckOnly);
        m_txtBinCode = GetResource<TextView>(Resource.Id.tvBinLabel);
        m_txtRow = GetResource<TextView>(Resource.Id.tvRowLabel);
        m_txtColumn = GetResource<TextView>(Resource.Id.tvColumnLabel);
        m_ebBinCode = GetResource<EditText>(Resource.Id.ebBin);
        m_ebRow = GetResource<EditText>(Resource.Id.ebRow);
        m_ebColumn = GetResource<EditText>(Resource.Id.ebColumn);
        m_alertHandler = new Handler();
        m_upcAlert = new UpcAlert(this, _AlertHandler);

        TextView tv = GetResource<TextView>(Resource.Id.tvLog);
        StatusBox sb = new StatusBox(tv, _UpcAlert, this);
        m_statusReporting = sb;
        m_droid = new DroidUpc(this, _UpcAlert, _StatusReporting, _LogProvider);
    }

    void SetupMainViewAndEvents()
    {
        Spinner spinner = GetResource<Spinner>(Resource.Id.spinType);

        spinner.Adapter = new UpcTypeSpinnerAdapter(this);
        spinner.ItemSelected += _Droid.SetNewMediaType;

        Button buttonScan = GetResource<Button>(Resource.Id.buttonScan);
        // can't use our convenient m_pbManual and m_ebScancode because we haven't
        // set those up yet...
        GetResource<Button>(Resource.Id.buttonManual).Click += _Droid.DoManual;

        GetResource<EditText>(Resource.Id.ebCode).KeyPress +=
            async (object? sender, View.KeyEventArgs args) =>
            {
                await DoKeyPressCommon("", sender!, args, null, _Droid.DispatchScanCode, null);
            };

        GetResource<EditText>(Resource.Id.ebBin).KeyPress +=
            async (object? sender, View.KeyEventArgs args) =>
            {
                // We're going to cleverly lie here and pass in the scancode control since
                // enter here means we might be ready to complete the scancode...
                await DoKeyPressCommon("", m_ebScanCode, args, null, _Droid.DispatchScanCode, null);
            };

        GetResource<EditText>(Resource.Id.ebColumn).KeyPress +=
            async (object? sender, View.KeyEventArgs args) =>
            {
                await DoKeyPressCommon(
                    "C_",
                    sender,
                    args,
                    _Droid.ZeroPrefixString,
                    _Droid.DispatchScanCode,
                    () => SetFocus(m_ebRow, true));
            };

        GetResource<EditText>(Resource.Id.ebRow).KeyPress +=
            async (object? sender, View.KeyEventArgs args) =>
            {
                await DoKeyPressCommon(
                    "R_",
                    sender,
                    args,
                    _Droid.ZeroPrefixString,
                    _Droid.DispatchScanCode,
                    () => SetFocus(m_ebBinCode, true));
            };


        buttonScan.Click += OnScanClick;
    }

    void EnsurePermissions(string[] permissions)
    {
        foreach (string permission in permissions)
        {
            Permission perm = ApplicationContext!.CheckSelfPermission(permission);

            if (perm != Permission.Granted)
            {
                RequestPermissions(permissions, 0);
                return;
            }
        }
    }

    private void SetupScannerFragment()
    {
        EnsurePermissions(new string[] { Android.Manifest.Permission.Camera });

        m_scanner = new Scanner(Application!);

        MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();

        options.PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.All_1D };

        m_resolutionSelector = new(this);

        options.CameraResolutionSelector = m_resolutionSelector.SelectLowestResolutionMatchingDisplayAspectRatio;

        options.ScanningArea = ScanningArea.From(0f, 0.49f, 1f, 0.51f);

        Scanner.SetupScanner(options, true);

        FragmentManager?.BeginTransaction()?.Add(Resource.Id.frameScanner, Scanner.Fragment)?.Hide(Scanner.Fragment)?.Commit();
    }

    private void SetupElementMaps()
    {
        m_textElementIds[DroidUITextElement.TastingNotes] = Resource.Id.ebTastingNotes;
        m_textElementIds[DroidUITextElement.Title] = Resource.Id.ebTitle;
        m_textElementIds[DroidUITextElement.ScanCode] = Resource.Id.ebCode;
        m_textElementIds[DroidUITextElement.Location] = Resource.Id.ebLocation;
        m_textElementIds[DroidUITextElement.BinCode] = Resource.Id.ebBin;
        m_textElementIds[DroidUITextElement.Row] = Resource.Id.ebRow;
        m_textElementIds[DroidUITextElement.Column] = Resource.Id.ebColumn;

        m_checkboxElementIds[DroidUICheckboxElement.CheckOnly] = Resource.Id.cbCheckOnly;

        m_labelElementIds[DroidUILabelElement.Location] = Resource.Id.tvLocationLabel;
        m_labelElementIds[DroidUILabelElement.Status] = Resource.Id.tvStatus;
        m_labelElementIds[DroidUILabelElement.BinCode] = Resource.Id.tvBinLabel;
        m_labelElementIds[DroidUILabelElement.Row] = Resource.Id.tvRowLabel;
        m_labelElementIds[DroidUILabelElement.Column] = Resource.Id.tvColumnLabel;
    }

    #endregion

    async void DoServiceHeartbeat()
    {
        _StatusReporting.AddMessage(AlertType.None, "Checking server for heartbeat...");

        ServiceStatus status = await _Droid.GetServiceStatusHeartBeat();

        if (status == ServiceStatus.Running)
            _StatusReporting.AddMessage(AlertType.None, "Server is running.");
        else
            _StatusReporting.AddMessage(AlertType.Halt, "Server is not running!");
    }

    internal T GetResource<T>(int resourceId, [CallerMemberName] string? resourceName = null) where T : Android.Views.View
    {
        return FindViewById<T>(resourceId) ?? throw new Exception($"could not load resource {resourceName}");
    }

    CheckBox GetCheckboxElement(DroidUICheckboxElement element)
    {
        switch (element)
        {
            case DroidUICheckboxElement.CheckOnly:
                return m_cbCheckOnly ?? throw new Exception("trying to checkbox before initialized");
            default:
                throw new Exception("unknown checkbox element");
        }
    }

    TextView GetTextViewElement(DroidUILabelElement element)
    {
        switch (element)
        {
            case DroidUILabelElement.BinCode:
                return m_txtBinCode ?? throw new Exception("trying to get text view before initialized");
            case DroidUILabelElement.Row:
                return m_txtRow ?? throw new Exception("trying to get text view before initialized");
            case DroidUILabelElement.Column:
                return m_txtColumn ?? throw new Exception("trying to get text view before initialized");
            case DroidUILabelElement.Location:
                return m_txtLocation ?? throw new Exception("trying to get text view before initialized");
            case DroidUILabelElement.Status:
                return m_txtStatus ?? throw new Exception("trying to get text view before initialized");
            default:
                throw new Exception("unknown textview element");
        }
    }

    EditText GetEditTextElement(DroidUITextElement element)
    {
        switch (element)
        {
            case DroidUITextElement.Location:
                return m_ebLocation ?? throw new Exception("trying to get text element before initialized");
            case DroidUITextElement.BinCode:
                return m_ebBinCode ?? throw new Exception("trying to get text element before initialized");
            case DroidUITextElement.Row:
                return m_ebRow ?? throw new Exception("trying to get text element before initialized");
            case DroidUITextElement.Column:
                return m_ebColumn ?? throw new Exception("trying to get text element before initialized");
            case DroidUITextElement.Notes:
                return m_ebNotes ?? throw new Exception("trying to get text element before initialized");
            case DroidUITextElement.ScanCode:
                return m_ebScanCode ?? throw new Exception("trying to get text element before initialized");
            case DroidUITextElement.Title:
                return m_ebTitle ?? throw new Exception("trying to get text element before initialized");
            case DroidUITextElement.TastingNotes:
                return m_ebNotes ?? throw new Exception("trying to get text element before initialized");
            default:
                throw new Exception("unknown edittextelement");
        }
    }

    public string GetTextElementValue(DroidUITextElement element)
    {
        return GetEditTextElement(element).Text ?? "";
    }

    public async Task SetTextElementValue(DroidUITextElement element, string value, ThreadPolicy policy)
    {
        EditText text = GetEditTextElement(element);

        if (policy == ThreadPolicy.Async)
            await RunOnUiThreadAsync(() => text.Text = value);
        else
            RunOnUiThread(() => text.Text = value);
    }

    public bool GetCheckboxElementValue(DroidUICheckboxElement element)
    {
        return GetCheckboxElement(element).Checked;
    }

    public void SetFocusOnTextElement(DroidUITextElement element, bool fWantKeyboard)
    {
        EditText text = GetEditTextElement(element);

        SetFocus(text, fWantKeyboard);
    }


    void OnScanClick(object? sender, EventArgs e)
    {
        if (!m_fScannerOn)
        {
            Permission perm = ApplicationContext!.CheckSelfPermission(Android.Manifest.Permission.Camera);

            if (perm != Permission.Granted)
            {
                RequestPermissions(new string[] { Android.Manifest.Permission.Camera }, 0);
            }
            else
            {
                // start scanner here so we can get lastResolutionSet populated
                Scanner.StartScanner(_Droid.ScannerControlDispatchScanCode);

//                ViewGroup.LayoutParams? newParams = m_resolutionSelector?.GetUpdatedLayoutParametersIfNecessary(_ScannerFrameLayout.LayoutParameters);
//
//                if (newParams != null)
//                    _ScannerFrameLayout.LayoutParameters = newParams;

                FragmentManager?.BeginTransaction()?.Show(Scanner.Fragment)?.Commit();
                _StatusReporting.AddMessage(AlertType.None, "Turning Scanner on");
                m_fScannerOn = true;
            }
        }
        else
        {
            Scanner.StopScanner();
            _StatusReporting.AddMessage(AlertType.None, "Turning Scanner off");

            FragmentManager?.BeginTransaction()?.Hide(Scanner.Fragment)?.Commit();
            m_fScannerOn = false;
        }
    }

    /*----------------------------------------------------------------------------
        %%Function: RunOnUiThreadAsync
        %%Qualified: DroidUpc2.MainActivity.RunOnUiThreadAsync
    ----------------------------------------------------------------------------*/
    public async Task RunOnUiThreadAsync(UiThreadDelegate del)
    {
        ManualResetEventSlim mres = new ManualResetEventSlim(false);

        RunOnUiThread(
            () =>
            {
                del();
                mres.Set();
            });

        // wait on a background thread. this way caller can choose to await it,
        // or just let it run...
        await Task.Run(() => mres.Wait());
    }

    void SetFocus(EditText? eb, bool fWantKeyboard)
    {
        if (eb == null)
            return;

        RunOnUiThread(
            () =>
            {
                eb.RequestFocus();
                eb.SelectAll();

                InputMethodManager? imm =
                    (InputMethodManager?)GetSystemService(global::Android.Content.Context.InputMethodService);

                if (fWantKeyboard)
                {
                    imm?.ShowSoftInput(eb, ShowFlags.Implicit);
                }
                else
                {
                    imm?.HideSoftInputFromWindow(eb.WindowToken, 0);
                }
            });
    }


    /*----------------------------------------------------------------------------
        %%Function: DoKeyPressCommon
        %%Qualified: DroidUpc2.MainActivity.DoKeyPressCommon
    ----------------------------------------------------------------------------*/
    private async Task DoKeyPressCommon(
        string sPrefix,
        object? sender,
        View.KeyEventArgs eventArgs,
        DroidUpc.AdjustTextDelegate? adjustText,
        DroidUpc.DispatchDelegate? dispatch,
        DroidUpc.AfterOnEnterDelegate? afterOnEnter)
    {
        eventArgs.Handled = false;

        if (eventArgs.KeyCode == Keycode.Enter)
        {
            if (eventArgs.Event?.Action == KeyEventActions.Up)
            {
                if (sender is EditText eb)
                {
                    string? sResultText = eb.Text;

                    if (sResultText != null)
                    {
                        if (adjustText != null)
                            sResultText = adjustText(sResultText);

                        CorrelationID crid = new CorrelationID();

                        _LogProvider.LogEvent(crid, EventType.Information, "Dispatching ScanCode: {0}", sResultText);

                        await RunOnUiThreadAsync(() => eb.Text = $"{sPrefix}{sResultText}");

                        if (dispatch != null)
                            await dispatch.Invoke(eb.Text ?? "", m_cbCheckOnly?.Checked ?? false, crid);

                        afterOnEnter?.Invoke();
                    }
                }
            }

            // handle both down and up so we don't get the default behavior...
            eventArgs.Handled = true;
        }
    }

    delegate void ActivityResultDelegate(Intent? intent, Result resultCode);


    private readonly Dictionary<int, ActivityResultDelegate?> m_activityDelegates = new();

    void RegisterActivityDelegate(int requestCode, ActivityResultDelegate del)
    {
        m_activityDelegates.TryAdd(requestCode, null);
        m_activityDelegates[requestCode] = del;
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        if (m_activityDelegates.TryGetValue(requestCode, out ActivityResultDelegate? _delegate))
            _delegate?.Invoke(data, resultCode);

        base.OnActivityResult(requestCode, resultCode, data);
    }

    public UpcInvCore.ADAS AdasCurrent()
    {
        Spinner spinner = FindViewById<Spinner>(Resource.Id.spinType) ?? throw new Exception("could not get spinner control");

        return (UpcInvCore.ADAS)spinner.SelectedItemPosition;
    }

    public void AdjustUIForMediaType(UpcInvCore.ADAS adas)
    {
        if (m_txtLocation == null || m_ebLocation == null || m_ebNotes == null)
            return;

        RunOnUiThread(
            () =>
            {
                ViewStates visLocation = ViewStates.Gone;
                ViewStates visNotes = ViewStates.Gone;
                ViewStates visBin = ViewStates.Gone;

                switch (adas)
                {
                    case UpcInvCore.ADAS.Book:
                        visLocation = ViewStates.Visible;
                        break;
                    case UpcInvCore.ADAS.Wine:
                        visNotes = ViewStates.Visible;
                        break;
                    case UpcInvCore.ADAS.WineRack:
                        visBin = ViewStates.Visible;
                        break;
                    case UpcInvCore.ADAS.DVD:
                        break;
                    case UpcInvCore.ADAS.Generic:
                        break;
                }

                m_txtLocation.Visibility = visLocation;
                m_ebLocation.Visibility = visLocation;
                m_ebNotes.Visibility = visNotes;
                m_txtBinCode!.Visibility = visBin;
                m_txtColumn!.Visibility = visBin;
                m_txtRow!.Visibility = visBin;
                m_ebBinCode!.Visibility = visBin;
                m_ebColumn!.Visibility = visBin;
                m_ebRow!.Visibility = visBin;

            });
    }

}
