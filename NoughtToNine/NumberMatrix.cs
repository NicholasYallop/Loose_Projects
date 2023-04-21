using Microsoft.ML.Data;

namespace NoughtToNine{
    public class NumberMatrix{
        [LoadColumn(0)]
        [ColumnName("Label")]
        [KeyType(10)]
        public UInt32 label {get;set;}

        [LoadColumn(1, 784)]
        [VectorType(784)]
        [ColumnName("Features")]
        public float[]? Pixels {get;set;}
    }

    public class ModelInput{
        [LoadColumn(1, 784)]
        [VectorType(784)]
        [ColumnName("Features")]
        public float[]? Pixels {get;set;}
    }

    public class ModelOutput{
        [LoadColumn(0)]
        [ColumnName("Label")]
        [KeyType(10)]
        public UInt32 label {get;set;}
    }
}
