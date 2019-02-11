using DigitalEyes.VoiceToText.Desktop.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.IO.Compression;
using DigitalEyes.VoiceToText.Desktop.Properties;
using DigitalEyes.VoiceToText.Desktop.Views;
using DigitalEyes.VoiceToText.Desktop.Helpers;

namespace DigitalEyes.VoiceToText.Desktop.ViewModels
{
    public class ProjectViewModel : BaseViewModel
    {
        private const int ADJUSTED_SIZE_MAX_WORD_LENGTH = 400;

        private readonly Timer scaleChangedTimer;

        private TranscribeEndpointsViewModel transcribeEndpointsViewModel;

        public TranscribeEndpointsViewModel TranscribeEndpointsVM
        {
            get
            {
                return transcribeEndpointsViewModel;
            }
            set
            {
                if (transcribeEndpointsViewModel != value)
                {
                    transcribeEndpointsViewModel = value;
                    RaisePropertyChanged("TranscribeEndpoints");
                }
            }
        }

        private VoiceEndpointsViewModel voiceEndpointsViewModel;

        public VoiceEndpointsViewModel VoiceEndpointsVM
        {
            get
            {
                return voiceEndpointsViewModel;
            }
            set
            {
                if (voiceEndpointsViewModel != value)
                {
                    voiceEndpointsViewModel = value;
                    RaisePropertyChanged("VoiceEndpointsVM");
                }
            }
        }

        public SettingsViewModel SettingsVM { get; } = new SettingsViewModel();

        public bool CanStartTranscribing
        {
            get
            {
                return SelectedFileName != null;
            }
        }

        private string transcribeInfo;

        public string TranscribeInfo
        {
            get
            {
                return transcribeInfo;
            }
            set
            {
                if (transcribeInfo != value)
                {
                    transcribeInfo = value;
                    RaisePropertyChanged("TranscribeInfo");
                }
            }
        }

        private bool isTranscribing;

        public bool IsTranscribing
        {
            get
            {
                return isTranscribing;
            }
            set
            {
                if (isTranscribing != value)
                {
                    isTranscribing = value;
                    RaisePropertyChanged("IsTranscribing");
                }
            }
        }

        private string selectedFileName;

        public string SelectedFileName
        {
            get
            {
                return selectedFileName;
            }
            set
            {
                if (selectedFileName != value)
                {
                    selectedFileName = value;
                    RaisePropertyChanged("SelectedFileName");
                    RaisePropertyChanged("CanStartTranscribing");

                    if (value != null)
                    {
                        var filePath = Path.Combine(FilesFolder, value);
                        try
                        {
                            using (MediaFoundationReader mediaFilereader = new MediaFoundationReader(filePath))
                            {
                                CurrentAudioVM = new AudoInfoViewModel
                                {
                                    WaveFormat = mediaFilereader.WaveFormat,
                                    Duration = mediaFilereader.TotalTime,
                                    FileName = selectedFileName
                                };

                                CheckCurrentFileSuitability();
                            }
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show("That file type cannot be imported. Only common media, including wav, m4a, mp3, mp4");
                        }
                    }
                    else
                    {
                        CurrentAudioVM = null;
                    }
                }
            }
        }

        private void CheckCurrentFileSuitability()
        {
            if (CurrentAudioVM.WaveFormat.Encoding != WaveFormatEncoding.Pcm)
            {
                MessageBox.Show("Warning. Some of the methods used in this tool are only reliable for PCM encoded WAVs.");
            }
        }

        public bool FoundFiles
        {
            get
            {
                return FilesFound.Count > 0;
            }
        }

        private List<string> filesFound = new List<string>();

        public List<string> FilesFound
        {
            get
            {
                return filesFound;
            }
            set
            {
                if (filesFound != value)
                {
                    filesFound = value;
                    RaisePropertyChanged("FilesFound");
                    RaisePropertyChanged("FoundFiles");
                }
            }
        }

