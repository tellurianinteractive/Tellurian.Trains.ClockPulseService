# Clock Pulse Service

**This software calls the [*Fastclock API*](https://github.com/tellurianinteractive/Tellurian.Trains.ModuleMeetingApp/wiki/API-Guidelines)
and creates logical pulses to drive analogue clocks.**

> If your clock is not a pulse-driven 12-hour clock, then my software is not the right solution. 
The alternative is to call the clock API and getting the time. 

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

