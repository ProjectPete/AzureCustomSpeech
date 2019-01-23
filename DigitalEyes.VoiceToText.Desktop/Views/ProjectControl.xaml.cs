using DigitalEyes.VoiceToText.Desktop.Models;
using DigitalEyes.VoiceToText.Desktop.ViewModels;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Gui;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DigitalEyes.VoiceToText.Desktop.Views
{
    /// <summary>
    /// Interaction logic for ProjectControl.xaml
    /// </summary>
    public partial class ProjectControl : UserControl, IDisposable
    {
        public ProjectViewModel ViewModel { get; set; }

        public ProjectControl(DE_VTT_Project project)
        {
            InitializeComponent();
            timer = new DispatcherTimer(TimeSpan.FromMilliseconds(300), DispatcherPriority.Normal, tick, this.Dispatcher);

            ViewModel = new ProjectViewModel(this, project);
            DataContext = ViewModel;
        }
        
        private void tick(object sender, EventArgs e)
        {
            // LoadPoints();
        }

        DispatcherTimer timer;

        private void LstSnippets_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            timer.Start();
        }

        private async void Transcribe_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ViewModel.AudioFilePath = System.IO.Path.Combine(ViewModel.FilesFolder, ViewModel.SelectedFileName);
                ViewModel.SampleLengthSeconds = ViewModel.CurrentAudioVM.Duration.TimeSpan.TotalSeconds; // WavFileUtils.GetSoundLength(ViewModel.AudioFilePath);
                ViewModel.TranscribeNewFile();
            });

        }

        private void OnTargetUpdated(object sender, DataTransferEventArgs e)
        {
            var mediaElement = sender as MediaElement;
        }

        private void OnMediaEnded(object sender, RoutedEventArgs e)
        {
            var mediaElement = sender as MediaElement;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var mediaElement = sender as MediaElement;
            var vm = mediaElement.DataContext as TrackSnippetViewModel;
            if (vm.FilePath == null)
            {
                return;
            }

            vm.CreatePlayerStoryboard(mediaElement);
        }

        private async void btnStopClick(object sender, RoutedEventArgs e)
        {
            var ctrl = sender as Button;
            var vm = ctrl.DataContext as TrackSnippetViewModel;

            vm.MarkerStoryboard.Stop();

            await Task.Delay(10);

            vm.TrackStopCommand.Execute(vm.PlayerStoryboard);
        }

        private async void btnPauseClick(object sender, RoutedEventArgs e)
        {
            var ctrl = sender as Button;
            var vm = ctrl.DataContext as TrackSnippetViewModel;

            if (!vm.IsPaused)
            {
                vm.MarkerStoryboard_Pause();
            }
            else
            {
                vm.MarkerStoryboard_Resume();
            }

            await Task.Delay(10);

            // TO DO: Yuk. Below SETS the IsPaused flag, so is after the MarkerStoryBoard which needs current state
            vm.TrackPauseCommand.Execute(vm.PlayerStoryboard);

        }

        private void btnMergeClick(object sender, RoutedEventArgs e)
        {
            var ctrl = sender as Button;
            var vm = ctrl.DataContext as TrackSnippetViewModel;
            vm.SelectedWordsMergeCommand.Execute(this);
        }

        private void btnRightSpacerClick(object sender, RoutedEventArgs e)
        {
            var ctrl = sender as Button;
            var vm = ctrl.DataContext as TrackSnippetViewModel;
            vm.AddSpaceBeforeCommand.Execute(this);
        }

        private void evtBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // Prevent scrolling, so it doesn't move when selected through trying to move the thumb
            e.Handled = true;
        }
        
        private void btnChooseFolder(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog { ShowNewFolderButton = false };
            dialog.SelectedPath = ViewModel.FilesFolder;
            dialog.ShowDialog();
            ViewModel.FilesFolder = dialog.SelectedPath;
        }

        bool isMovingTextStart;
        bool isMovingTextEnd;
        Point startPos;
        TextPart currentMovingTextPart;

        private void StartThumbMouseDown(object sender, MouseButtonEventArgs e)
        {
            isMovingTextEnd = false;
            isMovingTextStart = true;
            startPos = e.GetPosition(ctrlMain);
            var ele = sender as FrameworkElement;
            currentMovingTextPart = ele.DataContext as TextPart;
        }

        private void TextChangeMouseMoveEvent(object sender, MouseEventArgs e)
        {
            if (!isMovingTextEnd && !isMovingTextStart)
            {
                return;
            }

            var pos = e.GetPosition(ctrlMain);
            var xChange = pos.X - startPos.X;
            xChange /= ViewModel.Scale;

            if (isMovingTextStart)
            {
                if (currentMovingTextPart.TextWidth - xChange > 40)
                {
                    currentMovingTextPart.TextWidth -= xChange;
                    currentMovingTextPart.StartMills += xChange;
                }
            }
            else
            {
                if (currentMovingTextPart.TextWidth + xChange > 40)
                {
                    currentMovingTextPart.TextWidth += xChange;
                }
            }
            startPos = pos;
        }

        private void EndThumbMouseDown(object sender, MouseButtonEventArgs e)
        {
            isMovingTextStart = false;
            isMovingTextEnd = true;
            startPos = e.GetPosition(ctrlMain);
            var ele = sender as FrameworkElement;
            currentMovingTextPart = ele.DataContext as TextPart;
        }

        private void MouseLeftButtLeave(object sender, MouseEventArgs e)
        {
            isMovingTextStart = false;
            isMovingTextEnd = false;
        }

        private void MouseLeftButtUp(object sender, MouseButtonEventArgs e)
        {
            isMovingTextStart = false;
            isMovingTextEnd = false;
        }

        private void ImportWavFromMedia_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog { Multiselect = false };
            dialog.ShowDialog();
            var fileToExplode = dialog.FileName;
            var newWavFileName = fileToExplode.Substring(0, fileToExplode.LastIndexOf(".")) + ".wav";

            WavFileUtils.ExtractWavFromMedia(fileToExplode, newWavFileName);

            var newFilePath = System.IO.Path.GetFullPath(newWavFileName);
            if (!string.IsNullOrWhiteSpace(ViewModel.FilesFolder) && ViewModel.FilesFolder != newFilePath)
            {
                var answ = MessageBox.Show("Do you want to move the new wav file to the above projects folder?", "Import this file to the project folder?", MessageBoxButton.YesNo);
                if (answ == MessageBoxResult.Yes)
                {
                    var fileNamePart = System.IO.Path.GetFileName(newWavFileName);
                    File.Move(newWavFileName, System.IO.Path.Combine(ViewModel.FilesFolder, fileNamePart));
                    ViewModel.LoadFilesFromSelectedFolder();
                }
            }
        }

        public void Dispose()
        {
            ViewModel.CheckForChanges();
        }
    }
}