        public string FilesFolder
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Settings.Default.LastUserWorkspaceFolder))
                {
                    var workFolder = SettingsVM.ProjectsFolder;
                    Settings.Default.LastUserWorkspaceFolder = workFolder;
                    Settings.Default.Save();
                }
                if (!Directory.Exists(Settings.Default.LastUserWorkspaceFolder))
                {
                    Directory.CreateDirectory(Settings.Default.LastUserWorkspaceFolder);
                }
                return Settings.Default.LastUserWorkspaceFolder;
            }
            set
            {
                if (Settings.Default.LastUserWorkspaceFolder != value)
                {
                    Settings.Default.LastUserWorkspaceFolder = value;
                    Settings.Default.Save();

                    RaisePropertyChanged("FilesFolder");
                    LoadFilesFromSelectedFolder();
                }
            }
        }

        public void LoadFilesFromSelectedFolder()
        {
            var files = new List<string>();
            var filePaths = Directory.GetFiles(FilesFolder);
            foreach (var f in filePaths)
            {
                files.Add(Path.GetFileName(f));
            }
            FilesFound = files;
        }

        private AudoInfoViewModel currentAudioVM;

        public AudoInfoViewModel CurrentAudioVM
        {
            get
            {
                return currentAudioVM;
            }
            set
            {
                if (currentAudioVM != value)
                {
                    currentAudioVM = value;
                    RaisePropertyChanged("CurrentAudioVM");
                }
            }
        }

        private double gain;

        public double Gain
        {
            get
            {
                return gain;
            }
            set
            {
                if (gain != value)
                {
                    gain = value;
                    RaisePropertyChanged("Gain");
                    scaleChangedTimer.Change(500, Timeout.Infinite);
                    //ReloadSnippetAudioTracks(SelectedProject);
                }
            }
        }

        private RelayCommand showSettingsCommand;

        public RelayCommand ShowSettingsCommand
        {
            get
            {
                return showSettingsCommand ?? (showSettingsCommand = new RelayCommand(DoShowSettings));
            }
        }

        private RelayCommand generateSpeechCommand;

        public RelayCommand GenerateSpeechCommand
        {
            get
            {
                return generateSpeechCommand ?? (generateSpeechCommand = new RelayCommand(DoGenerateSpeech));
            }
        }

        private async void DoGenerateSpeech()
        {
            try
            {
                if (VoiceEndpointsVM == null || VoiceEndpointsVM.SelectedVoiceEndpoint == null)
                {
                    MessageBox.Show("Please select a Voice Service endpoint, or enter and save a new one.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(VoiceEndpointsVM.SelectedVoiceEndpoint.Endpoint)
                    || string.IsNullOrWhiteSpace(VoiceEndpointsVM.SelectedVoiceEndpoint.Name)
                    || string.IsNullOrWhiteSpace(VoiceEndpointsVM.SelectedVoiceEndpoint.Region)
                    || string.IsNullOrWhiteSpace(VoiceEndpointsVM.SelectedVoiceEndpoint.Key))
                {
                    MessageBox.Show("Please make sure all the endpoint fields are completed");
                    return;
                }

                LockUserInterface = true;

                Messenger.Default.Register<string>(this, DoTtsMessage);
                var textToSpeech = new TextToSpeechManager();

                var converted = false;
                var exportFolers = new List<string>();
                foreach (var snip in SelectedProject.Snippets)
                {
                    if (snip.IsDrawn)
                    {
                        var directory = Path.GetDirectoryName(snip.FilePath);
                        var newFile = Path.Combine(directory, Path.GetFileNameWithoutExtension(snip.FilePath) + $"_{DateTime.Now.ToString("yyyMMdd_HHmmss")}.wav");

                        await textToSpeech.GetSpeechAsync(snip.RawText, newFile, VoiceEndpointsVM.SelectedVoiceEndpoint);

                        converted = true;
                        if (!exportFolers.Contains(directory))
                        {
                            exportFolers.Add(directory);
                        }
                    }
                }

                if (converted)
                {
                    var answ = MessageBox.Show("Files generated. Do you want to open the export folder?", "All done", MessageBoxButton.YesNo);
                    if (answ == MessageBoxResult.Yes)
                    {
                        foreach (var dir in exportFolers)
                        {
                            Process.Start(dir);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("No snippets are opened for converting.");
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Error: {exc}");
            }
            finally
            {
                Messenger.Default.Unregister<string>(this, DoTtsMessage);
                LockUserInterface = false;
            }
        }

        private void DoTtsMessage(string msg)
        {
            ProgressingInfo = msg;
        }

        private RelayCommand stopTranscribeCommand;

        public RelayCommand StopTranscribeCommand
        {
            get
            {
                return stopTranscribeCommand ?? (stopTranscribeCommand = new RelayCommand(DoStopTranscribe));
            }
        }

        private void DoStopTranscribe()
        {
            stopRecognition.TrySetCanceled();
        }

        private TaskCompletionSource<int> stopRecognition;

        private RelayCommand deleteProjectCommand;

        public RelayCommand DeleteProjectCommand
        {
            get
            {
                return deleteProjectCommand ?? (deleteProjectCommand = new RelayCommand(DoDeleteProject));
            }
        }

        private void DoDeleteProject()
        {
            var answ = MessageBox.Show("Are you sure you want to delete this project? (Media will not be deleted)", "Remove Project", MessageBoxButton.YesNo);
            if (answ == MessageBoxResult.Yes)
            {
                Messenger.Default.Send<DE_VTT_Project>(SelectedProject, "delete");
            }
        }

        private void DoShowSettings()
        {
            ShowSettings();
        }

        private RelayCommand saveChangesCommand;

        public RelayCommand SaveChangesCommand
        {
            get
            {
                return saveChangesCommand ?? (saveChangesCommand = new RelayCommand(DoSaveChanges, CanSaveChanges));
            }
        }

        private bool CanSaveChanges()
        {
            return true;
        }

        private void DoSaveChanges()
        {
            SaveProjectsFile();
            SendNotificationUpdate("Save complete");
        }

        private void SendNotificationUpdate(string message)
        {
            ProgressingInfo = message;
        }

        private bool showImportSection;

        public bool ShowImportSection
        {
            get
            {
                return showImportSection;
            }
            set
            {
                showImportSection = value;
                RaisePropertyChanged("ShowImportSection");
            }
        }

        private bool lockUserInterface;

        public bool LockUserInterface
        {
            get
            {
                return lockUserInterface;
            }
            set
            {
                if (lockUserInterface != value)
                {
                    ProgressingInfo = value ? "Please wait..." : "";

                    lockUserInterface = value;
                    RaisePropertyChanged("LockUserInterface");
                    RaisePropertyChanged("ProgressingInfo");
                }
            }
        }

        private string progressingInfo;

        public string ProgressingInfo
        {
            get
            {
                return progressingInfo;
            }
            set
            {
                if (progressingInfo != value)
                {
                    progressingInfo = value;
                    RaisePropertyChanged("ProgressingInfo");
                }
            }
        }

        private DE_VTT_Project selectedProject;

        public DE_VTT_Project SelectedProject
        {
            get
            {
                return selectedProject;
            }
            set
            {
                if (selectedProject != value)
                {
                    if (projectAsJsonForLaterChangeCheck != null)
                    {
                        CheckForChanges();
                    }

                    selectedProject = value;
                    RaisePropertyChanged("SelectedProject");

                    parentControl.Dispatcher.Invoke(() =>
                    {
                        LoadProject(value);
                        GenerateCustomArtefactsCommand.RaiseCanExecuteChanged();
                        projectAsJsonForLaterChangeCheck = JsonConvert.SerializeObject(selectedProject);
                    });
                }
            }
        }

        public void CheckForChanges()
        {
            var currentProjectAsJson = JsonConvert.SerializeObject(selectedProject);
            if (projectAsJsonForLaterChangeCheck != currentProjectAsJson)
            {
                var answ = MessageBox.Show("Do you want to save the changes you made to the current project, before you continue?", "Save Changes?", MessageBoxButton.YesNo);
                if (answ == MessageBoxResult.Yes)
                {
                    SaveProjectsFile();
                }
                else
                {
                    var orig = JsonConvert.DeserializeObject<DE_VTT_Project>(projectAsJsonForLaterChangeCheck);
                    selectedProject.Snippets = orig.Snippets;
                }
            }
        }

        private string projectAsJsonForLaterChangeCheck = null;

        private async Task LoadProject(DE_VTT_Project value)
        {
            try
            {
                LockUserInterface = true;
                await Task.Delay(10).ConfigureAwait(false);
                SelectedProject = value;

                AudioFilePath = value.OriginalFilePath;

                if (SelectedProject.Snippets.Count == 0)
                {

                    parentControl.Dispatcher.Invoke(() =>
                    {
                        ShowImportSection = true;
                        SendNotificationUpdate("This project has no snippets saved yet. Please use the import section to import & transcribe some raw audio");
                        LockUserInterface = false;
                    });
                }
                else
                {
                    Scale = SelectedProject.Snippets[0].Scale;
                    Gain = SelectedProject.Snippets[0].Gain;
                    CurrentAudioVM = new AudoInfoViewModel
                    {
                        WaveFormat = SelectedProject.Snippets[0].AudioVM.WaveFormat,
                        Duration = SelectedProject.Snippets[0].Duration
                    };
                    if (SelectedProject.OriginalFilePath != null)
                    {
                        CurrentAudioVM.FileName = Path.GetFileName(SelectedProject.OriginalFilePath);
                    }

                    ShowImportSection = false;
                    LockUserInterface = false;
                    //parentControl.Dispatcher.Invoke(() =>
                    // {
                    //    //snippet.GetSampleFrequencyAddPoints();
                    //  //  await Task.Delay(200).ConfigureAwait(false);
                    ////     Messenger.Default.Send("Scale", "TrackSnippetViewModel");
                    // });
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine($"{exc}");
            }
            finally
            {
                LockUserInterface = false;
            }
        }

        private double scale = 0.1;

        public double Scale
        {
            get
            {
                return scale;
            }
            set
            {
                if (scale != value)
                {
                    scale = value;
                    RaisePropertyChanged("Scale");
                    scaleChangedTimer.Change(500, Timeout.Infinite);
                }
            }
        }

        private int sampleRate;

        public int SampleRate
        {
            get
            {
                return sampleRate;
            }
            set
            {
                if (sampleRate != value)
                {
                    sampleRate = value;
                    RaisePropertyChanged("SampleRate");
                }
            }
        }

        private RelayCommand generateCustomArtefactsCommand;

        public RelayCommand GenerateCustomArtefactsCommand
        {
            get
            {
                return generateCustomArtefactsCommand ?? (generateCustomArtefactsCommand = new RelayCommand(DoGenerateCustomArtefacts, CanExecuteGenCustomArte));
            }
        }

        private bool CanExecuteGenCustomArte()
        {
            return SelectedProject?.Snippets.Count > 0;
        }

        private void DoGenerateCustomArtefacts()
        {
            CheckForChanges();
            LockUserInterface = true;

            try
            {
                MessageBoxResult answ = MessageBoxResult.Cancel;
                var numberOfFiles = SelectedProject.SnippetsOkTextPartCount;

                //   await Task.Delay(50).ConfigureAwait(false);

                if (numberOfFiles > 0)
                {
                    answ = MessageBox.Show($"Click YES to export just the {numberOfFiles} approved text parts with matching audio.{Environment.NewLine}Click NO to use the original full snippet audio and full text.{Environment.NewLine}Click CANCEL to take no action.", "Generate from just approved text?", MessageBoxButton.YesNoCancel);
                    if (answ == MessageBoxResult.Cancel)
                    {
                      //  LockUserInterface = false;
                        return;
                    }
                }

                var formatAnsw = MessageBox.Show($"Click YES to export for Custom Voice.{Environment.NewLine}Click NO to export for Acoustic Dataset.{Environment.NewLine}Click CANCEL to take no action.", "Which index file format?", MessageBoxButton.YesNoCancel);
                if (formatAnsw == MessageBoxResult.Cancel)
                {
                   // LockUserInterface = false;
                    return;
                }

                var dateFolder = $"{DateTime.Now.ToString("yyyMMdd_HHmmss")}";
                var exportFolder = Path.Combine(Settings.Default.ProjectsFolder, selectedProject.FolderName, dateFolder);
                var exportRawFolder = Path.Combine(Settings.Default.ProjectsFolder, selectedProject.FolderName, dateFolder, "RAW");

                parentControl.Dispatcher.Invoke(() =>
                {
                    Directory.CreateDirectory(exportRawFolder);

                    var transcriptionsFileText = "";

                    if (answ == MessageBoxResult.Yes)
                    {
                        // Version 2 - Reworking snippets. Taking only where edits were made and ticked as OK
                        ProgressingInfo = $"Generating {numberOfFiles} approved text parts from {SelectedProject.Snippets.Count} snippets...";
                        var index = 1;
                        foreach (var snip in SelectedProject.Snippets)
                        {
                            transcriptionsFileText += CreateTextPartFilesFromSnippet(exportRawFolder, snip, formatAnsw, ref index);
                        }
                    }
                    else
                    {
                        // Version 1 - Take the raw text before curating
                        ProgressingInfo = $"Generating {SelectedProject.Snippets.Count} snippets with the raw text...";
                        var ix = 1;
                        foreach (var snip in SelectedProject.Snippets)
                        {
                            var fileNumberString = $"{(ix++).ToString().PadLeft(4, '0')}";
                            var fileName = $"{fileNumberString}.wav";
                            if (formatAnsw == MessageBoxResult.Yes)
                            {
                                transcriptionsFileText += $"{fileNumberString}\t{snip.RawText}{Environment.NewLine}"; // Custom Voice format is just a number
                            }
                            else
                            {
                                transcriptionsFileText += $"{fileName}\t{snip.RawText}{Environment.NewLine}"; // Acoustic Dataset requires .wav extension with the number
                            }
                            var newFileCopy = Path.Combine(exportRawFolder, fileName);
                            File.Copy(snip.FilePath, newFileCopy); // we're taking the whole snippet, so make a copy
                            WavFileUtils.ChangeWaveFormat(newFileCopy, 16000, 16, 1);
                        }
                    }

                    // create zip file
                    var newZipFile = Path.Combine(exportFolder, $"samples.zip");
                    ZipFile.CreateFromDirectory(exportRawFolder, newZipFile, CompressionLevel.Fastest, false);

                    // create transcription mapping file
                    var newTransFile = Path.Combine(exportFolder, $"speech.txt");
                    File.WriteAllText(newTransFile, transcriptionsFileText);

                    // delete raw temporary files, now they are zipped
                    Directory.Delete(exportRawFolder, true);
                });

                answ = MessageBox.Show("Files generated. Do you want to open the export folder?", "All done", MessageBoxButton.YesNo);
                if (answ == MessageBoxResult.Yes)
                {
                    Process.Start(exportFolder);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Error: {exc}");
            }
            finally
            {
                LockUserInterface = false;
            }
        }

        private RelayCommand generateTTMLCommand;

        public RelayCommand GenerateTTMLCommand
        {
            get
            {
                return generateTTMLCommand ?? (generateTTMLCommand = new RelayCommand(DogenerateTTML));
            }
        }

        private void DogenerateTTML()
        {
            var answExportAll = MessageBox.Show($"Do you want to export all approved texts to subtitles? Click YES to export all the snippets.{Environment.NewLine}Click NO to export just the 'opened' snippets.{Environment.NewLine}Click CANCEL to exit.", "Choose which snippets to export", MessageBoxButton.YesNoCancel);
            if (answExportAll == MessageBoxResult.Cancel)
            {
                return;
            }

            var answTtml = MessageBox.Show($"Do you want to export as TTML? Click YES For TTML format.{Environment.NewLine}Click NO for SRT format.{Environment.NewLine}Click CANCEL to exit.", "Choose which file format", MessageBoxButton.YesNoCancel);
            if (answTtml == MessageBoxResult.Cancel)
            {
                return;
            }

            string subtitleFileText;
            string fileName;

            if (answTtml == MessageBoxResult.Yes)
            {
                subtitleFileText = MakeTtml(answExportAll);
                fileName = $"{selectedProject.FolderName}.ttml";
            }
            else
            {
                subtitleFileText = MakeSrt(answExportAll);
                fileName = $"{selectedProject.FolderName}.srt";
            }

            var dateFolder = $"{DateTime.Now.ToString("yyyMMdd_HHmmss")}";
            var exportFolder = Path.Combine(Settings.Default.ProjectsFolder, selectedProject.FolderName, dateFolder);
            Directory.CreateDirectory(exportFolder);
            var file = Path.Combine(exportFolder, fileName);

            File.WriteAllText(file, subtitleFileText);

            var answShow = MessageBox.Show("File generated. Do you want to open the export folder?", "All done", MessageBoxButton.YesNo);
            if (answShow == MessageBoxResult.Yes)
            {
                Process.Start(exportFolder);
            }
        }

        private string MakeSrt(MessageBoxResult answExportAll)
        {
            var srt = "";
            int ix = 1;
            foreach (var snip in SelectedProject.Snippets)
            {
                if (answExportAll == MessageBoxResult.No && !snip.IsDrawn)
                {
                    continue; // Export only opened snippets
                }

                foreach (var bit in snip.TextParts.Where(a => a.IsOK))
                {
                    var startTime = TimeSpan.FromTicks(snip.OffsetInTicks) + TimeSpan.FromMilliseconds(bit.StartMills); // + snip.AudioSnippet.OffsetInTicks);
                    var endTime = startTime + TimeSpan.FromMilliseconds(bit.TextWidth);
                    srt += $"{ix++}{Environment.NewLine}{startTime.ToString(@"hh\:mm\:ss\,fff")} --> {endTime.ToString(@"hh\:mm\:ss\,fff")}{Environment.NewLine}{bit.Text}{Environment.NewLine}{Environment.NewLine}";
                }
            }

            return srt;
        }

        private string MakeTtml(MessageBoxResult answExportAll)
        {
            string ttml = GetTtmlTemplate() + Environment.NewLine;
            var subtitles = "";

            foreach (var snip in SelectedProject.Snippets)
            {
                if (answExportAll == MessageBoxResult.No && !snip.IsDrawn)
                {
                    continue; // Export only opened snippets
                }

                foreach (var txt in snip.TextParts.Where(a => a.IsOK))
                {
                    var startTime = TimeSpan.FromTicks(snip.OffsetInTicks) + TimeSpan.FromMilliseconds(txt.StartMills); // + snip.AudioSnippet.OffsetInTicks);
                    var endTime = startTime + TimeSpan.FromMilliseconds(txt.TextWidth);
                    subtitles += $"<p begin=\"{startTime}\" end=\"{endTime}\" style=\"s1\" region=\"rBottom\">{txt.Text}</p>{Environment.NewLine}";
                }
            }

            return ttml.Replace("$subtitles", subtitles);
        }

        private object GetTtmlTemplate()
        {
            return $"<tt xml:lang=\"en\" xmlns=\"http://www.w3.org/ns/ttml\" xmlns:tts=\"http://www.w3.org/ns/ttml#styling\"><head><styling><style xml:id=\"s1\" tts:fontFamily=\"SansSerif\" tts:fontWeight=\"bold\" tts:fontStyle=\"normal\" tts:textDecoration=\"none\" tts:color=\"white\" tts:backgroundColor=\"black\"/></styling><layout><region xml:id=\"rBottom\" tts:origin=\"20% 70%\" tts:extent=\"60% 30%\"/></layout></head><body><div xml:lang=\"en\">{Environment.NewLine}$subtitles</div></body></tt>";
        }

        private string CreateTextPartFilesFromSnippet(string ExportFolder, TrackSnippetViewModel snip, MessageBoxResult yesIfVoice, ref int index)
        {
            var transcriptionsFileText = "";

            foreach (var tp in snip.TextParts)
            {
                if (!tp.IsOK || string.IsNullOrWhiteSpace(tp.Text))
                {
                    continue;
                }

                var fileNumberString = $"{index.ToString().PadLeft(4, '0')}";
                var fileName = Path.Combine(ExportFolder, $"{fileNumberString}.wav"); //Path.GetFileName(snip.FilePath);
                if (yesIfVoice == MessageBoxResult.Yes)
                {
                    transcriptionsFileText += $"{fileNumberString}\t{tp.Text.Trim()}{Environment.NewLine}"; // is for Custom Voice
                }
                else
                {
                    transcriptionsFileText += $"{fileNumberString}.wav\t{tp.Text.Trim()}{Environment.NewLine}"; // is for Acoutic Dataset
                }

                WavFileUtils.TakeClipAddSilence(snip.FilePath, TimeSpan.FromMilliseconds(800), TimeSpan.FromMilliseconds(tp.StartMills), TimeSpan.FromMilliseconds(tp.TextWidth), fileName);

                index++;
            }

            return transcriptionsFileText;
        }

        public string AudioFilePath { get; set; }

        public byte[] SampleBytes { get; set; }
        public int SampleRateKbps { get; set; }
        public int SampleBits { get; set; }
        public double SampleLengthSeconds { get; set; }

        //     int lastCharLength = 0;
        private double lastMillisecondsTotal = 1;

        private List<TextPart> textPartsAll = new List<TextPart>();

        private readonly FrameworkElement parentControl;

        private void DoProcessMessage(string msg)
        {
            ProgressingInfo = msg;
        }

        private async static void ScaleTick(object state)
        {
            Debug.WriteLine("scale/gain changed tick");
            var me = state as ProjectViewModel;
            me.scaleChangedTimer.Change(Timeout.Infinite, Timeout.Infinite);

            await me.parentControl.Dispatcher.Invoke(async () =>
            {
                me.LockUserInterface = true;
                await Task.Delay(50).ConfigureAwait(false);

                bool redraw = false;
                foreach (var snippet in me.SelectedProject.Snippets) // TrackSnippetViewModels)
                {
                    redraw = snippet.ChangeScale(me.Scale, me.Gain);
                }

                if (redraw)
                {
                    Messenger.Default.Send("Scale", "TrackSnippetViewModel");
                }

                me.LockUserInterface = false;
            }).ConfigureAwait(false);
        }

        public async void TranscribeNewFile()
        {
            try
            {
                if (TranscribeEndpointsVM.SelectedTranscribeEndpoint == null)
                {
                    MessageBox.Show("You have not selected a service endpoint yet. Please select from the dropdown, or enter and save a new Azure Speech Service endpoint.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(TranscribeEndpointsVM.SelectedTranscribeEndpoint.Key) || string.IsNullOrWhiteSpace(TranscribeEndpointsVM.SelectedTranscribeEndpoint.Region))
                {
                    MessageBox.Show("You must enter your Azure Speech Service Key and Service Region.");
                    return;
                }

                SelectedProject.OriginalFilePath = AudioFilePath;
                LockUserInterface = true;
                IsTranscribing = true;
                TranscribeInfo = "";
                await Task.Delay(10).ConfigureAwait(false);

                stopRecognition = new TaskCompletionSource<int>();
                var newCollection = new List<TrackSnippetViewModel>();

                await GetTranscriptionFromAzure(stopRecognition, newCollection).ConfigureAwait(false);

                try
                {
                    if (newCollection.Count == 0 || stopRecognition.Task.IsCanceled)
                    {
                        MessageBox.Show("No files were generated. Please check settings and read more about source file formats and preconditions from the link in the following settings window.");
                        var res = stopRecognition.Task.Result;
                        ShowSettings();
                        return;
                    }
                }
                catch (Exception excp)
                {
                    Debug.WriteLine($"Error: EndTranscribe: {excp}");
                }

                ///
                ///
                /// 
                /// Finished recognition, now make files
                /// 
                /// 
                ///

                TranscribeInfo = "Finished transcribing. Please wait while the snippet files are created...";

                newCollection[0].IsDrawn = true; // Show the first only

                var ix = 1;
                foreach (var snippet in newCollection)
                {
                    MakeSnippetAudioFiles(snippet, ix++);
                }

                await parentControl.Dispatcher.Invoke(async () =>
                {
                    var tasks = new List<Task>();
                    foreach (var snippet in newCollection)
                    {
                        SelectedProject.Snippets.Add(snippet);
                        snippet.GetSampleFrequencyAddPoints();
                    }
                }).ContinueWith(async (_)=>
                {
                    await parentControl.Dispatcher.Invoke(async () =>
                    {
                        foreach (var snippet in newCollection)
                        {
                            snippet.GetSampleFrequencyAddPoints();
                        }
                        GenerateCustomArtefactsCommand.RaiseCanExecuteChanged();
                    });
                });
                
                SaveProjectsFile();
            }
            catch (Exception excp)
            {
                MessageBox.Show($"{excp}");
            }
            finally
            {
                LockUserInterface = false;
            }
        }

        private void SaveProjectsFile()
        {
            Messenger.Default.Send<ProjectViewModel>(this);
        }

        private void ShowSettings()
        {
            var win = new SettingsWindow { DataContext = SettingsVM };
            win.Show();
        }

        private async Task GetTranscriptionFromAzure(TaskCompletionSource<int> stopRecognition, List<TrackSnippetViewModel> newCollection)
        {
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechConfig.FromSubscription(TranscribeEndpointsVM.SelectedTranscribeEndpoint.Key, TranscribeEndpointsVM.SelectedTranscribeEndpoint.Region);
            if (!string.IsNullOrWhiteSpace(TranscribeEndpointsVM.SelectedTranscribeEndpoint.Endpoint))
            {
                config.EndpointId = TranscribeEndpointsVM.SelectedTranscribeEndpoint.Endpoint;
            }
            config.OutputFormat = OutputFormat.Detailed;

            var log = "";
            double OnePercent = 100D / CurrentAudioVM.Duration.TimeSpan.Ticks;
            var tLog = new transLog { Started = DateTime.Now, FilePath = AudioFilePath };

            try
            {
                if (Path.GetExtension(AudioFilePath).ToLower() != ".wav")
                {
                    // extract the WAV
                    var wavFileName = Path.Combine(Path.GetDirectoryName(AudioFilePath), Path.GetFileNameWithoutExtension(AudioFilePath) + ".wav");
                    WavFileUtils.ExtractWavFromMedia(AudioFilePath, wavFileName);

                    AudioFilePath = wavFileName;
                }

                using (var audioInput = AudioConfig.FromWavFileInput(AudioFilePath))
                {
                    using (var recognizer = new SpeechRecognizer(config, audioInput))
                    {
                        // Subscribes to events.
                        recognizer.Recognizing += (s, e) =>
                        {
                            transLogRow dyn = new transLogRow();
                            var txtCnt = textPartsAll.Count;
                            var dur = e.Result.Duration;
                            var totDone = TimeSpan.FromTicks(e.Result.OffsetInTicks + dur.Ticks);
                            var perc = OnePercent * (dur.Ticks + e.Result.OffsetInTicks);

                            TranscribeInfo = $"{Math.Floor(perc)}%: {newCollection.Count + 1}: {totDone.Hours}:{totDone.Minutes}:{totDone.Seconds}: {txtCnt}: {e.Result.Text}";

                            dyn.Args = e;

                            var textBit = e.Result.Text;
                            var ary = textBit.Split(' ');
                            var lastGoodIx = 0;
                            var goodTextParts = new List<TextPart>();
                            var isChange = false;

                            for (var a = 0; a < ary.Length; a++)
                            {
                                if (a > txtCnt - 1)
                                {
                                    dyn.Notes += $"[new={txtCnt - a}]";
                                    break;
                                }
                                if (ary[a].Trim() != textPartsAll[a].Text.Trim())
                                {
                                    dyn.Notes += $"[chg={a}:{textPartsAll[a].Text}={ary[a]}]";
                                    textPartsAll[a].Text = ary[a];
                                    isChange = true;
                                }
                                goodTextParts.Add(textPartsAll[a]);
                                lastGoodIx = a;
                            }

                            if (lastGoodIx < textPartsAll.Count - 1)
                            {
                                dyn.Notes += $"[lth chg]";
                            }

                            double detectedEstimate;

                            try
                            {
                                if (goodTextParts.Count >= ary.Length)
                                {
                                    return; // nothing new
                                }

                                var newBit = "";
                                for (var x = goodTextParts.Count; x < ary.Length; x++)
                                {
                                    newBit += ary[x] + " ";
                                }

                                // 200 magic number - identification time about 200 mills
                                detectedEstimate = e.Result.Duration.TotalMilliseconds - 200;

                                var sinceLastEvent = detectedEstimate - lastMillisecondsTotal;
                                if (sinceLastEvent < 1)
                                {
                                    sinceLastEvent = 1;
                                }

                                var cnt = goodTextParts.Count;

                                if (cnt > 1 && !isChange)
                                {
                                    if (sinceLastEvent < ADJUSTED_SIZE_MAX_WORD_LENGTH)
                                    {
                                        dyn.Notes += $"[adj={cnt - 1}, snc={sinceLastEvent}, lgt={goodTextParts[cnt - 1].TextWidth} - {ADJUSTED_SIZE_MAX_WORD_LENGTH - sinceLastEvent}]";
                                        goodTextParts[cnt - 1].TextWidth -= (ADJUSTED_SIZE_MAX_WORD_LENGTH - sinceLastEvent);
                                    }
                                }

                                // Finally, save any new bit(s)
                                goodTextParts.Add(new TextPart { Text = newBit.Trim(), StartMills = detectedEstimate, TextWidth = ADJUSTED_SIZE_MAX_WORD_LENGTH });
                            }
                            catch (Exception ex)
                            {
                                throw;
                            }
                            finally
                            {
                                log += JsonConvert.SerializeObject(dyn) + Environment.NewLine;
                            }
                            lastMillisecondsTotal = detectedEstimate; // start point for next text
                                                                      //           lastCharLength = textBit.Length;

                            textPartsAll = goodTextParts;
                        };

                        recognizer.Recognized += (s, e) =>
                        {
                            if (e.Result.Reason == ResultReason.RecognizedSpeech)
                            {
                                //Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                                var snippet = new TrackSnippetViewModel()
                                {
                                    AudioSnippet = e.Result,
                                    Scale = scale,
                                    Gain = gain,
                                    RawText = e.Result.Text,
                                    OffsetInTicks = e.Result.OffsetInTicks
                                };

                                snippet.DurationMilliseconds = e.Result.Duration.TotalMilliseconds; //WavFileUtils.GetSoundLength(AudioFilePath);

                                //var rawAry = e.Result.Text.Split(' ');
                                //if (rawAry.Length == textPartsAll.Count)
                                //{
                                //    for(var a = 0; a < rawAry.Length; a++)
                                //    {
                                //        textPartsAll[a].Text = rawAry[a];
                                //    }
                                //}
                                //else
                                //{
                                //    // to do, merge tidied text back into recognised text
                                //}

                                snippet.TextParts = new ObservableCollection<TextPart>(textPartsAll);

                                parentControl.Dispatcher.Invoke(() => newCollection.Add(snippet));

                                textPartsAll = new List<TextPart>();
                                lastMillisecondsTotal = 0;
                            }
                            else if (e.Result.Reason == ResultReason.NoMatch)
                            {
                                Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                            }
                        };

                        recognizer.Canceled += (s, e) =>
                        {
                            Console.WriteLine($"CANCELED: Reason={e.Reason}");

                            if (e.Reason == CancellationReason.Error)
                            {
                                MessageBox.Show($"FAILED: Are you using a valid subscription key and region? ErrorCode = {e.ErrorCode}: ErrorDetails = {e.ErrorDetails}");
                            }

                            stopRecognition.TrySetResult(0);
                        };

                        recognizer.SessionStarted += (s, e) => Console.WriteLine("\n    Session started event.");

                        recognizer.SessionStopped += (s, e) =>
                        {
                            Console.WriteLine("\n    Session stopped event.");
                            Console.WriteLine("\nStop recognition.");
                            stopRecognition.TrySetResult(0);
                        };

                        // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                        await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                        // Waits for completion.
                        // Use Task.WaitAny to keep the task rooted.
                        Task.WaitAny(new[] { stopRecognition.Task });

                        // Stops recognition.
                        await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception excp)
            {
                Debug.WriteLine($"{excp}");
            }
            finally
            {
                if (Settings.Default.TranscriptionLogging)
                {
                    try
                    {
                        var logFile = $"TranscriptionLog_{DateTime.Now.ToString($"{DateTime.Now.ToString("ddMMyyyyHHmmss")}")}.txt";
                        logFile = Path.Combine(Path.GetFullPath(SettingsVM.ProjectsFolder), logFile);
                        File.WriteAllText(logFile, log);

                        tLog.LogPath = logFile;
                        SelectedProject.TranscribeLogs.Add(tLog);
                        Debug.WriteLine($"Log file created at {logFile}");
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
            }
        }

        private void MakeSnippetAudioFiles(
            TrackSnippetViewModel snippet,
            int ix)
        {
            var newProjectFolder = Path.Combine(Settings.Default.ProjectsFolder, SelectedProject.Name, $"{DateTime.Now.ToString("ddMMyyyyHHmmss")}");
            Directory.CreateDirectory(newProjectFolder);

            snippet.FilePath = Path.Combine(newProjectFolder, $"{Path.GetFileNameWithoutExtension(AudioFilePath)}_{ix++}.wav");

            var startTime = TimeSpan.FromTicks(snippet.AudioSnippet.OffsetInTicks);

            WavFileUtils.TakeClipAddSilence(AudioFilePath, TimeSpan.Zero, startTime, snippet.AudioSnippet.Duration, snippet.FilePath);
        }

        public ProjectViewModel(FrameworkElement ParentControl, DE_VTT_Project project, bool showImportSection)
        {
            parentControl = ParentControl;

            TranscribeEndpointsVM = new TranscribeEndpointsViewModel(SettingsVM);
            VoiceEndpointsVM = new VoiceEndpointsViewModel(SettingsVM);

            Messenger.Default.Register<string>(this, DoProcessMessage);

            SelectedProject = project;

            LoadFilesFromSelectedFolder();

            scaleChangedTimer = new Timer(ScaleTick, this, Timeout.Infinite, Timeout.Infinite);
            this.showImportSection = showImportSection;
        }
    }
}
