using DigitalEyes.VoiceToText.Desktop.ViewModels;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DigitalEyes.VoiceToText.Desktop.Models
{
    [DataContract]
    public class TextPart : BaseViewModel
    {
        [IgnoreDataMember]
        RelayCommand makeShorterCommand;
        public RelayCommand MakeShorterCommand
        {
            get
            {
                if (makeShorterCommand == null)
                {
                    makeShorterCommand = new RelayCommand(makeShorterExecute, false);
                }
                return makeShorterCommand;
            }
            set
            {
                if (makeShorterCommand != value)
                {
                    makeShorterCommand = value;
                    RaisePropertyChanged("MakeShorterCommand");
                }
            }
        }

        RelayCommand makeLongerCommand;
        [IgnoreDataMember]
        public RelayCommand MakeLongerCommand
        {
            get
            {
                if (makeLongerCommand == null)
                {
                    makeLongerCommand = new RelayCommand(makeLongerExecute, false);
                }
                return makeLongerCommand;
            }
            set
            {
                if (makeLongerCommand != value)
                {
                    makeLongerCommand = value;
                    RaisePropertyChanged("MakeLongerCommand");
                }
            }
        }

        //RelayCommand<TrackSnippetViewModel> playThisBitCommand;
        //[IgnoreDataMember]
        //public RelayCommand<TrackSnippetViewModel> PlayThisBitCommand
        //{
        //    get
        //    {
        //        if (playThisBitCommand == null)
        //        {
        //            playThisBitCommand = new RelayCommand<TrackSnippetViewModel>(doPlayThisBit);
        //        }
        //        return playThisBitCommand;
        //    }
        //}

        bool isOK;
        [DataMember]
        public bool IsOK
        {
            get
            {
                return isOK;
            }
            set
            {
                if (isOK != value)
                {
                    isOK = value;
                    RaisePropertyChanged("IsOK");
                    //GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<TextPart>(this, "OK");
                }
            }
        }
        
        //private void doPlayThisBit(TrackSnippetViewModel snippetVM)
        //{
        //    snippetVM.TrackStartCommand.Execute(null);
        //    GalaSoft.MvvmLight.Messaging.Messenger.Default.Send(this);
        //}
        
        private void makeLongerExecute()
        {
            TextWidth += 10;
        }

        private void makeShorterExecute()
        {
            TextWidth -= 10;
        }

        double textWidth;
        [DataMember]
        public double TextWidth
        {
            get
            {
                return textWidth;
            }
            set
            {
                if (textWidth != value)
                {
                    textWidth = value;
                    RaisePropertyChanged("TextWidth");
                }
            }
        }

        bool isSelected = false;
        [IgnoreDataMember]
        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    RaisePropertyChanged("IsSelected");
                }
            }
        }

        string text;
        [DataMember]
        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                if (text != value)
                {
                    text = value;
                    RaisePropertyChanged("Text");
                }
            }
        }

        double startMills;
        [DataMember]
        public double StartMills
        {
            get
            {
                return startMills;
            }
            set
            {
                if (startMills != value)
                {
                    startMills = value;
                    RaisePropertyChanged("StartMills");
                }
            }
        }
        
    }
}
