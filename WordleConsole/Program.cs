using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace WordleConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: WordleConsole.exe <word len> <attempts allowed> <dictionary file>");
                return;
            }

            WordleGame wordleGame = new WordleGame(Int32.Parse(args[0]), Int32.Parse(args[1]), args[2]);

            for (; ; )
            {
                if (!wordleGame.Play() || wordleGame.CheckGuesses()) break;
            }
        }
    }

    public class WordleGame
    {
        private int wordLen = 0;
        private int attempts = 0;
        private int currentAttempt = 0;
        private string[] words = null;
        private string[] board = null;
        private string dictionary = "";
        private Hashtable htWords = null;
        private string theWord = "";
        public WordleGame(int wordLen, int attempts, string dictionary)
        {
            this.wordLen = wordLen;
            this.attempts = attempts;
            this.dictionary = dictionary;

            board = new string[attempts];
            htWords = new Hashtable();

            ReadWords();
            SelectTheWord();
        }

        private void ReadWords()
        {
            FileInfo fi = new FileInfo(dictionary);
            StreamReader sr = fi.OpenText();
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine().ToUpper().Trim();
                if (!String.IsNullOrEmpty(line) && line.Length == wordLen && !htWords.ContainsKey(line))
                {
                    htWords.Add(line, true);
                }
            }
            sr.Close();

            words = new string[htWords.Count];
            int index = 0;
            foreach (string word in htWords.Keys)
            {
                words[index++] = word;
            }
        }

        private void SelectTheWord()
        {
            Random rd = new Random();
            theWord = words[rd.Next(0, words.Length)];
        }

        public bool Play()
        {
            if (currentAttempt >= attempts)
            {
                Console.WriteLine("Game Over: You lost :(. Secret Word = {0}", theWord);
                return false;
            }

            currentAttempt++;
            for (; ; )
            {
                Console.WriteLine("What's your guess number #{0}?", currentAttempt);
                string line = Console.ReadLine().ToUpper().Trim();
                if (line.Length != wordLen || !htWords.ContainsKey(line))
                {
                    Console.WriteLine("Guess must be a valid {0}-length word in the dicitonary!", wordLen);
                }
                else
                {
                    board[currentAttempt - 1] = line;
                    break;
                }
            }

            return true;
        }

        public bool CheckGuesses()
        {
            Hashtable setOfChars = new Hashtable();
            foreach (char c in theWord)
            {
                if (!setOfChars.ContainsKey(c)) setOfChars.Add(c, 0);
                setOfChars[c] = (int)setOfChars[c] + 1;
            }

            Console.WriteLine();
            Console.WriteLine("BOARD:");
            for (int i = 0; i < currentAttempt; i++)
            {
                Hashtable tempSetOfChars = (Hashtable)setOfChars.Clone();
                for (int j = 0; j < wordLen; j++)
                {
                    if (board[i][j] == theWord[j])
                    {
                        tempSetOfChars[board[i][j]] = (int)tempSetOfChars[board[i][j]] - 1;
                        if ((int)tempSetOfChars[board[i][j]] == 0) tempSetOfChars.Remove(board[i][j]);
                    }
                }

                int matches = 0;
                for (int j = 0; j < wordLen; j++)
                {
                    if (theWord[j] == board[i][j])
                    {
                        WriteLineColored((ConsoleColor.DarkGreen, board[i][j].ToString()));
                        matches++;
                    }
                    else if (!tempSetOfChars.ContainsKey(board[i][j]))
                    {
                        WriteLineColored((ConsoleColor.DarkRed, board[i][j].ToString()));
                    }
                    else
                    {
                        WriteLineColored((ConsoleColor.DarkYellow, board[i][j].ToString()));
                        tempSetOfChars[board[i][j]] = (int)tempSetOfChars[board[i][j]] - 1;
                        if ((int)tempSetOfChars[board[i][j]] == 0) tempSetOfChars.Remove(board[i][j]);
                    }
                }
                Console.WriteLine();
                if (matches == wordLen)
                {
                    Console.WriteLine();
                    Console.WriteLine("You Won!");
                    return true;
                }
            }
            Console.WriteLine();

            return false;
        }

        private void WriteLineColored(params (ConsoleColor color, string value)[] values)
        {
            foreach (var value in values)
            {
                Console.ForegroundColor = value.color;
                Console.Write(value.value);
            }
            Console.ResetColor();
        }
    }
}
