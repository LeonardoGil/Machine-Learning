using Microsoft.ML.Data;

namespace MachineLearning
{
    public class MovieRating
    {
        [LoadColumn(0)]
        public float userId { get; set; }

        [LoadColumn(1)]
        public float movieId { get; set; }

        [LoadColumn(2)]
        public float Label { get; set; }
    }
}
