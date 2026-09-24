namespace Patroclus.Avalonia.ViewModels
{
    //DH1KLM: Board identifiers are taken from Thetis HPSDRHW and its P1 discovery mapping.
    public sealed class HpsdrBoardProfile
    {
        public string Name { get; }
        public byte P1BoardId { get; }
        public byte P2BoardId { get; }
        public bool P1 { get; }
        public bool P2 { get; }
        public bool P1DiscoveryMapsToDifferentThetisEnum { get; }

        public HpsdrBoardProfile(string name, byte p1BoardId, byte p2BoardId, bool p1, bool p2, bool p1DiscoveryMapsToDifferentThetisEnum = false)
        {
            Name = name;
            P1BoardId = p1BoardId;
            P2BoardId = p2BoardId;
            P1 = p1;
            P2 = p2;
            P1DiscoveryMapsToDifferentThetisEnum = p1DiscoveryMapsToDifferentThetisEnum;
        }

        public override string ToString() => Name;
    }

    public static class HpsdrBoards
    {
        //DH1KLM: P1 values follow Thetis clsRadioDiscovery.cs mapping:
        // 0 Atlas, 1 Hermes, 2 HermesII, 4 Angelia, 5 Orion, 10 OrionMKII.
        // P2 uses the HPSDRHW enum values directly.
        public static readonly HpsdrBoardProfile Hermes =
            new HpsdrBoardProfile("Hermes", 1, 1, true, true);

        public static readonly HpsdrBoardProfile Angelia =
            new HpsdrBoardProfile("Angelia", 4, 3, true, true);

        public static readonly HpsdrBoardProfile Orion =
            new HpsdrBoardProfile("Orion", 5, 4, true, true);

        public static readonly HpsdrBoardProfile OrionMkII =
            new HpsdrBoardProfile("Orion MkII", 10, 5, true, true);

        //DH1KLM: Thetis P1 discovery byte 10 is mapped to OrionMKII.
        //DH1KLM: Saturn is a P2 board identity (HPSDRHW value 10) and cannot be
        //DH1KLM: uniquely represented in Thetis P1 discovery, so do not expose a
        //DH1KLM: misleading Saturn-P1 profile.
        public static readonly HpsdrBoardProfile Saturn =
            new HpsdrBoardProfile("Saturn", 10, 10, false, true);

        public static readonly HpsdrBoardProfile HermesLite =
            new HpsdrBoardProfile("Hermes Lite", 6, 6, true, true);
    }
}
