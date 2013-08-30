﻿namespace Minuteman
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    using BookSleeve;

    public class UserActivity : IUserActivity
    {
        private const string EventsKeyName = "EVENTS";

        private static Func<RedisConnection> connectionFactory = () => 
            new RedisConnection("localhost");
 
        public UserActivity() : this(new UserActivitySettings())
        {
        }

        public UserActivity(UserActivitySettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            Settings = settings;
        }

        public static Func<RedisConnection> ConnectionFactory
        {
            get
            {
                return connectionFactory; 
            }

            set
            {
                connectionFactory = value;
            }
        }

        public UserActivitySettings Settings { get; private set; }

        public virtual async Task Track(
            string eventName,
            DateTime timestamp,
            params long[] users)
        {
            Validation.ValidateEventName(eventName);
            Validation.ValidateUsers(users);

            var timeframeKeys = GenerateEventTimeframeKeys(
                eventName,
                timestamp).ToList();

            var eventsKey = GenerateKey(EventsKeyName);

            using (var connection = await OpenConnection())
            {
                var tasks = new List<Task>();

                foreach (var timeframeKey in timeframeKeys)
                {
                    tasks.AddRange(users.Select(user => 
                        connection.Strings.SetBit(
                            Settings.Db,
                            timeframeKey,
                            user,
                            true)));
                }

                tasks.Add(connection.Sets.Add(
                    Settings.Db,
                    eventsKey,
                    eventName));

                await Task.WhenAll(tasks);
            }
        }

        public virtual UsersResult Users(string eventName, DateTime timestamp)
        {
            Validation.ValidateEventName(eventName);

            var eventKey = GenerateEventTimeframeKeys(eventName, timestamp)
                .ElementAt((int)Settings.Drilldown);

            return new UsersResult(Settings.Db, eventKey);
        }

        public virtual async Task<IEnumerable<string>> EventNames()
        {
            var eventsKey = GenerateKey(EventsKeyName);
            string[] names;

            using (var connection = await OpenConnection())
            {
                names = await connection.Sets
                    .GetAllString(Settings.Db, eventsKey);
            }

            var result = names.Select(n => RemoveKeyPrefix(EventsKeyName, n));

            return result;
        }

        public virtual async Task<long> Reset()
        {
            long result = 0;

            using (var connection = await OpenConnection())
            {
                var keys = await connection.Keys.Find(
                    Settings.Db,
                    Settings.KeyPrefix + "*");

                if (keys.Any())
                {
                    result = await connection.Keys.Remove(Settings.Db, keys);
                }
            }

            return result;
        }

        internal static async Task<RedisConnection> OpenConnection()
        {
            RedisConnection connection = null;

            try
            {
                connection = ConnectionFactory();
                await connection.Open();
            }
            catch (Exception)
            {
                if (connection != null)
                {
                    connection.Dispose();
                }

                throw;
            }

            return connection;
        }

        internal string GenerateKey(params string[] parts)
        {
            var key = Settings.KeyPrefix +
                Settings.KeySeparator +
                string.Join(
                    Settings.KeySeparator,
                    parts.Select(p => p.ToUpperInvariant()));

            return key;
        }

        internal IEnumerable<string> GenerateEventTimeframeKeys(
            string eventName,
            DateTime timestamp)
        {
            Func<string> formatYear = () => Format(timestamp.Year, "d4");
            Func<string> formatMonth = () => Format(timestamp.Month);
            Func<string> formatDay = () => Format(timestamp.Day);
            Func<string> formatHour = () => Format(timestamp.Hour);
            Func<string> formatMinute = () => Format(timestamp.Minute);

            var drilldown = (int)Settings.Drilldown;

            yield return GenerateKey(
                eventName,
                formatYear());

            if (drilldown > (int)UserActivityDrilldownType.Year)
            {
                yield return GenerateKey(
                    eventName, 
                    formatYear(),
                    formatMonth());
            }

            if (drilldown > (int)UserActivityDrilldownType.Month)
            {
                yield return GenerateKey(
                    eventName, 
                    formatYear(),
                    formatMonth(),
                    formatDay());
            }

            if (drilldown > (int)UserActivityDrilldownType.Day)
            {
                yield return GenerateKey(
                    eventName, 
                    formatYear(),
                    formatMonth(),
                    formatDay(),
                    formatHour());
            }

            if (drilldown > (int)UserActivityDrilldownType.Hour)
            {
                yield return GenerateKey(
                    eventName, 
                    formatYear(),
                    formatMonth(),
                    formatDay(),
                    formatHour(),
                    formatMinute());
            }
        }

        private static string Format(int value, string format = "d2")
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private string RemoveKeyPrefix(string prefix, string value)
        {
            var fullPrefix = GenerateKey(prefix);
            var index = value.IndexOf(fullPrefix, StringComparison.Ordinal);

            var result = index < 0 ? value : value.Substring(index);

            return result;
        }
    }
}