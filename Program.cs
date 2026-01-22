namespace SchereSteinPapier
{
    internal class Program
    {
        enum Werkzeuge
        {
            None,
            Schere,
            Stein,
            Papier,
            Echse,
            Spock
        }

        static bool weiterspielen = true;
        static int Spieler1Sieg = 0;
        static int Spieler2Sieg = 0;
        static int Unetschieden = 0;
        static Werkzeuge Spieler1Option;
        static Werkzeuge Spieler2Option;
        static void Main(string[] args)
        {
          
            while (true)
            {
                Console.WriteLine("Willkommen zu Schere Stein Papier Echse Spock");
                Console.WriteLine("-----------------------------------------------");
                Console.WriteLine();
                Console.WriteLine(" 1 vs 1  drücke ------  1 ");
                Console.WriteLine(" 1 vs KI  drücke die ------ 2 ");

                //File laden//
                //!File dann erstelle//

                string inputGameoption = Console.ReadLine();


                if (int.TryParse(inputGameoption, out int gameOption))
                {
                    if (gameOption == 1 || gameOption == 2)
                    {
                        if (gameOption == 1)
                        {
                            OneVsOneGame();
                        }
                        else if (gameOption == 2)
                        {
                            OneVsKIGame();
                        }
                        
                      



                    }
                    else
                    {
                        Console.WriteLine("Ungültige Option");
                        continue;
                    }

                }

                Console.WriteLine("\nNoch ein Spiel? (Drücke 'n' zum Beenden, jede andere Taste zum Weitermachen)");


                ConsoleKeyInfo eingabe = Console.ReadKey(true);

                if (eingabe.Key == ConsoleKey.N)
                {
                    weiterspielen = false;
                    Console.WriteLine("Danke fürs Spielen! Bis zum nächsten Mal.");
                }
                else
                {
                    Console.Clear();
                }

            }

        }

        static void OneVsOneGame()
        {
            
     
                do
                {
                    Console.WriteLine("--- 1 vs 1 Modus ---");

                     Spieler1Option = HoleSpielerWahl("Spieler 1");

                     Spieler2Option = HoleSpielerWahl("Spieler 2");

                    whoWins();

                    Console.WriteLine();
                    Console.WriteLine($"Aktuelle Statistik lauter Spieler 1 : {Spieler1Sieg} Siege; Spieler 2 : {Spieler2Sieg} Siege; Unentschieden: {Unetschieden}");
                    Console.WriteLine();
                    Console.WriteLine("\nNoch eine Runde? (Drücke 'n' zum Beenden, jede andere Taste zum Weitermachen)");

             
                    ConsoleKeyInfo eingabe = Console.ReadKey(true);

                    if (eingabe.Key == ConsoleKey.N)
                    {
                        weiterspielen = false;
                        Console.WriteLine("Danke fürs Spielen! Bis zum nächsten Mal.");
                    }
                    else
                    {
                        Console.Clear();
                    }


                } while (weiterspielen);

            }



        static void OneVsKIGame()
        {
       

            do
            {
                Console.WriteLine("--- 1 vs KI Modus ---");

                 Spieler1Option = HoleSpielerWahl("Spieler 1");

                Random rnd = new Random();

                Werkzeuge[] werte = Enum.GetValues<Werkzeuge>();

                Spieler2Option = werte[rnd.Next(1, werte.Length)];

                whoWins();

                Console.WriteLine();
                Console.WriteLine($"Aktuelle Statistik lauter Spieler 1 : {Spieler1Sieg} Siege; Spieler 2 : {Spieler2Sieg} Siege; Unentschieden: {Unetschieden}");
                Console.WriteLine();



                Console.WriteLine("\nNoch eine Runde? (Drücke 'n' zum Beenden, jede andere Taste zum Weitermachen)");

                ConsoleKeyInfo eingabe = Console.ReadKey(true);

                if (eingabe.Key == ConsoleKey.N)
                {
                    weiterspielen = false;
                    Console.WriteLine("Danke fürs Spielen! Bis zum nächsten Mal.");
                }
                else
                {
                    Console.Clear(); 
                }


            } while (weiterspielen);


        }

            static Werkzeuge HoleSpielerWahl(string spielerName)
            {
                while (true)
                {
                    Console.WriteLine($"{spielerName}, wähle dein Werkzeug (1: Schere, 2: Stein, 3: Papier, 4: Echse, 5: Spock):");
                    string input = Console.ReadLine();

                    if (Enum.TryParse(input, out Werkzeuge wahl) &&
                        Enum.IsDefined(typeof(Werkzeuge), wahl) &&
                        wahl != Werkzeuge.None)
                    {
                        
                        Console.Clear();
                        return wahl;
                    }

                    Console.WriteLine("Ungültige Eingabe, versuch's nochmal!");
                }
            }

            static void whoWins()
            {
            if (Spieler1Option == Spieler2Option)
            {
                Console.WriteLine("Unentschieden!");
                Unetschieden++;
            }
            else if (

                (Spieler1Option == Werkzeuge.Schere && (Spieler2Option == Werkzeuge.Papier || Spieler2Option == Werkzeuge.Echse)) ||

                (Spieler1Option == Werkzeuge.Stein && (Spieler2Option == Werkzeuge.Schere || Spieler2Option == Werkzeuge.Echse)) ||

                (Spieler1Option == Werkzeuge.Papier && (Spieler2Option == Werkzeuge.Stein || Spieler2Option == Werkzeuge.Spock)) ||

                (Spieler1Option == Werkzeuge.Echse && (Spieler2Option == Werkzeuge.Spock || Spieler2Option == Werkzeuge.Papier)) ||

                (Spieler1Option == Werkzeuge.Spock && (Spieler2Option == Werkzeuge.Schere || Spieler2Option == Werkzeuge.Stein))
            )
            {
                Console.WriteLine("Spieler 1 gewinnt!");
                Spieler1Sieg++;


            }
            else
            {
                Console.WriteLine("Spieler 2 gewinnt!");
                Spieler2Sieg++;
            }
        }

        }
    }

