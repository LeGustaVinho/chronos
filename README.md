# Chronos
Chronos is a time management library for Unity projects, allowing you to track elapsed time while your application is paused or out of focus, as well as synchronize the current date and time using multiple date/time providers. Ideal for applications that require precise time tracking, even when the application is not active.

#### Features
- Time Tracking: Monitors elapsed time while the application is paused or has lost focus.
- Date/Time Synchronization: Requests the current date and time from multiple providers, following a defined priority order.
- Data Persistence: Stores the last recorded date/time to calculate elapsed time even after the application is closed.
- Custom Events: Fires events when the application is paused or loses focus, allowing integration with other parts of your code.

### How to install
#### - From OpenUPM:

- Open **Edit -> Project Settings -> Package Manager**
- Add a new Scoped Registry (or edit the existing OpenUPM entry)

| Name  | package.openupm.com  |
| ------------ | ------------ |
| URL  | https://package.openupm.com  |
| Scope(s)  | com.legustavinho  |

- Open Window -> Package Manager
- Click `+`
- Select `Add package by name...`
- Paste `com.legustavinho.legendary-tools-chronos` and click `Add`

#### Usage & Initialization
To use Chronos, first initialize it in your main script:

```csharp
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    private IChronos chronos;

    private async void Start()
    {
        ChronosConfig config = new ChronosConfig();
        config.WaterfallProviders = new List<DateTimeProvider> { /* your providers */ };

        chronos = new Chronos(config, this);
        await chronos.Initialize();
    }

    private void OnDestroy()
    {
        chronos.Dispose();
    }
}
```
#### Events
You can subscribe to the `ElapsedTimeWhileAppWasPause` and `ElapsedTimeWhileAppLostFocus` events to perform actions when elapsed time is calculated:

```csharp
chronos.ElapsedTimeWhileAppWasPause += OnAppPaused;
chronos.ElapsedTimeWhileAppLostFocus += OnAppLostFocus;

private void OnAppPaused(TimeSpan elapsed)
{
    Debug.Log($"Elapsed time while paused: {elapsed.TotalSeconds} seconds");
}

private void OnAppLostFocus(TimeSpan elapsed)
{
    Debug.Log($"Elapsed time while out of focus: {elapsed.TotalSeconds} seconds");
}
```
#### Manual Synchronization
If you need to manually synchronize the date/time, use the `Sync` method:

#### Date/Time Request
You can request the current date/time using the `RequestDateTime` and `RequestDateTimeUtc` methods:

```csharp
var (success, dateTime) = await chronos.RequestDateTimeUtc();
if (success)
{
    Debug.Log($"Data/Hora atual UTC: {dateTime}");
}
```

#### Classe Abstrata DateTimeProvider
```csharp
public abstract class DateTimeProvider : ScriptableObject 
{
    public int TimeOut;
    
    public abstract Task<(bool, DateTime)> GetDateTime(); 
    
    public abstract Task<(bool, DateTime)> GetDateTimeUtc(); 
}
```
Providers should implement methods to obtain the current date/time, preferably from trusted sources.

#### Contribution
Contributions are welcome! Feel free to open issues or submit pull requests with improvements and fixes.

#### License
This project is licensed under the MIT License. See the LICENSE file for more details.
