using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Media;
using Android.OS;
using Android.Test;
using Java.Lang;
using UpcShared;
using Exception = Java.Lang.Exception;

#pragma warning disable 1998

namespace DroidUpc
{
    public class UpcAlert : IAlert
    {
        private readonly SoundPool? m_sp;
        private readonly Activity m_act;
        private Handler m_handler;

        private readonly Dictionary<AlertType, int> m_mpAlertMedia = new Dictionary<AlertType, int>();

        private async Task<int> LoadSoundFile(SoundPool sp, string v, AssetManager? assets = null)
        {
#if debugging_assets
            string[] rgs = global::Android.App.Application.Context.Assets.List("");
            string[] rgs2 = global::Android.App.Application.Context.Assets.List("Audio");
            string[] rgs3 = global::Android.App.Application.Context.Assets.List("images");

            if (assets != null)
            {
                string[] args = assets.List("");
                string[] args2 = assets.List("Audio");
                string[] args3 = assets.List("images");
            }
#endif
            AssetFileDescriptor? afd =
                global::Android.App.Application.Context.Assets?.OpenFd(v) ?? throw new Exception("couldn't load audio assets");

            int soundId = sp.Load(afd, 1);
            return soundId;
        }

        async Task<SoundPool> LoadEffects(AssetManager assets)
        {
            SoundPool sp = new SoundPool.Builder()?.SetMaxStreams(2)?.Build() ?? throw new Exception("Failed to create SoundPool");

            m_mpAlertMedia.Add(AlertType.GoodInfo, await LoadSoundFile(sp, "exclamation.wav", assets));
            m_mpAlertMedia.Add(AlertType.Duplicate, await LoadSoundFile(sp, "doh.wav"));
            m_mpAlertMedia.Add(AlertType.Drink, await LoadSoundFile(sp, "hicup_392.wav"));
            m_mpAlertMedia.Add(AlertType.BadInfo, await LoadSoundFile(sp, "ding.wav"));
            m_mpAlertMedia.Add(AlertType.Halt, await LoadSoundFile(sp, "ding.wav"));
            m_mpAlertMedia.Add(AlertType.UPCScanBeep, await LoadSoundFile(sp, "263133__pan14__tone-beep.wav"));

            return sp;
        }

        public UpcAlert(Activity act, Handler handler)
        {
            m_act = act;
            m_handler = handler;

            if (act.Assets != null)
            {
                Task<SoundPool> tsp = LoadEffects(act.Assets);

                tsp.Wait();
                m_sp = tsp.Result;
            }
        }

        public void DoAlert(AlertType at)
        {
            Play(at);
        }

        public void Play(AlertType at)
        {
            if (at == AlertType.None)
                return;

//            m_handler.Post(() =>
//            {
            AudioManager? audioManager = m_act.GetSystemService(global::Android.Content.Context.AudioService) as AudioManager;

            if (audioManager == null)
                throw new Exception("could not get AudioManager");

            float actualVolume = (float)audioManager.GetStreamVolume(Android.Media.Stream.Music);
            float maxVolume = (float)audioManager.GetStreamMaxVolume(Android.Media.Stream.Music);
            float volume = actualVolume / maxVolume;

            m_sp?.Play(m_mpAlertMedia[at], volume, volume, 1, 0, 1f);
//            });
        }
    }
}
