using Microsoft.ML;
using Newtonsoft.Json.Linq;

public class CustomCrossValResults{
    public int Fold {get;set;}

    public Dictionary<string, JToken>? Metrics {get;set;}
    
    public ITransformer? Model {get;set;}
}