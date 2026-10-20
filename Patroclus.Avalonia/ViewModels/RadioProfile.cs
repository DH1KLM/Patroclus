using System;
using System.Collections.Generic;

namespace Patroclus.Avalonia.ViewModels
{
    // Model names are local labels. HPSDR discovery advertises a board, not a model.
    // Reference: ramdor/Thetis, Console/enums.cs and HPSDR/clsRadioDiscovery.cs.
    /// <summary>Model initialization settings mirrored from Thetis clsHardwareSpecific.</summary>
    public sealed class RadioHardwareConfiguration
    {
        public int AdcCount { get; }
        public bool MkiiBpfEnabled { get; }
        public int AdcSupplyVolts { get; }
        public bool SwapLeftRightAudio { get; }
        public bool IsFamilyBasedFallback { get; }

        public RadioHardwareConfiguration(int rxAdcSelection, bool mkiiBpfEnabled,
                                          int adcSupplyVolts, bool swapLeftRightAudio,
                                          bool isFamilyBasedFallback = false)
        {
            AdcCount = rxAdcSelection;
            MkiiBpfEnabled = mkiiBpfEnabled;
            AdcSupplyVolts = adcSupplyVolts;
            SwapLeftRightAudio = swapLeftRightAudio;
            IsFamilyBasedFallback = isFamilyBasedFallback;
        }

        public string Description => $"RX ADC {AdcCount}; MKII BPF {(MkiiBpfEnabled ? "on" : "off")}; " +
            $"ADC supply {AdcSupplyVolts} V; LR audio swap {(SwapLeftRightAudio ? "on" : "off")}" +
            (IsFamilyBasedFallback ? " (board-family fallback)" : "");
    }

    public sealed class RadioProfile
    {
        public string Name { get; }
        public int Protocol { get; }
        public byte BoardId { get; }
        public byte FirmwareVersion { get; }
        public string BoardName { get; }
        public RadioHardwareConfiguration HardwareConfiguration { get; }
        public string Description => $"{BoardName} | Protocol {Protocol} | Board ID {BoardId} | " +
            (HardwareConfiguration?.Description ?? "Hardware behavior: unspecified in Thetis");

        public RadioProfile(string name, int protocol, byte boardId, string boardName,
                            byte firmwareVersion = 30,
                            RadioHardwareConfiguration hardwareConfiguration = null)
        {
            if (protocol != 1 && protocol != 2)
                throw new ArgumentOutOfRangeException(nameof(protocol));
            Name = name;
            Protocol = protocol;
            BoardId = boardId;
            BoardName = boardName;
            FirmwareVersion = firmwareVersion;
            HardwareConfiguration = hardwareConfiguration;
        }

        public override string ToString() => $"{Name} (P{Protocol})";

        public byte[] CreateDiscoveryResponse()
        {
            var response = new byte[60];
            if (Protocol == 1)
            {
                response[0] = 0xef;
                response[1] = 0xfe;
                response[2] = 2;
                response[8] = 1; // locally simulated MAC 00:00:00:00:00:01
                response[9] = FirmwareVersion;
                response[10] = BoardId;
            }
            else
            {
                response[4] = 2;
                response[10] = 1;
                response[11] = BoardId;
                response[12] = 2; // protocol supported; firmware is byte 13
                response[13] = FirmwareVersion;
                response[20] = 7; // preserve emulator's advertised DDC count
                response[21] = 1;
            }
            return response;
        }

        public static IReadOnlyList<RadioProfile> All { get; } = CreateProfiles();

        private static IReadOnlyList<RadioProfile> CreateProfiles()
        {
            // These are the client-side hardware initialization values used by
            // Thetis clsHardwareSpecific.cs at the pinned reference revision.
            var hermes = new RadioHardwareConfiguration(1, false, 33, true);
            var hermesII = new RadioHardwareConfiguration(1, false, 33, true);
            var angelia = new RadioHardwareConfiguration(2, false, 33, false);
            var orion = new RadioHardwareConfiguration(2, false, 50, false);
            var orionMkII = new RadioHardwareConfiguration(2, true, 50, false);
            var saturn = new RadioHardwareConfiguration(2, true, 50, false);
            var saturnFallback = new RadioHardwareConfiguration(2, true, 50, false, true);
            var orionMkIIFallback = new RadioHardwareConfiguration(2, true, 50, false, true);
            var hermesC10 = new RadioHardwareConfiguration(1, true, 33, false);
            var redPitaya = new RadioHardwareConfiguration(2, false, 50, false);

            var profiles = new List<RadioProfile>
            {
                new RadioProfile("HPSDR Hermes", 1, 1, "Hermes", 30, hermes),
                new RadioProfile("HPSDR Hermes", 2, 1, "Hermes", 30, hermes),
                new RadioProfile("Hermes Lite", 1, 6, "Hermes Lite"),
                new RadioProfile("Hermes Lite 2", 1, 6, "Hermes Lite", 40)
            };
            // P1 and P2 board numbers differ for Angelia, Orion and Orion MkII.
            AddBoth(profiles, "ANAN-10", 1, 1, "Hermes", hermes);
            AddBoth(profiles, "ANAN-10E", 2, 2, "Hermes II", hermesII);
            AddBoth(profiles, "ANAN-100", 1, 1, "Hermes", hermes);
            AddBoth(profiles, "ANAN-100B", 2, 2, "Hermes II", hermesII);
            AddBoth(profiles, "ANAN-100D", 4, 3, "Angelia", angelia);
            AddBoth(profiles, "ANAN-200D", 5, 4, "Orion", orion);
            AddBoth(profiles, "ANAN-7000DLE", 10, 5, "Orion MkII", orionMkII);
            AddBoth(profiles, "ANAN-7000DLE MKII", 10, 5, "Orion MkII", orionMkII);
            AddBoth(profiles, "ANAN-7000DLE MKIII", 10, 5, "Orion MkII compatible", orionMkIIFallback);
            AddBoth(profiles, "ANAN-8000DLE", 10, 5, "Orion MkII", orionMkII);
            AddBoth(profiles, "ANAN-8000DLE MKII", 10, 5, "Orion MkII compatible", orionMkII);
            profiles.Add(new RadioProfile("ANAN-G2", 2, 10, "Saturn", 30, saturn));
            profiles.Add(new RadioProfile("ANAN-G2 Ultra", 2, 10, "Saturn", 30, saturnFallback));
            profiles.Add(new RadioProfile("ANAN-G2-1K", 2, 10, "Saturn", 30, saturn));
            profiles.Add(new RadioProfile("ANAN-G2E", 2, 20, "Hermes C10", 30, hermesC10));
            AddBoth(profiles, "Red Pitaya", 10, 5, "Orion MkII compatible", redPitaya);
            AddBoth(profiles, "ANVELINA Pro3", 10, 5, "Orion MkII compatible", orionMkII);
            return profiles.AsReadOnly();
        }

        private static void AddBoth(List<RadioProfile> profiles, string name,
                                    byte p1Board, byte p2Board, string boardName,
                                    RadioHardwareConfiguration hardwareConfiguration)
        {
            profiles.Add(new RadioProfile(name, 1, p1Board, boardName, 30, hardwareConfiguration));
            profiles.Add(new RadioProfile(name, 2, p2Board, boardName, 30, hardwareConfiguration));
        }
    }
}
