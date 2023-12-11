using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaAndLsDtw;

class EbDbaAndLsDtwClassifier : IClassifier
{
    private const int EB_DBA_ITERATION_COUNT = 10;

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
            var dtwResult = DtwResult<double, double>.Dtw(template, references.ElementAt(i), (a, b, _) => (a - b) * (a - b));

            for (int j = 0; j < template.Count(); j++)
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
            genuineSignatures.Select(s => s.GetFeature(OriginalFeatures.NormalizedX));
        var yCoordsReferences =
            genuineSignatures.Select(s => s.GetFeature(OriginalFeatures.NormalizedY));
        var penPressureReferences =
            genuineSignatures.Select(s => s.GetFeature(OriginalFeatures.PenPressure));
        var pathTangentAngleReferences =
            genuineSignatures.Select(s => s.GetFeature(DerivedFeatures.PathTangentAngle));
        var pathVelocityMagnitudeReferences =
            genuineSignatures.Select(s => s.GetFeature(DerivedFeatures.PathVelocityMagnitude));
        var logCurvatureRadiusReferences =
            genuineSignatures.Select(s => s.GetFeature(DerivedFeatures.LogCurvatureRadius));
        var totalAccelerationMagnitudeReferences =
            genuineSignatures.Select(s => s.GetFeature(DerivedFeatures.TotalAccelerationMagnitude));

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

        return new MeanTemplateSignerModel
        { 
            SignerID = genuineSignatures[0].Signer.ID,
            Threshold = 460,
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

        static Func<double, double, int, double> lsWeightedEuclideanDistance(IEnumerable<double> stability) =>
            (double a, double b, int i) => stability.ElementAt(i) * (a - b) * (a - b);

        var xCoordsDistance = DtwResult<double, double>.Dtw(
            signerModel.XCoordsTemplate,
            xCoordsTest,
            lsWeightedEuclideanDistance(signerModel.XCoordsLocalStability)
        ).Distance;

        var yCoordsDistance = DtwResult<double, double>.Dtw(
            signerModel.YCoordsTemplate,
            yCoordsTest,
            lsWeightedEuclideanDistance(signerModel.YCoordsLocalStability)
        ).Distance;

        var penPressureDistance = DtwResult<double, double>.Dtw(
            signerModel.PenPressureTemplate,
            penPressureTest,
            lsWeightedEuclideanDistance(signerModel.PenPressureStability)
        ).Distance;

        var pathTangentAngleDistance = DtwResult<double, double>.Dtw(
            signerModel.PathTangentAngleTemplate,
            pathTangentAngleTest,
            lsWeightedEuclideanDistance(signerModel.PathTangentAngleLocalStability)
        ).Distance;

        var pathVelocityMagnitudeDistance = DtwResult<double, double>.Dtw(
            signerModel.PathVelocityMagnitudeTemplate,
            pathVelocityMagnitudeTest,
            lsWeightedEuclideanDistance(signerModel.PathVelocityMagnitudeLocalStability)
        ).Distance;

        var logCurvatureRadiusDistance = DtwResult<double, double>.Dtw(
            signerModel.LogCurvatureRadiusTemplate,
            logCurvatureRadiusTest,
            lsWeightedEuclideanDistance(signerModel.LogCurvatureRadiusLocalStability)
        ).Distance;

        var totalAccelerationMagnitudeDistance = DtwResult<double, double>.Dtw(
            signerModel.TotalAccelerationMagnitudeTemplate,
            totalAccelerationMagnitudeTest,
            lsWeightedEuclideanDistance(signerModel.TotalAccelerationMagnitudeLocalStability)
        ).Distance;

        var distance = xCoordsDistance +
            yCoordsDistance +
            penPressureDistance +
            pathTangentAngleDistance +
            pathVelocityMagnitudeDistance +
            logCurvatureRadiusDistance +
            totalAccelerationMagnitudeDistance;

        return distance < signerModel.Threshold ? 1 : 0;
    }
}
