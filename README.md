# Clock Pulse Service

**This software calls the [*Fastclock API*](https://github.com/tellurianinteractive/Tellurian.Trains.ModuleMeetingApp/wiki/API-Guidelines)
and creates logical pulses to drive analogue clocks.**

> If your clock is not a pulse-driven 12-hour clock, then my software is not the right solution. 
The alternative is to call the clock API and getting the time. 
### News
Latest release is 1.3.2
- **Fix** of issue #1 *Handle when clock server is not available*.

Release 1.3.0 at 2022-03-31 adds the following:
- **Monitoring of clock running or not** through the new sink type *IStatusSink*. 
If this interface is implemented in a sink, you can control indicators whether the clock is running or not.
- **Analogue time initialisation** is now in a separate file *AnalogueTime.txt*. While this file can be manual edited, it will be overwritten
with the current analouge time. This makes restart of the service better, because it *remebers* the last analogue time.
If the file does not exist or for some reason cannon be written, the *AnalogueClockStartTime* in *appsettings.json* will be used.
### Motivation 
The whole point with the software is to eliminate the need for any 
logic translating time to pulses in other software. 
This software also automatically *fast-forward* the pulsing if the analogue time is not 
same or one minute after master clock time.
Fast-forwarding speed should be determined by the slowest fast-forwarding clock. 

Note that this application just gives the information, 
it is up to hardware providers to translate the logical pulses to actual voltages 
for driving an analogue clock.

The logical pulses are:
- **Positive voltage**, sent when moving analouge clock to even minute.
- **Negative voltage**, sent when moving analouge clock to odd minute.
- **Zero voltage**, is sent after the configured pulse length duration.


Much of the behaviour is [configurable](https://github.com/fjallemark/ClockPulseService/blob/master/Service/appsettings.json). 

### Sinks
A *sink* is a component that translates logical clock signals to some forme of I/O:
- **LoggingPulseSink** logs pulses to the console.
- **UdpBroadcastPulseSink** broadcasts ASCII bytes '+', '-' and '_'.
- **SerialPortPulseSink** sets DTR high for positive voltage, and RTS high for negative voltage. 
However, it can be configured to use DTR only for both positive and negative voltage.
- **RpiRelayBoardPulseSink** using three relays to control pulses and polarities.
The [Rpi Relay Board](https://www.waveshare.com/wiki/RPi_Relay_Board) is a relay hat for *Raspberry Pi*
ans works with this software when running on Linux.

### Deployments
To run the app locally, see [available deployments](https://onedrive.live.com/?id=DF287081A732D0D8%21302250&cid=DF287081A732D0D8). Select a folder and click download. 
It will be downloaded as a ZIP-file. Unzip and transfer to the computer you will run it on.

### A Real Example
As discussed earlier in this thread and also on [this discussion on GitHub](https://github.com/tellurianinteractive/Tellurian.Trains.ModuleMeetingApp/discussions/44), there are many ideas of how to translate a stream of times to pulses for controlling an pulsedriven analogue clock.

During March 2022, I started to make [Clock Pulse Service](https://github.com/fjallemark/ClockPulseService) for polling the [clock API](https://github.com/tellurianinteractive/Tellurian.Trains.ModuleMeetingApp/wiki/API-Guidelines) and generate data about pulses to send. 
I also bought a [RPi Relay Board](https://www.waveshare.com/wiki/RPi_Relay_Board) and mounted it om my old Raspberry Pi 2. Then I wrote an implementation of [IPulseSink](https://github.com/fjallemark/ClockPulseService/blob/master/Service/RpiRelayBoardPulseSink.cs) for that relay board, and have now run it on the Raspberry Pi to test it out, and after some adjustments, it  seems to work as expected.

With the software, it is also possible to directly use the I/O-pins on the Raspberry Pi without the relay board. This is fine if another device controlling the clock only needs low current I/O. 

There are three I/O-pins/relays:
- The first is set for every second.
- The second is set for every even second.
- The third is set for ever odd second.

I would like to test it out, or having someone to test it out against a real clock. 
There is currently a [distribution for **linux-arm**](https://onedrive.live.com/?id=DF287081A732D0D8%21302250&cid=DF287081A732D0D8) that can run on a Raspberry Pi. Download it, unzip and then follow these [instructions](https://docs.microsoft.com/en-us/dotnet/iot/deployment#deploying-a-self-contained-app), starting from point 3.

The app defaults to use the **Demo clock** at https://fastclock.azurewebsites.net/, 
but it can be changed in the *appsettings.json* file. 
Use the demo clock to start, stop and reset to check if the analogue pulse driven clock ticks!


