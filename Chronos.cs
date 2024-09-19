using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

namespace LegendaryTools.Chronos
{
    public interface IChronos : IDisposable
    {
        bool IsInitialized { get; }
        bool WasFirstStart { get; }
        TimeSpan LastElapsedTimeWhileAppIsClosed { get; }
        DateTime LastRecordedDateTimeUtc { get; }
        DateTime NowUtc { get; }
        event Action<TimeSpan> ElapsedTimeWhileAppWasPause;
        event Action<TimeSpan> ElapsedTimeWhileAppLostFocus;
        Task Initialize();
        void UpdateUtcNow();
        Task<(bool, DateTime)> GetDateTime();
        Task<(bool, DateTime)> GetDateTimeUtc();
    }

    public class Chronos : IChronos
    {
#if ODIN_INSPECTOR 
        [Sirenix.OdinInspector.ShowInInspector]
#endif
        public bool IsInitialized => isInitialized;
        
#if ODIN_INSPECTOR 
        [Sirenix.OdinInspector.ShowInInspector]
#endif
        public bool WasFirstStart { get; private set; }
        
#if ODIN_INSPECTOR 
        [Sirenix.OdinInspector.ShowInInspector]
#endif
        public TimeSpan LastElapsedTimeWhileAppIsClosed { get; private set; }
        
#if ODIN_INSPECTOR 
        [Sirenix.OdinInspector.ShowInInspector]
#endif
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

#if ODIN_INSPECTOR 
        [Sirenix.OdinInspector.ShowInInspector]
#endif
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
        
#if ODIN_INSPECTOR 
        [Sirenix.OdinInspector.ShowInInspector]
#endif
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
            (bool, DateTime) currentTime = await GetDateTimeUtc();

            if (FirstStart)
            {
                LastElapsedTimeWhileAppIsClosed = TimeSpan.Zero;
                LastRecordedDateTimeUtc = currentTime.Item2;
                FirstStart = false;
                WasFirstStart = true;
                isInitialized = true;
            }
            else
            {
                if (currentTime.Item2 > LastRecordedDateTimeUtc)
                {
                    LastElapsedTimeWhileAppIsClosed = currentTime.Item2 - LastRecordedDateTimeUtc;
                    LastRecordedDateTimeUtc = currentTime.Item2;
                    isInitialized = true;
                }
            }
        }
        
        public async void UpdateUtcNow()
        {
            (bool, DateTime) result = await GetDateTimeUtc();
            if (result.Item2 > LastRecordedDateTimeUtc)
            {
                LastRecordedDateTimeUtc = result.Item2;
            }
        }

#if ODIN_INSPECTOR 
        [Sirenix.OdinInspector.Button]
#endif
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                Debug.Log($"OnApplicationFocus({hasFocus}) Time between lose focus {Time.unscaledTimeAsDouble - lastUnscaledTimeAsDoubleSinceLoseFocus} seconds");
                
                if(IsInitialized)
                    ElapsedTimeWhileAppLostFocus?.Invoke(TimeSpan.FromSeconds(Time.unscaledTimeAsDouble - lastUnscaledTimeAsDoubleSinceLoseFocus));
            }
            else
            {
                lastUnscaledTimeAsDoubleSinceLoseFocus = Time.unscaledTimeAsDouble;
            }
            
            Debug.Log($"OnApplicationFocus({hasFocus}) -> {NowUtc}");
        }

#if ODIN_INSPECTOR 
        [Sirenix.OdinInspector.Button]
#endif
        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                lastUnscaledTimeAsDoubleSinceGamePaused = Time.unscaledTimeAsDouble;
                
            }
            else
            {
                Debug.Log($"OnApplicationPause({isPaused}) Time between pause {Time.unscaledTimeAsDouble - lastUnscaledTimeAsDoubleSinceGamePaused} seconds");
                if(IsInitialized)
                    ElapsedTimeWhileAppWasPause?.Invoke(TimeSpan.FromSeconds(Time.unscaledTimeAsDouble - lastUnscaledTimeAsDoubleSinceGamePaused));
            }
            
            Debug.Log($"OnApplicationPause({isPaused}) -> {NowUtc}");
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