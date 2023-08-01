using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class CellularAutomata : MonoBehaviour
{
    [SerializeField]
    int height;
    [SerializeField]
    int halfWidth;
    
    [SerializeField]
    Texture2D texture;
    [SerializeField]
    SpriteRenderer sr;

    [SerializeField]
    Vector3 a;
    [SerializeField]
    Vector3 b;
    [SerializeField]
    Vector3 c;
    [SerializeField]
    Vector3 d;

    [SerializeField]
    float scale;
    [SerializeField]
    int octaves;
    [SerializeField]
    [Range(0, 1)]
    float persistance;
    [SerializeField]
    [Range(0, 1)]
    float lacunarity;
    
    [SerializeField]
    Color co;

    [SerializeField]
    [Range(0, 1)]
    float paletteRange;
    [SerializeField]
    [Range(0, 1)]
    float paletteCycle;

    [SerializeField]
    bool random;

    public enum ColorRelation
    {
        Analogous,
        Complementary,
        Triad,
        Tetrad
    }

    [SerializeField]
    ColorRelation colorRelation;

    float[,] pNoise;
    NativeArray<float> noise;
    NativeArray<Color> colors;
    NativeArray<bool> flagArr;
    NativeArray<bool> fullArr;
    NativeArray<Color> textureColors;

    float h, s, v;
    int startY;
    Vector3 color;

    [SerializeField]
    uint fullSeed;

    public void Flag()
    {
        if (random)
        {
            d = new Vector3(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f));
            paletteCycle = Random.Range(0, 1f);
        }

        textureColors = new NativeArray<Color>(height * halfWidth * 2, Allocator.Persistent);

        texture.Reinitialize(halfWidth * 2, height);

        colors = new NativeArray<Color>(halfWidth * 2 * height, Allocator.Persistent);

        float t = paletteRange / 2 + paletteCycle;

        color = a + Vector3.Scale(b, new Vector3(Mathf.Cos(2 * Mathf.PI * (c.x * t + d.x)), Mathf.Cos(2 * Mathf.PI * (c.y * t + d.y)), Mathf.Cos(2 * Mathf.PI * (c.z * t + d.z))));
        
        Color.RGBToHSV(new Color(color.x, color.y, color.z), out h, out s, out v);

        switch (colorRelation)
        {
            case ColorRelation.Analogous:
                h = (h + (float)30 / 360 * Random.Range(1, 3)) % 1;
                break;
            case ColorRelation.Complementary:
                h = (h + 0.5f) % 1;
                break;
            case ColorRelation.Triad:
                h = (h + (float)120 / 360 * Random.Range(1, 3)) % 1;
                break;
            case ColorRelation.Tetrad:
                h = (h + (float)90 / 360 * Random.Range(1, 4)) % 1;
                break;
        }

        v += (0.5f - v) * 1.8f;

        noise = new NativeArray<float>(height * halfWidth * 2, Allocator.Persistent);

        pNoise = Noise.GenerateNoiseMap(halfWidth * 2, height, Random.Range(int.MinValue, int.MaxValue), scale, octaves, persistance, lacunarity, Vector2.zero);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < halfWidth * 2; x++)
            {
                noise[x + y * halfWidth * 2] = pNoise[x, y];
            }
        }

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.HSVToRGB(h, s, v);
        }

        flagArr = new NativeArray<bool>(height * halfWidth * 2, Allocator.Persistent);
        for (int i = 0; i < flagArr.Length; i++)
        {
            flagArr[i] = true;
        }

        CreateSprite(6000, true, new Vector2Int(halfWidth - 1, height - 2), new NativeArray<int>(3, Allocator.Persistent) { [0] = 6, [1] = 7, [2] = 8 }, new NativeArray<int>(8, Allocator.Persistent) { [0] = 1, [1] = 2, [2] = 3, [3] = 4, [4] = 5, [5] = 6, [6] = 7, [7] = 8 }, colors, flagArr, 20, true, true);
    }

    public void Pattern()
    {
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.HSVToRGB(h, s, v > 0.5f ? v - 0.05f : v + 0.05f);
        }

        CreateSprite(500, false, new Vector2Int(halfWidth - 1, startY), new NativeArray<int>(1, Allocator.Persistent) { [0] = 3 }, new NativeArray<int>(5, Allocator.Persistent) { [0] = 1, [1] = 2, [2] = 3, [3] = 4 }, colors, flagArr, 100, false, false);
    }

    public void Sigil()
    {
        pNoise = Noise.GenerateNoiseMap(halfWidth, height, Random.Range(int.MinValue, int.MaxValue), scale, octaves, persistance, lacunarity, Vector2.zero);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < halfWidth; x++)
            {
                noise[x + y * halfWidth * 2] = pNoise[x, y];
                noise[halfWidth * 2 - 1 - x + y * halfWidth * 2] = pNoise[x, y];
            }
        }

        var paletteJob = new PaletteJob()
        {
            noise = noise,
            colors = colors,
            paletteRange = paletteRange,
            paletteCycle = paletteCycle,
            a = a,
            b = b,
            c = c,
            d = d
        };

        var handler = paletteJob.Schedule(colors.Length, 32);
        handler.Complete();

        fullArr.Dispose();

        CreateSprite(150, true, new Vector2Int(halfWidth - 1, startY), new NativeArray<int>(3, Allocator.Persistent) { [0] = 6, [1] = 7, [2] = 8 }, new NativeArray<int>(8, Allocator.Persistent) { [0] = 1, [1] = 2, [2] = 3, [3] = 4, [4] = 5, [5] = 6, [6] = 7, [7] = 8 }, colors, flagArr, 5, true, true);
    }

    public void CreateBanner()
    {
        Flag();

        for (int i = 0; i < flagArr.Length; i++)
        {
            flagArr[i] = fullArr[i];
        }

        fullArr.Dispose();

        int l = 0;

        for (int i = 0; i < flagArr.Length; i++)
        {
            if (flagArr[i])
            {
                l++;
            }
            if (l == halfWidth * 2 - 2)
            {
                startY = (height + i / (halfWidth * 2)) / 2;
                break;
            }
            else if (i % (halfWidth * 2) == 0)
                l = 0;
        }

        Pattern();

        Sigil();

        colors.Dispose();

        texture.SetPixels(textureColors.ToArray());

        textureColors.Dispose();

        flagArr.Dispose();

        noise.Dispose();

        fullArr.Dispose();

        texture.Apply();

        sr.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
    }

    public void CreateSprite(int walkIterations, bool finiteEdges, Vector2Int startPos, NativeArray<int> birthRules, NativeArray<int> surviveRules, NativeArray<Color> colors, NativeArray<bool> flagArr, int iterations, bool outline, bool symmetry)
    {
        fullArr = new NativeArray<bool>(halfWidth * 2 * height, Allocator.Persistent);
        if (random)
        {
            uint thirtyBits = (uint)Random.Range(1, 1 << 30);
            uint twoBits = (uint)Random.Range(0, 1 << 2);
            fullSeed = (thirtyBits << 2) | twoBits;
        }

        var RandomWalk = new RandomWalkJob()
        {
            arr = fullArr,
            flagArr = flagArr,
            walkIterations = walkIterations,
            width = halfWidth * 2,
            height = height,
            seed = fullSeed,
            finiteEdges = finiteEdges,
            symmetry = symmetry,
            startPos = startPos
        };

        var h = RandomWalk.Schedule();
        h.Complete();
        
        NativeArray<bool> tempArr = new NativeArray<bool>(fullArr.Length, Allocator.Persistent);

        var cellularAutomata = new CellularAutomataJob()
        {
            arr = fullArr,
            flagArr = flagArr,
            tempArr = tempArr,
            birthRules = birthRules,
            surviveRules = surviveRules,
            width = halfWidth * 2,
            height = height
        };

        for (int i = 0; i < iterations; i++)
        {
            for (int j = 0; j < fullArr.Length; j++)
            {
                tempArr[j] = fullArr[j];
            }
            var handle = cellularAutomata.Schedule(fullArr.Length, 32);
            handle.Complete();
        }

        tempArr.Dispose();
        birthRules.Dispose();
        surviveRules.Dispose();

        Draw(colors, fullArr, outline);
    }

    private void Draw(NativeArray<Color> colors, NativeArray<bool> fullArr, bool outline)
    {
        var colorJob = new ColorJob()
        {
            arr = fullArr,
            textureColors = textureColors,
            colors = colors,
            noise = noise,
            outline = outline,
            width = halfWidth * 2,
            height = height
        };
        
        var handler = colorJob.Schedule(textureColors.Length, 32);
        handler.Complete();
    }
    
    [BurstCompile]
    public struct PaletteJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> noise;
        [WriteOnly]
        public NativeArray<Color> colors;

        public float paletteRange;
        public float paletteCycle;

        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
        public Vector3 d;

        public void Execute(int index)
        {
            float t = noise[index] * paletteRange + paletteCycle;
            
            Vector3 color = a + Vector3.Scale(b, new Vector3(Mathf.Cos(2 * Mathf.PI * (c.x * t + d.x)), Mathf.Cos(2 * Mathf.PI * (c.y * t + d.y)), Mathf.Cos(2 * Mathf.PI * (c.z * t + d.z))));

            colors[index] = new Color(color.x, color.y, color.z);
        }
    }


    [BurstCompile]
    public struct ColorJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<bool> arr;
        [WriteOnly]
        public NativeArray<Color> textureColors;
        [ReadOnly]
        public NativeArray<Color> colors;
        [ReadOnly]
        public NativeArray<float> noise;
        public bool outline;
        public int width;
        public int height;

        public void Execute(int index)
        {
            float h, s, v;
            int curRow = index / width;
            int curCol = index % width;
            
            Color.RGBToHSV(colors[index], out h, out s, out v);

            //v -= -downNeigh[index] / (21 / 0.4f) + upNeigh[index] / (21 / 0.4f);
            v -= Mathf.Floor(3 - ((float)curRow / (height - 1) * 3 * 3 + noise[index] * 3) / 4) / 3 * v * 0.2f;
            s += Mathf.Floor(3 - ((float)curRow / (height - 1) * 3 * 3 + noise[index] * 3) / 4) / 3 * s * 0.2f; ;

            v -= Mathf.Round(Mathf.Cos((float)curCol / (width - 1) * Mathf.PI * 2 * 4) + 1) / 2 * v * 0.2f;
            s += Mathf.Round(Mathf.Cos((float)curCol / (width - 1) * Mathf.PI * 2 * 4) + 1) / 2 * s * 0.2f;

            if (arr[index])
                textureColors[index] = Color.HSVToRGB(h, s, v) + new Color(0, 0, Mathf.Floor(3 - ((float)curRow / (height - 1) * 3 * 3 + noise[index] * 3) / 4) / 3 * 0.02f + Mathf.Round(Mathf.Cos((float)curCol / (width - 1) * Mathf.PI * 2 * 4) + 1) / 2 * 0.02f);

            else if (outline)
            {
                if (arr[(curCol - 1 >= 0 ? curCol - 1 : 0) + curRow * width] || arr[(curCol + 1 < width ? curCol + 1 : width - 1) + curRow * width] || arr[curCol + (curRow - 1 >= 0 ? curRow - 1 : 0) * width] || arr[curCol + (curRow + 1 < height ? curRow + 1 : height - 1) * width])
                    textureColors[index] = Color.black;
            }
        }
    }

    [BurstCompile]
    public struct RandomWalkJob : IJob
    {
        [WriteOnly]
        public NativeArray<bool> arr;
        [ReadOnly]
        public NativeArray<bool> flagArr;
        public int walkIterations;
        public int width;
        public int height;
        public uint seed;
        public bool finiteEdges;
        public bool symmetry;
        public Vector2Int startPos;

        public void Execute()
        {
            Unity.Mathematics.Random rnd = new Unity.Mathematics.Random(seed);
            
            Vector2Int curPos = Vector2Int.zero;

            if (flagArr[startPos.x + startPos.y * width])
                curPos = startPos;

            else
            {
                for (int y = height - 1; y >= 0; y--)
                {
                    if (flagArr[startPos.x + y * width] && Mathf.Abs(startPos.y - y) < Mathf.Abs(startPos.y - curPos.y))
                        curPos = new Vector2Int(startPos.x, y);
                    else if (Mathf.Abs(startPos.y - y) > Mathf.Abs(startPos.y - curPos.y))
                        break;
                }
            }

            arr[curPos.x + curPos.y * width] = true;

            if (symmetry)
                arr[width - 1 - curPos.x + curPos.y * width] = true;

            Vector2Int rndDir;
            
            for (int i = 0; i < walkIterations; i++)
            {
                rndDir = new Vector2Int(rnd.NextInt(-1, 2), rnd.NextInt(-1, 2));
                Vector2Int newPos = curPos + rndDir;

                if (finiteEdges)
                    newPos = new Vector2Int(newPos.x >= 1 && newPos.x < width - 1 ? newPos.x : Mathf.Abs(1 - newPos.x), newPos.y >= 1 && newPos.y < height - 1 ? newPos.y : Mathf.Abs(1 - newPos.y));
                else
                    newPos = new Vector2Int(newPos.x >= 1 && newPos.x < width - 1 ? newPos.x : Mathf.Abs(newPos.x - width + 2), newPos.y >= 1 && newPos.y < height - 1 ? newPos.y : Mathf.Abs(newPos.y - height + 2));

                int index = newPos.x + newPos.y * width;

                if (flagArr[index])
                {
                    curPos = newPos;

                    arr[index] = true;

                    if (symmetry)
                        arr[width - 1 - curPos.x + curPos.y * width] = true;
                }
            }
        }
    }

    [BurstCompile]
    public struct CellularAutomataJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeArray<bool> arr;
        [ReadOnly]
        public NativeArray<bool> tempArr;
        [ReadOnly]
        public NativeArray<bool> flagArr;
        [ReadOnly]
        public NativeArray<int> birthRules;
        [ReadOnly]
        public NativeArray<int> surviveRules;
        public int width;
        public int height;

        public void Execute(int index)
        {
            int liveNeighbours = 0;
            int curRow = index / width;
            int curCol = index % width;
            
            if (curCol == 0 || curRow == 0 || curRow == height - 1 || curCol == width - 1)
                return;

            for (int y = curRow - 1; y < curRow + 2; y++)
            {
                for (int x = curCol - 1; x < curCol + 2; x++)
                {
                    if (tempArr[x + y * width] && x + y * width != index)
                    {
                        liveNeighbours++;
                    }
                }
            }

            if (!tempArr[index] && birthRules.Contains(liveNeighbours) && flagArr[index])
                arr[index] = true;

            else if (tempArr[index] && !surviveRules.Contains(liveNeighbours))
                arr[index] = false;
        }
    }
}
