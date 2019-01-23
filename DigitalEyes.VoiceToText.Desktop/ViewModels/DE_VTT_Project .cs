using DigitalEyes.VoiceToText.Desktop.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DigitalEyes.VoiceToText.Desktop.ViewModels
{
    [DataContract]
    public class DE_VTT_Project
    {
        [DataMemberAttribute]
        public string Name { get; set; }
        [DataMemberAttribute]
        public ObservableCollection<TrackSnippetViewModel> Snippets { get; set; } = new ObservableCollection<TrackSnippetViewModel>();
        [DataMemberAttribute]
        public string OriginalFilePath { get; set; }

        public List<transLog> TranscribeLogs { get; set; } = new List<transLog>();

        public int SnippetsOkTextPartCount
        {
            get
            {
                var ix = 0;
                foreach(var snip in Snippets)
                {
                    ix += snip.TextParts.Where(a => a.IsOK).Count();
                }
                return ix;
            }
        }

        string folderName;
        public string FolderName
        {
            get
            {
                if (folderName == null)
                {
                    folderName = Path.GetFileNameWithoutExtension(OriginalFilePath);
                    string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
                    string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
                    folderName = System.Text.RegularExpressions.Regex.Replace(folderName, invalidRegStr, "_");
                }
                return folderName;
            }
            set
            {
                if (folderName != value)
                {
                    folderName = value;
                }
            }
        }

    }
}
