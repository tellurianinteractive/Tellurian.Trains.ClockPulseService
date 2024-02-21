# Clock Pulse Service

**This software calls the [*Fastclock API*](https://github.com/tellurianinteractive/Tellurian.Trains.ModuleMeetingApp/wiki/API-Guidelines)
and creates logical pulses to drive analogue clocks.**

> If your clock is not a pulse-driven 12-hour clock, then my software is not the right solution. 
The alternative is to call the clock API and getting the time. 

## Release Notes
Release 1.10.0 at 2024-02-21:
- **Upgrade to .NET8** with latest Microsoft packages.
- **API update** to latest version of the Fastclock API to handle *session break*.

Release 1.9.0 at 2024-01-11:
- **Upgrade to .NET8** with latest Microsoft packages.
- **API update** to latest version of the Fastclock API.
- **Clock started/stopped indication** for the *RPI Relay Board* can now be configured for red/green or alarm indication.
  - *Red/green* means that the third relay can be used separate indication when clock is running and when stopped.
  - *Alarm* means that a single attention indication is striggered for a few seconds when clock is started or stopped.

Release 1.8.0 at 2023-07-15:
- **Security update** with latest Microsoft packages.
- **API update** to latest version of the Fastclock API.

Release 1.7.1 at 2023-06-24:
- **Bug fix** of stopped criteria.

Release 1.7.0 at 2023-06-18:
- **Rpi Relay Board** sink now uses relay 1 to signal when the clock is stopped
  and relay 2 & 3 to control pulses to clocks.
  During the pause between pulses, the clock wires are shortcut.
  > NOTE: Wiring has changed. Zero voltage is now when both relays are in the **same** position. 
  > This is to prevent current from flowing when both relays are off. 
- **Session completed** can now be detected by sinks. The event is sent some seconds
  after the clock stopped, so that a *stopped indicator* can be active during this time.
  The duration of the session ended signal can be set in *appsettings.json*.

Release 1.6.0 at 2023-05-02:
- **Rpi Relay Board Sink** now shortcuts the clock wires when voltage becomes zero
  to reduce inductive spikes in the clock circuit.
  Thanks to Claudia Mühl for the information.

Release 1.5.5 at 2023-04-27:
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

Release 1.4.0 at 2022-07-22 adds the following:
- **Improved analouge clock synchronisation** with the option to flip polarity in *appsettings.json*.
- **More robust error handling** for example that sinks that casuses errors does not influence other parts of the application.

Release 1.3.0 at 2022-03-31 adds the following:
- **Monitoring of clock running or not** through the new sink type *IStatusSink*. 
If this interface is optional to implement in a sink. 
You can control indicators whether the clock is running or not.
- **Analogue time initialisation** is now in a separate file *AnalogueTime.txt*. While this file can be manual edited, it will be overwritten
with the current analouge time. This makes restart of the service better, because it *remebers* the last analogue time.
If the file does not exist or for some reason cannon be written, the *AnalogueClockStartTime* in *appsettings.json* will be used.

## Motivation 
The whole point with the software is to eliminate the need for any other
logic translating time to pulses in other software. 
This software also automatically *fast-forward* the pulsing if the analogue time is not 
same or one minute after master clock time.

### Running the Software
This application must be run on a computer with an I/O device that can 
be connected to analouge clocks in the hall, for example a RUT-socket.

If the *fastclock server* is running in the cloud, you need an Internet connection,
but if you run the *fastclock server* locally, 
you can run both this application and the *fastclock server* on the same computer,
for example on a Raspberry Pi.

### From Clock API to Pulses 
The main logic of this application makes frequent calls to the *Fasclock API*
and determines when pulses to analogue clocks should be changed.
The logical pulses are:
- **Positive voltage**, sent when moving analouge clock to even minute.
- **Negative voltage**, sent when moving analouge clock to odd minute.
- **Zero voltage**, is sent after the configured pulse length duration.

### Fast Forwarding
When the time provided by calling the *fastclock* API differs more that one minute, 
the pulses are generated at a faster speed to make analouge clocks to sync faster.

When the *fastclock* is reset to a new session, it sets the time the session start time.
This often differs from what is presented on the analogue clocks,
so fast-forwarding starts automatically until the analogue clocks also is at
the session start time.
Fast-forwarding speed should be determined by the slowest fast-forwarding clock. 

### Start and Stop Indication
It is possible to indicate when the clock is stopped and started.

### Support of Hardware
This is a .NET application that can be compiled to run on most platforms.
The software is desiged to make it easy to support different ways to control an analogue clock.
You only need to implement a piece of software called a *sink* (see below).

### Flexible Configuration
Much of the behaviour is configurable. 
See this [example](https://github.com/fjallemark/ClockPulseService/blob/master/Service/appsettings.json). 

### Sinks
A *sink* is a component that translates logical clock signals to some form of I/O.
There are some ready-made *sinks*:
- **UdpBroadcastPulseSink** broadcasts ASCII bytes for pulses '+', '-' and '_', for start/stop '|' and '-' and session completed 'X'.
- **SerialPortPulseSink** sets DTR high for positive voltage, and RTS high for negative voltage. 
However, it can be configured to use DTR only for both positive and negative voltage.
- **RpiRelayBoardPulseSink** using three relays to control pulses and polarities.
The [Rpi Relay Board](https://www.waveshare.com/wiki/RPi_Relay_Board) is a relay hat for *Raspberry Pi*
that works with this software when running on Linux.
- There is also a **LoggingPulseSink** that logs to the console.

### Deployments
To run the app locally, see [available deployments](https://onedrive.live.com/?id=DF287081A732D0D8%21302250&cid=DF287081A732D0D8). Select a folder and click download. 
It will be downloaded as a ZIP-file. Unzip and transfer to the computer you will run it on.

### Some Experiences
During March 2022, I started to make [Clock Pulse Service](https://github.com/fjallemark/ClockPulseService) 
for polling the [clock API](https://github.com/tellurianinteractive/Tellurian.Trains.ModuleMeetingApp/wiki/API-Guidelines) 
and generate data about pulses to send. 
I also bought a [RPi Relay Board](https://www.waveshare.com/wiki/RPi_Relay_Board) 
and mounted it om my old Raspberry Pi 2. 
Then I wrote an implementation of [IPulseSink](https://github.com/fjallemark/ClockPulseService/blob/master/Service/RpiRelayBoardPulseSink.cs) 
for that relay board, and have now run it on the Raspberry Pi to test it out, and after some adjustments, it seems to work as expected.

With the software, it is also possible to directly use the I/O-pins on the Raspberry Pi without the relay board. 
This is fine if another device controlling the clock only needs low current I/O. 

### Discussions
In [this discussion on GitHub](https://github.com/tellurianinteractive/Tellurian.Trains.ModuleMeetingApp/discussions/44), 
there are many ideas of how to translate a stream of times to pulses for controlling an pulsedriven analogue clock.
