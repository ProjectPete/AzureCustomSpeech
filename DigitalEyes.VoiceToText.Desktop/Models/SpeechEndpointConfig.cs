using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalEyes.VoiceToText.Desktop.Models
{
    public class SpeechEndpointConfig
    {
        public string Name { get; set; }
        public string Endpoint { get; set; }
        public string Key { get; set; }
        public string Region { get; set; }
    }
}
