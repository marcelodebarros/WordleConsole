using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace WordleConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: WordleConsole.exe <word len> <attempts allowed> <dictionary file> <human OR computer>");
                return;
            }

            WordleGame wordleGame = new WordleGame(Int32.Parse(args[0]), Int32.Parse(args[1]), args[2]);

            bool human = args[3].Equals("human");

            for (; ; )
                if (!wordleGame.Play(human) || wordleGame.CheckGuesses()) break;
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

        //For computer use only
        private Hashtable guessedWords = null;
        Hashtable wrongLetters = null;
        Hashtable rightLettersWrongPlace= null;
        Hashtable rightLetters = null;

        public WordleGame(int wordLen, int attempts, string dictionary)
        {
            this.wordLen = wordLen;
            this.attempts = attempts;
            this.dictionary = dictionary;

            board = new string[attempts];
            htWords = new Hashtable();

            guessedWords = new Hashtable();
            wrongLetters = new Hashtable();
            rightLettersWrongPlace = new Hashtable();
            rightLetters = new Hashtable();

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
                    bool validWord = true;
                    foreach (char c in line)
                    {
                        if (!(c >= 'A' && c <= 'Z'))
                        {
                            validWord = false;
                            break;
                        }
                    }
                    if (validWord) htWords.Add(line, true);
                }
            }
            sr.Close();

            words = new string[htWords.Count];
            int index = 0;
            foreach (string word in htWords.Keys)
                words[index++] = word;
        }

        private void SelectTheWord()
        {
            theWord = words[(new Random()).Next(0, words.Length)];
        }

        public bool Play(bool human)
        {
            if (currentAttempt >= attempts)
            {
                Console.WriteLine("Game Over: {0} lost :(. Secret Word = {1}", human ? "You" : "Computer", theWord);
                return false;
            }

            currentAttempt++;
            if (human)
            {
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
            }
            else
            {
                Console.WriteLine("Computer, what's your guess number #{0}?", currentAttempt);
                string computerGuess = ComputerGuess();
                Console.WriteLine("HERE IS MY GUESS: {0}", computerGuess);
                Thread.Sleep(3000);
                board[currentAttempt - 1] = computerGuess;

                if (!guessedWords.ContainsKey(computerGuess)) guessedWords.Add(computerGuess, true);
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

                        if (!rightLetters.ContainsKey(board[i][j])) rightLetters.Add(board[i][j], new Hashtable());
                        Hashtable positions = (Hashtable)rightLetters[board[i][j]];
                        if (!positions.ContainsKey(j)) positions.Add(j, true);
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

                        if (!rightLetters.ContainsKey(board[i][j]) && !rightLettersWrongPlace.ContainsKey(board[i][j]))
                        {
                            if (!wrongLetters.ContainsKey(board[i][j])) wrongLetters.Add(board[i][j], true);
                        }
                    }
                    else
                    {
                        WriteLineColored((ConsoleColor.DarkYellow, board[i][j].ToString()));
                        tempSetOfChars[board[i][j]] = (int)tempSetOfChars[board[i][j]] - 1;
                        if ((int)tempSetOfChars[board[i][j]] == 0) tempSetOfChars.Remove(board[i][j]);

                        if (!rightLettersWrongPlace.ContainsKey(board[i][j])) rightLettersWrongPlace.Add(board[i][j], new Hashtable());
                        Hashtable positions = (Hashtable)rightLettersWrongPlace[board[i][j]];
                        if (!positions.ContainsKey(j)) positions.Add(j, true);
                    }
                }
                Console.WriteLine();
                if (matches == wordLen)
                {
                    Console.WriteLine();
                    Console.WriteLine("Victory!!!");
                    return true;
                }
            }
            Console.WriteLine();

            return false;
        }

        private string ComputerGuess()
        {
            //First candidates: ignore wrong letters
            List<string> candidates = new List<string>();

            foreach (string str in htWords.Keys)
            {
                if (!guessedWords.ContainsKey(str))
                {
                    //Ignore wrong letters
                    bool validWord = true;
                    foreach (char c in str)
                    {
                        if (wrongLetters.ContainsKey(c))
                        {
                            validWord = false;
                            break;
                        }
                    }

                    if(validWord)
                        candidates.Add(str);
                }
            }

            //Now the more right matches, the merrier
            int countPerfectMatches = 0;
            foreach (char c in rightLetters.Keys) countPerfectMatches += ((Hashtable)rightLetters[c]).Count;
            int countImperfectMatches = 0;
            foreach (char c in rightLettersWrongPlace.Keys) countImperfectMatches += ((Hashtable)rightLettersWrongPlace[c]).Count;

            List<string> weightedCandidates = new List<string>();
            foreach (string str in candidates)
            {
                if (rightLetters.Count == 0 &&
                    rightLettersWrongPlace.Count == 0)
                {
                    weightedCandidates.Add(str);
                    continue;
                }

                int perfectMatches = 0;
                int countMatches = 0;
                for (int i = 0; i < str.Length; i++)
                {
                    if (rightLetters.ContainsKey(str[i]))
                    {
                        Hashtable positions = (Hashtable)rightLetters[str[i]];
                        if (positions.ContainsKey(i))
                        {
                            perfectMatches++;
                        }
                    }
                    else if (rightLettersWrongPlace.ContainsKey(str[i]))
                    {
                        Hashtable positions = (Hashtable)rightLettersWrongPlace[str[i]];
                        if (!positions.ContainsKey(i))
                        {
                            countMatches++;
                        }
                    }
                }

                if (rightLetters.Count > 0)
                {
                    if (perfectMatches == countPerfectMatches)
                    {
                        weightedCandidates.Add(str);
                        for (int i = 0; i < countMatches * wordLen; i++) weightedCandidates.Add(str);
                    }
                }
                else
                {
                    if (countMatches == countImperfectMatches)
                        for (int i = 0; i < countMatches; i++) weightedCandidates.Add(str);
                }
            }

            string[] arr = weightedCandidates.ToArray();

            return arr[(new Random()).Next(0, arr.Length)];
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
