using Microsoft.ML.Data;

public class NumberMatrix{
    [LoadColumn(0)]
    [ColumnName("Label")]
    [KeyType(10)]
    public UInt32 label;

    [LoadColumn(1, 784)]
    [VectorType(784)]
    [ColumnName("Features")]
    public float[]? Pixels {get;set;}
}