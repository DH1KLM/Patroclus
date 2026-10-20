using System;

namespace Patroclus.Avalonia.ViewModels
{
    /// <summary>
    /// Applies the selected board's ADC inventory to the DDC-to-ADC assignments
    /// carried in receiver-specific control packets.
    /// </summary>
    public static class RadioHardwareBehavior
    {
        public static bool SupportsReceiverAdc(RadioHardwareConfiguration configuration,
                                               int hostAdcCount, int adcIndex)
        {
            // Profiles without a source-backed hardware mapping preserve the
            // emulator's legacy behavior.
            if (configuration == null) return true;
            if (hostAdcCount <= 0 || adcIndex < 0 || adcIndex >= hostAdcCount)
                return false;

            // Thetis SetRxADC stores this value as num_adc and sends it as the
            // board's available ADC count.
            return adcIndex < configuration.AdcCount;
        }
    }
}
