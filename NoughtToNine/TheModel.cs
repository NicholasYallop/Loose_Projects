using Microsoft.ML;

namespace NoughtToNine{
    public static class TheModel{
        const string csvFilename = "data/mnist_train.csv";

        public static DataViewSchema? trainDataSchema;

        public static MLContext? mlContext{
            get {
                if (_mlContext == null) PopulateModel();   
                return _mlContext;
            }
            private set {_mlContext = value;}
        }

        public static MLContext? _mlContext = new MLContext();

        static ITransformer? _transformer;

        public static ITransformer? Transformer {
            get {
                if (_transformer == null) PopulateModel();   
                return _transformer;
            }
            private set {_transformer = value;}
        }

        public static void PopulateModel(bool refreshModel = false){
            ITransformer trainedModel;

            if (refreshModel){
                IDataView? trainData = DataReader.Build<NumberMatrix>(mlContext, csvFilename);
                if (trainData is null) return;
                trainDataSchema = trainData.Schema; 

                IEstimator<ITransformer>? estimator = mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy();

                trainedModel = estimator.Fit(trainData);

                var sw = new StreamWriter("/home/work/Repos/Loose_Projects/NoughtToNine/saves/model.zip");

                mlContext.Model.Save(trainedModel, trainData.Schema, sw.BaseStream);

                sw.Dispose();
            }
            else{
                var sr = new StreamReader("/home/work/Repos/Loose_Projects/NoughtToNine/saves/model.zip"); 

                trainedModel = mlContext.Model.Load(sr.BaseStream, out trainDataSchema);

                sr.Dispose();
            }

            Transformer = trainedModel;

        }
    }
}