// Program.cs
// Voll funktionsfähiges Schere-Stein-Papier-Echse-Spock inkl. Namenseingabe + Highscore (AES-GCM + HMAC) + UTF-8
// OHNE csproj-Änderungen: KEIN ProtectedData/DPAPI.
// Keys werden deterministisch aus Pepper + Salt + (User+Machine) abgeleitet.
// Speichert unter %AppData%\ScheresteinPapierLS\highscore.dat

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SchereSteinPapier
{
    internal class Program
    {
        enum Werkzeuge
        {
            None = 0,
            Schere = 1,
            Stein = 2,
            Papier = 3,
            Echse = 4,
            Spock = 5
        }

        enum RoundResult
        {
            Draw,
            Player1,
            Player2
        }

        // --- Spielzustand ---
        static bool weiterspielen = true;

        static int Spieler1Sieg = 0;
        static int Spieler2Sieg = 0;
        static int Unentschieden = 0;

        static Werkzeuge Spieler1Option;
        static Werkzeuge Spieler2Option;

        static string Spieler1Name = "Spieler 1";
        static string Spieler2Name = "Spieler 2";

        // Random: NICHT pro Runde neu erzeugen
        static readonly Random rnd = new Random();

        // Highscore
        static HighscoreData Highscore = new HighscoreData();

        static void Main(string[] args)
        {
            // Highscore laden (wenn Datei kaputt/manipuliert: startet leer)
            Highscore = HighscoreService.Load();

            while (true)
            {
                weiterspielen = true; // pro Modus zurücksetzen

                Console.Clear();
                Console.WriteLine("Willkommen zu Schere Stein Papier Echse Spock");
                Console.WriteLine("--------------------------------------------");
                Console.WriteLine();
                Console.WriteLine(" 1 vs 1      drücke  1");
                Console.WriteLine(" 1 vs KI     drücke  2");
                Console.WriteLine(" Highscore   drücke  3");
                Console.WriteLine(" Beenden     drücke  0");
                Console.WriteLine();

                string? inputGameoption = Console.ReadLine();

                if (!int.TryParse(inputGameoption, out int gameOption))
                {
                    Console.WriteLine("Ungültige Eingabe. Drücke eine Taste ...");
                    Console.ReadKey(true);
                    continue;
                }

                if (gameOption == 0)
                {
                    HighscoreService.Save(Highscore);
                    Console.WriteLine("Danke fürs Spielen! Bis zum nächsten Mal.");
                    return;
                }

                if (gameOption == 3)
                {
                    Console.Clear();
                    PrintHighscore();
                    Console.WriteLine("\nDrücke eine Taste für zurück ins Menü ...");
                    Console.ReadKey(true);
                    continue;
                }

                if (gameOption != 1 && gameOption != 2)
                {
                    Console.WriteLine("Ungültige Option. Drücke eine Taste ...");
                    Console.ReadKey(true);
                    continue;
                }

                // Session-Statistik zurücksetzen
                ResetSessionStats();

                // Namen abfragen
                if (gameOption == 1)
                {
                    Console.Clear();
                    Spieler1Name = ReadPlayerName("Name Spieler 1 eingeben");
                    Spieler2Name = ReadPlayerName("Name Spieler 2 eingeben");
                    Console.Clear();
                    OneVsOneGame();
                }
                else
                {
                    Console.Clear();
                    Spieler1Name = ReadPlayerName("Dein Name");
                    Spieler2Name = "KI";
                    Console.Clear();
                    OneVsKIGame();
                }

                // Nach Spielmodus: Highscore speichern
                HighscoreService.Save(Highscore);

                Console.WriteLine("\nZurück ins Hauptmenü (beliebige Taste) oder Beenden mit 'n':");
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.N)
                {
                    HighscoreService.Save(Highscore);
                    Console.WriteLine("Danke fürs Spielen! Bis zum nächsten Mal.");
                    return;
                }
            }
        }

        static void ResetSessionStats()
        {
            Spieler1Sieg = 0;
            Spieler2Sieg = 0;
            Unentschieden = 0;
        }

        static void OneVsOneGame()
        {
            do
            {
                Console.WriteLine("--- 1 vs 1 Modus ---\n");

                Spieler1Option = HoleSpielerWahl(Spieler1Name);
                Spieler2Option = HoleSpielerWahl(Spieler2Name);

                var result = WhoWinsAndApply();
                PrintRoundSummary(result);

                Console.WriteLine();
                Console.WriteLine($"Session-Statistik: {Spieler1Name}: {Spieler1Sieg} Siege; {Spieler2Name}: {Spieler2Sieg} Siege; Unentschieden: {Unentschieden}");
                Console.WriteLine();

                Console.WriteLine("Noch eine Runde? (Drücke 'n' zum Beenden, jede andere Taste zum Weitermachen)");
                ConsoleKeyInfo eingabe = Console.ReadKey(true);

                if (eingabe.Key == ConsoleKey.N)
                {
                    weiterspielen = false;
                    Console.WriteLine("Spielmodus beendet.");
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
                Console.WriteLine("--- 1 vs KI Modus ---\n");

                Spieler1Option = HoleSpielerWahl(Spieler1Name);

                Werkzeuge[] werte = Enum.GetValues<Werkzeuge>();
                // 1..Length-1 (None ausschließen)
                Spieler2Option = werte[rnd.Next(1, werte.Length)];

                var result = WhoWinsAndApply();
                PrintRoundSummary(result);

                Console.WriteLine();
                Console.WriteLine($"Session-Statistik: {Spieler1Name}: {Spieler1Sieg} Siege; {Spieler2Name}: {Spieler2Sieg} Siege; Unentschieden: {Unentschieden}");
                Console.WriteLine();

                Console.WriteLine("\nNoch eine Runde? (Drücke 'n' zum Beenden, jede andere Taste zum Weitermachen)");
                ConsoleKeyInfo eingabe = Console.ReadKey(true);

                if (eingabe.Key == ConsoleKey.N)
                {
                    weiterspielen = false;
                    Console.WriteLine("Spielmodus beendet.");
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
                Console.WriteLine($"{spielerName}, wähle dein Werkzeug:");
                Console.WriteLine(" 1: Schere");
                Console.WriteLine(" 2: Stein");
                Console.WriteLine(" 3: Papier");
                Console.WriteLine(" 4: Echse");
                Console.WriteLine(" 5: Spock");
                Console.Write("Eingabe: ");

                string? input = Console.ReadLine();

                if (int.TryParse(input, out int val) &&
                    Enum.IsDefined(typeof(Werkzeuge), val) &&
                    val != (int)Werkzeuge.None)
                {
                    Console.Clear();
                    return (Werkzeuge)val;
                }

                Console.WriteLine("Ungültige Eingabe, versuch's nochmal!\n");
            }
        }

        static RoundResult WhoWins()
        {
            if (Spieler1Option == Spieler2Option) return RoundResult.Draw;

            bool p1 =
                (Spieler1Option == Werkzeuge.Schere && (Spieler2Option == Werkzeuge.Papier || Spieler2Option == Werkzeuge.Echse)) ||
                (Spieler1Option == Werkzeuge.Stein && (Spieler2Option == Werkzeuge.Schere || Spieler2Option == Werkzeuge.Echse)) ||
                (Spieler1Option == Werkzeuge.Papier && (Spieler2Option == Werkzeuge.Stein || Spieler2Option == Werkzeuge.Spock)) ||
                (Spieler1Option == Werkzeuge.Echse && (Spieler2Option == Werkzeuge.Spock || Spieler2Option == Werkzeuge.Papier)) ||
                (Spieler1Option == Werkzeuge.Spock && (Spieler2Option == Werkzeuge.Schere || Spieler2Option == Werkzeuge.Stein));

            return p1 ? RoundResult.Player1 : RoundResult.Player2;
        }

        static RoundResult WhoWinsAndApply()
        {
            var result = WhoWins();

            switch (result)
            {
                case RoundResult.Draw:
                    Unentschieden++;
                    HighscoreService.UpsertResult(Highscore, Spieler1Name, win: false, loss: false, draw: true);
                    HighscoreService.UpsertResult(Highscore, Spieler2Name, win: false, loss: false, draw: true);
                    break;

                case RoundResult.Player1:
                    Spieler1Sieg++;
                    HighscoreService.UpsertResult(Highscore, Spieler1Name, win: true, loss: false, draw: false);
                    HighscoreService.UpsertResult(Highscore, Spieler2Name, win: false, loss: true, draw: false);
                    break;

                case RoundResult.Player2:
                    Spieler2Sieg++;
                    HighscoreService.UpsertResult(Highscore, Spieler1Name, win: false, loss: true, draw: false);
                    HighscoreService.UpsertResult(Highscore, Spieler2Name, win: true, loss: false, draw: false);
                    break;
            }

            return result;
        }

        static void PrintRoundSummary(RoundResult result)
        {
            Console.WriteLine($"Auswahl {Spieler1Name}: {Spieler1Option}");
            Console.WriteLine($"Auswahl {Spieler2Name}: {Spieler2Option}\n");

            Console.WriteLine(result switch
            {
                RoundResult.Draw => "Unentschieden!",
                RoundResult.Player1 => $"{Spieler1Name} gewinnt!",
                _ => $"{Spieler2Name} gewinnt!"
            });
        }

        static string ReadPlayerName(string prompt)
        {
            while (true)
            {
                Console.Write($"{prompt}: ");
                string? name = Console.ReadLine();
                name = (name ?? "").Trim();

                if (name.Length < 2)
                {
                    Console.WriteLine("Name zu kurz (min 2 Zeichen).");
                    continue;
                }
                if (name.Length > 20)
                {
                    Console.WriteLine("Name zu lang (max 20).");
                    continue;
                }

                return name;
            }
        }

        static void PrintHighscore()
        {
            var top = HighscoreService.GetTop10(Highscore);

            Console.WriteLine("----- HIGHSCORE (Top 10) -----");
            if (top.Count == 0)
            {
                Console.WriteLine("Noch keine Einträge.");
                Console.WriteLine("------------------------------");
                return;
            }

            for (int i = 0; i < top.Count; i++)
            {
                var e = top[i];
                Console.WriteLine($"{i + 1,2}. {e.Name,-20}  Wins:{e.Wins,3}  Loss:{e.Losses,3}  Draw:{e.Draws,3}  Games:{e.Games,3}  WR:{e.WinRate:P1}");
            }
            Console.WriteLine("------------------------------");
            Console.WriteLine();
            Console.WriteLine($"Speicherort: {HighscoreService.StorageInfo}");
        }
    }

    // -------------------- Datenmodelle --------------------

    public record HighscoreEntry
    {
        public string Name { get; init; } = "";
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }

        [JsonIgnore]
        public int Games => Wins + Losses + Draws;

        [JsonIgnore]
        public double WinRate => Games == 0 ? 0 : (double)Wins / Games;
    }

    public class HighscoreData
    {
        public List<HighscoreEntry> Entries { get; set; } = new();
    }

    // -------------------- Highscore Service (AES-GCM + HMAC, ohne DPAPI/csproj Änderungen) --------------------

    public static class HighscoreService
    {
        private static readonly string Folder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScheresteinPapierLS");
        private static readonly string FilePath = Path.Combine(Folder, "highscore.dat");

        // Wir speichern nur einen Salt (kein Key), Keys werden deterministisch daraus abgeleitet.
        private static readonly string SaltFile = Path.Combine(Folder, "salt.bin");

        private const int TagSize = 16; // 128-bit Tag
        private static readonly byte[] AAD = Encoding.UTF8.GetBytes("SSP_LS_v1");

        // Pepper = kompiliertes Geheimnis. Für dein Projekt ok.
        // Wenn du den String änderst, sind alte Dateien nicht mehr entschlüsselbar.
        private const string Pepper = "SSP_LS_Pepper_v1_CHANGE_ME_TO_SOMETHING_RANDOM_2026";

        public static string StorageInfo => FilePath;

        public static HighscoreData Load()
        {
            Directory.CreateDirectory(Folder);

            if (!File.Exists(FilePath))
                return new HighscoreData();

            byte[] fileBytes;
            try { fileBytes = File.ReadAllBytes(FilePath); }
            catch { return new HighscoreData(); }

            // Layout: [HMAC(32)] [Nonce(12)] [Tag(16)] [Ciphertext(n)]
            if (fileBytes.Length < 32 + 12 + TagSize)
                return new HighscoreData();

            byte[] storedHmac = fileBytes[..32];
            byte[] nonce = fileBytes[32..(32 + 12)];
            byte[] tag = fileBytes[(32 + 12)..(32 + 12 + TagSize)];
            byte[] ciphertext = fileBytes[(32 + 12 + TagSize)..];

            (byte[] aesKey, byte[] hmacKey) = GetOrCreateDerivedKeys();

            // 1) Integrität prüfen
            byte[] dataToAuth = Combine(nonce, tag, ciphertext);
            byte[] computedHmac = ComputeHmac(hmacKey, dataToAuth);

            if (!CryptographicOperations.FixedTimeEquals(storedHmac, computedHmac))
                return new HighscoreData();

            // 2) Entschlüsseln
            byte[] plaintext = new byte[ciphertext.Length];
            try
            {
                using var aesgcm = new AesGcm(aesKey, TagSize);
                aesgcm.Decrypt(nonce, ciphertext, tag, plaintext, AAD);
            }
            catch
            {
                return new HighscoreData();
            }

            // 3) JSON -> Objekt
            try
            {
                string json = Encoding.UTF8.GetString(plaintext);
                return JsonSerializer.Deserialize<HighscoreData>(json) ?? new HighscoreData();
            }
            catch
            {
                return new HighscoreData();
            }
        }

        public static void Save(HighscoreData data)
        {
            Directory.CreateDirectory(Folder);

            (byte[] aesKey, byte[] hmacKey) = GetOrCreateDerivedKeys();

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            byte[] plaintext = Encoding.UTF8.GetBytes(json);

            byte[] nonce = RandomNumberGenerator.GetBytes(12);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[TagSize];

            using (var aesgcm = new AesGcm(aesKey, TagSize))
            {
                aesgcm.Encrypt(nonce, plaintext, ciphertext, tag, AAD);
            }

            byte[] dataToAuth = Combine(nonce, tag, ciphertext);
            byte[] hmac = ComputeHmac(hmacKey, dataToAuth);

            byte[] outBytes = Combine(hmac, nonce, tag, ciphertext);

            try { File.WriteAllBytes(FilePath, outBytes); }
            catch { /* optional logging */ }
        }

        public static void UpsertResult(HighscoreData data, string playerName, bool win, bool loss, bool draw)
        {
            if (string.IsNullOrWhiteSpace(playerName)) return;

            var entry = data.Entries.FirstOrDefault(e =>
                e.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                entry = new HighscoreEntry { Name = playerName.Trim() };
                data.Entries.Add(entry);
            }

            if (win) entry.Wins++;
            else if (loss) entry.Losses++;
            else if (draw) entry.Draws++;
        }

        public static List<HighscoreEntry> GetTop10(HighscoreData data)
        {
            return data.Entries
                .OrderByDescending(e => e.Wins)
                .ThenByDescending(e => e.WinRate)
                .ThenByDescending(e => e.Games)
                .Take(10)
                .ToList();
        }

        // ---------------- Key Derivation (ohne DPAPI) ----------------

        private static (byte[] aesKey, byte[] hmacKey) GetOrCreateDerivedKeys()
        {
            byte[] salt = GetOrCreateSalt(); // persistent, random
            string bind = $"{Environment.UserName}|{Environment.MachineName}|{Environment.OSVersion.VersionString}";

            byte[] pepperBytes = Encoding.UTF8.GetBytes(Pepper);
            byte[] bindBytes = Encoding.UTF8.GetBytes(bind);

            // master = SHA256(pepper || salt || bind)
            byte[] master = SHA256.HashData(Combine(pepperBytes, salt, bindBytes));

            // domain separation
            byte[] aesKey = DeriveKey(master, "AES", 32);
            byte[] hmacKey = DeriveKey(master, "HMAC", 32);

            return (aesKey, hmacKey);
        }

        private static byte[] GetOrCreateSalt()
        {
            try
            {
                if (File.Exists(SaltFile))
                {
                    var s = File.ReadAllBytes(SaltFile);
                    if (s.Length >= 16) return s;
                }

                byte[] salt = RandomNumberGenerator.GetBytes(32);
                File.WriteAllBytes(SaltFile, salt);
                return salt;
            }
            catch
            {
                // Notfalls ephemeral => Highscore nicht persistent (sehr selten)
                return RandomNumberGenerator.GetBytes(32);
            }
        }

        private static byte[] DeriveKey(byte[] master, string label, int length)
        {
            using var h = new HMACSHA256(master);
            byte[] full = h.ComputeHash(Encoding.UTF8.GetBytes(label));

            if (full.Length == length) return full;

            byte[] outKey = new byte[length];
            Buffer.BlockCopy(full, 0, outKey, 0, Math.Min(length, full.Length));
            return outKey;
        }

        // ---------------- Crypto helpers ----------------

        private static byte[] ComputeHmac(byte[] key, byte[] data)
        {
            using var h = new HMACSHA256(key);
            return h.ComputeHash(data);
        }

        private static byte[] Combine(params byte[][] arrays)
        {
            int len = arrays.Sum(a => a.Length);
            byte[] result = new byte[len];
            int offset = 0;

            foreach (var a in arrays)
            {
                Buffer.BlockCopy(a, 0, result, offset, a.Length);
                offset += a.Length;
            }
            return result;
        }
    }
}
