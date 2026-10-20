using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using System.Windows.Data;
using System.Windows.Input;

namespace Patroclus.Avalonia.ViewModels
{
    public class FakeHermesNewProtocol : FakeRadio
    {
        private udpConnection generalClient;
        private udpConnection rxSpecificClient;
        private udpConnection txSpecificClient;
        private udpConnection highPriorityClient;
        private udpConnection tx0IQClient;
        private udpConnection rxAudioClient;

        const int maxReceivers = 80;
        private bool usePhaseWord = false;

        Dictionary<receiver, UdpClient> rxClients = new Dictionary<receiver, UdpClient>();

        receiver[] receiversByIdx = new receiver[maxReceivers];
        private readonly int[] receiverAdcByIdx = new int[maxReceivers];
        private readonly int[] receiverHostAdcCountByIdx = new int[maxReceivers];

        private Thread handleCommsThread;

        public int port { get; set; } //Port for the Client to use
        public RadioProfile Profile { get; set; } = RadioProfile.All[1];


        IPEndPoint ClientIpEndPoint;

        const byte sync = 0x7f;
        const int max24int = 0x7fffff;
        const int min24int = -0x800000;


        byte[] databuf = new byte[1444];
        uint seqNo = 0;
        uint micSeqNo = 0;

        double timebase = 0.0;
        readonly Stopwatch transmissionClock = new Stopwatch();
        bool running = false;

        double clk = 122880000;
        private volatile bool closing = false;

        private int _RxSpecificPort = 1025;
        public int RxSpecificPort
        {
            get { return _RxSpecificPort; }
            set { this.RaiseAndSetIfChanged(ref _RxSpecificPort, value); }
        }

        private int _TxSpecificPort = 1026;
        public int TxSpecificPort
        {
            get { return _TxSpecificPort; }
            set { this.RaiseAndSetIfChanged(ref _TxSpecificPort, value); }
        }
        private int _HighPriorityFromPCPort = 1027;
        public int HighPriorityFromPCPort
        {
            get { return _HighPriorityFromPCPort; }
            set { this.RaiseAndSetIfChanged(ref _HighPriorityFromPCPort, value); }
        }

        private int _HighPriorityToPCPort = 1025;
        public int HighPriorityToPCPort
        {
            get { return _HighPriorityToPCPort; }
            set { this.RaiseAndSetIfChanged(ref _HighPriorityToPCPort, value); }
        }
        private int _ReceiverAudioPort = 1028;
        public int ReceiverAudioPort
        {
            get { return _ReceiverAudioPort; }
            set { this.RaiseAndSetIfChanged(ref _ReceiverAudioPort, value); }
        }
        private int _Tx0IQPort = 1029;
        public int Tx0IQPort
        {
            get { return _Tx0IQPort; }
            set { this.RaiseAndSetIfChanged(ref _Tx0IQPort, value); }
        }

        private int _Rx0Port = 1035;
        public int Rx0Port
        {
            get { return _Rx0Port; }
            set { this.RaiseAndSetIfChanged(ref _Rx0Port, value); }
        }

        private int _MicSamplesPort = 1026;
        public int MicSamplesPort
        {
            get { return _MicSamplesPort; }
            set { this.RaiseAndSetIfChanged(ref _MicSamplesPort, value); }
        }

        private int _WidebandADC0Port = 1027;
        public int WidebandADC0Port
        {
            get { return _WidebandADC0Port; }
            set { this.RaiseAndSetIfChanged(ref _WidebandADC0Port, value); }
        }
        private int _clockError = 1000;
        public int clockError
        {
            get { return _clockError; }
            set
            {
                //prevent system trying to correct timing from original start time

          //      resetTransmission();
                this.RaiseAndSetIfChanged(ref _clockError, value);
            }
        }
        /*
Wideband Enable [7:0]
Wideband Samples per packet [15:8]
Wideband sample size 
Wideband update rate 
Pure Signal - Rx(n) to use for off air signal
Pure Signal - Rx(n) to use for DAC signal
Pure Signal Sampling Rate [15:8]
Pure Signal Sampling Rate [7:0]
Envelope PWM_max
Envelope PWM_max
Envelope PWM_min
Envelope PWM_min
Bits - [0]Time stamp, [1]VITA-49, [2]VNA mode
*/

        public FakeHermesNewProtocol()
        {
   //         BindingOperations.CollectionRegistering += BindingOperations_CollectionRegistering;

        }

