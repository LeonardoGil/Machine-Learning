using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.ML;
using Microsoft.ML.Trainers;

namespace MachineLearning
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                MLContext mlContext = new MLContext();

                //Carrega os dados da Pasta Dados
                (IDataView trainingDataView, IDataView testDataView) = LoadData(mlContext);

                ITransformer model = BuildAndTrainModel(mlContext, trainingDataView);
                EvaluateModel(mlContext, testDataView, model);
                UseModelForSinglePrediction(mlContext, model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Concat("Ocorreu um erro", " => ", ex.Message));
            }
        }

        public static (IDataView training, IDataView testing) LoadData(MLContext mlContext)
        {
            //Monta o path dos arquivos da Pasta Dados
            var trainingDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "recommendation-ratings-train.csv");
            var testDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "recommendation-ratings-test.csv");

            //Correga os dados de uma coleção para um IDataView
            IDataView otherDataView = mlContext.Data.LoadFromEnumerable(new List<MovieRating>());

            //Carrega os dados do arquivo para a Interface IDataView.
            //IDataView e uma Interface otimazada para guardar dados tabulados (Caracteres Alfabetico e Numerico).
            IDataView trainingDataView = mlContext.Data.LoadFromTextFile<MovieRating>(trainingDataPath, hasHeader: true, separatorChar: ',');
            IDataView testDataView = mlContext.Data.LoadFromTextFile<MovieRating>(testDataPath, hasHeader: true, separatorChar: ',');

            return (trainingDataView, testDataView);
        }

        //Data, Transformers, and Estimators.
        public static ITransformer BuildAndTrainModel(MLContext mlContext, IDataView trainingDataView)
        {
            //MatrixFactorizarTrainer e um modelo COO de comparação
            //COO uma lista de tuplas (linha, coluna, valor). As entradas são classificadas primeiro pelo índice de linha e depois pelo índice de coluna, e por fim índice de valor. 
            var options = new MatrixFactorizationTrainer.Options
            {
                MatrixColumnIndexColumnName = "userIdEncoded",
                MatrixRowIndexColumnName = "movieIdEncoded",
                LabelColumnName = "Label",

                NumberOfIterations = 20,
                ApproximationRank = 100
            };

            //Data são os dados tabulados (IDataView)
            //Os Estimators são usandos para transformar Data em Transformers
            //Transformers são dados formatados, que serão utilizados como modelo para futuros testes.
            IEstimator<ITransformer> estimator = mlContext
                .Transforms
                .Conversion
                .MapValueToKey(outputColumnName: "userIdEncoded", inputColumnName: "userId")
                .Append(mlContext
                    .Transforms
                    .Conversion
                    .MapValueToKey(outputColumnName: "movieIdEncoded", inputColumnName: "movieId"))
                .Append(mlContext
                    .Recommendation()
                    .Trainers
                    .MatrixFactorization(options));

            //Forma simplificada
            //Obtem o mesmo resultado do metodo anterior
            IEstimator<ITransformer> estimatorSimplificado = mlContext
                .Transforms
                .Conversion
                .MapValueToKey(new[]
                {
                    new  InputOutputColumnPair("userIdEncoded", "userId"),
                    new  InputOutputColumnPair("movieIdEncoded", "movieId")
                })
                .Append(mlContext
                    .Recommendation()
                    .Trainers
                    .MatrixFactorization(options));

            return estimator.Fit(trainingDataView);
        }

        public static void EvaluateModel(MLContext mlContext, IDataView testDataView, ITransformer model)
        {
            Console.WriteLine("=============== Evaluating the model ===============");
            //Trasnforma a IDataView de teste em um formato compitavel para testar o modelo.
            var prediction = model.Transform(testDataView);


            var metrics = mlContext.Regression.Evaluate(prediction, labelColumnName: "Label", scoreColumnName: "Score");

            //A root of mean squared error(RMS ou RMSE) é usada para medir as diferenças entre os valores previstos do modelo e os valores observados do conjunto de dados de teste. 
            //Tecnicamente, ela é a raiz quadrada da média dos quadrados dos erros.
            //Quanto menor, melhor o modelo.
            Console.WriteLine("Root Mean Squared Error : " + metrics.RootMeanSquaredError.ToString());

            //R Squared indica o quanto os dados se ajustam a um modelo. 
            //Varia de 0 a 1. Um valor de 0 significa que os dados são aleatórios ou não podem ser ajustados ao modelo. 
            //O valor 1 significa que o modelo corresponde exatamente aos dados. 
            //Você deseja que a pontuação R Squared esteja o mais próximo possível de 1.
            Console.WriteLine("RSquared: " + metrics.RSquared.ToString());
        }

        public static void UseModelForSinglePrediction(MLContext mlContext, ITransformer model)
        {
            Console.WriteLine("=============== Making a prediction ===============");
            //Prediction Engine e uma API que permite uma previsão em um unico conjunto de dadaos
            //Não utiliza-la em produção, devido a falta de segurança que a mesma proporciona
            //Caso necessario, procure na documentação da Microsoft Prediction Engine Pool
            var predictionEngine = mlContext.Model.CreatePredictionEngine<MovieRating, MovieRatingPrediction>(model);

            //Objeto para analise
            var testInput = new MovieRating { userId = 6, movieId = 10 };

            //Realiza uma previsão em cima do objeto de analise
            var movieRatingPrediction = predictionEngine.Predict(testInput);

            //Score determina a recomendação do filme, quanto mais alta melhor.
            if (Math.Round(movieRatingPrediction.Score, 1) > 3.5)
            {
                Console.WriteLine("Movie " + testInput.movieId + " is recommended for user " + testInput.userId);
            }
            else
            {
                Console.WriteLine("Movie " + testInput.movieId + " is not recommended for user " + testInput.userId);
            }
        }
    }
}
