﻿namespace Minuteman
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public static class EventActivityExtensions
    {
        public static Task<long> Track(
            this IEventActivity instance,
            string eventName)
        {
            return Track(instance, eventName, DateTime.UtcNow);
        }

        public static Task<long> Track(
            this IEventActivity instance,
            string eventName,
            bool publishable)
        {
            return Track(instance, eventName, DateTime.UtcNow, publishable);
        }

        public static Task<long> Track(
            this IEventActivity instance,
            string eventName,
            DateTime timestamp)
        {
            return Track(instance, eventName, timestamp, false);
        }

        public static Task<long> Track(
            this IEventActivity instance,
            string eventName,
            DateTime timestamp,
            bool publishable)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            return instance.Track(
                eventName,
                instance.Settings.Timeframe,
                timestamp,
                publishable);
        }

        public static Task<long[]> Counts(
            this IEventActivity instance,
            string eventName,
            DateTime startTimestamp,
            DateTime endTimestamp)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            return instance.Counts(
                eventName,
                startTimestamp,
                endTimestamp,
                instance.Settings.Timeframe);
        }

        public static Task<long> Count(
            this IEventActivity instance,
            string eventName,
            DateTime startTimestamp,
            DateTime endTimestamp)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            return Count(
                instance,
                eventName,
                startTimestamp,
                endTimestamp,
                instance.Settings.Timeframe);
        }

        public static async Task<long> Count(
            this IEventActivity instance,
            string eventName,
            DateTime startTimestamp,
            DateTime endTimestamp,
            ActivityTimeframe timeframe)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            var result = await instance.Counts(
                eventName,
                startTimestamp,
                endTimestamp,
                timeframe);

            return result.First();
        }
    }
}