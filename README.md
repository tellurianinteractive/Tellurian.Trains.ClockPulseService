# Clock Integration Service

**Clock Integration Service connects the [*Fastclock*](https://github.com/tellurianinteractive/Tellurian.Trains.ModuleMeetingApp/wiki/API-Guidelines)
used at model railway meetings to clocks, displays and systems in the hall.**

It polls the Fastclock API and forwards the time and clock status to one or more *sinks*.
Each sink talks to a particular kind of hardware or protocol, and you can run several at once.
Driving pulse-controlled analogue clocks is one of these sinks; there are others too.

The service runs locally in the hall, on a computer such as a Raspberry Pi on the hall's local Wi-Fi network.
It can get the time from either of these:
- **A clock server running locally** on the hall network. This needs no Internet connection.
- **The cloud clock**, when Internet is available.

You choose the source with one setting (`UseRemoteClock`), so the same setup works with or without Internet at the venue.

## Sinks
| Sink | What it does | Contributed by |
|------|-------------|----------------|
| **ZNServerSink** | Sends the fast clock time to a [*Zugnummern-Meldeanlage* (ZN)](https://bmertins.de/FREMO/ZN/index.html) server using the FREMO ZN protocol. The server is found automatically by UDP discovery. | [Dirkjan Kaper](https://github.com/nh-finescale) |
| **RpiRelayBoardSink** | Drives 12-hour analogue clocks (for example FREMO RUT clocks) with alternating-polarity pulses through a [Waveshare RPi Relay Board](https://www.waveshare.com/wiki/RPi_Relay_Board) on a Raspberry Pi, together with a 24 V power supply for the clocks. A third relay shows whether the clock is stopped or running. | Stefan Fjällemark |
| **SerialPortSink** | Drives analogue clock pulses through the DTR and RTS lines of a serial port, or through DTR only. | Stefan Fjällemark |
| **UdpBroadcastSink** | Broadcasts single ASCII characters over UDP for pulses, start/stop and session end, so other software can react. | Stefan Fjällemark |
| **LoggingSink** | Writes all signals to the console. Useful for testing without hardware. | Stefan Fjällemark |

You turn sinks on and off in `appsettings.json`. If one sink fails, the others keep working.

Want to connect something else? Writing a new sink is straightforward, and contributions are welcome.
See [Writing a Sink](https://github.com/tellurianinteractive/Tellurian.Trains.ClockPulseService/wiki/Writing-a-Sink) in the wiki.

## Get the Software
Download the ZIP file for your platform from [Releases](https://github.com/tellurianinteractive/Tellurian.Trains.ClockPulseService/releases):
`win-x64`, `win-arm64`, `linux-arm` (Raspberry Pi 32-bit) or `linux-arm64` (Raspberry Pi 64-bit).
The builds are self-contained, so .NET does not need to be installed.

## Documentation
The [wiki](https://github.com/tellurianinteractive/Tellurian.Trains.ClockPulseService/wiki) covers:
- [Background](https://github.com/tellurianinteractive/Tellurian.Trains.ClockPulseService/wiki/Background): why the service exists and how it started.
- [How It Works](https://github.com/tellurianinteractive/Tellurian.Trains.ClockPulseService/wiki/How-It-Works): pulses, fast-forwarding and start/stop indication.
- [Installation and Running](https://github.com/tellurianinteractive/Tellurian.Trains.ClockPulseService/wiki/Installation-and-Running): Windows, Linux and Raspberry Pi, including running as a service.
- [Configuration](https://github.com/tellurianinteractive/Tellurian.Trains.ClockPulseService/wiki/Configuration): all settings in `appsettings.json`.
- [Sinks](https://github.com/tellurianinteractive/Tellurian.Trains.ClockPulseService/wiki/Sinks): details and wiring for each sink.

See [RELEASENOTES.md](RELEASENOTES.md) for what has changed.

## Contributors
- **Stefan Fjällemark**: author and maintainer.
- **Dirkjan Kaper** ([nh-finescale](https://github.com/nh-finescale)): ZNServerSink.

## License
GPL-3.0-or-later.
