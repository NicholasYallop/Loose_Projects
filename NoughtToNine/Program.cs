using Microsoft.ML;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal class Program
{
    private static void Main(string[] args)
    {
        var csvFilename = "data/mnist_train.csv";

        bool createNewModel = false;
        bool refreshCrossval = false;

        int crossvalFolds = 5;

        var mlContext = new MLContext();

        IDataView? trainData = DataReader.Build<NumberMatrix>(mlContext, csvFilename);
        if (trainData is null) return;
        DataViewSchema trainDataSchema = trainData.Schema; 

        IEstimator<ITransformer>? estimator = mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy();
        ITransformer trainedModel;

        if (createNewModel){
            trainedModel = estimator.Fit(trainData);
            mlContext.Model.Save(trainedModel, trainData.Schema, "saves/model.zip");
        }
        else{
            trainedModel = mlContext.Model.Load("saves/model.zip", out trainDataSchema);
        }

        List<CustomCrossValResults> customCrossValResults;

        if (refreshCrossval){
            var crossValidateReturn = mlContext.MulticlassClassification.CrossValidate(trainData, estimator, numberOfFolds: crossvalFolds);

            using(StreamWriter sr = new StreamWriter("saves/previousFoldcount.txt", append: false)){
                sr.Write(crossvalFolds);
            }

            customCrossValResults = crossValidateReturn.ToList().Select(cvResult =>
                new CustomCrossValResults(){
                    Fold = cvResult.Fold,
                    Metrics = new Dictionary<string, JToken>(
                        cvResult.Metrics.GetType().GetProperties().Select(
                            property => new KeyValuePair<string, JToken>(property.Name, 
                            JToken.FromObject(property.GetValue(cvResult.Metrics) ?? string.Empty))
                        )),
                    Model = cvResult.Model
                }).ToList();
            

            customCrossValResults.ToList().ForEach(x =>
            {
                using(StreamWriter sr = new StreamWriter("saves/metrics_fold" + x.Fold + ".json", append: false)){
                    sr.Write(JsonConvert.SerializeObject(x.Metrics));
                }
                using(StreamWriter sr = new StreamWriter("saves/model_fold" + x.Fold + ".zip", append: false)){
                    mlContext.Model.Save(x.Model, trainDataSchema, sr.BaseStream);
                }
            });
        }
        else{
            customCrossValResults = new List<CustomCrossValResults>();

            int previousFoldcount;
            using (StreamReader sr = new StreamReader("saves/previousFoldcount.txt")){
                previousFoldcount = int.Parse(sr.ReadToEnd());
            }

            for (int i=0; i < previousFoldcount; i++){
                CustomCrossValResults customCrossvalEntry = new CustomCrossValResults();
                using (StreamReader srMetrics = new StreamReader("saves/metrics_fold" + i + ".json")){
                    using (StreamReader srModel = new StreamReader("saves/model_fold" + i + ".zip")){
                        customCrossvalEntry.Fold = i;
                        customCrossvalEntry.Metrics = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(srMetrics.ReadToEnd());
                        customCrossvalEntry.Model = mlContext.Model.Load(srModel.BaseStream, out trainDataSchema);
                    }
                }
                customCrossValResults.Add(customCrossvalEntry);
            }
        }

        var pcp = customCrossValResults[0].Metrics["ConfusionMatrix"]["PerClassPrecision"];
        var pcc = customCrossValResults[0].Metrics["ConfusionMatrix"]["PerClassRecall"];
    }
}