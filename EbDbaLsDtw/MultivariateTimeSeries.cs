using SigStat.Common;

namespace EbDbaLsDtw;

internal class MultivariateTimeSeries
{
    private readonly List<FeatureDescriptor> _features;

    private readonly double[][] _data;

    public MultivariateTimeSeries(Dictionary<FeatureDescriptor, List<double>> univariateTimeSeriesByFeatures)
    {
        _features = [.. univariateTimeSeriesByFeatures.Keys];
        _data = new double[_features.Count][];

        var dataCount = univariateTimeSeriesByFeatures.Values.Select(x => x.Count).Max();
        for (var i = 0; i < _features.Count; i++)
        {
            _data[i] = new double[dataCount];
            for (var j = 0; j < dataCount; j++)
            {
                // TODO: Identify why one of the features has fewer data points.
                try
                {
                    _data[i][j] = univariateTimeSeriesByFeatures[_features[i]][j];
                }
                catch (Exception)
                {
                    _data[i][j] = univariateTimeSeriesByFeatures[_features[i]][^1];
                }
            }
        }
    }

    private int Dimension => _features.Count;

    public int Count => _data[0].Length;

    public List<double> this[FeatureDescriptor feature] => GetTimeSeriesByFeature(feature);

    public Dictionary<FeatureDescriptor, double> this[int index] => GetValueAtIndex(index);

    private List<double> GetTimeSeriesByFeature(FeatureDescriptor feature)
    {
        return [.._data[GetFeatureIndex(feature)]];
    }

    private Dictionary<FeatureDescriptor, double> GetValueAtIndex(int index)
    {
        var columnValue = new Dictionary<FeatureDescriptor, double>();

        for (var i = 0; i < Dimension; i++)
            columnValue.Add(_features[i], _data[i][index]);

        return columnValue;
    }

    private int GetFeatureIndex(FeatureDescriptor feature)
    {
        if (!_features.Contains(feature)) {
            throw new ArgumentException($"{feature.Name} is not supported");
        }

        return _features.IndexOf(feature);
    }

    public List<double[]> ToColumnList()
    {
        var colCount = _data[0].Length;
        var columns = new List<double[]>(colCount);
        for (var colIndex = 0; colIndex < colCount; colIndex++)
        {
            var column = new double[Dimension];
            for (var rowIndex = 0; rowIndex < Dimension; rowIndex++)
            {
                column[rowIndex] = _data[rowIndex][colIndex];
            }

            columns[colIndex] = column;
        }

        return columns;
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
