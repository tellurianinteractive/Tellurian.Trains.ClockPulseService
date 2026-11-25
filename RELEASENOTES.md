# Release Notes
## Release 2.0.1
Release date 2026-11-25
- **Fixed ZNServerSink time with 12-hour analogue clocks.** When `Use12HourClock` was set, the ZN server could get the wrong time: 06:00 instead of 18:00, and times that drifted across noon. It could also start from the saved analogue clock time instead of the Fastclock time.
  The ZN server now always gets the Fastclock time in 24-hour format, sent each time the time changes.
- **New optional sink interface `ITimeSink`** for sinks that need the actual Fastclock time instead of minute pulses. See [Writing a Sink](https://github.com/tellurianinteractive/Tellurian.Trains.ClockPulseService/wiki/Writing-a-Sink).
## Release 2.0.0
Release date 2026-11-25
> The major version marks the new name. The release is backwards compatible; see below.
- **Renamed to Clock Integration Service** (formerly *Clock Pulse Service*). The service now does more than drive analogue clocks with pulses. For example, it also sends the time to FREMO ZN servers.
  - The README now lists the available sinks and who contributed them.
  - Background, installation, configuration and sink details have moved to the [wiki](https://github.com/tellurianinteractive/Tellurian.Trains.ClockPulseService/wiki).
  - Log output and the systemd service description now use the new name.
- **Backwards compatible**: you can update an existing installation without changes. These stay the same:
  - The `appsettings.json` format, including all section and setting names. Your existing file still works.
  - `AnalogueTime.txt` and the `-r` and `-t` command line options.
  - The executable name `Tellurian.Trains.ClockPulseApp.Service`.
  - On Linux, the systemd unit `clockpulse.service` and the default installation folder `/opt/clockpulse`, so `deploy.sh` updates an existing installation.
  - The download ZIP names (`clockpulseservice-<platform>.zip`) and the GitHub repository address.
  > NOTE: The only visible difference is that log lines are tagged *Clock Integration Service* instead of *Tellurian.Trains.ClockPulseApp.Service.Worker*.
  If you filter or search logs by the old name, update the filter.
- **Upgraded to .NET 11** with the latest Microsoft packages.
- **Updated Tellurian.Trains.MeetingApp.Contracts** to 3.9.0.
## Release 1.14.0
Release date 2026-03-26
- **Updated components** to latest release.
- **Upgraded test framework** to Microsoft Testing Platform with MSTest SDK.
- **Added CI/CD** with GitHub Actions for automated releases on version bump.
- **Deployment** now publishes to GitHub Releases instead of folder publishing.
## Release 1.13.1
Release date 2025-11-24
- **Upgrade to .NET10** with latest Microsoft packages.
- **Deployment for Linux 32-bit ARM** changed name of zip folder from linux-arm-32 to to **linux-arm** to match runtime identifier used in .NET.
## Release 1.13.0
Release date 2025-06-24
- **Upgrade to .NET9** with latest Microsoft packages.
- **ZNServerSink** added. Thanks to Dirkjan Kaper.
- **UdpBroadcast** sink renamed to **UdpBroadcastSink**. If you use this sink, the name change is a ***breaking change***. You need to change the name in *appsettings.json*.
## Release 1.12.0
Release date 2025-09-16
- **Security update** with latest external components.
## Release 1.11.0
Release date 2024-03-25:
- **Simplified settings**, where you only sets *clock name* and optionally set if cloud clock shoud be used.
  > NOTE that the *RemoteClockTimeHref* is now obsolete and replaced by new settings in *appsettings.json*,
so you need to update this file.
- **Better documentation** in the *appsettings.json* file.
- **Updated wiring** of the *RPI Relay Board*.

## Release 1.10.0 
Release date 2024-02-21:
- **Upgrade to .NET8** with latest Microsoft packages.
- **API update** to latest version of the Fastclock API to handle *session break*.

## Release 1.9.0
Release date 2024-01-11:
- **Upgrade to .NET8** with latest Microsoft packages.
- **API update** to latest version of the Fastclock API.
- **Clock started/stopped indication** for the *RPI Relay Board* can now be configured for red/green or alarm indication.
  - *Red/green* means that the third relay can be used separate indication when clock is running and when stopped.
  - *Alarm* means that a single attention indication is striggered for a few seconds when clock is started or stopped.

## Release 1.8.0
Release date 2023-07-15:
- **Security update** with latest Microsoft packages.
- **API update** to latest version of the Fastclock API.

## Release 1.7.1
Release date 2023-06-24:
- **Bug fix** of stopped criteria.

## Release 1.7.0
Release date 2023-06-18:
- **Rpi Relay Board** sink now uses relay 1 to signal when the clock is stopped
  and relay 2 & 3 to control pulses to clocks.
  During the pause between pulses, the clock wires are shortcut.
  > NOTE: Wiring has changed. Zero voltage is now when both relays are in the **same** position. 
  > This is to prevent current from flowing when both relays are off. 
- **Session completed** can now be detected by sinks. The event is sent some seconds
  after the clock stopped, so that a *stopped indicator* can be active during this time.
  The duration of the session ended signal can be set in *appsettings.json*.

## Release 1.6.0
Release date 2023-05-02:
- **Rpi Relay Board Sink** now shortcuts the clock wires when voltage becomes zero
  to reduce inductive spikes in the clock circuit.
  Thanks to Claudia M�hl for the information.

## Release 1.5.5
Release date 2023-04-27:
- **Parallelized notifications** to sinks.
- **Time restrictions** for sink methods documented for those who implement a sink.
- **Analogue time** logging is now presented in better sync with how pulses influence an analogue clock.
- **RPI Relay Board** sink implementation changed to eliminate need of extra relays.
  - Relay 1 is now *voltage on* and is acitvated for each pulse.
  - Relay 2 and 3 controls *polarity* and feeds the RUT clocks.
  Relay 2 & 3 is activated 250 ms before relay 1 to ensure that polarity is set
  before voltage is turned on.
- Interface **IControlSink** is not any longer inherited in *IPulseSink*, 
  because initialization and clean-up is not needed for all type of implementations.
  Implementation of *IControlSink* is optional.
- **Actual analouge time** as displayed on clocks can now be given when starting the app.
  The time must be in format *hh:mm*.
  Example: 
    ```./Tellurian.Trains.ClockPulseApp.Service -t 10:35```

## Release 1.4.0
Release date 2022-07-22:
- **Improved analouge clock synchronisation** with the option to flip polarity in *appsettings.json*.
- **More robust error handling** for example that sinks that casuses errors does not influence other parts of the application.

## Release 1.3.0
Release date 2022-03-31:
- **Monitoring of clock running or not** through the new sink type *IStatusSink*. 
If this interface is optional to implement in a sink. 
You can control indicators whether the clock is running or not.
- **Analogue time initialisation** is now in a separate file *AnalogueTime.txt*. While this file can be manual edited, it will be overwritten
with the current analouge time. This makes restart of the service better, because it *remebers* the last analogue time.
If the file does not exist or for some reason cannon be written, the *AnalogueClockStartTime* in *appsettings.json* will be used.

END OF RELEASE NOTE
