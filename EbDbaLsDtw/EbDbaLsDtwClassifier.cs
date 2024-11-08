using System.Collections.ObjectModel;
using SigStat.Common;
using SigStat.Common.Pipeline;

namespace EbDbaLsDtw;

public class EbDbaLsDtwClassifier : IClassifier
{
    private const int EB_DBA_ITERATION_COUNT = 10;

    readonly ReadOnlyCollection<FeatureDescriptor> examinedFeatures = new([
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
                    distance: (a, b, _) => (a - b) * (a - b)
                );

                foreach (var (row, col) in dtwResult.WarpingPath)
                {
                    assoc[row - 1].Add(ts[col - 1]);
                }
            }

            for (int i = 0; i < timeSeriesAverageLength; i++)
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

    private static List<double> EstimateLocalStatibilty(MultivariateTimeSeries template, List<MultivariateTimeSeries> references)
    {
        // Step 1.
        // Compute the "standard DTW" between the template multivariate time-series and
        // the reference multivariate time-series set elements.
        // The result is a set of warping paths, with a length equal to the reference set's.
        var optimalWarpingPaths = new List<IEnumerable<(int Row, int Col)>>(references.Count);

        for (int i = 0; i < references.Count; i++)
        {
            var dtwResult = DtwResult<double[], double>.Dtw(
                template.ToColumnList(),
                references[i].ToColumnList(),
                distance: (a, b, _) => MultivariateTimeSeries.EuclideanDistanceBetweenMultivariatePoints(a, b)
            );

            optimalWarpingPaths.Add(dtwResult.WarpingPath);
        }

        // Step 2.
        // Calculate the direct matching points. The DMP's set has the same length as the referneces set
        // and the warping paths' set. The elements from the DMP's set have the same length as the template's.
        var directMatchingPoints = new List<List<bool>>(references.Count);
        for (int i = 0; i < references.Count; i++)
        {
            directMatchingPoints.Add(new List<bool>(template.Count));
        }

        for (int i = 0; i < references.Count; i++)
        {
            for (int j = 0; j < template.Count; j++)
            {
                var matchingPointsRow = optimalWarpingPaths[i].Where(w => w.Row == j);
                var matchingPointsCol = optimalWarpingPaths[i].Where(w => w.Col == j);

                var isDmp = matchingPointsCol.Count() == 1 && matchingPointsRow.Count() == 1;
                directMatchingPoints[i].Add(isDmp);
            }
        }

        // Step 3.
        // Construct the local stability sequence from the DMP's set.
        var localStability = new List<double>();
        for (int i = 0; i < template.Count; i++)
        {
            localStability.Add(directMatchingPoints.Select(x => x[i]).Where(x => x).Count() / (double) references.Count);
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
        static FeatureDescriptor keySelector(FeatureDescriptor f) => f;
        return examinedFeatures.ToDictionary(keySelector, elementSelector);
    }

    public ISignerModel Train(List<Signature> signatures)
    {
        var referenceSeriesByFeatures = AggregateByExaminedFeatures(f => signatures.Select(s => s.GetFeature<List<double>>(f)).ToList());
        var templateSeriesByFeatures = AggregateByExaminedFeatures(f => EbDba(referenceSeriesByFeatures[f], EB_DBA_ITERATION_COUNT));

        var references = signatures
            .Select(s =>
            {
                var dictionary = AggregateByExaminedFeatures(f => s.GetFeature<List<double>>(f).ToList());
                return new MultivariateTimeSeries(dictionary);
            })
            .ToList();
        var template = new MultivariateTimeSeries(templateSeriesByFeatures);
        var localStability = EstimateLocalStatibilty(template, references);

        var distancesFromTemplate = references
            .Select(reference => LsDtwDistance(template, reference, localStability))
            .ToList();
        var threshold = distancesFromTemplate.Average();

        return new MeanTemplateSignerModel
        {
            SignerID = signatures[0].Signer!.ID,
            Template = template,
            LocalStability = localStability,
            Threshold = threshold, 
        };
    }

    public double Test(ISignerModel model, Signature testSignature)
    {
        if (model is not MeanTemplateSignerModel)
            throw new ApplicationException("Cannot test using the provided model. Please provide an MeanTemplateSignerModel type model.");
        var signerModel = (MeanTemplateSignerModel)model;

        var testSignatureDataByFeature = examinedFeatures.ToDictionary(
            keySelector: f => f,
            elementSelector: testSignature.GetFeature<List<double>>
        );
        var test = new MultivariateTimeSeries(testSignatureDataByFeature);

        var lsDtwDistance = LsDtwDistance(signerModel.Template, test, signerModel.LocalStability);

        if (lsDtwDistance <= signerModel.Threshold)
            return 1;
        
        return 0;
    }
}
