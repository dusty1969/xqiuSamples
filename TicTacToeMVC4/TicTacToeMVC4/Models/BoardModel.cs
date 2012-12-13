using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace TicTacToeMVC.Models
{
    public class BoardModel
    {
        /// <summary>
        /// 0 represent turn for player X, 1 turn for player O
        /// </summary>
        [Required]
        public int Turn { get; set; }

        /// <summary>
        /// boardState is 9 intgers to indicate each square information
        /// 0 1 2
        /// 3 4 5 
        /// 6 7 8
        /// For each square, 0 means empty, 1 means occupied by X, 2 means occupied by O
        /// </summary>
        [Required]
        public List<int> BoardState { get; private set; }

        [Required]
        public int GameState { 
            get {
                return GetGameState();
            } 
        }

        internal string LastError { get; set; }

        public BoardModel()
        {
            BoardState = new List<int>();
            for (int i = 0; i < 9; i++)
            {
                BoardState.Add(0);
            }
        }

        public void Reset()
        {
            for (int i = 0; i < 9; i++)
            {
                BoardState[i]=0;
            }
            Turn = 0;
            LastError = "";
        }

        /// <summary>
        ///     Get Game state from the board
        /// </summary>
        /// <returns>0 for game still going, 1 for X win, 2 for O win, 3 for tie</returns>
        private int GetGameState()
        {
            // 0 1 2
            // 3 4 5
            // 6 7 8
            int result=0;

            //check result
            foreach (int[] line in Lines)
            {
                result = CheckLine(line[0], line[1], line[2]);
                if (result > 0) return result;
            }

            //still ongoing?
            for (int i = 0; i < 9; i++)
            {
                if (BoardState[i] == 0) return 0;
            }

            //must be a tie
            return 3;
        }

        //todo: localization...
        //todo: mobile app

        public bool SetStep(int whoseTurn, int index)
        {
            if (whoseTurn != Turn)
            {
                LastError = "Not your turn, please wait";
                return false;
            }

            if (index < 0 || index >= BoardState.Count)
            {
                LastError = "board square out of range, please contact administrator";
                return false;
            }

            if (BoardState[index] > 0)
            {
                //the place has been taken
                return false;
            }

            if (Turn == 0)
            {
                //X's turn
                BoardState[index] = 1;
                Turn = 1;
            }
            else
            {   //O's turn
                BoardState[index] = 2;
                Turn = 0;
            }

            //check to see if only 1 space left, if it is the case, fill it by turn automatically, 
            //do not do this step in GetGameState to avoid race conditions
            int left = 0;
            for (int i = 0; i < 9; i++)
            {
                if (BoardState[i] == 0) left++;
            }
            if (left == 1)
            {
                int fillNumber = 1;
                if (Turn == 1) fillNumber = 2;
                for (int i = 0; i < 9; i++)
                {
                    if (BoardState[i] == 0)
                    {
                        BoardState[i] = fillNumber;
                        break;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// The lines that we should check for any winning result
        /// </summary>
        private List<int[]> Lines = new List<int[]> { new int[]{0, 1, 2}, 
                                                      new int[]{ 3, 4, 5 },
                                                      new int[]{ 6, 7, 8 },
                                                      new int[]{ 0, 3, 6 },
                                                      new int[]{ 1, 4, 7 },
                                                      new int[]{ 2, 5, 8 },
                                                      new int[]{ 0, 4, 8 },
                                                      new int[]{ 2, 4, 6 },
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private int CheckLine(int a, int b, int c)
        {
            if (BoardState[a] == 1 && BoardState[b] == 1 && BoardState[c] == 1)
            {
                return 1;
            }

            if (BoardState[a] == 2 && BoardState[b] == 2 && BoardState[c] == 2)
            {
                return 2;
            }

            return 0;
        }
    }
}