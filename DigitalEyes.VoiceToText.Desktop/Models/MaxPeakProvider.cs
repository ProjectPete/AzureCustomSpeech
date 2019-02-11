using System;
using System.Linq;

namespace DigitalEyes.VoiceToText.Desktop.Models
{
    public class MaxPeakProvider : PeakProvider
    {
        public override PeakInfo GetNextPeak()
        {
            try
            {
                var samplesRead = Provider.Read(ReadBuffer, 0, ReadBuffer.Length);
                var max = (samplesRead == 0) ? 0 : ReadBuffer.Take(samplesRead).Max();
                var min = (samplesRead == 0) ? 0 : ReadBuffer.Take(samplesRead).Min();
                return new PeakInfo(min, max);
            }
            catch (Exception ex)
            {
                ErrorMessage.Raise(ex);
                return null;
            }
        }
    }
}