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

        var references = examinedFeatures.Select(f => new {
            Feature = f,
            References = signatures
                .Select(s => s.GetFeature<List<double>>(f))
                .ToList(),
        });

        var templates = references.Select((r) => new {
            r.Feature,
            Template = EbDba(r.References, EB_DBA_ITERATION_COUNT),
        });

        var stabilityValues = references.Zip(templates).Select((r_t) =>
        {
            var feature = r_t.First.Feature;
            var references = r_t.First.References;
            var template = r_t.Second.Template;

            return new
            {
                Feature = feature,
                Stability = EstimateLocalStatibilty(references, template),
            };
        });

        var thresholds = references.Zip(templates, stabilityValues).Select((r_t_s) => {
            var feature = r_t_s.First.Feature;
            var references = r_t_s.First.References;
            var template = r_t_s.Second.Template;
            var stability = r_t_s.Third.Stability;

            return new {
                Feature = feature,
                Threshold = references
                    .Select(r => Distance(template, r, stability))
                    .Max()
            };
        });

        return new MeanTemplateSignerModel
        {
            SignerID = signatures[0].Signer.ID,
            Thresholds = thresholds.ToDictionary(t => t.Feature, t => t.Threshold),
            Templates = templates.ToDictionary((t) => t.Feature, (t) => t.Template),
            LocalStabiltyValues = stabilityValues.ToDictionary((s) => s.Feature, (s) => s.Stability),
        };
    }

    public double Test(ISignerModel model, Signature testSignature)
    {
        if (model is not MeanTemplateSignerModel)
            throw new ApplicationException("Cannot test using the provided model. Please provide a MeanTemplateSignerModel type model.");
        var signerModel = (MeanTemplateSignerModel)model;

        List<FeatureDescriptor> examinedFeatures = [
            OriginalFeatures.NormalizedX,
            OriginalFeatures.NormalizedY,
            OriginalFeatures.PenPressure,
            DerivedFeatures.PathTangentAngle,
            DerivedFeatures.PathVelocityMagnitude,
            DerivedFeatures.LogCurvatureRadius,
            DerivedFeatures.TotalAccelerationMagnitude,
        ];

        var featuresFromTestSignature = examinedFeatures.ToDictionary(
            keySelector: f => f,
            elementSelector: testSignature.GetFeature<List<double>>
        );

        var lsDtwDistances = examinedFeatures.Select(f =>
        {
            var lsDtwDistance = Distance(
                signerModel.Templates[f],
                featuresFromTestSignature[f],
                signerModel.LocalStabiltyValues[f]
            );

            return lsDtwDistance / signerModel.Thresholds[f];
        });

        var probabilities = lsDtwDistances.Select(d => 1 - d).Select(p => Math.Min(Math.Max(p, 0.1), 1)).ToList();

        var genuinityProbability = probabilities.Aggregate(
            seed: 1.0,
            (acc, next) => acc * next
        );

        genuinityProbability *= 41500;

        Console.WriteLine(genuinityProbability);
        return genuinityProbability;
    }
}
