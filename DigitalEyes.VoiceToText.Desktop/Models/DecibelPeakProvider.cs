using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalEyes.VoiceToText.Desktop.Models
{
    class DecibelPeakProvider : IPeakProvider
    {
        private readonly IPeakProvider sourceProvider;
        private readonly double dynamicRange;

        public DecibelPeakProvider(IPeakProvider sourceProvider, double dynamicRange)
        {
            this.sourceProvider = sourceProvider;
            this.dynamicRange = dynamicRange;
        }

        public void Init(ISampleProvider reader, int samplesPerPixel)
        {
            throw new NotImplementedException();
        }

        public PeakInfo GetNextPeak()
        {
            var peak = sourceProvider.GetNextPeak();
            var decibelMax = 20 * Math.Log10(peak.Max);
            if (decibelMax < 0 - dynamicRange) decibelMax = 0 - dynamicRange;
            var linear = (float)((dynamicRange + decibelMax) / dynamicRange);
            return new PeakInfo(0 - linear, linear);
        }
    }
}
