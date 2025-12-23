// DirectVolumeRendering_step5.cs
// Assignment 7: Direct Volume Rendering of DICOM Data
// Step 5: Voxel Filtering

using itk.simple;
using System.Threading.Tasks;
using UnityEngine;
using UnityVolumeRendering;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;

public class DirectVolumeRendering_step5 : MonoBehaviour
{
    // Static folder path
    private string folderPath = "Assets/Assignment_7_Assets/PATIENT_DICOM";

    // Step 4: Transfer function path (assign in inspector or hardcode)
    public string colorMapPath = "Assets/Assignment_7_Assets/erdc_rainbow_dark.tf";

    // Struct to store transfer function data
    [Serializable]
    private struct TF1DSerialisationData
    {
        public List<TFColourControlPoint> colourPoints;
        public List<TFAlphaControlPoint> alphaPoints;
    }

    void Start()
    {
        // Build absolute path to Assets/Assignment_7_Assets/PATIENT_DICOM
        folderPath = Path.Combine(Application.dataPath, "Assignment_7_Assets", "PATIENT_DICOM");

        var volumeImage = LoadVolumeData();
        if (volumeImage == null)
        {
            Debug.LogError($"Failed to load volume image from path: {folderPath}");
            return;
        }

        // Fire-and-forget async call
        _ = LoadVolumeDataAsRenderedObject(volumeImage);
    }

    public itk.simple.Image LoadVolumeData()
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError("Folder path is empty! Please check the hardcoded path.");
            return null;
        }

        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"DICOM folder not found at path: {folderPath}");
            return null;
        }

        // Load DICOM series
        var imageFileReader = new ImageSeriesReader();
        VectorString dicomNames = ImageSeriesReader.GetGDCMSeriesFileNames(folderPath);
        imageFileReader.SetFileNames(dicomNames);
        var volImage = imageFileReader.Execute();

        // Cast to Float32
        var _volumeImage = SimpleITK.Cast(volImage, PixelIDValueEnum.sitkFloat32);

        // Print total number of pixels
        Debug.Log("Total number of pixels: " + _volumeImage.GetNumberOfPixels());

        return _volumeImage;
    }

    public async Task<VolumeRenderedObject> LoadVolumeDataAsRenderedObject(itk.simple.Image volumeImage)
    {
        // Read image buffer dimensions
        int nx = (int)volumeImage.GetWidth();
        int ny = (int)volumeImage.GetHeight();
        int nz = (int)volumeImage.GetDepth();
        int numPixels = nx * ny * nz;

        // Get full image buffer
        float[] voxelData = new float[numPixels];
        IntPtr bufferImg = volumeImage.GetBufferAsFloat();
        Marshal.Copy(bufferImg, voxelData, 0, numPixels);

        // Step 5: Voxel Filtering (only keep pixels > 0)
        for (int i = 0; i < numPixels; i++)
        {
            if (voxelData[i] <= 0f)
            {
                voxelData[i] = 0f; // zero out invisible voxels
            }
        }

        // Create dataset
        VolumeDataset dataset = new VolumeDataset
        {
            data = voxelData,
            dimX = nx,
            dimY = ny,
            dimZ = nz
        };

        // Create volume rendered object
        var volRendObject = await VolumeObjectFactory.CreateObjectAsync(dataset);

        // Step 4: Transfer Function & Lighting
        var newTF = LoadTransferFunction(colorMapPath);
        if (newTF != null)
            volRendObject.SetTransferFunction(newTF);

        await volRendObject.SetLightingEnabledAsync(true);

        // Step 5: Adjust visibility window to 0 - 1
        volRendObject.SetVisibilityWindow(new Vector2(0f, 1f));

        return volRendObject;
    }

    // Step 4: Load transfer function from JSON-style preset
    public UnityVolumeRendering.TransferFunction LoadTransferFunction(string colorMapPath)
    {
        if (!File.Exists(colorMapPath))
        {
            Debug.LogError($"File does not exist: {colorMapPath}");
            return null;
        }

        string colorMap = File.ReadAllText(colorMapPath);
        TF1DSerialisationData data = JsonUtility.FromJson<TF1DSerialisationData>(colorMap);

        // Create TransferFunction
        var tf = new UnityVolumeRendering.TransferFunction
        {
            colourControlPoints = data.colourPoints,
            alphaControlPoints = data.alphaPoints
        };

        return tf;
    }
}
