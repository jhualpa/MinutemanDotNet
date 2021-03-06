﻿namespace Minuteman.Dashboard
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class DataPublisher : IDisposable
    {
        private readonly EventActivity publisher;
        private readonly Timer timer;

        public DataPublisher()
        {
            publisher = new EventActivity(
                new ActivitySettings(ActivityTimeframe.Second));

            timer = new Timer(
                Update,
                null,
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(1));
        }

        public void Dispose()
        {
            timer.Dispose();
        }

        private void Update(object state)
        {
            var count = new Random().Next(0, 11);
            var timestamp = DateTime.UtcNow;

            if (count < 1)
            {
                return;
            }

            var counter = 1;
            var tasks = new List<Task>();

            while (counter <= count)
            {
                tasks.Add(publisher.Track("order:placed", timestamp, true));
                counter++;
            }

            Task.WhenAll(tasks).Wait();
        }
    }
}