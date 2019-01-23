using DigitalEyes.VoiceToText.Desktop.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalEyes.VoiceToText.Desktop.Properties
{
    public class ProjectSettings : ApplicationSettingsBase
    {
        private ProjectSettings() { } // Make it a singleton, can only use Instance.get
        
        static ProjectSettings singleton;

        [UserScopedSetting()]
        public Dictionary<string, SpeechEndpointConfig> SpeechEndpointConfigs
        {
            get
            {
                if (this["SpeechEndpointConfigs"] == null)
                {
                    this["SpeechEndpointConfigs"] = new Dictionary<string, SpeechEndpointConfig>();
                }
                return ((Dictionary<string, SpeechEndpointConfig>)this["SpeechEndpointConfigs"]);
            }
            set
            {
                this["SpeechEndpointConfigs"] = value;
            }
        }

        public static ProjectSettings Instance
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new ProjectSettings();
                }
                return singleton;
            }
        }
    }
}
