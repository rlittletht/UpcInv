using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Media;
using Android.OS;
using Android.Support.V7.App;
using Java.Lang;

namespace DroidUpc
{
    public interface IAlert
    {
        void DoAlert(UpcAlert.AlertType at);
    }

    public class UpcAlert : IAlert
    {
        private SoundPool m_sp;
        private AppCompatActivity m_act;
        private Handler m_handler;

        public enum AlertType
        {
            GoodInfo,
            BadInfo,
            Halt,
            Duplicate,
            Drink,
            UPCScanBeep,
            None
        };

    private Dictionary<AlertType, int> m_mpAlertMedia;

        private async Task<int> LoadSoundFile(SoundPool sp, string v)
        {
            AssetFileDescriptor afd = global::Android.App.Application.Context.Assets.OpenFd(v);

            int soundId = sp.Load(afd, 1);
            return soundId;
        }

        async Task<SoundPool> LoadEffects()
        {
            SoundPool sp = new SoundPool.Builder().SetMaxStreams(2).Build();

            m_mpAlertMedia.Add(AlertType.GoodInfo, await LoadSoundFile(sp, "Audio/exclamation.wav"));
            m_mpAlertMedia.Add(AlertType.Duplicate, await LoadSoundFile(sp, "Audio/doh.wav"));
            m_mpAlertMedia.Add(AlertType.Drink, await LoadSoundFile(sp, "Audio/hicup_392.wav"));
            m_mpAlertMedia.Add(AlertType.BadInfo, await LoadSoundFile(sp, "Audio/ding.wav"));
            m_mpAlertMedia.Add(AlertType.Halt, await LoadSoundFile(sp, "Audio/ding.wav"));
            m_mpAlertMedia.Add(AlertType.UPCScanBeep, await LoadSoundFile(sp, "Audio/263133__pan14__tone-beep.wav"));

            return sp;
        }

        public UpcAlert(AppCompatActivity act, Handler handler)
        {
            m_mpAlertMedia = new Dictionary<AlertType, int>();
            m_act = act;
            m_handler = handler;

            Task<SoundPool> tsp = LoadEffects();

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
