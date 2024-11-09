using System.Numerics;

namespace EbDbaLsDtw;

class DtwResult<TElement, TDistance>
    where TDistance : INumber<TDistance>, IMinMaxValue<TDistance>
{
    public static DtwResult<TElement, TDistance> Dtw(
        List<TElement> source,
        List<TElement> target,
        Func<TElement, TElement, int, TDistance> distance)
    {
        var n = source.Count + 1;
        var m = target.Count + 1;
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
                var cost = distance(source[row - 1], target[col - 1], row - 1);

                // Find the minimum value of the previous costs
                var minPrev = accumulatedCostMatrix[row - 1, col];
                if (accumulatedCostMatrix[row, col - 1] < minPrev)
                    minPrev = accumulatedCostMatrix[row, col - 1];
                if (accumulatedCostMatrix[row - 1, col - 1] < minPrev)
                    minPrev = accumulatedCostMatrix[row - 1, col - 1];

                accumulatedCostMatrix[row, col] = cost + minPrev;
            }
        }

        var warpingPath = new List<(int Row, int Col)>(n + m);

        for (int row = n - 1, col = m - 1;  row > 1 || col > 1;)
        {
            warpingPath.Add((row, col));

            if (row == 1)
            {
                col--;
            }
            else if (col == 1)
            {
                row--;
            }
            else
            {
                // Find the minimum value of the previous costs
                var minPrev = accumulatedCostMatrix[row - 1, col];
                if (accumulatedCostMatrix[row, col - 1] < minPrev)
                    minPrev = accumulatedCostMatrix[row, col - 1];
                if (accumulatedCostMatrix[row - 1, col - 1] < minPrev)
                    minPrev = accumulatedCostMatrix[row - 1, col - 1];

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

    public TDistance Distance => CostMatrix[CostMatrix.GetUpperBound(0), CostMatrix.GetUpperBound(1)];
}
