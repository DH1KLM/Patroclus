Patroclus - Software Defined Radio Emulator
===========================================

## Introduction

This software emulates a HPSDR Hermes transceiver.
It adds together a user selectable number of sine wave signals and recorded .wav IQ files for each enabled receiver.
It will generate signals for as many receivers as the client requests.
Currently only the command and control messages needed for signal generation are emulated. 

It was written as an exercise to gain understanding of Hermes and sdr in general.
If time and motivation allow it might be extended to emulate other radios and protocols.

It has been tested with the following sdr clients

  * [CuSDR64 0.3.2.14](https://plus.google.com/107168125384405552048/posts) - Works well, although CuSDR does send out wrong tx frequency commands.

  * [KISS Konsole 1.1.28](http://openhpsdr.org/wiki/index.php?title=KISS_Konsole) - This does not allow connections to a receiver on the same ip address as itself so you either have to run the emulator somewhere else on the network or remove the check from the source of KISS (in EthernetDevice.cs)

  * [PowerSDR mRX PS 3.2.19](http://openhpsdr.org/wiki/index.php?title=PowerSDR) - No problems.

  * [SDR#](http://sdrsharp.com/) using [ExtIO_hpsdr_msvc.dll](https://github.com/amontefusco/extio-hermes) - The extio dll rejects connections from the same ip address as itself so again the emulator has to run elsewhere on the network. 

  * [HDSDR](http://www.hdsdr.de/) using [ExtIO_hpsdr_msvc.dll](https://github.com/amontefusco/extio-hermes) - Same as SDR#

  * Thetis V2.1.18 will run on same pc.

  * [SparkSDR](http://www.ihopper.org/radio/) my client for [Hermes Lite](https://github.com/softerhardware/Hermes-Lite) - emulator will provide up to 32 receivers for hermes lite extended old protocol.

## Build

The original Patroclus project is depreciated but can be built with Visual Studio 2013 or later.
It uses .net4.5

Patroclus.Avalonia is where any further development will be, it has the advantage of running on windows, linux and macos. To build install the .net6.0 sdk then from the Patroclus.Avalonia directory run the following:-

dotnet restore

dotnet run

![Patroclus](docs/patroclus.JPG)


## ANAN and compatible radio profiles (Avalonia)

The Radio selector includes the model and protocol. The line below it shows the
board name, protocol and wire board ID, so the selected identity remains visible.
The original Hermes and Hermes Lite choices are retained.

| Model / family | Board | P1 ID | P2 ID |
| --- | --- | --- | --- |
| ANAN-10, ANAN-100 | Hermes | 1 | 1 |
| ANAN-10E, ANAN-100B | Hermes II | 2 | 2 |
| ANAN-100D | Angelia | 4 | 3 |
| ANAN-200D | Orion | 5 | 4 |
| ANAN-7000DLE, MKII, MKIII | Orion MkII compatible | 10 | 5 |
| ANAN-8000DLE, MKII | Orion MkII compatible | 10 | 5 |
| ANAN-G2, G2 Ultra, G2-1K | Saturn | — | 10 |
| ANAN-G2E | Hermes C10 | — | 20 |
| Red Pitaya, ANVELINA Pro3 | Orion MkII compatible | 10 | 5 |

Thetis model setup also differs by board family. The profile detail line shows the
available ADC count, MKII BPF mode, ADC supply, and left/right audio swap values
from the pinned [Thetis clsHardwareSpecific.cs](https://github.com/ramdor/Thetis/blob/852bf0ef0b4f3886a13fc2846489aee16f361872/Project%20Files/Source/Console/clsHardwareSpecific.cs)
model switch. The emulator applies the model's ADC count to receiver DDC-to-ADC
assignments, so a receiver mapped to an unavailable ADC is not created or streamed.
Clipping detected in generated samples assigned to ADC1 is reflected in the
high-priority status packet. The G2 Ultra and 7000DLE MKIII entries explicitly
label board-family fallback
values because this Thetis revision has no dedicated model case for those names.
MKII BPF, supply voltage, audio swapping, and physical RF/PA/filter behavior remain
configuration metadata; the emulator does not yet simulate those circuits.
Profiles with no explicit Thetis mapping retain the legacy receiver behavior.

Board mappings follow [Thetis enums.cs](https://github.com/ramdor/Thetis/blob/852bf0ef0b4f3886a13fc2846489aee16f361872/Project%20Files/Source/Console/enums.cs)
and [its discovery parser](https://github.com/ramdor/Thetis/blob/852bf0ef0b4f3886a13fc2846489aee16f361872/Project%20Files/Source/Console/HPSDR/clsRadioDiscovery.cs).
Mk variants and G2 Ultra are local model labels for the corresponding board
family; discovery cannot distinguish commercial models sharing the same board.
Thetis clients may therefore require selecting the intended model in their setup.
Red Pitaya is an Orion-compatible profile, not a claim that every Red Pitaya
firmware advertises this board. Firmware versions are synthetic emulator values.
Saturn MkII (P2 ID 11) is not assigned to a commercial model: Thetis itself marks
that association as uncertain. The newly announced G2E-20 has no separate verified
model/board association in that Thetis revision and is not given a guessed ID.

These profiles select network identity and the existing P1/P2 signal generator.
They do not add model-specific RF hardware, PA, ADC routing, PureSignal or full TX
emulation. The deprecated WPF application is unchanged.

The P2 discovery response now puts protocol support in byte 12 and firmware in
byte 13. Switching profiles releases P2 receiver sockets before starting another
radio, including switches after streaming has begun.

Run the dependency-free profile and discovery packet checks with:

```
dotnet run --project tests/RadioProfiles/RadioProfiles.csproj
```
