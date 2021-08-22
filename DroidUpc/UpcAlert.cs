using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Media;
using Android.OS;
using AndroidX.AppCompat.App;
using Android.Test;
using Java.Lang;
using UpcShared;

#pragma warning disable 1998

namespace DroidUpc
{
    public class UpcAlert : IAlert
    {
        private SoundPool m_sp;
        private AppCompatActivity m_act;
        private Handler m_handler;

        private Dictionary<AlertType, int> m_mpAlertMedia;

        private async Task<int> LoadSoundFile(SoundPool sp, string v, AssetManager assets = null)
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
            AssetFileDescriptor afd = global::Android.App.Application.Context.Assets.OpenFd(v);

            int soundId = sp.Load(afd, 1);
            return soundId;
        }

        async Task<SoundPool> LoadEffects(AssetManager assets)
        {
            SoundPool sp = new SoundPool.Builder().SetMaxStreams(2).Build();

            m_mpAlertMedia.Add(AlertType.GoodInfo, await LoadSoundFile(sp, "exclamation.wav", assets));
            m_mpAlertMedia.Add(AlertType.Duplicate, await LoadSoundFile(sp, "doh.wav"));
            m_mpAlertMedia.Add(AlertType.Drink, await LoadSoundFile(sp, "hicup_392.wav"));
            m_mpAlertMedia.Add(AlertType.BadInfo, await LoadSoundFile(sp, "ding.wav"));
            m_mpAlertMedia.Add(AlertType.Halt, await LoadSoundFile(sp, "ding.wav"));
            m_mpAlertMedia.Add(AlertType.UPCScanBeep, await LoadSoundFile(sp, "263133__pan14__tone-beep.wav"));

            return sp;
        }

        public UpcAlert(AppCompatActivity act, Handler handler)
        {
            m_mpAlertMedia = new Dictionary<AlertType, int>();
            m_act = act;
            m_handler = handler;

            Task<SoundPool> tsp = LoadEffects(act.Assets);

            tsp.Wait();
            m_sp = tsp.Result;
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
                var audioManager = (AudioManager) m_act.GetSystemService(global::Android.Content.Context.AudioService);
                var actualVolume = (float) audioManager.GetStreamVolume(Stream.Music);
                var maxVolume = (float) audioManager.GetStreamMaxVolume(Stream.Music);
                var volume = actualVolume / maxVolume;

                m_sp.Play(m_mpAlertMedia[at], volume, volume, 1, 0, 1f);
//            });
        }
    }
}