    //    void BindingOperations_CollectionRegistering(object sender, CollectionRegisteringEventArgs e)
   //     {
      //      BindingOperations.EnableCollectionSynchronization(receivers, _receiversLock);
    //    }
        private object _receiversLock = new object();

        private ObservableCollection<receiver> _receivers = new ObservableCollection<receiver>();
        public ObservableCollection<receiver> receivers
        {
            get { return _receivers; }
            set { this.RaiseAndSetIfChanged(ref _receivers, value); }
        }

        private int _bandwidth = 192000;
        public int bandwidth
        {
            get { return _bandwidth; }
            set { this.RaiseAndSetIfChanged(ref _bandwidth, value); }
        }

        private int _txNCO = 0;
        public int txNCO
        {
            get { return _txNCO; }
            set { this.RaiseAndSetIfChanged(ref _txNCO, value); }
        }

        private bool _duplex = false;
        public bool duplex
        {
            get { return _duplex; }
            set { this.RaiseAndSetIfChanged(ref _duplex, value); }
        }
        private bool _adc1clip = false;
        public bool adc1clip
        {
            get { return _adc1clip; }
            set { this.RaiseAndSetIfChanged(ref _adc1clip, value); }
        }

        private string _status = "Off";
        public string status
        {
            get { return _status; }
            set { this.RaiseAndSetIfChanged(ref _status, value); }
        }
        private int _packetsSent = 0;
        public int packetsSent
        {
            get { return _packetsSent; }
            set { this.RaiseAndSetIfChanged(ref _packetsSent, value); }
        }
        private int _packetsReceived = 0;
        public int packetsReceived
        {
            get { return _packetsReceived; }
            set { this.RaiseAndSetIfChanged(ref _packetsReceived, value); }
        }


