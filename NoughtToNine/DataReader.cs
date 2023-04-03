using Microsoft.ML;

public static class DataReader{
    public static IDataView? Build<T>(MLContext MLContext, string CSVFilename){
        return MLContext.Data.LoadFromTextFile<T>(CSVFilename, separatorChar: ',', hasHeader: true);
    }


}