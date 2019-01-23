using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalEyes.VoiceToText.Desktop.Models
{
    public abstract class PeakProvider : IPeakProvider
    {
        protected ISampleProvider Provider { get; private set; }
        protected int SamplesPerPeak { get; private set; }
        protected float[] ReadBuffer { get; private set; }

        public void Init(ISampleProvider provider, int samplesPerPeak)
        {
            Provider = provider;
            SamplesPerPeak = samplesPerPeak;
            ReadBuffer = new float[samplesPerPeak];
        }

        public abstract PeakInfo GetNextPeak();
    }
}
