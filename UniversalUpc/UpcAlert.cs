using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using System.Reflection;
using System.IO;
using Windows.Storage;
using Windows.UI.Core;

namespace UniversalUpc
{
    public class UpcAlert
    {
        public enum AlertType
            {
            GoodInfo,
            BadInfo,
            Halt,
            Duplicate,
			Drink
            };

        private Dictionary<AlertType, MediaElement> m_mpAlertMedia;

        private async Task<MediaElement> LoadSoundFile(string v)
        {
            MediaElement snd = new MediaElement();

            snd.AutoPlay = false;
            StorageFolder folder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Assets");
            StorageFile file = await folder.GetFileAsync(v);
            var stream = await file.OpenAsync(FileAccessMode.Read);
            snd.SetSource(stream, file.ContentType);
            return snd;
        }
        async void LoadEffects()
        {
            m_mpAlertMedia.Add(AlertType.GoodInfo, await LoadSoundFile("Exclamation.wav"));
            m_mpAlertMedia.Add(AlertType.Duplicate, await LoadSoundFile("doh.wav"));
            m_mpAlertMedia.Add(AlertType.Drink, await LoadSoundFile("hicup_392.wav"));
            m_mpAlertMedia.Add(AlertType.BadInfo, await LoadSoundFile("Ding.wav"));
            m_mpAlertMedia.Add(AlertType.Halt, await LoadSoundFile("Ding.wav"));
        }
        public UpcAlert()
        {
            m_mpAlertMedia = new Dictionary<AlertType, MediaElement>();

            LoadEffects();
        }

        public async void Play(AlertType at)
        {
            MediaElement me = m_mpAlertMedia[at];

            await me.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                me.Stop();
                me.Play();
                });
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
