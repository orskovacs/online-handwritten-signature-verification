using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaAndLsDtw;

class EbDbaAndLsDtwClassifier : IClassifier
{
    private const int EB_DBA_ITERATION_COUNT = 10;

    private static List<double> EbDba(List<List<double>> referenceTimeSeriesSet, int iterationCount)
    {
        var timeSeriesCount = referenceTimeSeriesSet.Count;

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
                assoc.Add([]);
            }

            foreach (var ts in referenceTimeSeriesSet)
            {
                var dtwResult = DtwResult<double, double>.Dtw(
                    averageEbDbaSequence,
                    ts,
                    (a, b, _) => (a - b) * (a - b));

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

    private static List<double> ResampleLinerInterplotaion(List<double> ts, int length)
    {
        var resampledTs = new List<double>(length);
        var factor = (double)ts.Count / length;

        for (int i = 0; i < length; i++)
        {
            var index = i * factor;
            var indexFloor = (int)Math.Floor(index);
            var indexCeil = (int)Math.Ceiling(index);

            if (indexCeil >= ts.Count)
                indexCeil = ts.Count - 1;

            resampledTs.Insert(i, ts.ElementAt(indexFloor) + (ts.ElementAt(indexCeil) - ts.ElementAt(indexFloor)) * (index - indexFloor));
        }

        return resampledTs;
    }

    private static List<double> EstimateLocalStatibilty(List<List<double>> references, List<double> template)
    {
        var directMatchingPoints = new List<List<bool>>();
        for (int i = 0; i < references.Count; i++)
        {
            directMatchingPoints.Add([]);
        }

        // Find the direct matching points (DMPs)
        for (int i = 0; i < references.Count; i++)
        {
            var dtwResult = DtwResult<double, double>.Dtw(template, references.ElementAt(i), (a, b, _) => (a - b) * (a - b));

            for (int j = 0; j < template.Count; j++)
            {
                var matchingPointsRow = dtwResult.WarpingPath.Where(w => w.Row == j);
                var matchingPointsCol = dtwResult.WarpingPath.Where(w => w.Col == j);

                var isDmp = matchingPointsCol.Count() == 1 && matchingPointsRow.Count() == 1;
                directMatchingPoints[i].Add(isDmp);
            }
        }

        var localStability = new List<double>();
        for (int i = 0; i < template.Count; i++)
        {
            localStability.Add(directMatchingPoints.Select(x => x[i]).Where(x => x).Count() / (double) references.Count);
        }

        return localStability;
    }

    private static Func<double, double, int, double> LsWeightedEuclideanDistance(List<double> stability)
    {
        return (double a, double b, int i) => stability.ElementAt(i) * (a - b) * (a - b);
    }

    private static double Distance(List<double> template, List<double> test, List<double> stability)
    {
        return DtwResult<double, double>.Dtw(template, test, LsWeightedEuclideanDistance(stability)).Distance;
    }

    public ISignerModel Train(List<Signature> genuineSignatures)
    {
        var xCoordsReferences =
            genuineSignatures.Select(s => s.GetFeature(OriginalFeatures.NormalizedX)).ToList();
        var yCoordsReferences =
            genuineSignatures.Select(s => s.GetFeature(OriginalFeatures.NormalizedY)).ToList();
        var penPressureReferences =
            genuineSignatures.Select(s => s.GetFeature(OriginalFeatures.PenPressure)).ToList();
        var pathTangentAngleReferences =
            genuineSignatures.Select(s => s.GetFeature(DerivedFeatures.PathTangentAngle)).ToList();
        var pathVelocityMagnitudeReferences =
            genuineSignatures.Select(s => s.GetFeature(DerivedFeatures.PathVelocityMagnitude)).ToList();
        var logCurvatureRadiusReferences =
            genuineSignatures.Select(s => s.GetFeature(DerivedFeatures.LogCurvatureRadius)).ToList();
        var totalAccelerationMagnitudeReferences =
            genuineSignatures.Select(s => s.GetFeature(DerivedFeatures.TotalAccelerationMagnitude)).ToList();

        var xCoordsTemplate = EbDba(xCoordsReferences, EB_DBA_ITERATION_COUNT);
        var yCoordsTemplate = EbDba(yCoordsReferences, EB_DBA_ITERATION_COUNT);
        var penPressureTemplate = EbDba(penPressureReferences, EB_DBA_ITERATION_COUNT);
        var pathTangentAngleTemplate = EbDba(pathTangentAngleReferences, EB_DBA_ITERATION_COUNT);
        var pathVelocityMagnitudeTemplate = EbDba(pathVelocityMagnitudeReferences, EB_DBA_ITERATION_COUNT);
        var logCurvatureRadiusTemplate = EbDba(logCurvatureRadiusReferences, EB_DBA_ITERATION_COUNT);
        var totalAccelerationMagnitudeTemplate = EbDba(totalAccelerationMagnitudeReferences, EB_DBA_ITERATION_COUNT);

        var xCoordsStability = EstimateLocalStatibilty(xCoordsReferences, xCoordsTemplate);
        var yCoordsStability = EstimateLocalStatibilty(yCoordsReferences, yCoordsTemplate);
        var penPressureStability = EstimateLocalStatibilty(penPressureReferences, penPressureTemplate);
        var pathTangentAngleStability = EstimateLocalStatibilty(pathTangentAngleReferences, pathTangentAngleTemplate);
        var pathVelocityMagnitudeStability = EstimateLocalStatibilty(pathVelocityMagnitudeReferences, pathVelocityMagnitudeTemplate);
        var logCurvatureRadiusStability = EstimateLocalStatibilty(logCurvatureRadiusReferences, logCurvatureRadiusTemplate);
        var totalAccelerationMagnitudeStability = EstimateLocalStatibilty(totalAccelerationMagnitudeReferences, totalAccelerationMagnitudeTemplate);

        var xCoordsDistance = xCoordsReferences
            .Select(r => Distance(xCoordsTemplate, r, xCoordsStability))
            .Max();
        var yCoordsDistance = yCoordsReferences
            .Select(r => Distance(yCoordsTemplate, r, yCoordsStability))
            .Max();
        var penPressureDistance = penPressureReferences
            .Select(r => Distance(penPressureTemplate, r, penPressureStability))
            .Max();
        var pathTangentAngleDistance = pathTangentAngleReferences
            .Select(r => Distance(pathTangentAngleTemplate, r, pathTangentAngleStability))
            .Max();
        var pathVelocityMagnitudeDistance = pathVelocityMagnitudeReferences
            .Select(r => Distance(pathVelocityMagnitudeTemplate, r, pathVelocityMagnitudeStability))
            .Max();
        var logCurvatureRadiusDistance = logCurvatureRadiusReferences
            .Select(r => Distance(logCurvatureRadiusTemplate, r, logCurvatureRadiusStability))
            .Max();
        var totalAccelerationMagnitudeDistance = totalAccelerationMagnitudeReferences
            .Select(r => Distance(totalAccelerationMagnitudeTemplate, r, totalAccelerationMagnitudeStability))
            .Max();

        return new MeanTemplateSignerModel
        {
            SignerID = genuineSignatures[0].Signer.ID,
            Threshold = 
                xCoordsDistance * xCoordsStability.Median() +
                yCoordsDistance * yCoordsStability.Median() +
                penPressureDistance * penPressureStability.Median() +
                pathTangentAngleDistance * pathTangentAngleStability.Median() +
                pathVelocityMagnitudeDistance * pathVelocityMagnitudeStability.Median() +
                logCurvatureRadiusDistance * logCurvatureRadiusStability.Median() +
                totalAccelerationMagnitudeDistance * totalAccelerationMagnitudeStability.Median(),
            XCoordsTemplate = xCoordsTemplate,
            YCoordsTemplate = yCoordsTemplate,
            PenPressureTemplate = penPressureTemplate,
            PathTangentAngleTemplate = pathTangentAngleTemplate,
            PathVelocityMagnitudeTemplate = pathVelocityMagnitudeTemplate,
            LogCurvatureRadiusTemplate = logCurvatureRadiusTemplate,
            TotalAccelerationMagnitudeTemplate = totalAccelerationMagnitudeTemplate,
            XCoordsLocalStability = xCoordsStability,
            YCoordsLocalStability = yCoordsStability,
            PenPressureStability = penPressureStability,
            PathTangentAngleLocalStability = pathTangentAngleStability,
            PathVelocityMagnitudeLocalStability = pathVelocityMagnitudeStability,
            LogCurvatureRadiusLocalStability = logCurvatureRadiusStability,
            TotalAccelerationMagnitudeLocalStability = totalAccelerationMagnitudeStability,
        };
    }

    public double Test(ISignerModel model, Signature testSignature)
    {
        if (model is not MeanTemplateSignerModel)
            throw new ApplicationException("Cannot test using the provided model. Please provide a MeanTemplateSignerModel type model.");
        var signerModel = (MeanTemplateSignerModel)model;

        var xCoordsTest =
            testSignature.GetFeature(OriginalFeatures.NormalizedX);
        var yCoordsTest =
            testSignature.GetFeature(OriginalFeatures.NormalizedY);
        var penPressureTest =
            testSignature.GetFeature(OriginalFeatures.PenPressure);
        var pathTangentAngleTest =
            testSignature.GetFeature(DerivedFeatures.PathTangentAngle);
        var pathVelocityMagnitudeTest =
            testSignature.GetFeature(DerivedFeatures.PathVelocityMagnitude);
        var logCurvatureRadiusTest =
            testSignature.GetFeature(DerivedFeatures.LogCurvatureRadius);
        var totalAccelerationMagnitudeTest =
            testSignature.GetFeature(DerivedFeatures.TotalAccelerationMagnitude);

        var distance =
            Distance(signerModel.XCoordsTemplate, xCoordsTest, signerModel.XCoordsLocalStability) *
                signerModel.XCoordsLocalStability.Median() +
            Distance(signerModel.YCoordsTemplate, yCoordsTest, signerModel.YCoordsLocalStability) *
                signerModel.YCoordsLocalStability.Median() +
            Distance(signerModel.PenPressureTemplate, penPressureTest, signerModel.PenPressureStability) *
                signerModel.PenPressureStability.Median() +
            Distance(signerModel.PathTangentAngleTemplate, pathTangentAngleTest, signerModel.PathTangentAngleLocalStability) *
                signerModel.PathTangentAngleLocalStability.Median() +
            Distance(signerModel.PathVelocityMagnitudeTemplate, pathVelocityMagnitudeTest, signerModel.PathVelocityMagnitudeLocalStability) *
                signerModel.PathVelocityMagnitudeLocalStability.Median() +
            Distance(signerModel.LogCurvatureRadiusTemplate, logCurvatureRadiusTest, signerModel.LogCurvatureRadiusLocalStability) *
                signerModel.LogCurvatureRadiusLocalStability.Median() +
            Distance(signerModel.TotalAccelerationMagnitudeTemplate, totalAccelerationMagnitudeTest, signerModel.TotalAccelerationMagnitudeLocalStability) *
                signerModel.TotalAccelerationMagnitudeLocalStability.Median();

        return distance < signerModel.Threshold - 2100 ? 1 : 0;
    }
}
