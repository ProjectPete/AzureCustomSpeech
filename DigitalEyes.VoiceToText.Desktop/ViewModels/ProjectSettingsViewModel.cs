using DigitalEyes.VoiceToText.Desktop.Models;
using DigitalEyes.VoiceToText.Desktop.Properties;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalEyes.VoiceToText.Desktop.ViewModels
{
    class ProjectSettingsViewModel : BaseViewModel
    {
        RelayCommand<SpeechEndpointConfig> addConfigCommand;
        public RelayCommand<SpeechEndpointConfig> AddConfigCommand
        {
            get
            {
                if (addConfigCommand == null)
                {
                    addConfigCommand = new RelayCommand<SpeechEndpointConfig>(doAddConfig);
                }
                return addConfigCommand;
            }
        }

        RelayCommand<SpeechEndpointConfig> deleteConfigCommand;
        public RelayCommand<SpeechEndpointConfig> DeleteConfigCommand
        {
            get
            {
                if (deleteConfigCommand == null)
                {
                    deleteConfigCommand = new RelayCommand<SpeechEndpointConfig>(doDeleteConfig);
                }
                return deleteConfigCommand;
            }
        }

        private void doDeleteConfig(SpeechEndpointConfig config)
        {
            ProjectSettings.Instance.SpeechEndpointConfigs.Remove(config.Name);
            ProjectSettings.Instance.Save();
        }

        private void doAddConfig(SpeechEndpointConfig config)
        {
            ProjectSettings.Instance.SpeechEndpointConfigs.Add(config.Name, config);
            ProjectSettings.Instance.Save();
        }
        
        public Dictionary<string, SpeechEndpointConfig> EndpointConfigs
        {
            get
            {
                return ProjectSettings.Instance.SpeechEndpointConfigs;
            }
        }

    }
}
