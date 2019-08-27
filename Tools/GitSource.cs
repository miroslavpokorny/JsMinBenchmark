using System.ComponentModel.DataAnnotations;

namespace JsMinBenchmark.Tools
{
    public class GitSource
    {
        public string Url { get; set; }
        public string BuildCommand { get; set; }
    }
}