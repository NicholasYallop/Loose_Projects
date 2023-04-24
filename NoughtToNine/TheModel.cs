using Microsoft.ML;

namespace NoughtToNine{
    public static class TheModel{
        const string csvFilename = "data/mnist_train.csv";

        private static DataViewSchema? _trainDataSchema = null;

        public static DataViewSchema? trainDataSchema{
            get {
                if (_trainDataSchema == null) PopulateModel();   
                return _trainDataSchema;
            }
            private set {_trainDataSchema = value;}
        }

        public static MLContext? _mlContext = null;

        public static MLContext mlContext {
            get{
                if (_mlContext is null){
                    _mlContext = new MLContext();
                    PopulateModel();
                }
                return _mlContext;
            }
            set{
                _mlContext=value;
            }
        }

        private static ITransformer? _transformer = null;

        public static ITransformer Transformer {
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
                _trainDataSchema = trainData.Schema; 

                IEstimator<ITransformer>? estimator = mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy();

                trainedModel = estimator.Fit(trainData);

                var sw = new StreamWriter("/home/work/Repos/Loose_Projects/NoughtToNine/saves/model.zip");

                mlContext.Model.Save(trainedModel, trainData.Schema, sw.BaseStream);

                sw.Dispose();
            }
            else{
                var sr = new StreamReader("/home/work/Repos/Loose_Projects/NoughtToNine/saves/model.zip"); 

                trainedModel = mlContext.Model.Load(sr.BaseStream, out _trainDataSchema);

                sr.Dispose();
            }

            Transformer = trainedModel;

        }
    }
}