        public void start()
        {
            generalClient = new udpConnection() { Client = connect(port), msgQueue = new ConcurrentQueue<receivedPacket>() };
            generalClient.Client.BeginReceive(new AsyncCallback(incomming), generalClient);

            rxSpecificClient = new udpConnection() { Client = connect(RxSpecificPort), msgQueue = new ConcurrentQueue<receivedPacket>() };
            rxSpecificClient.Client.BeginReceive(new AsyncCallback(incomming), rxSpecificClient);

            txSpecificClient = new udpConnection() { Client = connect(TxSpecificPort), msgQueue = new ConcurrentQueue<receivedPacket>() };
            txSpecificClient.Client.BeginReceive(new AsyncCallback(incomming), txSpecificClient);

            highPriorityClient = new udpConnection() { Client = connect(HighPriorityFromPCPort), msgQueue = new ConcurrentQueue<receivedPacket>() };
            highPriorityClient.Client.BeginReceive(new AsyncCallback(incomming), highPriorityClient);

            tx0IQClient = new udpConnection() { Client = connect(Tx0IQPort), msgQueue = new ConcurrentQueue<receivedPacket>() };
            tx0IQClient.Client.BeginReceive(new AsyncCallback(incomming), tx0IQClient);

            rxAudioClient = new udpConnection() { Client = connect(ReceiverAudioPort), msgQueue = new ConcurrentQueue<receivedPacket>() };
            rxAudioClient.Client.BeginReceive(new AsyncCallback(incomming), rxAudioClient);


            handleCommsThread = new Thread(handleComms);
            handleCommsThread.IsBackground = true;
            handleCommsThread.Start();

        }
        private UdpClient connect(int port)
        {
            UdpClient client = new UdpClient(port);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                const int SIO_UDP_CONNRESET = -1744830452;
                byte[] inValue = new byte[] { 0 };
                byte[] outValue = new byte[] { 0 };
                client.Client.IOControl(SIO_UDP_CONNRESET, inValue, outValue);
            }
            return client;
        }
        public override void Stop()
        {
            closing = true;
            handleCommsThread?.Join();
           // handleCommsThread.Abort();
            generalClient.Client.Close();
            rxSpecificClient.Client.Close();
            txSpecificClient.Client.Close();
            highPriorityClient.Client.Close();
            tx0IQClient.Client.Close();
            rxAudioClient.Client.Close();
            foreach (var rxClient in rxClients.Values) rxClient.Close();
            rxClients.Clear();

            base.Stop();
        }
        long actualPacketCount = 0;
        void handleComms()
        {
            while (!closing)
            {
                while (!generalClient.msgQueue.IsEmpty)
                {
                    receivedPacket packet;
                    if (generalClient.msgQueue.TryDequeue(out packet)) handleGeneralPacket(packet);
                }
                while (!rxSpecificClient.msgQueue.IsEmpty)
                {
                    receivedPacket packet;
                    if (rxSpecificClient.msgQueue.TryDequeue(out packet)) handleRxSpecificPacket(packet);
                }
                while (!txSpecificClient.msgQueue.IsEmpty)
                {
                    receivedPacket packet;
                    if (txSpecificClient.msgQueue.TryDequeue(out packet)) handleTxSpecificPacket(packet);
                }
                while (!highPriorityClient.msgQueue.IsEmpty)
                {
                    receivedPacket packet;
                    if (highPriorityClient.msgQueue.TryDequeue(out packet)) handleHighPriorityPacket(packet);
                }
                while (!tx0IQClient.msgQueue.IsEmpty)
                {
                    receivedPacket packet;
                    if (tx0IQClient.msgQueue.TryDequeue(out packet)) handleTxIQPacket(packet, 0);
                }
                while (!rxAudioClient.msgQueue.IsEmpty)
                {
                    receivedPacket packet;
                    if (rxAudioClient.msgQueue.TryDequeue(out packet)) handleRXAudioPacket(packet);
                }
                //send any output
                if (running)
                {
                    
                    adc1clip = false;
                    int nSamples = 238;
                   
                    long elapsedTicks = transmissionClock.Elapsed.Ticks;

                    // Generate from the network-owned slot table. The UI collection is
                    // updated asynchronously and must not control packet scheduling.
                    for (int ri = 0; ri < receiversByIdx.Length; ri++)
                    {
                        var rx = receiversByIdx[ri];
                        if (rx != null && rx.bandwidth > 0 && rxClients.TryGetValue(rx, out var rxClient) &&
                            RadioHardwareBehavior.SupportsReceiverAdc(Profile?.HardwareConfiguration,
                                receiverHostAdcCountByIdx[ri], receiverAdcByIdx[ri]))
                        {
                            double timeStep = 1.0 / rx.bandwidth;
                            long nPacketsCalculated = SamplePacketSchedule.CalculateDuePackets(
                                rx.bandwidth, nSamples, elapsedTicks);
                            long packetsToSend = Math.Max(0, nPacketsCalculated - rx.packetCount);



                            for (int i = 0; i < packetsToSend; i++)
                            {
                                int seqNo = rx.seq;
                                rx.seq++;
                                //sequence no
                                databuf[0] = (byte)(seqNo >> 24);
                                databuf[1] = (byte)((seqNo >> 16) & 0xff);
                                databuf[2] = (byte)((seqNo >> 8) & 0xff);
                                databuf[3] = (byte)(seqNo & 0xff);
                                //timestamp
                                /*     databuf[4] = (byte)(rx.timestamp>>56);
                                     databuf[5] = (byte)((rx.timestamp >> 48 ) & 0xff);
                                     databuf[6] = (byte)((rx.timestamp >> 40) & 0xff);
                                     databuf[7] = (byte)((rx.timestamp >> 32) & 0xff);
                                     databuf[8] = (byte)((rx.timestamp >> 24) & 0xff);
                                     databuf[9] = (byte)((rx.timestamp >> 16) & 0xff);
                                     databuf[10] = (byte)((rx.timestamp >> 8) & 0xff);
                                     databuf[11] = (byte)(rx.timestamp  & 0xff);

                                     rx.timestamp += (ulong)(clk * nSamples / rx.bandwidth);
                                     */
                                //bits per sample
                                databuf[12] = (byte)(0);
                                databuf[13] = (byte)(24);

                                //no of samples
                                databuf[14] = (byte)(nSamples >> 8);
                                databuf[15] = (byte)(nSamples & 0xff);



                                rx.GenerateSignal(databuf, 16, 6, nSamples, rx.timebase, timeStep);
                                if (RadioHardwareBehavior.ReportsAdc1Clip(
                                    Profile?.HardwareConfiguration,
                                    receiverHostAdcCountByIdx[ri],
                                    receiverAdcByIdx[ri],
                                    rx.adcClip))
                                    adc1clip = true;
                                rxClient.Send(databuf, databuf.Length, ClientIpEndPoint);
                                rx.packetCount++;
                                packetsSent++;
                                rx.timebase += nSamples * timeStep;
                            }
                        }
                        //seqNo++;
                        //actualPacketCount++;
                         
                    }
                //    timebase = ttimebase;
                    //  if (highPriorityToPC != null) 
                    sendHighPriorityToPC();

                    long nMicPacketsCalculated = SamplePacketSchedule.CalculateDuePackets(
                        48000, 720, elapsedTicks);
                    long micPacketsToSend = Math.Max(0, nMicPacketsCalculated - micSeqNo);
                    for (long i = 0; i < micPacketsToSend; i++) sendMicData();
                }


                Thread.Sleep(1);
            }
        }
        byte[] hpbuf = new byte[60];

