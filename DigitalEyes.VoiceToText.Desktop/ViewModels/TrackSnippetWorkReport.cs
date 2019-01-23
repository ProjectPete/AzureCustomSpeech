using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalEyes.VoiceToText.Desktop.ViewModels
{
    public class TrackSnippetWorkReport
    {
        public int TotalParts { get; set; }
        public int TotalApproved { get; set; }
        public bool HasApproved { get; set; }
    }
}
