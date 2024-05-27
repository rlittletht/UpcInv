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
    private Scanner m_ups;
    private DroidUpc m_droid;
    private UpcLogProvider m_lp;
    private LogProvider m_logProviderActual;
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
    private UpcAlert m_upca;

    private Dictionary<DroidUITextElement, int> m_textElementIds = new Dictionary<DroidUITextElement, int>();
    private Dictionary<DroidUILabelElement, int> m_labelElementIds = new Dictionary<DroidUILabelElement, int>();
    private Dictionary<DroidUICheckboxElement, int> m_checkboxElementIds = new Dictionary<DroidUICheckboxElement, int>();

    private LowestResolutionMatchingAspectRatioSelector? m_resolutionSelector;

    public MainActivity()
    {
        m_logProviderActual = new LogProvider(null);
        m_lp = new UpcLogProvider(m_logProviderActual);
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

        m_frmScanner = GetResource<FrameLayout>(Resource.Id.frameScanner);
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
        m_handlerAlert = new Handler();
        m_upca = new UpcAlert(this, m_handlerAlert);

        TextView tv = GetResource<TextView>(Resource.Id.tvLog);
        StatusBox sb = new StatusBox(tv, m_upca, this);
        m_isr = sb;
        m_droid = new DroidUpc(this, m_upca, m_isr, m_lp);
    }

    void SetupMainViewAndEvents()
    {
        Spinner spinner = GetResource<Spinner>(Resource.Id.spinType);

        spinner.Adapter = new UpcTypeSpinnerAdapter(this);
        spinner.ItemSelected += m_droid.SetNewMediaType;

        Button buttonScan = GetResource<Button>(Resource.Id.buttonScan);
        // can't use our convenient m_pbManual and m_ebScancode because we haven't
        // set those up yet...
        GetResource<Button>(Resource.Id.buttonManual).Click += m_droid.DoManual;

        GetResource<EditText>(Resource.Id.ebCode).KeyPress +=
            async (object? sender, View.KeyEventArgs args) =>
            {
                await DoKeyPressCommon("", sender!, args, null, m_droid.DispatchScanCode, null);
            };

        GetResource<EditText>(Resource.Id.ebBin).KeyPress +=
            async (object? sender, View.KeyEventArgs args) =>
            {
                // We're going to cleverly lie here and pass in the scancode control since
                // enter here means we might be ready to complete the scancode...
                await DoKeyPressCommon("", m_ebScanCode, args, null, m_droid.DispatchScanCode, null);
            };

        GetResource<EditText>(Resource.Id.ebColumn).KeyPress +=
            async (object? sender, View.KeyEventArgs args) =>
            {
                await DoKeyPressCommon(
                    "C_",
                    sender,
                    args,
                    m_droid.ZeroPrefixString,
                    m_droid.DispatchScanCode,
                    () => SetFocus(m_ebRow, true));
            };

        GetResource<EditText>(Resource.Id.ebRow).KeyPress +=
            async (object? sender, View.KeyEventArgs args) =>
            {
                await DoKeyPressCommon(
                    "R_",
                    sender,
                    args,
                    m_droid.ZeroPrefixString,
                    m_droid.DispatchScanCode,
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

        m_ups = new Scanner(Application!);

        MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions();

        options.PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.All_1D };

        m_resolutionSelector = new(this);

        options.CameraResolutionSelector = m_resolutionSelector.SelectLowestResolutionMatchingDisplayAspectRatio;

        options.ScanningArea = ScanningArea.From(0f, 0.49f, 1f, 0.51f);

        m_ups.SetupScanner(options, true);

        FragmentManager?.BeginTransaction()?.Add(Resource.Id.frameScanner, m_ups.Fragment)?.Hide(m_ups.Fragment)?.Commit();
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
        m_isr.AddMessage(AlertType.None, "Checking server for heartbeat...");

        ServiceStatus status = await m_droid.GetServiceStatusHeartBeat();

        if (status == ServiceStatus.Running)
            m_isr.AddMessage(AlertType.None, "Server is running.");
        else
            m_isr.AddMessage(AlertType.Halt, "Server is not running!");
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
                return m_cbCheckOnly;
            default:
                throw new Exception("unknown checkbox element");
        }
    }

    TextView GetTextViewElement(DroidUILabelElement element)
    {
        switch (element)
        {
            case DroidUILabelElement.BinCode:
                return m_txtBinCode;
            case DroidUILabelElement.Row:
                return m_txtRow;
            case DroidUILabelElement.Column:
                return m_txtColumn;
            case DroidUILabelElement.Location:
                return m_txtLocation;
            case DroidUILabelElement.Status:
                return m_txtStatus;
            default:
                throw new Exception("unknown textview element");
        }
    }

    EditText GetEditTextElement(DroidUITextElement element)
    {
        switch (element)
        {
            case DroidUITextElement.Location:
                return m_ebLocation;
            case DroidUITextElement.BinCode:
                return m_ebBinCode;
            case DroidUITextElement.Row:
                return m_ebRow;
            case DroidUITextElement.Column:
                return m_ebColumn;
            case DroidUITextElement.Notes:
                return m_ebNotes;
            case DroidUITextElement.ScanCode:
                return m_ebScanCode;
            case DroidUITextElement.Title:
                return m_ebTitle;
            case DroidUITextElement.TastingNotes:
                return m_ebNotes;
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
                m_ups.StartScanner(m_droid.ScannerControlDispatchScanCode);

                ViewGroup.LayoutParams? newParams = m_resolutionSelector?.GetUpdatedLayoutParametersIfNecessary(m_frmScanner.LayoutParameters);

                if (newParams != null)
                    m_frmScanner.LayoutParameters = newParams;

                FragmentManager.BeginTransaction().Show(m_ups.Fragment).Commit();
                m_isr.AddMessage(AlertType.Drink, "Turning Scanner on");
                m_fScannerOn = true;
            }
        }
        else
        {
            m_ups.StopScanner();
            m_isr.AddMessage(AlertType.Duplicate, "Turning Scanner off");

            FragmentManager.BeginTransaction().Hide(m_ups.Fragment).Commit();
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

    void SetFocus(EditText eb, bool fWantKeyboard)
    {
        RunOnUiThread(
            () =>
            {
                eb.RequestFocus();
                eb.SelectAll();

                InputMethodManager imm =
                    (InputMethodManager)GetSystemService(global::Android.Content.Context.InputMethodService);

                if (fWantKeyboard)
                {
                    imm.ShowSoftInput(eb, ShowFlags.Implicit);
                }
                else
                {
                    imm.HideSoftInputFromWindow(eb.WindowToken, 0);
                }
            });
    }


    /*----------------------------------------------------------------------------
        %%Function: DoKeyPressCommon
        %%Qualified: DroidUpc2.MainActivity.DoKeyPressCommon
    ----------------------------------------------------------------------------*/
    private async Task DoKeyPressCommon(
        string sPrefix,
        object sender,
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

                        m_lp.LogEvent(crid, EventType.Information, "Dispatching ScanCode: {0}", sResultText);

                        await RunOnUiThreadAsync(() => eb.Text = $"{sPrefix}{sResultText}");

                        await dispatch?.Invoke(eb.Text, m_cbCheckOnly.Checked, crid);
                        afterOnEnter?.Invoke();
                    }
                }
            }

            // handle both down and up so we don't get the default behavior...
            eventArgs.Handled = true;
        }
    }

    delegate void ActivityResultDelegate(Intent? intent, Result resultCode);


    private Dictionary<int, ActivityResultDelegate> m_activityDelegates =
        new Dictionary<int, ActivityResultDelegate>();

    void RegisterActivityDelegate(int requestCode, ActivityResultDelegate del)
    {
        if (!m_activityDelegates.ContainsKey(requestCode))
            m_activityDelegates.Add(requestCode, null);

        m_activityDelegates[requestCode] = del;
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        if (m_activityDelegates.ContainsKey(requestCode))
            m_activityDelegates[requestCode](data, resultCode);

        base.OnActivityResult(requestCode, resultCode, data);
    }

    public UpcInvCore.ADAS AdasCurrent()
    {
        Spinner spinner = FindViewById<Spinner>(Resource.Id.spinType);

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
                m_txtBinCode.Visibility = visBin;
                m_txtColumn.Visibility = visBin;
                m_txtRow.Visibility = visBin;
                m_ebBinCode.Visibility = visBin;
                m_ebColumn.Visibility = visBin;
                m_ebRow.Visibility = visBin;

            });
    }

}
