using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using System.Reflection;
using System.IO;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.Storage;
using Windows.UI.Core;
using UpcShared;

namespace UniversalUpc
{
    public class UpcAlert : IAlert
    {
        private Dictionary<AlertType, IMediaPlaybackSource> m_mpAlertMedia;

        private async Task<IMediaPlaybackSource> LoadSoundFile(string v)
        {
            StorageFolder folder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Assets");
            StorageFile file = await folder.GetFileAsync(v);
            var i = MediaSource.CreateFromStream(await file.OpenAsync(FileAccessMode.Read), file.ContentType);

            return i;
        }

        async void LoadEffects()
        {
            m_mpAlertMedia.Add(AlertType.GoodInfo, await LoadSoundFile("Exclamation.wav"));
            m_mpAlertMedia.Add(AlertType.Duplicate, await LoadSoundFile("doh.wav"));
            m_mpAlertMedia.Add(AlertType.Drink, await LoadSoundFile("hicup_392.wav"));
            m_mpAlertMedia.Add(AlertType.BadInfo, await LoadSoundFile("Ding.wav"));
            m_mpAlertMedia.Add(AlertType.Halt, await LoadSoundFile("Ding.wav"));
            m_mpAlertMedia.Add(AlertType.UPCScanBeep, await LoadSoundFile("263133__pan14__tone-beep.wav"));
        }

        private MediaPlayer m_player;

        public UpcAlert()
        {
            m_player = new MediaPlayer();
            m_mpAlertMedia = new Dictionary<AlertType, IMediaPlaybackSource>();

            LoadEffects();
        }

        public void DoAlert(AlertType at)
        {
            Play(at);
        }

        public void Play(AlertType at)
        {
            if (at == AlertType.None)
                return;

            m_player.Source = m_mpAlertMedia[at];
            m_player.Play();
        }
#if none
        void DoAlert(AlertType at, string s)
		{
		    // SystemSound ssnd;
		    
            // string sSound;
            switch (at)
                {
                case AlertType.GoodInfo:
                    SystemSounds.Exclamation.Play();
                    // sSound = "SystemExclamation";
                    break;
                case AlertType.Duplicate:
                    m_snd.Stream = Resource1.doh; // m_rm.GetStream("doh.wav");
                    m_snd.PlaySync();
                    break;
				case AlertType.Drink:
					m_snd.Stream = Resource1.hicup_392; // m_rm.GetStream("doh.wav");
					m_snd.PlaySync();
					break;
                default:
                    SystemSounds.Hand.Play();
                    // sSound = "SystemHand";
                    break;
                }

           
       //      Win32.PlaySound(sSound, 0, (int)(Win32.SND.SND_ASYNC | Win32.SND.SND_ALIAS | Win32.SND.SND_NOWAIT));
            UpdateStatus(s);
        }
#endif
    }
}
