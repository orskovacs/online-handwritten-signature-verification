﻿using System.Collections.ObjectModel;
using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaLsDtw;

public class EbDbaLsDtwClassifier(Sampler realSampler, int ebDbaIterationCount = EbDbaLsDtwClassifier.DefaultEbDbaIterationCount)
    : IClassifier
{
    private const int DefaultEbDbaIterationCount = 10;

    private readonly ReadOnlyCollection<FeatureDescriptor> _examinedFeatures = new([
        OriginalFeatures.NormalizedX,
        OriginalFeatures.NormalizedY,
        OriginalFeatures.PenPressure,
        DerivedFeatures.PathTangentAngle,
        DerivedFeatures.PathVelocityMagnitude,
        DerivedFeatures.LogCurvatureRadius,
        DerivedFeatures.TotalAccelerationMagnitude,
    ]);

    private static List<double> EbDba(List<List<double>> referenceTimeSeriesSet, int iterationCount)
    {
        var timeSeriesCount = referenceTimeSeriesSet.Count;

        /* [Step 1] Calculate the average length of the time series. */
        var timeSeriesAverageLength = referenceTimeSeriesSet.Sum(ts => ts.Count) / timeSeriesCount;

        /* [Step 2] Resample each time series to the above calculated average
        length using linear interpolation. */
        var resampledTimeSeriesSet = referenceTimeSeriesSet
            .Select(ts => ResampleLinerInterpolation(ts, timeSeriesAverageLength))
            .ToList();

        /* [Step 3] Create the average Euclidean barycentre sequence from the
        resampled times series. It creates a series that has the per-point averages
        from all the resampled sequences. */
        var averageEbSequence = new List<double>(timeSeriesAverageLength);
        for (var i = 0; i < timeSeriesAverageLength; i++)
        {
            averageEbSequence.Add(resampledTimeSeriesSet.Sum(ts => ts[i]) / timeSeriesCount);
        }

        /* [Step 4] Compute the Euclidean barycentre-based DTW barycentre average series
        from the original reference time series set, using the above calculated
        Euclidean barycentre sequence as the initial sequence. */
        var averageEbDbaSequence = new List<double>(averageEbSequence);
        for (var t = 0; t < iterationCount; t++)
        {
            var assoc = new List<List<double>>(timeSeriesAverageLength);
            for (var i = 0; i < timeSeriesAverageLength; i++)
            {
                assoc.Add([]);
            }

            foreach (var ts in referenceTimeSeriesSet)
            {
                var dtwResult = DtwResult<double, double>.Dtw(
                    averageEbDbaSequence,
                    ts,
                    distance: (a, b, _) => (a - b) * (a - b)
                );

                foreach (var (row, col) in dtwResult.WarpingPath)
                {
                    assoc[row - 1].Add(ts[col - 1]);
                }
            }

            for (var i = 0; i < timeSeriesAverageLength; i++)
            {
                // Improvement is possible only if the assoc[i] list has elements.
                if (assoc[i].Count != 0)
                {
                    averageEbDbaSequence[i] = assoc[i].Average();
                }
            }
        }

        return averageEbDbaSequence;
    }

    private static List<double> ResampleLinerInterpolation(List<double> ts, int length)
    {
        var resampledTs = new List<double>(length);
        var factor = (double)ts.Count / length;

        for (var i = 0; i < length; i++)
        {
            var index = i * factor;
            var indexFloor = (int)Math.Floor(index);
            var indexCeil = (int)Math.Ceiling(index);

            if (indexCeil >= ts.Count)
                indexCeil = ts.Count - 1;

            resampledTs.Insert(i, ts[indexFloor] + (ts[indexCeil] - ts[indexFloor]) * (index - indexFloor));
        }

        return resampledTs;
    }

    private static List<double> EstimateLocalStability(MultivariateTimeSeries template, List<MultivariateTimeSeries> references)
    {
        // Step 1.
        // Compute the "standard DTW" between the template multivariate time-series and
        // the reference multivariate time-series set elements.
        // The result is a set of warping paths, with a length equal to the reference set's.
        var optimalWarpingPaths = new List<IEnumerable<(int Row, int Col)>>(references.Count);

        foreach (var reference in references)
        {
            var dtwResult = DtwResult<double[], double>.Dtw(
                template.ToColumnList(),
                reference.ToColumnList(),
                distance: (a, b, _) => MultivariateTimeSeries.EuclideanDistanceBetweenMultivariatePoints(a, b)
            );

            optimalWarpingPaths.Add(dtwResult.WarpingPath);
        }

        // Step 2.
        // Calculate the direct matching points. The DMP's set has the same length as the references set
        // and the warping paths' set. The elements from the DMP's set have the same length as the template's.
        var directMatchingPoints = new List<List<bool>>(references.Count);
        for (var i = 0; i < references.Count; i++)
        {
            directMatchingPoints.Add(new List<bool>(template.Count));
        }

        for (var i = 0; i < references.Count; i++)
        {
            for (var j = 0; j < template.Count; j++)
            {
                var matchingPointsRow = optimalWarpingPaths[i].Where(w => w.Row == j).ToList();
                var matchingPointsCol = optimalWarpingPaths[i].Where(w => w.Col == j).ToList();

                var isDmp = matchingPointsCol.Count == 1 && matchingPointsRow.Count == 1;
                directMatchingPoints[i].Add(isDmp);
            }
        }

        // Step 3.
        // Construct the local stability sequence from the DMP's set.
        var localStability = new List<double>();
        for (var i = 0; i < template.Count; i++)
        {
            localStability.Add(directMatchingPoints.Select(x => x[i]).Count(x => x) / (double) references.Count);
        }

        return localStability;
    }

    private static double LsDtwDistance(MultivariateTimeSeries template, MultivariateTimeSeries test, List<double> stability)
    {
        return DtwResult<double[], double>.Dtw(
            template.ToColumnList(),
            test.ToColumnList(),
            distance: (a, b, i) => stability[i] * MultivariateTimeSeries.EuclideanDistanceBetweenMultivariatePoints(a, b)
        ).Distance;
    }

    private Dictionary<FeatureDescriptor, T> AggregateByExaminedFeatures<T>(Func<FeatureDescriptor, T> elementSelector)
    {
        return _examinedFeatures.ToDictionary(KeySelector, elementSelector);
        
        static FeatureDescriptor KeySelector(FeatureDescriptor f) => f;
    }

    public ISignerModel Train(List<Signature> signatures)
    {
        List<Signature> trainSignatures = realSampler.SampleReferences(signatures);
        List<Signature> testGenuine = realSampler.SampleGenuineTests(signatures);
        List<Signature> testForged = realSampler.SampleForgeryTests(signatures);

        List<Signature> testSignatures = [..testGenuine, ..testForged];

        var referenceSeriesByFeatures =
            AggregateByExaminedFeatures(f => trainSignatures
                .Select(s => s.GetFeature<List<double>>(f))
                .ToList()
            );

        var templateSeriesByFeatures =
            AggregateByExaminedFeatures(f => EbDba(referenceSeriesByFeatures[f], ebDbaIterationCount));
        
        var references = trainSignatures
            .Select(s =>
            {
                var signatureUnivariateTimeSeriesByFeatures =
                    AggregateByExaminedFeatures(s.GetFeature<List<double>>);
                return new MultivariateTimeSeries(signatureUnivariateTimeSeriesByFeatures);
            })
            .ToList();
        var template = new MultivariateTimeSeries(templateSeriesByFeatures);
        var localStability = EstimateLocalStability(template, references);

        var lsDtwDistances = new DistanceMatrix<string, string, double>();
        foreach (var trainSignature in trainSignatures)
        {
            foreach (var testSignature in new List<Signature>([..trainSignatures, ..testSignatures]))
            {
                var testSignatureDataByFeature = _examinedFeatures.ToDictionary(
                    keySelector: f => f,
                    elementSelector: testSignature.GetFeature<List<double>>
                );
                var test = new MultivariateTimeSeries(testSignatureDataByFeature);

                var lsDtwDistance = LsDtwDistance(template, test, localStability);

                lsDtwDistances[testSignature.ID, trainSignature.ID] = lsDtwDistance;
            }
        }

        var averageDistances = testSignatures
            .Select(test => new SignatureDistance
                {
                    Id = test.ID,
                    Origin = test.Origin,
                    Distance = trainSignatures
                        .Where(train => train.ID != test.ID)
                        .Select(train => lsDtwDistances[test.ID, train.ID])
                        .Average()
                })
            .OrderBy(d => d.Distance)
            .ToList();

            List<double> thresholds = [0.0];
            for (int i = 0; i < averageDistances.Count - 1; i++)
            {
                thresholds.Add((averageDistances[i].Distance + averageDistances[i + 1].Distance) / 2);
            }

            thresholds.Add(averageDistances[^1].Distance + 1);

            var errorRates = thresholds
                .Select(th => new KeyValuePair<double, ErrorRate>(
                    th,
                    CalculateErrorRate(th, averageDistances)
                )).ToList();

        return new OptimalMeanTemplateSignerModel
        {
            SignerID = signatures[0].Signer!.ID,
            DistanceMatrix = lsDtwDistances,
            SignatureDistanceFromTraining = averageDistances.ToDictionary(sig => sig.Id, sig => sig.Distance),
            ErrorRates = errorRates,
            Threshold = errorRates.First(e => e.Value.Far >= e.Value.Frr).Key
        };
    }

    public double Test(ISignerModel model, Signature testSignature)
    {
        if (model is not OptimalMeanTemplateSignerModel signerModel)
            throw new ApplicationException("Cannot test using the provided model. Please provide an OptimalMeanTemplateSignerModel type model.");

        var distance = signerModel.SignatureDistanceFromTraining[testSignature.ID];

        return distance <= signerModel.Threshold ? 1 : 0;
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
    public required string SignerID { get; init; }

    public required object DistanceMatrix { get; init; }

    public required Dictionary<string, double> SignatureDistanceFromTraining { get; init; }

    public required List<KeyValuePair<double, ErrorRate>> ErrorRates { get; init; }

    public required double Threshold { get; init; }
}

internal readonly struct SignatureDistance : IEquatable<SignatureDistance>
{
    public required string Id { get; init; }

    public required Origin Origin { get; init; }

    public required double Distance { get; init; }

    public bool Equals(SignatureDistance other)
    {
        return
            Id == other.Id
            && Origin.Equals(other.Origin)
            && (Distance - other.Distance).EqualsZero();
    }

    public override bool Equals(object? obj)
    {
        return obj is SignatureDistance other && Equals(other);
    }
}

internal static class DoubleExtension {
    public static bool EqualsZero(this double d)
    {
        return d is <= double.Epsilon and >= -double.Epsilon;
    }
}
