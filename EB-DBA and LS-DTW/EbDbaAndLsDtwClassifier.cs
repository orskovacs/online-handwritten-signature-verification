using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaAndLsDtw;

class EbDbaAndLsDtwClassifier : IClassifier
{
    private const int ITERATION_COUNT = 1;

    private static List<double> EbDba(IEnumerable<List<double>> referenceTimeSeriesSet, int iterationCount)
    {
        var timeSeriesCount = referenceTimeSeriesSet.Count();

        /* [Step 1] Calculate the average length of the time series. */
        var timeSeriesAverageLength = referenceTimeSeriesSet.Sum(ts => ts.Count) / timeSeriesCount;

        /* [Step 2] Resample each time series to the above calculated average
        length using linear interpolation. */
        var resampledTimeSeriesSet = referenceTimeSeriesSet
            .Select(ts => ResampleLinerInterplotaion(ts, timeSeriesAverageLength))
            .ToList();

        /* [Step 3] Create the average Euclidean barycentre sequence from the
        resampled times series. It creates a series that has the per-point averages
        from all of the resampled sequences. */
        var averageEbSequence = new List<double>(timeSeriesAverageLength);
        for (int i = 0; i < timeSeriesAverageLength; i++)
        {
            averageEbSequence.Add(resampledTimeSeriesSet.Sum(ts => ts[i]) / timeSeriesCount);
        }

        /* [Step 4] Compute the Euclidian barycentre-based DTW barycentre average series
        from the original reference time series set, using the above calculated
        Euclidean barycentre sequence as the initial sequence. */
        var averageEbDbaSequence = new List<double>(averageEbSequence);
        for (int t = 0; t < iterationCount; t++)
        {
            var assoc = new List<List<double>>(timeSeriesAverageLength);
            for (int i = 0; i < timeSeriesAverageLength; i++)
            {
                assoc.Add(new List<double>());
            }

            foreach (var ts in referenceTimeSeriesSet)
            {
                var dtwResult = DtwResult<double, double>.Dtw(
                    averageEbDbaSequence,
                    ts,
                    (a, b) => (a - b) * (a - b));

                foreach (var (row, col) in dtwResult.WarpingPath)
                {
                    assoc[row - 1].Add(ts[col - 1]);
                }
            }

            for (int i = 0; i < timeSeriesAverageLength; i++)
            {
                averageEbDbaSequence[i] = assoc[i].Average();
            }
        }

        return averageEbDbaSequence;
    }

    private static List<double> ResampleLinerInterplotaion(List<double> timeSeries, int length)
    {
        // TODO: Implement resampling
        var resampledTimeSeries = new List<double>(length);

        for (int i = 0; i < length; i++)
        {
            if (i < timeSeries.Count)
            {
                resampledTimeSeries.Add(timeSeries[i]);
            }
            else
            {
                resampledTimeSeries.Add(timeSeries[^1]);
            }
        }

        return resampledTimeSeries;
    }

    public ISignerModel Train(List<Signature> genuineSignatures)
    {
        var xCoordsTemplate = EbDba(genuineSignatures.Select(
                s => s.GetFeature(AdditionalFeatures.NormalizedX)), ITERATION_COUNT);

        var yCoordsTemplate = EbDba(genuineSignatures.Select(
                s => s.GetFeature(AdditionalFeatures.NormalizedY)), ITERATION_COUNT);

        var pathTangentAngleTemplate = EbDba(genuineSignatures.Select(
                s => s.GetFeature(AdditionalFeatures.PathTangentAngle)), ITERATION_COUNT);

        var pathVelocityMagnitudeTemplate = EbDba(genuineSignatures.Select(
                s => s.GetFeature(AdditionalFeatures.PathVelocityMagnitude)), ITERATION_COUNT);

        var logCurvatureRadiusTemnplate = EbDba(genuineSignatures.Select(
                s => s.GetFeature(AdditionalFeatures.LogCurvatureRadius)), ITERATION_COUNT);

        var totalAccelerationMagnitudeTemplate = EbDba(genuineSignatures.Select(
                s => s.GetFeature(AdditionalFeatures.TotalAccelerationMagnitude)), ITERATION_COUNT);

        var signerId = genuineSignatures[0].Signer.ID;

        var signerModel = new MeanTemplateSignerModel
        { 
            SignerID = signerId,
            XCoordsTemplate = xCoordsTemplate,
            YCoordsTemplate = yCoordsTemplate,
            PathTangentAngleTemplate = pathTangentAngleTemplate,
            PathVelocityMagnitudeTemplate = pathVelocityMagnitudeTemplate,
            LogCurvatureRadiusTemnplate = logCurvatureRadiusTemnplate,
            TotalAccelerationMagnitudeTemplate = totalAccelerationMagnitudeTemplate,
        };

        return signerModel;
    }

    public double Test(ISignerModel model, Signature signature)
    {
        throw new NotImplementedException();
    }
}
