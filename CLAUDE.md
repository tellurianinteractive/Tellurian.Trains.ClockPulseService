# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Test Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~Service.Tests.PulseGeneratorTests.TestMethodName"

# Publish for a specific platform (e.g., linux-arm for Raspberry Pi)
dotnet publish Service/Service.csproj -c Release -r linux-arm --self-contained

# Run the service locally
dotnet run --project Service
```

## Architecture

This is a .NET 10.0 Worker Service that polls the Fastclock API and generates voltage pulses to synchronize analogue model train clocks. Solution file: `Clock Pulse Service.slnx`.

### Core Flow

**Program.cs** sets up DI and registers `Worker` as a `BackgroundService`. **Worker.cs** polls the Fastclock API on a configurable interval (default 2s), deserializes `ClockStatus`, and passes it to **PulseGenerator.cs**. The pulse generator determines whether to pulse (positive/negative/zero voltage), fast-forward (when >1 minute behind), or do nothing, then delegates to sinks.

### Sink System

Sinks implement one or more of four interfaces (`IPulseSink`, `IControlSink`, `IStatusSink`, `IAnalogueClockStatus`) and are instantiated based on `appsettings.json` configuration. Each sink translates logical signals to hardware I/O:

- **SerialPortSink** — DTR/RTS serial lines
- **RpiRelayBoardSink** — GPIO via Waveshare RPi Relay Board (pins 20, 21, 26)
- **UdpBroadcastSink** — ASCII UDP packets
- **ZNServerSink** — FREMO ZN protocol (UDP discovery)
- **LoggingSink** — Console output for debugging

### Key Design Details

- Pulse polarity alternates: even minutes = positive, odd = negative (configurable via `FlipPolarity`)
- Analogue clock time is persisted to `AnalogueTime.txt` for recovery across restarts
- Worker accepts CLI args: `-r` for reset, `-t HH:mm` for time override
- One failing sink does not affect others — errors are handled per-sink
- External dependency: `Tellurian.Trains.MeetingApp.Contracts` provides the `ClockStatus` model from the Fastclock API

### Test Setup

Tests use **MSTest** with **Moq** for mocking. Test helpers `MonitoringPulseSink` and `FailingPulseSink` are in the test project for observing and fault-testing pulse behavior.

### Deployment

`Service/Scripts/deploy.sh` handles Linux deployment with optional systemd service installation (`-i` flag). The build produces self-contained executables for win-x64, win-arm64, linux-arm, linux-arm64, with automatic ZIP packaging on publish.
