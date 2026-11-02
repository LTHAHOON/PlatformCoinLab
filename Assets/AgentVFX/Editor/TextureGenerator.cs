using System.IO;
using UnityEditor;
using UnityEngine;

namespace AgentVFX.Editor
{
    public static class TextureGenerator
    {
        public static string Generate(VfxSpec spec, string outputDirectory, string hash)
        {
            string path = $"{outputDirectory}/{spec.id}_{hash}_Texture.png";
            if (File.Exists(path))
            {
                return path;
            }

            TextureSpec settings = spec.texture;
            Texture2D texture = new Texture2D(settings.width, settings.height, TextureFormat.RGBA32, false, true);
            Color[] pixels = new Color[settings.width * settings.height];
            System.Random random = new System.Random(spec.seed);
            float offset = (float)random.NextDouble() * 100f;
            for (int y = 0; y < settings.height; y++)
            {
                for (int x = 0; x < settings.width; x++)
                {
                    float u = x / (settings.width - 1f);
                    float v = y / (settings.height - 1f);
                    float noise = Mathf.PerlinNoise(u * settings.noiseScale + offset, v * settings.noiseScale + offset);
                    float dx = u * 2f - 1f;
                    float dy = v * 2f - 1f;
                    float radial = Mathf.Clamp01(1f - Mathf.Pow(Mathf.Sqrt(dx * dx + dy * dy), settings.radialPower));
                    Color gradient = Color.Lerp(settings.gradientStart, settings.gradientEnd, v);
                    pixels[y * settings.width + x] = new Color(gradient.r * noise, gradient.g * noise, gradient.b * noise, gradient.a * radial);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return path;
        }
    }
}
