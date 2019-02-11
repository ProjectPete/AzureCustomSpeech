using DigitalEyes.VoiceToText.Desktop.Models;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DigitalEyes.VoiceToText.Desktop
{
    public static class WavFileUtils
    {
        [DllImport("winmm.dll")]
        private static extern uint MciSendString(
            string command,
            StringBuilder returnValue,
            int returnLength,
            IntPtr winHandle);

        public static void TrimWavFile(string inPath, string outPath, TimeSpan cutFromStart, TimeSpan cutFromEnd)
        {
            using (WaveFileReader reader = new WaveFileReader(inPath))
            {
                using (WaveFileWriter writer = new WaveFileWriter(outPath, reader.WaveFormat))
                {
                    int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;

                    int startPos = (int)cutFromStart.TotalMilliseconds * bytesPerMillisecond;
                    startPos -= startPos % reader.WaveFormat.BlockAlign;

                    int endBytes = (int)cutFromEnd.TotalMilliseconds * bytesPerMillisecond;
                    endBytes -= endBytes % reader.WaveFormat.BlockAlign;
                    int endPos = (int)reader.Length - endBytes;

                    TrimWavFile(reader, writer, startPos, endPos);
                }
            }
        }

        private static void TrimWavFile(WaveFileReader reader, WaveFileWriter writer, int startPos, int endPos)
        {
            reader.Position = startPos;
            byte[] buffer = new byte[1024];
            while (reader.Position < endPos)
            {
                int bytesRequired = (int)(endPos - reader.Position);
                if (bytesRequired > 0)
                {
                    int bytesToRead = Math.Min(bytesRequired, buffer.Length);
                    int bytesRead = reader.Read(buffer, 0, bytesToRead);
                    if (bytesRead > 0)
                    {
                        writer.WriteData(buffer, 0, bytesRead);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public static int GetSoundLength(string fileName)
        {
            StringBuilder lengthBuf = new StringBuilder(32);

            MciSendString(string.Format("open \"{0}\" type waveaudio alias wave", fileName), null, 0, IntPtr.Zero);
            MciSendString("status wave length", lengthBuf, lengthBuf.Capacity, IntPtr.Zero);
            MciSendString("close wave", null, 0, IntPtr.Zero);

            return lengthBuf.Length;
        }

        public static void ExtractWavFromMp3(string sourceFile, string destinationFile)
        {
            using (Mp3FileReader reader = new Mp3FileReader(sourceFile))
            {
                WaveFileWriter.CreateWaveFile(destinationFile, reader);
            }
        }

        public static string ExtractWavFromMedia(string sourceFile, string destinationFile)
        {
            try
            {
                using (MediaFoundationReader reader = new MediaFoundationReader(sourceFile))
                {
                    var newFormat = new WaveFormat(8000, 16, 1);
                    using (var conversionStream = new WaveFormatConversionStream(newFormat, reader))
                    {
                        WaveFileWriter.CreateWaveFile(destinationFile, conversionStream);
                    }
                }

                return null;
            }
            catch (Exception exc)
            {
                return exc.Message;
            }
        }

        public static bool TakeClipAddSilence(string file, TimeSpan lengthSilence, TimeSpan startTime, TimeSpan length, string newFile)
        {
            try
            {
                //byte[] buffer = new byte[1024];
                AudioFileReader afr = new AudioFileReader(file);
                OffsetSampleProvider offsetter = new OffsetSampleProvider(afr)
                {
                    DelayBy = lengthSilence,
                    LeadOut = lengthSilence,
                    SkipOver = startTime,
                    Take = length
                };
                offsetter.ToMono();

                var provider = new SampleToWaveProvider(offsetter);

                WaveFileWriter.CreateWaveFile(newFile, provider);

                ChangeWaveFormat(newFile, 16000, 16, 1);

                return true;
            }
            catch (Exception exc)
            {
                ErrorMessage.Raise(exc);
                return false;
            }
        }

        public static void ChangeWaveFormat(string newFile, int rate, int bits, int channels)
        {
            using (MediaFoundationReader reader = new MediaFoundationReader(newFile))
            {
                var newFormat = new WaveFormat(rate, bits, channels);
                using (var conversionStream = new WaveFormatConversionStream(newFormat, reader))
                {
                    WaveFileWriter.CreateWaveFile(newFile.Replace(".wav", "b.wav"), conversionStream);
                }
                File.Delete(newFile);
                File.Move(newFile.Replace(".wav", "b.wav"), newFile);
            }
        }
    }
}
