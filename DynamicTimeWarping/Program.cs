﻿using System.Numerics;

Console.Write("Separator: ");
var separator = Console.ReadLine() ?? " ";

Console.Write("Source: ");
var sourceString = Console.ReadLine();
if (string.IsNullOrEmpty(sourceString)) return;

Console.Write("Target: ");
var targetString = Console.ReadLine();
if (string.IsNullOrEmpty(targetString)) return;

Console.WriteLine();

var source = sourceString.Split(separator).Select(double.Parse).ToArray();
var target = targetString.Split(separator).Select(double.Parse).ToArray();

var dtwResult = DtwMatrix(source, target, (s, t) => Math.Abs(s - t));

WriteMatrixToConsole(dtwResult, source.Length + 1, target.Length + 1, 1, 1);

return;

static void WriteMatrixToConsole<T>(T[,] matrix, int n, int m, int nOffset = 0, int mOffset = 0)
{
    for (var i = nOffset; i < n; i++)
    {
        for (var j = mOffset; j < m; j++)
        {
            Console.Write(matrix[i, j]);
            Console.Write(' ');
        }
        Console.WriteLine();
    }
}

static TResult[,] DtwMatrix<T, TResult>(T[] source, T[] target, Func<T, T, TResult> distance)
    where TResult : INumber<TResult>, IMinMaxValue<TResult>
{
    var n = source.Length + 1;
    var m = target.Length + 1;
    var dtw = new TResult[n, m];

    for (var i = 0; i < n; i++)
    {
        for (var j = 0; j < m; j++)
        {
            dtw[i, j] = TResult.MaxValue;
        }
    }

    dtw[0, 0] = TResult.Zero;

    for (var i = 1; i < n; i++)
    {
        for (var j = 1; j < m; j++)
        {
            var cost = distance(source[i - 1], target[j - 1]);
            var minPrev = new [] {
                dtw[i - 1, j],
                dtw[i, j - 1],
                dtw[i - 1, j - 1]
            }.Min()!;

            dtw[i, j] = cost + minPrev;
        }
    }

    return dtw;
}
