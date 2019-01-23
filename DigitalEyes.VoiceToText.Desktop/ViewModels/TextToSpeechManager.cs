using DigitalEyes.VoiceToText.Desktop.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DigitalEyes.VoiceToText.Desktop.ViewModels
{
    class TextToSpeechManager
    {
        public async Task GetSpeechAsync(string text, string filePath, SpeechEndpointConfig config)
        {
            string accessToken;
            SendNotification("Attempting token exchange. Please wait...\n");

            TTS_Authentication auth = new TTS_Authentication($"https://{config.Region}.api.cognitive.microsoft.com/sts/v1.0/issueToken", config.Key);
            try
            {
                accessToken = await auth.FetchTokenAsync().ConfigureAwait(false);
                SendNotification("Successfully obtained an access token. \n");
            }
            catch (Exception ex)
            {
                SendNotification("Failed to obtain an access token.");
                SendNotification(ex.ToString());
                SendNotification(ex.Message);
                return;
            }

            //string host = "https://westus.tts.speech.microsoft.com/cognitiveservices/v1";
            string host = config.Endpoint;

            //string body = @"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>
            //  <voice name='Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)'>" +
            //  text + "</voice></speak>";

            var body = text;

            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage())
                {
                    // Set the HTTP method
                    request.Method = HttpMethod.Post;
                    // Construct the URI
                    request.RequestUri = new Uri(host);
                    // Set the content type header
                    request.Content = new StringContent(body, Encoding.UTF8, "application/ssml+xml");
                    // Set additional header, such as Authorization and User-Agent
                    request.Headers.Add("Authorization", "Bearer " + accessToken);
                    request.Headers.Add("Connection", "Keep-Alive");
                    // Update your resource name
                    request.Headers.Add("User-Agent", "TranscribeSpeech");
                    request.Headers.Add("X-Microsoft-OutputFormat", "riff-24khz-16bit-mono-pcm");
                    // Create a request
                    SendNotification("Calling the TTS service. Please wait... \n");
                    using (var response = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        // Asynchronously read the response
                        using (var dataStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            SendNotification("Your speech file is being written to file...");
                            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Write))
                            {
                                await dataStream.CopyToAsync(fileStream).ConfigureAwait(false);
                                fileStream.Close();
                            }
                            SendNotification("\nYour file is ready. Press any key to exit.");
                        }
                    }
                }
            }
        }
        
        void SendNotification(string message)
        {
            GalaSoft.MvvmLight.Messaging.Messenger.Default.Send(this, message);
        }
    }
}
