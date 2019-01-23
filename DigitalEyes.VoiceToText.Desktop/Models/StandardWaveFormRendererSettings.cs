using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalEyes.VoiceToText.Desktop.Models
{
    public class StandardWaveFormRendererSettings : WaveFormRendererSettings
    {
        public StandardWaveFormRendererSettings()
        {
            PixelsPerPeak = 1;
            SpacerPixels = 0;
            TopPeakPen = Pens.Maroon;
            BottomPeakPen = Pens.Peru;
        }


        public override Pen TopPeakPen { get; set; }

        // not needed
        public override Pen TopSpacerPen { get; set; }

        public override Pen BottomPeakPen { get; set; }

        // not needed
        public override Pen BottomSpacerPen { get; set; }
    }
}
