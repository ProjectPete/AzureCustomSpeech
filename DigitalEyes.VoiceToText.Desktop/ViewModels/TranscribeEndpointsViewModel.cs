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
    public class TranscribeEndpointsViewModel : BaseViewModel
    {
        public SettingsViewModel settingsVM;

        ObservableCollection<SpeechEndpointConfig> transcribeEndpoints;
        public ObservableCollection<SpeechEndpointConfig> TranscribeEndpoints
        {
            get
            {
                if (transcribeEndpoints == null)
                {
                    transcribeEndpoints = new ObservableCollection<SpeechEndpointConfig>();
                }
                return transcribeEndpoints;
            }
            set
            {
                if (transcribeEndpoints != value)
                {
                    transcribeEndpoints = value;
                    RaisePropertyChanged("TranscribeEndpoints");
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
            TranscribeEndpoints.Remove(SelectedTranscribeEndpoint);
            SaveEndpointsFile();
            SelectedTranscribeEndpoint = null;
            LoadEndpointsFile();
        }

        private void doNewConfigSave()
        {
            var currentValues = SelectedTranscribeEndpoint;
            LoadEndpointsFile();                        // reset before changes
            TranscribeEndpoints.Add(currentValues);     // add back in
            SaveEndpointsFile();
            SelectedTranscribeEndpoint = currentValues; // select again
        }

        SpeechEndpointConfig selectedTranscribeEndpoint = new SpeechEndpointConfig();
        public SpeechEndpointConfig SelectedTranscribeEndpoint
        {
            get
            {
                return selectedTranscribeEndpoint;
            }
            set
            {
                if (selectedTranscribeEndpoint != value)
                {
                    selectedTranscribeEndpoint = value;
                    RaisePropertyChanged("SelectedTranscribeEndpoint");
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
                if (!File.Exists(settingsVM.TranscribeEndpointsFile))
                {
                    SaveEndpointsFile();
                    return;
                }

                FileStream fs = new FileStream(settingsVM.TranscribeEndpointsFile, FileMode.Open);

                DataContractSerializer ser = new DataContractSerializer(typeof(List<SpeechEndpointConfig>));
                using (var reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas()))
                {
                    List<SpeechEndpointConfig> list = (List<SpeechEndpointConfig>)ser.ReadObject(reader, true);
                    TranscribeEndpoints = new ObservableCollection<SpeechEndpointConfig>(list);
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
                using (var writer = XmlWriter.Create(settingsVM.TranscribeEndpointsFile, xmlSettings))
                {
                    ser.WriteObject(writer, TranscribeEndpoints.ToList());
                }
            }
            catch (Exception exc)
            {
                var msg = $"Error serialising or saving the projects file: {exc}";
                MessageBox.Show(msg);
            }
        }


        public TranscribeEndpointsViewModel(SettingsViewModel settingsvm)
        {
            settingsVM = settingsvm;
            LoadEndpointsFile();
        }
    }
}
