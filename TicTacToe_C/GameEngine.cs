using System;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToe_C;

public static class GameEngine
{
    public const int Player   =  1;
    public const int Computer = -1;
    public const int Empty    =  0;

    public static readonly int[][] WinLines =
    [
        [0,1,2],[3,4,5],[6,7,8],
        [0,3,6],[1,4,7],[2,5,8],
        [0,4,8],[2,4,6]
    ];

    public static int? CheckWinner(int[] board)
    {
        foreach (var line in WinLines)
        {
            int s = board[line[0]] + board[line[1]] + board[line[2]];
            if (s ==  3) return Player;
            if (s == -3) return Computer;
        }
        return null;
    }

    public static int[]? GetWinLine(int[] board)
    {
        foreach (var line in WinLines)
        {
            int s = board[line[0]] + board[line[1]] + board[line[2]];
            if (s == 3 || s == -3) return line;
        }
        return null;
    }

    public static List<int> GetEmpty(int[] board) =>
        [.. Enumerable.Range(0, 9).Where(i => board[i] == Empty)];

    private static int Minimax(int[] board, int depth, bool isMax, int? maxDepth)
    {
        var winner = CheckWinner(board);
        if (winner == Player)   return 100 - depth;
        if (winner == Computer) return depth - 100;

        var empty = GetEmpty(board);
        if (empty.Count == 0) return 0;
        if (maxDepth.HasValue && depth >= maxDepth.Value) return 0;

        var next = (int[])board.Clone();

        if (isMax)
        {
            int best = int.MinValue;
            foreach (int i in empty)
            {
                next[i] = Player;
                best = Math.Max(best, Minimax(next, depth + 1, false, maxDepth));
                next[i] = Empty;
            }
            return best;
        }
        else
        {
            int best = int.MaxValue;
            foreach (int i in empty)
            {
                next[i] = Computer;
                best = Math.Min(best, Minimax(next, depth + 1, true, maxDepth));
                next[i] = Empty;
            }
            return best;
        }
    }

    public static int GetComputerMove(int[] board, int level)
    {
        var empty = GetEmpty(board);
        if (level == 1)
            return empty[Random.Shared.Next(empty.Count)];

        int? maxDepth = level switch { 2 => 1, 3 => 2, 4 => 3, _ => null };

        int bestScore = int.MaxValue;
        var bestMoves = new List<int>();
        var next = (int[])board.Clone();

        foreach (int i in empty)
        {
            next[i] = Computer;
            int score = Minimax(next, 1, true, maxDepth);
            next[i] = Empty;

            if (score < bestScore)       { bestScore = score; bestMoves = [i]; }
            else if (score == bestScore)   bestMoves.Add(i);
        }

        return bestMoves[Random.Shared.Next(bestMoves.Count)];
    }
}
