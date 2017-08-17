using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SharpFish
{
    public class Engine
    {
        private static StreamWriter _input;
        private readonly Process _process;
        private readonly Dictionary<string, string> _defaultOptions;
        public Action<string> Result;

        private Player _player;

        public string CurrentPlayer => _player == Player.White ? "w" : "b";

        public Engine()
        {
            var processStartInfo = new ProcessStartInfo("stockfish.exe");
            processStartInfo.UseShellExecute = false;

            processStartInfo.RedirectStandardError = true;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardOutput = true;

            Result = (r) => {};

            _process = new Process { StartInfo = processStartInfo };
            _process.OutputDataReceived += (sender, e) => Result(e.Data);
            _process.Start();
            _input = _process.StandardInput;

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            _defaultOptions = new Dictionary<string, string>();
            _defaultOptions.Add("Write Debug Log", "false");
            _defaultOptions.Add("Contempt Factor", "0");
            _defaultOptions.Add("Contempt", "0");
            _defaultOptions.Add("Min Split Depth", "0");
            _defaultOptions.Add("Threads", "4");
            _defaultOptions.Add("Hash", "1024");
            _defaultOptions.Add("MultiPV", "1");
            _defaultOptions.Add("Skill Level", "20");
            _defaultOptions.Add("Move Overhead", "30");
            _defaultOptions.Add("Minimum Thinking Time", "20");
            _defaultOptions.Add("Slow Mover", "80");
            _defaultOptions.Add("UCI_Chess960", "false");
        }

        public Engine(Player currPlayer): this()
        {
            SetCurrentPlayer(currPlayer);
        }

        public void Init(Dictionary<string, string> options = null)
        {
            _input.WriteLine("uci");

            foreach (var option in _defaultOptions)
            {
                SetOption(option.Key, option.Value);
            }

            if (options != null)
            {
                foreach (var option in options)
                {
                    SetOption(option.Key, option.Value);
                }
            }
        }

        //2kr4/1pp2ppp/p3b3/4p2P/4P3/1P2PK1P/2PrN1B1/2R5 b - - 0 1
        public void NewGame()
        {
            SetFenPosition("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            _input.WriteLine("ucinewgame");            
        }

        public void SetCurrentPlayer(Player p)
        {
            _player = p;
        }

        public void SetPosition(IEnumerable<string> move)
        {
            _input.WriteLine("position startpos moves {0}", string.Join(" ", move));
        }

        public void SetFenPosition(string fen)
        {
            SetFenPosition(fen, "");
        }
        public void SetFenPosition(string fen, string currentPlayer)
        {
            _input.WriteLine("position fen {0} {1}", fen, currentPlayer);
        }

        public void SetOption(string name, string value)
        {
            _input.WriteLine("setoption name {0} value {1}", name, value);
        }

        private void Go(int depth)
        {
            _input.WriteLine("go depth {0}", depth);
        }

        public void FindBest()
        {
            Go(20);
        }
    }

    public enum Player
    {
        White,
        Black
    }
}