        void sendHighPriorityToPC()
        {
            hpbuf[0] = (byte)(seqNo >> 24);
            hpbuf[1] = (byte)((seqNo >> 16) & 0xff);
            hpbuf[2] = (byte)((seqNo >> 8) & 0xff);
            hpbuf[3] = (byte)(seqNo & 0xff);

            hpbuf[5] = adc1clip ? (byte)1 : (byte)0;


            rxSpecificClient.Client.Send(hpbuf, hpbuf.Length, ClientIpEndPoint);
            seqNo++;

        }
        byte[] micbuf = new byte[1444];

        void sendMicData()
        {

            micbuf[0] = (byte)(micSeqNo >> 24);
            micbuf[1] = (byte)((micSeqNo >> 16) & 0xff);
            micbuf[2] = (byte)((micSeqNo >> 8) & 0xff);
            micbuf[3] = (byte)(micSeqNo & 0xff);

            txSpecificClient.Client.Send(micbuf, micbuf.Length, ClientIpEndPoint);
            micSeqNo++;

        }

        private void incomming(IAsyncResult res)
        {
            udpConnection con = res.AsyncState as udpConnection;

            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, port);
            byte[] received = null;
            packetsReceived++;
            try
            {
                received = con.Client.EndReceive(res, ref RemoteIpEndPoint);
            }
            catch (Exception)
            {

            }
            try
            {
                con.Client.BeginReceive(new AsyncCallback(incomming), con);
                if (received != null)
                {
                    con.msgQueue.Enqueue(new receivedPacket() { received = received, endPoint = RemoteIpEndPoint });
                }
            }
            catch (Exception)
            {
            }
        }

        IPAddress udpBroadcast = new IPAddress(new byte[] { 255, 255, 255, 255 });
        public void handleGeneralPacket(receivedPacket packet)
        {
            //     Console.Out.WriteLine("gp");
            if (packet == null || packet.received == null || packet.endPoint == null) return;
            byte[] received = packet.received;
            if (packet.endPoint.Address.Equals(udpBroadcast))
            {

            }
            //old style discovery
            else if (received.Length > 4 && received[4] == 2)
            {
                //discovery
                byte[] response = Profile.CreateDiscoveryResponse();

                status = "Discovered";
                seqNo = 1;
                generalClient.Client.Send(response, response.Length, packet.endPoint);
                packetsSent++;

                ClientIpEndPoint = packet.endPoint;

                Console.WriteLine("disc");
            }
            else if (received.Length >= 19 && received[4] == 0)
            {
                RxSpecificPort = (received[5] << 8) + received[6];
                TxSpecificPort = (received[7] << 8) + received[8];

                HighPriorityFromPCPort = (received[9] << 8) + received[10];
                //   if (highPriorityToPC == null) highPriorityToPC = new UdpClient(HighPriorityToPCPort);

                Rx0Port = (received[17] << 8) + received[18];

                //   usePhaseWord = (received[37] & 8) != 0;

                usePhaseWord = true;
                ClientIpEndPoint = packet.endPoint;


            }


        }
        private void handleRxSpecificPacket(receivedPacket packet)
        {
            //     Console.Out.WriteLine("rxsp");
            byte[] received = packet.received;
            if (received == null || received.Length < 20 + 6 * (maxReceivers - 1)) return;
            int adcs = received[4];


            for (int f = 0; f < 10; f++)
            {
                int mask = 1;
                for (int i = 0; i < 8; i++)
                {
                    int idx = f * 8 + i;
                    int adcIndex = received[17 + idx * 6];
                    bool slotEnabled = (received[7 + f] & mask) != 0;
                    bool adcAvailable = RadioHardwareBehavior.SupportsReceiverAdc(
                        Profile?.HardwareConfiguration, adcs, adcIndex);
                    if (slotEnabled && adcAvailable)
                    {
                        if (receiversByIdx[idx] == null)
                        {
                            var rx = new receiver("RX" + idx);
                            receiversByIdx[idx] = rx;
                            rxClients.Add(rx, new UdpClient(Rx0Port + idx));
                            Dispatcher.UIThread.InvokeAsync(new Action(() => receivers.Add(rx)));
                        }
                    }
                    else
                    {
                        if (receiversByIdx[idx] != null)
                        {
                          //  lock (_receiversLock)
                          //  {
                                var rx = receiversByIdx[idx];
                               // receivers.Remove(rx);
                                Dispatcher.UIThread.InvokeAsync(new Action(() => {
                                    receivers.Remove(rx);
                                }));

                                rxClients[rx].Close();
                                rxClients.Remove(rx);
                                receiversByIdx[idx] = null;
                           // }
                        }
                    }
                    if (receiversByIdx[idx] != null)
                    {
                        receiverAdcByIdx[idx] = adcIndex;
                        receiverHostAdcCountByIdx[idx] = adcs;
                        int srate = (received[18 + idx * 6] << 8) + received[19 + idx * 6];
                        receiversByIdx[idx].bandwidth = srate * 1000;
                    }

                    mask <<= 1;
                }
            }
            // find sync'd and multiplexed receivers
         //   byte mux = received[1443];

        }
        private void handleRXAudioPacket(receivedPacket packet)
        {
            //            Console.Out.WriteLine("rxap");
        }
        private void handleTxSpecificPacket(receivedPacket packet)
        {
            //              Console.Out.WriteLine("txsp");
        }
        private void handleTxIQPacket(receivedPacket packet, int adc)
        {
            //             Console.Out.WriteLine("txiq");
        }
        private void handleHighPriorityPacket(receivedPacket packet)
        {
            //         Console.Out.WriteLine("hpp");

            byte[] received = packet.received;
            if (received == null || received.Length < 333) return;
            bool run = ((received[4] & 0x01) != 0);
            if (run != running)
            {
                if (run)
                {
                    resetTransmission();
                    running = true;
                    status = "Running";
                }
                else
                {
                    running = false;
                    status = "Off";
                }
            }
            bool ptt0 = ((received[4] & 0x02) != 0);
            if (ptt0)
            {
                //   Console.Out.WriteLine("ptt");
            }
            int rxi = 9;
            for (int i = 0; i < maxReceivers; i++)
            {
                if (receiversByIdx[i] != null)
                {
                    if (usePhaseWord)
                    {
                        //phase_word[31:0] = 2^32 * frequency(Hz)/DSP clock frequency (Hz) 

                        int phaseword = (((int)received[rxi]) << 24) + (((int)received[rxi + 1]) << 16) + (((int)received[rxi + 2]) << 8) + (int)received[rxi + 3];

                        receiversByIdx[i].vfo = (int)(phaseword * clk / 4294967296.0);
                    }
                    else receiversByIdx[i].vfo = (((int)received[rxi]) << 24) + (((int)received[rxi + 1]) << 16) + (((int)received[rxi + 2]) << 8) + (int)received[rxi + 3];

                    receiversByIdx[i].generators[0].SetDefaults(receiversByIdx[i].vfo);
                    receiversByIdx[i].generators[1].SetDefaults(receiversByIdx[i].vfo + 10000);
                }
                rxi += 4;
            }
            int txi = 329;
            if (usePhaseWord)
            {
                int phaseword = (((int)received[txi]) << 24) + (((int)received[txi + 1]) << 16) + (((int)received[txi + 2]) << 8) + (int)received[txi + 3];
                txNCO = (int)(phaseword * clk / 4294967296.0);
            }
            else txNCO = (((int)received[txi]) << 24) + (((int)received[txi + 1]) << 16) + (((int)received[txi + 2]) << 8) + (int)received[txi + 3];
        }
        void resetTransmission()
        {
            actualPacketCount = 0;
            micSeqNo = 0;
            foreach (receiver rx in receiversByIdx)
            {
                if (rx == null) continue;
                rx.seq = 0;
                rx.packetCount = 0;
                rx.timebase = 0;
            }
            transmissionClock.Restart();
        }
    }
    public class udpConnection
    {
        public UdpClient Client { get; set; }
        public ConcurrentQueue<receivedPacket> msgQueue { get; set; }
    }
}
