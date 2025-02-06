using System;
using System.Collections.Generic;

namespace Assets.GameUtilities.Utils
{
    using System.IO;
    using UnityEngine;

    public static class TestBitmapMatrix
    {
        private static readonly Dictionary<int, Color> ColorMap = new()
        {
            { 0, Color.white },
            { 1, Color.red },
            { 2, Color.green },
            { 3, Color.blue },
            { 4, Color.yellow },
            { 5, new Color(1f, 0f, 1f) }, // Purple
        };

        /// <summary>
        /// Converts a 2D integer array to a Texture2D where each value corresponds to a specific color
        /// </summary>
        /// <param name="matrix">2D integer array input</param>
        /// <returns>Texture2D representation of the matrix</returns>
        public static Texture2D ConvertToTexture(int[,] matrix)
        {
            if (matrix == null)
                throw new ArgumentNullException(nameof(matrix));

            var width = matrix.GetLength(1);
            var height = matrix.GetLength(0);

            var texture = new Texture2D(width, height);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var value = matrix[y, x];
                    var color = GetColorForValue(value);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Gets the color corresponding to a matrix value
        /// </summary>
        /// <param name="value">Integer value from the matrix</param>
        /// <returns>Color corresponding to the value</returns>
        private static Color GetColorForValue(int value)
        {
            if (ColorMap.TryGetValue(value, out var color))
                return color;

            // Return black for undefined values
            return Color.black;
        }

        /// <summary>
        /// Saves the texture to a PNG file
        /// </summary>
        /// <param name="matrix">2D integer array input</param>
        /// <param name="filePath">Path where to save the texture (relative to Application.dataPath)</param>
        public static void SaveMatrixAsTexture(int[,] matrix, string filePath)
        {
            var texture = ConvertToTexture(matrix);
            var bytes = texture.EncodeToPNG();
            var fullPath = Path.Combine(Application.dataPath, filePath);
            File.WriteAllBytes(fullPath, bytes);

        }
    }
}
