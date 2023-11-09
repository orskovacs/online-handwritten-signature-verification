using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaAndLsDtw;

class EbDbaAndLsDtwClassifier : IClassifier
{
    private const int EB_DBA_ITERATION_COUNT = 1;

    private static IEnumerable<double> EbDba(IEnumerable<IEnumerable<double>> referenceTimeSeriesSet, int iterationCount)
    {
        var timeSeriesCount = referenceTimeSeriesSet.Count();

        /* [Step 1] Calculate the average length of the time series. */
        var timeSeriesAverageLength = referenceTimeSeriesSet.Sum(ts => ts.Count()) / timeSeriesCount;

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
            averageEbSequence.Add(resampledTimeSeriesSet.Sum(ts => ts.ElementAt(i)) / timeSeriesCount);
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
                    assoc[row - 1].Add(ts.ElementAt(col - 1));
                }
            }

            for (int i = 1; i < timeSeriesAverageLength; i++)
            {
                averageEbDbaSequence[i] = assoc[i].Average();
            }
        }

        return averageEbDbaSequence;
    }

    private static IEnumerable<double> ResampleLinerInterplotaion(IEnumerable<double> ts, int length)
    {
        var resampledTs = new List<double>(length);
        var factor = (double)ts.Count() / length;

        for (int i = 0; i < length; i++)
        {
            var index = i * factor;
            var indexFloor = (int)Math.Floor(index);
            var indexCeil = (int)Math.Ceiling(index);

            if (indexCeil >= ts.Count())
                indexCeil = ts.Count() - 1;

            resampledTs.Insert(i, ts.ElementAt(indexFloor) + (ts.ElementAt(indexCeil) - ts.ElementAt(indexFloor)) * (index - indexFloor));
        }

        return resampledTs;
    }

    private static IEnumerable<double> EstimateLocalStatibilty(IEnumerable<IEnumerable<double>> references, IEnumerable<double> template)
    {
        var directMatchingPoints = new List<List<bool>>();
        for (int i = 0; i < references.Count(); i++)
        {
            directMatchingPoints.Add(new List<bool>());
        }

        // Find the direct matching points (DMPs)
        for (int i = 0; i < references.Count(); i++)
        {
            var dtwResult = DtwResult<double, double>.Dtw(template, references.ElementAt(i), (a, b) => (a - b) * (a - b));

            for (int j = 0; j < dtwResult.WarpingPath.Count(); j++)
            {
                var matchingPointsRow = dtwResult.WarpingPath.Where(w => w.Row == j);
                var matchingPointsCol = dtwResult.WarpingPath.Where(w => w.Col == j);

                var isDmp = matchingPointsCol.Count() == 1 && matchingPointsRow.Count() == 1;
                directMatchingPoints[i].Add(isDmp);
            }
        }

        var localStability = new List<double>();
        for (int i = 0; i < template.Count(); i++)
        {
            localStability.Add(directMatchingPoints.Select(x => x[i]).Where(x => x).Count() / (double) references.Count());
        }

        return localStability;
    }

    public ISignerModel Train(List<Signature> genuineSignatures)
    {
        var xCoordsReferences =
            genuineSignatures.Select(s => s.GetFeature(AdditionalFeatures.NormalizedX));
        var yCoordsReferences =
            genuineSignatures.Select(s => s.GetFeature(AdditionalFeatures.NormalizedY));
        var pathTangentAngleReferences =
            genuineSignatures.Select(s => s.GetFeature(AdditionalFeatures.PathTangentAngle));
        var pathVelocityMagnitudeReferences =
            genuineSignatures.Select(s => s.GetFeature(AdditionalFeatures.PathVelocityMagnitude));
        var logCurvatureRadiusReferences =
            genuineSignatures.Select(s => s.GetFeature(AdditionalFeatures.LogCurvatureRadius));
        var totalAccelerationMagnitudeReferences =
            genuineSignatures.Select(s => s.GetFeature(AdditionalFeatures.TotalAccelerationMagnitude));

        var xCoordsTemplate = EbDba(xCoordsReferences, EB_DBA_ITERATION_COUNT);
        var yCoordsTemplate = EbDba(yCoordsReferences, EB_DBA_ITERATION_COUNT);
        var pathTangentAngleTemplate = EbDba(pathTangentAngleReferences, EB_DBA_ITERATION_COUNT);
        var pathVelocityMagnitudeTemplate = EbDba(pathVelocityMagnitudeReferences, EB_DBA_ITERATION_COUNT);
        var logCurvatureRadiusTemplate = EbDba(logCurvatureRadiusReferences, EB_DBA_ITERATION_COUNT);
        var totalAccelerationMagnitudeTemplate = EbDba(totalAccelerationMagnitudeReferences, EB_DBA_ITERATION_COUNT);

        var xCoordsStability = EstimateLocalStatibilty(xCoordsReferences, xCoordsTemplate);
        var yCoordsStability = EstimateLocalStatibilty(yCoordsReferences, yCoordsTemplate);
        var pathTangentAngleStability = EstimateLocalStatibilty(pathTangentAngleReferences, pathTangentAngleTemplate);
        var pathVelocityMagnitudeStability = EstimateLocalStatibilty(pathVelocityMagnitudeReferences, pathVelocityMagnitudeTemplate);
        var logCurvatureRadiusStability = EstimateLocalStatibilty(logCurvatureRadiusReferences, logCurvatureRadiusTemplate);
        var totalAccelerationMagnitudeStability = EstimateLocalStatibilty(totalAccelerationMagnitudeReferences, totalAccelerationMagnitudeTemplate);

        return new MeanTemplateSignerModel
        { 
            SignerID = genuineSignatures[0].Signer.ID,
            XCoordsTemplate = xCoordsTemplate,
            YCoordsTemplate = yCoordsTemplate,
            PathTangentAngleTemplate = pathTangentAngleTemplate,
            PathVelocityMagnitudeTemplate = pathVelocityMagnitudeTemplate,
            LogCurvatureRadiusTemplate = logCurvatureRadiusTemplate,
            TotalAccelerationMagnitudeTemplate = totalAccelerationMagnitudeTemplate,
            XCoordsLocalStability = xCoordsStability,
            YCoordsLocalStability= yCoordsStability,
            PathTangentAngleLocalStability = pathTangentAngleStability,
            PathVelocityMagnitudeLocalStability = pathVelocityMagnitudeStability,
            LogCurvatureRadiusLocalStability = logCurvatureRadiusStability,
            TotalAccelerationMagnitudeLocalStability = totalAccelerationMagnitudeStability,
        };
    }

    public double Test(ISignerModel model, Signature signature)
    {
        throw new NotImplementedException();
    }
}
