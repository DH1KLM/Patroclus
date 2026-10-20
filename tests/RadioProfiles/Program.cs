using System;
using System.Linq;
using Patroclus.Avalonia.ViewModels;

static class Program
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static void Main()
    {
        var expected = new[]
        {
            ("ANAN-10", 1, 1), ("ANAN-10E", 2, 2),
            ("ANAN-100", 1, 1), ("ANAN-100B", 2, 2),
            ("ANAN-100D", 4, 3), ("ANAN-200D", 5, 4),
            ("ANAN-7000DLE", 10, 5), ("ANAN-7000DLE MKII", 10, 5),
            ("ANAN-7000DLE MKIII", 10, 5), ("ANAN-8000DLE", 10, 5),
            ("ANAN-8000DLE MKII", 10, 5),
            ("Red Pitaya", 10, 5), ("ANVELINA Pro3", 10, 5)
        };
        foreach (var model in expected)
        {
            Check(RadioProfile.All.Single(p => p.Name == model.Item1 && p.Protocol == 1).BoardId == model.Item2,
                  model.Item1 + " P1 mapping");
            Check(RadioProfile.All.Single(p => p.Name == model.Item1 && p.Protocol == 2).BoardId == model.Item3,
                  model.Item1 + " P2 mapping");
        }
        foreach (var name in new[] { "ANAN-G2", "ANAN-G2 Ultra", "ANAN-G2-1K", "ANAN-G2E" })
        {
            var profile = RadioProfile.All.Single(p => p.Name == name);
            Check(profile.Protocol == 2, name + " must use P2");
            Check(profile.BoardId == (name == "ANAN-G2E" ? 20 : 10), name + " board");
        }
        Check(RadioProfile.All[0].Name == "HPSDR Hermes" && RadioProfile.All[0].Protocol == 1, "default changed");
        Check(RadioProfile.All.Single(p => p.Name == "Hermes Lite 2").FirmwareVersion == 40, "Lite 2 identity");
        Check(RadioProfile.All.Select(p => p.ToString()).Distinct().Count() == RadioProfile.All.Count, "duplicate selection labels");
        foreach (var profile in RadioProfile.All)
        {
            var packet = profile.CreateDiscoveryResponse();
            Check(packet.Length == 60, "discovery length");
            var expectedPacket = new byte[60];
            if (profile.Protocol == 1)
            {
                expectedPacket[0] = 0xef; expectedPacket[1] = 0xfe; expectedPacket[2] = 2;
                expectedPacket[8] = 1; expectedPacket[9] = profile.FirmwareVersion;
                expectedPacket[10] = profile.BoardId;
            }
            else
            {
                expectedPacket[4] = 2; expectedPacket[10] = 1;
                expectedPacket[11] = profile.BoardId;
                expectedPacket[12] = 2; expectedPacket[13] = profile.FirmwareVersion;
                expectedPacket[20] = 7; expectedPacket[21] = 1;
                Check(packet[0] == 0 && packet[1] == 0 && packet[2] == 0 && packet[3] == 0, "P2 header");
            }
            Check(packet.SequenceEqual(expectedPacket), profile + " discovery bytes");
            packet[10] = 255;
            Check(profile.CreateDiscoveryResponse()[10] != 255, "shared response buffer");
        }
        var hardwareDefaults = new[]
        {
            ("ANAN-10", 1, false, 33, true), ("ANAN-10E", 1, false, 33, true),
            ("ANAN-100D", 2, false, 33, false), ("ANAN-200D", 2, false, 50, false),
            ("ANAN-7000DLE", 2, true, 50, false), ("ANAN-8000DLE", 2, true, 50, false),
            ("ANAN-G2", 2, true, 50, false), ("ANAN-G2E", 1, true, 33, false),
            ("Red Pitaya", 2, false, 50, false), ("ANVELINA Pro3", 2, true, 50, false)
        };
        foreach (var expectedHardware in hardwareDefaults)
        {
            var hardware = RadioProfile.All.First(p => p.Name == expectedHardware.Item1).HardwareConfiguration;
            Check(hardware != null, expectedHardware.Item1 + " hardware configuration missing");
            Check(hardware.RxAdcSelection == expectedHardware.Item2 &&
                  hardware.MkiiBpfEnabled == expectedHardware.Item3 &&
                  hardware.AdcSupplyVolts == expectedHardware.Item4 &&
                  hardware.SwapLeftRightAudio == expectedHardware.Item5,
                  expectedHardware.Item1 + " Thetis hardware setup mapping");
        }
        Check(RadioProfile.All.Single(p => p.Name == "ANAN-G2 Ultra").HardwareConfiguration.IsFamilyBasedFallback,
              "G2 Ultra must disclose its board-family fallback");
        Check(RadioProfile.All.Single(p => p.Name == "ANAN-7000DLE MKIII" && p.Protocol == 1)
                  .HardwareConfiguration.IsFamilyBasedFallback,
              "7000DLE MKIII must disclose its board-family fallback");
        Check(RadioProfile.All.Single(p => p.Name == "Hermes Lite").HardwareConfiguration == null,
              "unverified Hermes Lite model setup must stay unspecified");
        Check(SamplePacketSchedule.CalculateDuePackets(192000, 238, TimeSpan.TicksPerSecond) == 806,
              "192 kHz packet rate");
        Check(SamplePacketSchedule.CalculateDuePackets(48000, 720, TimeSpan.TicksPerSecond) == 66,
              "48 kHz microphone packet rate");
        Check(SamplePacketSchedule.CalculateDuePackets(192000, 238, TimeSpan.TicksPerSecond / 2) == 403,
              "half-second packet deadline");
        Check(SamplePacketSchedule.CalculateDuePackets(192000, 238, 0) == 0,
              "initial packet deadline");
        Console.WriteLine($"PASS: {RadioProfile.All.Count} profiles, model/board mappings and discovery packets");
    }
}
