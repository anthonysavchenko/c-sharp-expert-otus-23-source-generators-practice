using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Demo.App;
using Newtonsoft.Json;

namespace Demo.Benchmarks
{
  [MemoryDiagnoser]
  public class SerializationBenchmark
  {
    private readonly Order _order = Order.CreateSample();

    private readonly JsonSerializerOptions _stjOptions = new JsonSerializerOptions(JsonSerializerDefaults.General);

    private readonly JsonSerializerSettings _newtonsoftSettings = new JsonSerializerSettings();

    [Benchmark(Baseline = true)]
    public byte[] NewtonsoftJson()
    {
      var json = JsonConvert.SerializeObject(_order, _newtonsoftSettings);
      return Encoding.UTF8.GetBytes(json);
    }

    [Benchmark]
    public byte[] SystemTextJson()
    {
      return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(_order, _stjOptions);
    }


    [Benchmark]
    public void SourceGenerator()
    {
      using (var ms = new MemoryStream())
      {
        _order.SerializeTo(ms);
      }
    }
  }
}