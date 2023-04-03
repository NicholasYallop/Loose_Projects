using Microsoft.ML;
using Microsoft.ML.Vision;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal class Program
{
    private static void Main(string[] args)
    {
        var trainCSVFilename = "data/mnist_train.csv";
        var testCSVFilename = "data/mnist_train.csv";

        bool createNewModel = true;
        bool refreshCrossval = true;

        int crossvalFolds = 5;

        var mlContext = new MLContext(seed: 1);
        var hold = mlContext.MulticlassClassification.Trainers.ImageClassification();

        IDataView? trainData = DataReader.Build<NumberMatrix>(mlContext, trainCSVFilename);
        IDataView? testData = DataReader.Build<NumberMatrix>(mlContext, testCSVFilename);
        if (trainData is null || testData is null) return;
        DataViewSchema trainDataSchema = trainData.Schema; 

        var options = new ImageClassificationTrainer.Options(){
                FeatureColumnName = "Image",
                LabelColumnName = "Label",
                // Just by changing/selecting InceptionV3/MobilenetV2/ResnetV250
                // here instead of ResnetV2101 you can try a different 
                // architecture/ pre-trained model. 
                Arch = ImageClassificationTrainer.Architecture.ResnetV2101,
                Epoch = 50,
                BatchSize = 10,
                LearningRate = 0.01f,
                MetricsCallback = (metrics) => Console.WriteLine(metrics),
                ValidationSetFraction = 0.1f,
                // Disable EarlyStopping to run to specified number of epochs.
                EarlyStoppingCriteria = null
            };
        IEstimator<ITransformer>? estimator = mlContext.MulticlassClassification.Trainers.ImageClassification();
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

        var pcp = customCrossValResults.Select(fold => fold.Metrics["ConfusionMatrix"]["PerClassPrecision"]);
        var bestAvgPrecision = customCrossValResults
                    .Select(fold => fold.Metrics["ConfusionMatrix"]["PerClassPrecision"])
                    .Select(ClasswidePrecision => ClasswidePrecision.Values<decimal>()).Max(x => x.Sum()/10);

        var pcr = customCrossValResults.Select(fold => fold.Metrics["ConfusionMatrix"]["PerClassRecall"]);
        var bestAvgRecall = customCrossValResults
                    .Select(fold => fold.Metrics["ConfusionMatrix"]["PerClassRecall"])
                    .Select(ClasswidePrecision => ClasswidePrecision.Values<decimal>()).Max(x => x.Sum()/10);
    }
}