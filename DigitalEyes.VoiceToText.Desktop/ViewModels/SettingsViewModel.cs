using DigitalEyes.VoiceToText.Desktop.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalEyes.VoiceToText.Desktop.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        public string ProjectsFolder
        {
            get
            {
                var folder = Settings.Default.ProjectsFolder;
                if (string.IsNullOrWhiteSpace(folder))
                {
                    folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TranscribeForCustomVoice");
                    Settings.Default.ProjectsFolder = folder;
                    Settings.Default.Save();
                }
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                return folder;
            }
            set
            {
                if (Settings.Default.ProjectsFolder != value)
                {
                    Settings.Default.ProjectsFolder = value;
                    Settings.Default.Save();
                    RaisePropertyChanged("ProjectsFolder");
                }
            }
        }

        public bool TranscriptionLogging
        {
            get
            {
                return Settings.Default.TranscriptionLogging;
            }
            set
            {
                if (Settings.Default.TranscriptionLogging != value)
                {
                    Settings.Default.TranscriptionLogging = value;
                    Settings.Default.Save();
                    RaisePropertyChanged("TranscriptionLogging");
                }
            }
        }

        public string ProjectsFile
        {
            get
            {
                return Path.Combine(ProjectsFolder, "Projects.json");
            }
        }

        public string TranscribeEndpointsFile
        {
            get
            {
                return Path.Combine(ProjectsFolder, "Transcribe.json");
            }
        }

        public string VoiceEndpointsFile
        {
            get
            {
                return Path.Combine(ProjectsFolder, "Voice.json");
            }
        }
        
    }
}
