using SigStat.Common.Loaders;
using SigStat.Common;
using SigStat.Common.Framework.Samplers;
using EbDbaLsDtw.Benchmark;

var path = args[0];

var benchmark = new VerifierBenchmark()
{
    Verifier = new EbDbaLsDtwVerifier(),
    Loader = new Svc2004Loader(path, true),
    Sampler = new FirstNSampler()
};

var result = benchmark.Execute(true);

Console.WriteLine($"AER: {result.FinalResult.Aer}");
Console.WriteLine($"FAR: {result.FinalResult.Far}");
Console.WriteLine($"FRR: {result.FinalResult.Frr}");
