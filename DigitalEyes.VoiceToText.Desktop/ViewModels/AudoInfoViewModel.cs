using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DigitalEyes.VoiceToText.Desktop.ViewModels
{
    public class AudoInfoViewModel : BaseViewModel
    {
        public string FileName { get; set; }

        private WaveFormat waveFormat;

        public WaveFormat WaveFormat
        {
            get
            {
                return waveFormat;
            }
            set
            {
                if (waveFormat != value)
                {
                    waveFormat = value;
                    RaisePropertyChanged("WaveFormat");
                }
            }
        }

        private Duration duration;

        public Duration Duration
        {
            get
            {
                return duration;
            }
            set
            {
                if (duration != value)
                {
                    duration = value;
                    RaisePropertyChanged("Duration");
                }
            }
        }

        private double actualPointsCollected;

        public double ActualPointsCollected
        {
            get
            {
                return actualPointsCollected;
            }
            set
            {
                if (actualPointsCollected != value)
                {
                    actualPointsCollected = value;
                    RaisePropertyChanged("ActualPointsCollected");
                }
            }
        }
    }
}
