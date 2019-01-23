using DigitalEyes.VoiceToText.Desktop.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DigitalEyes.VoiceToText.Desktop.Views
{
    /// <summary>
    /// Interaction logic for AudioAndMarkerControl.xaml
    /// </summary>
    public partial class AudioAndMarkerControl : UserControl
    {
        public AudioAndMarkerControl()
        {
            InitializeComponent();
        }
        
        private void OnTargetUpdated(object sender, DataTransferEventArgs e)
        {
           // var mediaElement = sender as MediaElement;
        }

        private void OnMediaEnded(object sender, RoutedEventArgs e)
        {
           // var mediaElement = sender as MediaElement;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var mediaElement = sender as MediaElement;
            var vm = mediaElement.DataContext as TrackSnippetViewModel;
            if (vm.FilePath == null)
            {
                return;
            }
            
        }
    }
}
