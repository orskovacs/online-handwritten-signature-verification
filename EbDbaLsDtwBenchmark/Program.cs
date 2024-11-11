using SigStat.Common.Loaders;
using SigStat.Common;
using SigStat.Common.Framework.Samplers;
using EbDbaLsDtw;
using SigStat.Common.Model;

var path = args[0];

var benchmark = new VerifierBenchmark()
{
    Verifier = new Verifier
    {
        Classifier = new EbDbaLsDtwClassifier(ThresholdCalculator),
        Pipeline = EbDbaLsDtwVerifierItems.FeatureExtractorPipeline,
    },
    Loader = new Svc2004Loader(path, true),
    Sampler = new FirstNSampler()
};

var result = benchmark.Execute(true);

Console.WriteLine("AER         FAR         FRR");
Console.WriteLine($"{result.FinalResult.Aer:F6}    {result.FinalResult.Far:F6}    {result.FinalResult.Frr:F6}");

return;

double ThresholdCalculator(List<double> distancesFromTemplate)
{
    return distancesFromTemplate.Average() + distancesFromTemplate.StandardDeviation() * 1.4;
}
