using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DigitalEyes.VoiceToText.Desktop.Models
{
    public sealed class ErrorMessage
    {
        public Exception Exception { get; set; }
        public string CallingMethod { get; set; }
        public string CallerFilePath { get; set; }
        public int CallerLineNumber { get; set; }
        public DateTime WhenError { get; set; }

        private ErrorMessage(){}

        public static void Raise(Exception exception, [CallerMemberName] string callingMethod = "",
        [CallerFilePath] string callingFilePath = "",
        [CallerLineNumber] int callingFileLineNumber = 0)
        {
            var erMsg = new ErrorMessage
            {
                Exception = exception,
                CallerFilePath = callingFilePath,
                CallerLineNumber = callingFileLineNumber,
                CallingMethod = callingMethod,
                WhenError = DateTime.Now
            };

            Messenger.Default.Send(erMsg);
        }
    }
}
