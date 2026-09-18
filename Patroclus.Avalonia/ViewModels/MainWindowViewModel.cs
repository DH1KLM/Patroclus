using ReactiveUI;
using System;

namespace Patroclus.Avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private FakeRadio _radio;
        public FakeRadio radio
        {
            get { return _radio; }
            set { this.RaiseAndSetIfChanged(ref _radio, value); }
        }

        private int _radioType = 0;
        public int radioType
        {
            get { return _radioType; }
            set
            {
                if (value != _radioType)
                {
                    LoadRadio(value);
                    this.RaiseAndSetIfChanged(ref _radioType, value);
                }
            }
        }

        public MainWindowViewModel()
        {
            LoadRadio(0);
        }

        private void LoadRadio(int type)
        {
            if (radio != null)
                radio.Stop();

            //DH1KLM: Keep protocol selection explicit. P1 uses the existing Hermes frame engine;
            // P2 uses the existing Ethernet frame engine. Only the board identity changes.
            switch (type)
            {
                case 0: LoadP1(HpsdrBoards.Hermes); break;
                case 1: LoadP1(HpsdrBoards.Angelia); break;
                case 2: LoadP1(HpsdrBoards.Orion); break;
                case 3: LoadP1(HpsdrBoards.OrionMkII); break;
                case 4: LoadP1(HpsdrBoards.Saturn); break;
                case 5: LoadP2(HpsdrBoards.Hermes); break;
                case 6: LoadP2(HpsdrBoards.Angelia); break;
                case 7: LoadP2(HpsdrBoards.Orion); break;
                case 8: LoadP2(HpsdrBoards.OrionMkII); break;
                case 9: LoadP2(HpsdrBoards.Saturn); break;
                case 10: LoadP1(HpsdrBoards.HermesLite); break;
                case 11: LoadP2(HpsdrBoards.HermesLite); break;
                default: LoadP1(HpsdrBoards.Hermes); break;
            }
        }

        private void LoadP1(HpsdrBoardProfile board)
        {
            var hermes = new FakeHermes
            {
                boardID = board.P1BoardId,
                boardName = board.Name,
                hermesCodeVersion = 30,
                port = 1024
            };

            hermes.start();
            radio = hermes;
        }

        private void LoadP2(HpsdrBoardProfile board)
        {
            var radioP2 = new FakeHermesNewProtocol
            {
                boardID = board.P2BoardId,
                boardName = board.Name,
                codeVersion = 23,
                protocolSupported = 0,
                numRxs = 7,
                port = 1024
            };

            radioP2.start();
            radio = radioP2;
        }
    }
}
