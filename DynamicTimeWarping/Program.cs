using System.Numerics;

Console.Write("Separator: ");
var separator = Console.ReadLine() ?? " ";

Console.Write("Source: ");
var sourceString = Console.ReadLine();
if (string.IsNullOrEmpty(sourceString)) return;

Console.Write("Target: ");
var targetString = Console.ReadLine();
if (string.IsNullOrEmpty(targetString)) return;

Console.WriteLine();

var source = sourceString.Split(separator).Select(s => double.Parse(s)).ToArray();
var target = targetString.Split(separator).Select(s => double.Parse(s)).ToArray();

var dtwResult = Dtw(source, target);

for (var i = 1; i < source.Length + 1; i++)
{
    for (var j = 1; j < target.Length + 1; j++)
    {
        Console.Write(dtwResult[i, j]);
        Console.Write(' ');
    }
    Console.WriteLine();
}

static T[,] Dtw<T>(T[] source, T[] target)
    where T : INumber<T>, IMinMaxValue<T>
{
    var n = source.Length + 1;
    var m = target.Length + 1;
    var dtw = new T[n, m];

    for (var i = 0; i < n; i++)
    {
        for (var j = 0; j < m; j++)
        {
            dtw[i, j] = T.MaxValue;
        }
    }

    dtw[0, 0] = T.Zero;

    for (var i = 1; i < n; i++)
    {
        for (var j = 1; j < m; j++)
        {
            var cost = T.Abs(source[i - 1] - target[j - 1]);
            var minPrev = new T[] {
                dtw[i - 1, j],
                dtw[i, j - 1],
                dtw[i - 1, j - 1]
            }.Min()!;

            dtw[i, j] = cost + minPrev;
        }
    }

    return dtw;
}
