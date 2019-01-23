using DigitalEyes.VoiceToText.Desktop.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DigitalEyes.VoiceToText.Desktop.Views
{
    /// <summary>
    /// Interaction logic for WaveformStatsWindow.xaml
    /// </summary>
    public partial class WaveformStatsWindow : Window
    {
        public WaveformStatsWindow()
        {
            InitializeComponent();
            if (DataContext != null)
            {
                var vm = DataContext as AudoInfoViewModel;
                Title = System.IO.Path.GetFileName(vm.FileName);
            }
        }
    }
}
