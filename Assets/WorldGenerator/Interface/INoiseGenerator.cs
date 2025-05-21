namespace WorldGenerator.Interface
{
    public interface INoiseGenerator
    {
        float [,] GenerateNoiseMap(int width, int height);
        void UpdateNoiseMap(object settings);
    }
}