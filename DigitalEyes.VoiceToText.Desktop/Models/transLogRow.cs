using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalEyes.VoiceToText.Desktop.Models
{
    class transLogRow
    {
        public SpeechRecognitionEventArgs Args { get; set; }
        public string Notes { get; set; }
    }
}
