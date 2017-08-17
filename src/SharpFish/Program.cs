using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SharpFish
{
    public class Program
    {
        private static Stack<string> moves;

        private static string chessId = "";
        private static Player player;
        private static bool robot;
        private static Engine engine;
        private static bool run;
        private const string url = "http://plainchess.timwoelfle.de/";

        public static void Main(string[] args)
        {
            engine = new Engine();
            moves = new Stack<string>();
            run = true;

            WriteColor("Robot? (y|n): ");
            var rp = Console.ReadKey();
            robot = rp.Key == ConsoleKey.Y;
            Console.WriteLine();

            WriteColor("Choose color (w|b): ");
            var color = Console.ReadKey();
            player = color.Key == ConsoleKey.W ? Player.White : Player.Black;
            Console.WriteLine();

            WriteColor("Chess ID: ");
            chessId = Console.ReadLine();

            engine.Init();
            engine.NewGame();
            engine.Result = Result;


            var t = Task.Run(async () =>
            {
                if (robot && player == Player.Black)
                    await JoinGame();

                engine.FindBest();
                while (run)
                {
                    await Task.Delay(5000);
                    await MainAsync();
                }
            });
            t.Wait();
        }

        static async Task MainAsync()
        {
            if (!string.IsNullOrEmpty(chessId))
            {
                string resultContent = await WaitForMove();
                var actualMove = ParseMove(resultContent);
                if (actualMove != moves.FirstOrDefault())
                {
                    moves.Push(actualMove);
                    engine.SetPosition(moves.Reverse());
                    engine.FindBest();
                }
            }

        }

        private static async Task JoinGame()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);
                var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("id", chessId) });
                await client.PostAsync("/php/joinGame.php", content);
            }
        }

        private static async Task<string> WaitForMove()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);
                var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("id", chessId) });
                var result = await client.PostAsync("/php/waitForMove.php", content);
                return await result.Content.ReadAsStringAsync();
            }
        }

        private static async Task MakeMove(string move)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(url);
                var content = new FormUrlEncodedContent(new[] {
                    new KeyValuePair<string, string>("id", chessId),
                    new KeyValuePair<string, string>("startColumn", CharToNumber(move[0])),
                    new KeyValuePair<string, string>("startRow", move[1].ToString()),
                    new KeyValuePair<string, string>("endColumn",  CharToNumber(move[2])),
                    new KeyValuePair<string, string>("endRow", move[3].ToString()),
                });
                var result = await client.PostAsync("php/makeMove.php", content);
            }
        }


        private static string ParseMove(string m)
        {
            if (m.Length > 4 && !m.Contains("join"))
                return NumberToChar(m[0]) + m[1] + NumberToChar(m[3]) + m[4];
            return null;
        }


        private static string NumberToChar(char n)
        {
            switch (n)
            {
                case '1': return "a";
                case '2': return "b";
                case '3': return "c";
                case '4': return "d";
                case '5': return "e";
                case '6': return "f";
                case '7': return "g";
                case '8': return "h";
            }

            throw new ArgumentOutOfRangeException();
        }

        private static string CharToNumber(char n)
        {
            switch (n)
            {
                case 'a': return "1";
                case 'b': return "2";
                case 'c': return "3";
                case 'd': return "4";
                case 'e': return "5";
                case 'f': return "6";
                case 'g': return "7";
                case 'h': return "8";
            }

            throw new ArgumentOutOfRangeException();
        }


        public static void WriteColor(string key)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(key);
            Console.ResetColor();
        }

        private static void Result(string s)
        {
            if (s.Contains("bestmove"))
            {
                Console.Clear();

                var r = s.Split(' ');
                WriteColor("Chess ID: ");
                Console.WriteLine(chessId);

                WriteColor("Last move: ");
                if (moves.Count() == 0)
                {
                    Console.WriteLine("STARTED");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write($"{(moves.Count() % 2 == 0 ? "B" : "W")} ");
                    Console.ResetColor();

                    Console.WriteLine(moves.FirstOrDefault());
                }

                WriteColor("Best move: ");
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write($"{(moves.Count() % 2 == 0 ? "W" : "B")} ");
                Console.ResetColor();
                Console.WriteLine(r[1]);

                //checkmate
                if (r[1].Contains("none"))
                    run = false;


                var isYourTurn = ((moves.Count() % 2 == 0 || !moves.Any()) && player == Player.White) || (moves.Count() % 2 != 0 && player == Player.Black);

                if (robot && isYourTurn)
                {
                    MakeMove(r[1]).GetAwaiter().GetResult();
                }
            }
        }
    }
}
