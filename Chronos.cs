using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

namespace LegendaryTools.Chronos
{
    public interface IChronos : IDisposable
    {
        bool IsInitialized { get; }
        TimeSpan LastElapsedTimeWhileAppIsClosed { get; }
        DateTime LastRecordedDateTimeUtc { get; }
        DateTime NowUtc { get; }
        event Action<TimeSpan> ElapsedTimeWhileAppWasPause;
        event Action<TimeSpan> ElapsedTimeWhileAppLostFocus;
        Task Initialize();
        void UpdateUtcNow();
        Task<(bool, DateTime)> GetDateTime();
        Task<(bool, DateTime)> GetDateTimeUtc();
        void Dispose();
    }

    public class Chronos : IChronos
    {
        public bool IsInitialized => isInitialized;
        public TimeSpan LastElapsedTimeWhileAppIsClosed { get; private set; }
        public DateTime LastRecordedDateTimeUtc
        {
            get => DateTime.ParseExact(
                PlayerPrefs.GetString(LastRecordedDateTimeKey,
                    DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)), "o", CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
            private set
            {
                lastUnscaledTimeAsDouble = Time.unscaledTimeAsDouble;
                PlayerPrefs.SetString(LastRecordedDateTimeKey, value.ToString("o", CultureInfo.InvariantCulture));
            }
        }
        
        public DateTime NowUtc => LastRecordedDateTimeUtc.AddSeconds(Time.unscaledTimeAsDouble - lastUnscaledTimeAsDouble);

        private readonly ChronosConfig config;
        private readonly IMonoBehaviourFacade monoBehaviourFacade;
        private static readonly string LastRecordedDateTimeKey = "LastRecordedDateTimeKey";
        private static readonly string FirstStartKey = "FirstStart";
        
        private double lastUnscaledTimeAsDouble;
        private double lastUnscaledTimeAsDoubleSinceLoseFocus;
        private double lastUnscaledTimeAsDoubleSinceGamePaused;
        
        private bool isInitialized;

        public event Action<TimeSpan> ElapsedTimeWhileAppWasPause;
        public event Action<TimeSpan> ElapsedTimeWhileAppLostFocus;
        
        private bool FirstStart
        {
            get => System.Convert.ToBoolean(PlayerPrefs.GetInt(FirstStartKey, 1));
            set => PlayerPrefs.SetInt(FirstStartKey, System.Convert.ToInt32(value));
        }

        public Chronos(ChronosConfig config, IMonoBehaviourFacade monoBehaviourFacade)
        {
            this.config = config;
            this.monoBehaviourFacade = monoBehaviourFacade;
            
            monoBehaviourFacade.OnApplicationFocused += OnApplicationFocus;
            monoBehaviourFacade.OnApplicationPaused += OnApplicationPause;
        }

        public async Task Initialize()
        {
            (bool, DateTime) result = await GetDateTimeUtc();

            if (FirstStart)
            {
                LastElapsedTimeWhileAppIsClosed = TimeSpan.Zero;
                LastRecordedDateTimeUtc = result.Item2;
                FirstStart = false;
                isInitialized = true;
            }
            else
            {
                if (result.Item2 > LastRecordedDateTimeUtc)
                {
                    LastElapsedTimeWhileAppIsClosed = result.Item2 - LastRecordedDateTimeUtc;
                    LastRecordedDateTimeUtc = result.Item2;
                    isInitialized = true;
                }
            }
        }
        
        public async void UpdateUtcNow()
        {
            (bool, DateTime) result = await GetDateTimeUtc();
            if (result.Item2 > LastRecordedDateTimeUtc)
            {
                LastElapsedTimeWhileAppIsClosed = result.Item2 - LastRecordedDateTimeUtc;
                LastRecordedDateTimeUtc = result.Item2;
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                if(IsInitialized)
                    ElapsedTimeWhileAppLostFocus?.Invoke(TimeSpan.FromSeconds(Time.unscaledTimeAsDouble - lastUnscaledTimeAsDoubleSinceLoseFocus));
            }
            else
            {
                lastUnscaledTimeAsDoubleSinceLoseFocus = Time.unscaledTimeAsDouble;
            }
        }
        
        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                lastUnscaledTimeAsDoubleSinceGamePaused = Time.unscaledTimeAsDouble;
                
            }
            else
            {
                if(IsInitialized)
                    ElapsedTimeWhileAppWasPause?.Invoke(TimeSpan.FromSeconds(Time.unscaledTimeAsDouble - lastUnscaledTimeAsDoubleSinceGamePaused));
            }
        }

        public async Task<(bool, DateTime)> GetDateTime()
        {
            foreach (DateTimeProvider provider in config.WaterfallProviders)
            {
                (bool, DateTime) result = await provider.GetDateTime();
                if (result.Item1) return result;
            }

            return (false, default);
        }

        public async Task<(bool, DateTime)> GetDateTimeUtc()
        {
            foreach (DateTimeProvider provider in config.WaterfallProviders)
            {
                (bool, DateTime) result = await provider.GetDateTimeUtc();
                if (result.Item1) return result;
            }

            return (false, default);
        }

        public static void ClearPersistentData()
        {
            PlayerPrefs.DeleteKey(LastRecordedDateTimeKey);
            PlayerPrefs.DeleteKey(FirstStartKey);
        }
        
        public void Dispose()
        {
            monoBehaviourFacade.OnApplicationFocused -= OnApplicationFocus;
            monoBehaviourFacade.OnApplicationPaused -= OnApplicationPause;
        }
    }
}