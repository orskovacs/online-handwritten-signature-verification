using System.Numerics;

namespace EbDbaAndLsDtw;

class DtwResult<TElement, TDistance>
    where TDistance : INumber<TDistance>, IMinMaxValue<TDistance>
{
    public static DtwResult<TElement, TDistance> Dtw(
        IEnumerable<TElement> source,
        IEnumerable<TElement> target,
        Func<TElement, TElement, TDistance> distance)
    {
        var n = source.Count() + 1;
        var m = target.Count() + 1;
        var accumulatedCostMatrix = new TDistance[n, m];

        for (var row = 0; row < n; row++)
        {
            for (var col = 0; col < m; col++)
            {
                accumulatedCostMatrix[row, col] = TDistance.MaxValue;
            }
        }

        accumulatedCostMatrix[0, 0] = TDistance.Zero;

        for (var row = 1; row < n; row++)
        {
            for (var col = 1; col < m; col++)
            {
                var cost = distance(source.ElementAt(row - 1), target.ElementAt(col - 1));
                var minPrev = new TDistance[]
                {
                    accumulatedCostMatrix[row - 1, col],
                    accumulatedCostMatrix[row, col - 1],
                    accumulatedCostMatrix[row - 1, col - 1]
                }.Min()!;

                accumulatedCostMatrix[row, col] = cost + minPrev;
            }
        }

        var warpingPath = new List<(int Row, int Col)>();

        for (int row = n - 1, col = m - 1;  row > 0 || col > 0;)
        {
            warpingPath.Add((row, col));

            if (row == 0)
            {
                col--;
            }
            else if (col == 0)
            {
                row--;
            }
            else
            {
                var minPrev = new TDistance[]
                {
                    accumulatedCostMatrix[row - 1, col],
                    accumulatedCostMatrix[row, col - 1],
                    accumulatedCostMatrix[row - 1, col - 1]
                }.Min()!;

                if (minPrev == accumulatedCostMatrix[row - 1, col])
                {
                    row--;
                }
                else if (minPrev == accumulatedCostMatrix[row, col - 1])
                {
                    col--;
                }
                else
                {
                    row--;
                    col--;
                }
            }
        }

        warpingPath.Reverse();

        return new DtwResult<TElement, TDistance>
        {
            WarpingPath = warpingPath,
            CostMatrix = accumulatedCostMatrix,
        };
    }

    private DtwResult() { }

    public required IEnumerable<(int Row, int Col)> WarpingPath { get; init; }

    public required TDistance[,] CostMatrix { get; init; }
}
