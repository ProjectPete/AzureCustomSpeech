﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalEyes.VoiceToText.Desktop.Models
{
    public class SampleAggregator
    {
        public event EventHandler<MaxSampleEventArgs> MaximumCalculated;
        public event EventHandler Restart = delegate { };
        private float maxValue;
        private float minValue;
        public int NotificationCount { get; set; }
        int count;
        public int total { get; set; }

        public void RaiseRestart()
        {
            Restart(this, EventArgs.Empty);
        }

        private void Reset()
        {
            count = 0;
            maxValue = minValue = 0;
        }

        public void Add(float value)
        {
            maxValue = Math.Max(maxValue, value);
            minValue = Math.Min(minValue, value);
            count++;
            total++;
            if (count >= NotificationCount && NotificationCount > 0)
            {
                if (MaximumCalculated != null)
                {
                    MaximumCalculated(this, new MaxSampleEventArgs(minValue, maxValue, count));
                }
                Reset();
            }
        }
    }

    public class MaxSampleEventArgs : EventArgs
    {
        [DebuggerStepThrough]
        public MaxSampleEventArgs(float minValue, float maxValue, int totalPoints)
        {
            MaxSample = maxValue;
            MinSample = minValue;
            TotalPoints = totalPoints;
        }
        public float MaxSample { get; private set; }
        public float MinSample { get; private set; }
        public int TotalPoints { get; private set; }
    }

}
