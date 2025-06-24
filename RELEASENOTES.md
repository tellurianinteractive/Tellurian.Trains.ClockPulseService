# Release Notes
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
  Thanks to Claudia Mühl for the information.

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
