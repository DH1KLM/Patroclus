using System;

namespace Patroclus.Avalonia.ViewModels
{
    public static class SamplePacketSchedule
    {
        public static long CalculateDuePackets(int sampleRate, int samplesPerPacket, long elapsedTicks)
        {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (samplesPerPacket <= 0) throw new ArgumentOutOfRangeException(nameof(samplesPerPacket));
            if (elapsedTicks < 0) throw new ArgumentOutOfRangeException(nameof(elapsedTicks));
            decimal samples = (decimal)sampleRate * elapsedTicks / TimeSpan.TicksPerSecond;
            return (long)(samples / samplesPerPacket);
        }
    }
}
