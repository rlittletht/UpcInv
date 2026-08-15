using UpcShared;

namespace DroidUpc2;

public delegate void UiThreadDelegate();

public interface IAppContext
{
    string GetTextElementValue(DroidUITextElement element);
    Task SetTextElementValue(DroidUITextElement element, string value, ThreadPolicy policy);
    bool GetCheckboxElementValue(DroidUICheckboxElement element);
    void SetFocusOnTextElement(DroidUITextElement element, bool fWantKeyboard);
    void AdjustUIForMediaType(UpcInvCore.ADAS adas);
    UpcInvCore.ADAS AdasCurrent();

    /*async*/ Task RunOnUiThreadAsync(UiThreadDelegate del);
    void RunOnUiThread(Action action);
}
