// Step 1: Create 3D Texture from volume data

// File: Create3DTextureVolume.cs
// Location: Assets/Scripts/Editor/Create3DTextureVolume.cs

using UnityEngine;
using UnityEditor;
using System;
using System.Runtime.InteropServices;

public class Create3DTextureVolume : MonoBehaviour
{
    [MenuItem("CS116A/3DTexture")]
    static void CreateTexture3DVolume()
    {
        // Ask user to select the volume image
        string path = EditorUtility.OpenFilePanel("Select Volume Image", "", "tif");
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("No file selected. Operation cancelled.");
            return;
        }

        try
        {
            // Load the image using SimpleITK
            var imageFileReader = new itk.simple.ImageFileReader();
            imageFileReader.SetFileName(path);
            var volImage = imageFileReader.Execute();

            Debug.Log("Volume loaded successfully!");
            Debug.Log($"Total pixels: {volImage.GetNumberOfPixels()}");

            // Cast to float32
            var volumeImage = itk.simple.SimpleITK.Cast(volImage, itk.simple.PixelIDValueEnum.sitkFloat32);

            // Get dimensions w*h*d
            int depth = (int)volumeImage.GetDepth();
            int height = (int)volumeImage.GetHeight();
            int width = (int)volumeImage.GetWidth();

            int length = width * height * depth;

            // Copy buffer data to float array
            IntPtr bufferImg = volumeImage.GetBufferAsFloat();
            float[] bufferAsArrayImg = new float[length];
            Marshal.Copy(bufferImg, bufferAsArrayImg, 0, length);

            Debug.Log($"Volume Dimensions: {width} x {height} x {depth}");

            // Create color array
            Color[] colors = new Color[length];

            for (int k = 0; k < depth; k++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        int idx = i + width * (j + height * k);
                        float pixel = bufferAsArrayImg[idx];

                        // Assign grayscale color if non-zero pixel
                        if (pixel > 0)
                        {
                            byte val = (byte)Mathf.Clamp(pixel, 0, 255);
                            colors[idx] = new Color32(val, val, val, 255);
                        }
                        else
                        {
                            colors[idx] = Color.clear;
                        }
                    }
                }
            }

            // Create 3D texture
            TextureFormat format = TextureFormat.RGBA32;
            TextureWrapMode wrapMode = TextureWrapMode.Clamp;

            Texture3D texture = new Texture3D(width, height, depth, format, false);
            texture.wrapMode = wrapMode;
            texture.SetPixels(colors);
            texture.Apply();

            // Save texture to Assets folder
            string assetPath = "Assets/MyCS116A_3DTexture.asset";
            AssetDatabase.CreateAsset(texture, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"3D Texture saved to: {assetPath}");
            EditorUtility.DisplayDialog("3D Texture Created", "Successfully created MyCS116A_3DTexture.asset!", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating 3D texture: {ex.Message}");
        }
    }
}

