using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class Noise
{
    [BurstCompile]
    public struct MapGenerationJob : IJobParallelFor
    {
        public int mapWidth;
        public int mapHeight;
        public float scale;
        public int octaves;
        public float persistance;
        public float lacunarity;

        [ReadOnly]
        public NativeArray<float2> octaveOffsets;

        [WriteOnly]
        public NativeArray<float> result;

        public void Execute(int index)
        {
            var halfWidth = mapWidth / 2;
            var halfHeight = mapHeight / 2;

            var amplitude = 1f;
            var frequency = 1f;
            var noiseHeight = 0f;

            var x = index % mapWidth;
            var y = index / mapWidth;

            for (var i = 0; i < octaves; i++)
            {
                var sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
                var sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;

                var perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;

                noiseHeight += perlinValue * amplitude;

                amplitude *= persistance;
                frequency *= lacunarity;
            }

            result[index] = noiseHeight;
        }
    }

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, float2 offset)
    {
        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        System.Random random = new System.Random(seed);

        var jobResult = new NativeArray<float>(mapWidth * mapHeight, Allocator.TempJob);
        var octaveOffsets = new NativeArray<float2>(octaves, Allocator.TempJob);

        for (var i = 0; i < octaves; i++)
        {
            var offsetX = random.Next(-100000, 100000) + offset.x;
            var offsetY = random.Next(-100000, 100000) + offset.y;
            var nativeOctaveOffsets = octaveOffsets;
            nativeOctaveOffsets[i] = new float2(offsetX, offsetY);
        }

        var job = new MapGenerationJob()
        {
            mapWidth = mapWidth,
            mapHeight = mapHeight,
            lacunarity = lacunarity,
            octaves = octaves,
            octaveOffsets = octaveOffsets,
            persistance = persistance,
            result = jobResult,
            scale = scale,
        };

        var handle = job.Schedule(jobResult.Length, 32);
        handle.Complete();

        octaveOffsets.Dispose();

        return SmoothNoiseMap(mapWidth, mapHeight, jobResult);
    }

    private static float[,] SmoothNoiseMap(int mapWidth, int mapHeight, NativeArray<float> jobResult)
    {
        var result = new float[mapWidth, mapHeight];

        var maxNoiseHeight = float.MinValue;
        var minNoiseHeight = float.MaxValue;

        for (var y = 0; y < mapHeight; y++)
        {
            for (var x = 0; x < mapWidth; x++)
            {
                var noiseHeight = jobResult[y * mapWidth + x];

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }

                result[x, y] = noiseHeight;
            }
        }

        for (var y = 0; y < mapHeight; y++)
        {
            for (var x = 0; x < mapWidth; x++)
            {
                result[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, result[x, y]);
            }
        }

        jobResult.Dispose();

        return result;
    }
}
