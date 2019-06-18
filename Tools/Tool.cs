namespace JsMinBenchmark.Tools
{
    public class Tool
    {
        public string Name { get; set; }
        public Npm Npm { get; set; }
        public Download Download { get; set; }
        public string ExecCommand { get; set; }
        public string ExecArguments { get; set; }
        public string ExecDir { get; set; }
    }
}