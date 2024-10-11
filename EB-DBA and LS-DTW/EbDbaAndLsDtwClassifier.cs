using SigStat.Common;
using SigStat.Common.Framework.Samplers;
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
                    (a, b, _) => Math.Abs(a - b));

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
            var dtwResult = DtwResult<double, double>.Dtw(
                template,
                references[i],
                (a, b, _) => Math.Abs(a - b)
            );

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
        return (double a, double b, int i) => stability[i] * Math.Abs(a - b);
    }

    private static double Distance(List<double> template, List<double> test, List<double> stability)
    {
        return DtwResult<double, double>.Dtw(template, test, LsWeightedEuclideanDistance(stability)).Distance;
    }

    public ISignerModel Train(List<Signature> signatures)
    {
        List<FeatureDescriptor> examinedFeatures = [
            OriginalFeatures.NormalizedX,
            OriginalFeatures.NormalizedY,
            OriginalFeatures.PenPressure,
            DerivedFeatures.PathTangentAngle,
            DerivedFeatures.PathVelocityMagnitude,
            DerivedFeatures.LogCurvatureRadius,
            DerivedFeatures.TotalAccelerationMagnitude,
        ];

        var realSampler = new FirstNSampler();

        List<Signature> trainSignatures = realSampler.SampleReferences(signatures);
        List<Signature> testGenuine = realSampler.SampleGenuineTests(signatures);
        List<Signature> testForged = realSampler.SampleForgeryTests(signatures);

        List<Signature> testSignatures = [..testGenuine, ..testForged];

        var references = examinedFeatures.ToDictionary(
            keySelector: f => f,
            elementSelector: f =>
                trainSignatures.Select(s => s.GetFeature<List<double>>(f)).ToList()
        );

        var templates = examinedFeatures.ToDictionary(
            keySelector: f => f,
            elementSelector: f => EbDba(references[f], EB_DBA_ITERATION_COUNT)
        );

        var localStabilityValues = examinedFeatures.ToDictionary(
            keySelector: f => f,
            elementSelector: f => EstimateLocalStatibilty(references[f], templates[f])
        );

        var combinedLsDtwDistances = new DistanceMatrix<string, string, double>();

        foreach (var trainSignature in trainSignatures)
        {
            foreach (var testSignature in new List<Signature>([..trainSignatures, ..testSignatures]))
            {
                var featuresFromTestSignature = examinedFeatures.ToDictionary(
                    keySelector: f => f,
                    elementSelector: testSignature.GetFeature<List<double>>
                );

                var lsDtwDistances = examinedFeatures.Select(f =>
                {
                    var lsDtwDistance = Distance(
                        templates[f],
                        featuresFromTestSignature[f],
                        localStabilityValues[f]
                    );

                    return lsDtwDistance; // / signerModel.Thresholds[f];
                }).ToList();

                // TODO: How to combine the per-feature distances?
                static double CombinePerFeatureDistances(List<double> distances)
                {
                    throw new NotImplementedException();
                }

                var combinedLsDtwDistance = CombinePerFeatureDistances(lsDtwDistances);
                combinedLsDtwDistances[testSignature.ID, trainSignature.ID] = combinedLsDtwDistance;
            }
        }

        var averageDistances = testSignatures
            .Select(test => new SignatureDistance
                {
                    ID = test.ID,
                    Origin = test.Origin,
                    Distance = trainSignatures
                        .Where(train => train.ID != test.ID)
                        .Select(train => combinedLsDtwDistances[test.ID, train.ID])
                        .Average()
                })
            .OrderBy(d => d.Distance)
            .ToList();

            List<double> thresholds = [0.0];
            for (int i = 0; i < averageDistances.Count - 1; i++)
            {
                thresholds.Add((averageDistances[i].Distance + averageDistances[i + 1].Distance) / 2);
            }

            thresholds.Add(averageDistances[averageDistances.Count - 1].Distance + 1);

            var errorRates = thresholds
                .Select(th => new KeyValuePair<double, ErrorRate>(
                    th,
                    CalculateErrorRate(th, averageDistances)
                )).ToList();

        return new OptimalMeanTemplateSignerModel
        {
            SignerID = signatures[0].Signer!.ID,
            DistanceMatrix = combinedLsDtwDistances,
            SignatureDistanceFromTraining = averageDistances.ToDictionary(sig => sig.ID, sig => sig.Distance),
            ErrorRates = errorRates,
            Threshold = errorRates.First(e => e.Value.Far >= e.Value.Frr).Key
        };
    }

    public double Test(ISignerModel model, Signature testSignature)
    {
        if (model is not OptimalMeanTemplateSignerModel)
            throw new ApplicationException("Cannot test using the provided model. Please provide an OptimalMeanTemplateSignerModel type model.");
        var signerModel = (OptimalMeanTemplateSignerModel)model;

        var distance = signerModel.SignatureDistanceFromTraining[testSignature.ID];

        if (distance <= signerModel.Threshold)
            return 1;
        
        return 0;
    }

    private static ErrorRate CalculateErrorRate(double threshold, List<SignatureDistance> distances)
    {
        int genuineCount = 0, genuineError = 0;
        int forgedCount = 0, forgedError = 0;
        foreach (var d in distances)
        {
            switch (d.Origin)
            {
                case Origin.Genuine:
                    genuineCount++;
                    if (d.Distance > threshold)
                        genuineError++;
                    break;
                case Origin.Forged:
                    forgedCount++;
                    if (d.Distance <= threshold)
                        forgedError++;
                    break;
                case Origin.Unknown:
                default:
                    throw new NotSupportedException();
            }
        }

        return new ErrorRate {
            Far = (double)forgedError / forgedCount,
            Frr = (double)genuineError / genuineCount
        };
    }
}

class OptimalMeanTemplateSignerModel : ISignerModel
{
    public required string SignerID { get; set; }

    public required object DistanceMatrix { get; set; }

    public required Dictionary<string, double> SignatureDistanceFromTraining { get; set; }

    public required List<KeyValuePair<double, ErrorRate>> ErrorRates { get; set; }

    public required double Threshold { get; set; }
}

struct SignatureDistance : IEquatable<SignatureDistance>
{
    public string ID;

    public Origin Origin;

    public double Distance;

    public bool Equals(SignatureDistance other)
    {
        return
            ID == other.ID
            && Origin.Equals(other.Origin)
            && (Distance - other.Distance).EqualsZero();
    }
}

static class DoubleExtension {
    public static bool EqualsZero(this double d)
    {
        return double.Epsilon >= d && d >= -double.Epsilon;
    }
}
