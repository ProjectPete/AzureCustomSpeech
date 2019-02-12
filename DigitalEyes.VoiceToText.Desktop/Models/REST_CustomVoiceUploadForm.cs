using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalEyes.VoiceToText.Desktop.Models
{
    class REST_CustomVoiceUploadForm
    {
        public string name { get; set; }
        public string description { get; set; }
        public string locale { get; set; } = "en-US";

        /// <summary>
        /// Can be: None, Language, Acoustic, Pronunciation, CustomVoice, LanguageGeneration
        /// </summary>
        public string dataImportKind { get; set; } = "CustomVoice";
        public string properties { get; set; }
        public string audiodata { get; set; }
        public string transcriptions { get; set; }
        public string languagedata { get; set; }

    }
}
