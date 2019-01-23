using DigitalEyes.VoiceToText.Desktop.Models;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace DigitalEyes.VoiceToText.Desktop.ViewModels
{
    public class VoiceEndpointsViewModel : BaseViewModel
    {
        public SettingsViewModel settingsVM;

        ObservableCollection<SpeechEndpointConfig> voiceEndpoints;
        public ObservableCollection<SpeechEndpointConfig> VoiceEndpoints
        {
            get
            {
                if (voiceEndpoints == null)
                {
                    voiceEndpoints = new ObservableCollection<SpeechEndpointConfig>();
                }
                return voiceEndpoints;
            }
            set
            {
                if (voiceEndpoints != value)
                {
                    voiceEndpoints = value;
                    RaisePropertyChanged("VoiceEndpoints");
                }
            }
        }

        RelayCommand newConfigSaveCommand;
        public RelayCommand NewConfigSaveCommand
        {
            get
            {
                if (newConfigSaveCommand == null)
                {
                    newConfigSaveCommand = new RelayCommand(doNewConfigSave);
                }
                return newConfigSaveCommand;
            }
        }

        RelayCommand updateConfigCommand;
        public RelayCommand UpdateConfigCommand
        {
            get
            {
                if (updateConfigCommand == null)
                {
                    updateConfigCommand = new RelayCommand(doUpdateConfig);
                }
                return updateConfigCommand;
            }
        }

        private void doUpdateConfig()
        {
            if (string.IsNullOrWhiteSpace(SelectedVoiceEndpoint.Endpoint)
                || string.IsNullOrWhiteSpace(SelectedVoiceEndpoint.Name)
                || string.IsNullOrWhiteSpace(SelectedVoiceEndpoint.Region)
                || string.IsNullOrWhiteSpace(SelectedVoiceEndpoint.Key))
            {
                MessageBox.Show("Please make sure all the endpoint fields are completed");
                return;
            }
            
            SaveEndpointsFile();
            MessageBox.Show("Updated!");
        }

        RelayCommand deleteConfigCommand;
        public RelayCommand DeleteConfigCommand
        {
            get
            {
                if (deleteConfigCommand == null)
                {
                    deleteConfigCommand = new RelayCommand(doDeleteConfig);
                }
                return deleteConfigCommand;
            }
        }

        private void doDeleteConfig()
        {
            VoiceEndpoints.Remove(SelectedVoiceEndpoint);
            SaveEndpointsFile();
            SelectedVoiceEndpoint = null;
            LoadEndpointsFile();
        }

        private void doNewConfigSave()
        {
            if (string.IsNullOrWhiteSpace(SelectedVoiceEndpoint.Endpoint)
                || string.IsNullOrWhiteSpace(SelectedVoiceEndpoint.Name)
                || string.IsNullOrWhiteSpace(SelectedVoiceEndpoint.Region)
                || string.IsNullOrWhiteSpace(SelectedVoiceEndpoint.Key))
            {
                MessageBox.Show("Please make sure all the endpoint fields are completed");
                return;
            }

            var currentValues = SelectedVoiceEndpoint;
            LoadEndpointsFile();                        // reset before changes
            VoiceEndpoints.Add(currentValues);     // add back in
            SaveEndpointsFile();
            SelectedVoiceEndpoint = currentValues; // select again
        }

        SpeechEndpointConfig selectedVoiceEndpoint = new SpeechEndpointConfig();
        public SpeechEndpointConfig SelectedVoiceEndpoint
        {
            get
            {
                return selectedVoiceEndpoint;
            }
            set
            {
                if (selectedVoiceEndpoint != value)
                {
                    selectedVoiceEndpoint = value;
                    RaisePropertyChanged("SelectedVoiceEndpoint");
                }
            }
        }

        private void LoadEndpointsFile()
        {
            if (!Directory.Exists(settingsVM.ProjectsFolder))
            {
                Directory.CreateDirectory(settingsVM.ProjectsFolder);
            }

            try
            {
                if (!File.Exists(settingsVM.VoiceEndpointsFile))
                {
                    SaveEndpointsFile();
                    return;
                }

                FileStream fs = new FileStream(settingsVM.VoiceEndpointsFile, FileMode.Open);

                DataContractSerializer ser = new DataContractSerializer(typeof(List<SpeechEndpointConfig>));
                using (var reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas()))
                {
                    List<SpeechEndpointConfig> list = (List<SpeechEndpointConfig>)ser.ReadObject(reader, true);
                    VoiceEndpoints = new ObservableCollection<SpeechEndpointConfig>(list);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"{exc}");
                MessageBox.Show("Error deserialising the transcription file. Has anything on the class models changed, no longer matching the json schema?");
                return;
            }

        }

        public void SaveEndpointsFile()
        {
            if (!Directory.Exists(settingsVM.ProjectsFolder))
            {
                Directory.CreateDirectory(settingsVM.ProjectsFolder);
            }

            try
            {
                DataContractSerializer ser = new DataContractSerializer(typeof(List<SpeechEndpointConfig>));
                var xmlSettings = new XmlWriterSettings { Indent = true, IndentChars = "\t" };
                using (var writer = XmlWriter.Create(settingsVM.VoiceEndpointsFile, xmlSettings))
                {
                    ser.WriteObject(writer, VoiceEndpoints.ToList());
                }
            }
            catch (Exception exc)
            {
                var msg = $"Error serialising or saving the projects file: {exc}";
                MessageBox.Show(msg);
            }
        }


        public VoiceEndpointsViewModel(SettingsViewModel settingsvm)
        {
            settingsVM = settingsvm;
            LoadEndpointsFile();
        }
    }
}
