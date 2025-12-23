// Step 3: Extension to Liver Segments - Adding Static Color mapping
// Added +1 (yellow) gallbladder segment

// basically Step 1 + Step 2 + liver segment coloring

// File Name: Create3DTextureVolume_ColorSegments.cs
// Location: Assets/Assignment_6_Script/
// Description: Generates a color-coded 3D texture from a segmented liver volume
// Remember to use texturevolume shader material that we created prior

using UnityEngine;
using UnityEditor;
using System;
using System.Runtime.InteropServices;

public class Create3DTextureVolume_ColorSegments_GB : MonoBehaviour // MARKED AS CHANGED (renamed class to avoid conflicts)
{
    [MenuItem("CS116A/3DTexture Liver + Gallbladder Segments (GB)")] // MARKED AS CHANGED (updated menu name)
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

                        // Assign liver segment colors (Starting from pixel 1)
                        // MARKED AS CHANGED — pixel==0 is now transparent background (not gallbladder)
                        if (pixel == 0)
                            colors[idx] = Color.clear; // Transparent for background
                        else if (pixel == 1)
                            colors[idx] = Color.yellow; // Segment 1 = Yellow (gallbladder)
                        else if (pixel == 2)
                            colors[idx] = Color.blue;   // Segment 2 = Blue
                        else if (pixel == 3)
                            colors[idx] = Color.black;  // Segment 3 = Black
                        else if (pixel == 4)
                            colors[idx] = Color.red;    // Segment 4 = Red
                        else if (pixel == 5)
                            colors[idx] = new Color(0.6f, 0.3f, 0.0f); // Segment 5 = Brown
                        else if (pixel == 6)
                            colors[idx] = new Color(1f, 0.75f, 0.8f);  // Segment 6 = Pink
                        else if (pixel == 7)
                            colors[idx] = new Color(1f, 0.5f, 0.0f);   // Segment 7 = Orange
                        else if (pixel == 8)
                            colors[idx] = new Color(0.5f, 0.0f, 0.5f); // Segment 8 = Purple
                        else if (pixel == 9)
                            colors[idx] = new Color(0.53f, 0.81f, 0.98f); // Segment 9 = LightSkyBlue
                        else
                            colors[idx] = Color.clear; // Transparent for undefined labels

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
            string assetPath = "Assets/Assignment_6_Assets/MyCS116A_3DTexture_LiverGallbladderSegments.asset"; // MARKED AS CHANGED (output includes gallbladder)
            AssetDatabase.CreateAsset(texture, assetPath);
            AssetDatabase.SaveAssets();

            // Confirmation log
            Debug.Log($"✅ 3D Texture saved to: {assetPath}");
            EditorUtility.DisplayDialog(
                "3D Texture Created",
                "Successfully created MyCS116A_3DTexture_LiverGallbladderSegments.asset!\n(1 Gallbladder + 8 Liver Segments)",
                "OK"
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Error creating 3D texture: {ex.Message}");
        }
    }
}
