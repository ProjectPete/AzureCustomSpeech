using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalEyes.VoiceToText.Desktop.Models
{
        public interface IPeakProvider
        {
            void Init(ISampleProvider reader, int samplesPerPixel);

            PeakInfo GetNextPeak();
        }
}
