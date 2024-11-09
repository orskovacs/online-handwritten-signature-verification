using SigStat.Common;

namespace EbDbaLsDtw;

class MultivariateTimeSeries
{
    private readonly List<FeatureDescriptor> features;

    private readonly double[][] data;

    public MultivariateTimeSeries(Dictionary<FeatureDescriptor, List<double>> univariateTimeSeriesByFeatures)
    {
        features = [.. univariateTimeSeriesByFeatures.Keys];
        data = new double[features.Count][];

        var dataCount = univariateTimeSeriesByFeatures.Values.Select(x => x.Count).Max();
        for (int i = 0; i < features.Count; i++)
        {
            data[i] = new double[dataCount];
            for (int j = 0; j < dataCount; j++)
            {
                // TODO: Identify why one of the features has fewer datapoints.
                try
                {
                    data[i][j] = univariateTimeSeriesByFeatures[features[i]][j];
                }
                catch (Exception)
                {
                    data[i][j] = univariateTimeSeriesByFeatures[features[i]][^1];
                }
            }
        }
    }

    public int Dimension { get => features.Count; }

    public int Count { get => data[0].Length; }

    public List<double> this[FeatureDescriptor feature]
    {
        get => GetTimeSeriesByFeature(feature);
    }

    public Dictionary<FeatureDescriptor, double> this[int index]
    {
        get => GetValueAtIndex(index);
    }

    public List<double> GetTimeSeriesByFeature(FeatureDescriptor feature)
    {
        return new List<double>(data[GetFeatureIndex(feature)]);
    }

    public Dictionary<FeatureDescriptor, double> GetValueAtIndex(int index)
    {
        var columnValue = new Dictionary<FeatureDescriptor, double>();

        for (int i = 0; i < Dimension; i++)
            columnValue.Add(features[i], data[i][index]);

        return columnValue;
    }

    private int GetFeatureIndex(FeatureDescriptor feature)
    {
        if (!features.Contains(feature)) {
            throw new ArgumentException($"{feature.Name} is not supported");
        }

        return features.IndexOf(feature);
    }

    public List<double[]> ToColumnList()
    {
        var colCount = data[0].Length;
        var columns = new List<double[]>(colCount);
        for (int colIndex = 0; colIndex < colCount; colIndex++)
        {
            var column = new double[Dimension];
            for (int rowIndex = 0; rowIndex < Dimension; rowIndex++)
            {
                column[rowIndex] = data[rowIndex][colIndex];
            }

            columns[colIndex] = column;
        }

        return columns;
    }

    public static double EuclideanDistanceBetweenPoints(Dictionary<FeatureDescriptor, double> a, Dictionary<FeatureDescriptor, double> b)
    {
        return EuclideanDistanceBetweenMultivariatePoints([..a.Values], [..b.Values]);
    }

    public static double EuclideanDistanceBetweenMultivariatePoints(double[] a, double[] b)
    {
        if (a.Length != b.Length) {
            throw new ArgumentException($"The dimension of the two points differs: {a.Length} != {b.Length}");
        }

        var sum = 0.0;
        for (var i = 0; i < a.Length; i++)
        {
            sum += (a[i] - b[i]) * (a[i] - b[i]);
        }

        return sum;
    }
}
