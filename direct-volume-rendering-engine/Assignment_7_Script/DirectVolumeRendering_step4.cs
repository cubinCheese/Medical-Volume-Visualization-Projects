// Assignment 7: Direct Volume Rendering of DICOM Data
// Step 2 & 4: Config Volume Rendering Object from loaded DICOM data + Transfer Function Preset

// In step 3, we manually configured transfer function (coloring) and enabling lighting
// In step 4, we load a preset transfer function from givenfile and set visibility window

using itk.simple;
using System.Threading.Tasks;
using UnityEngine;
using UnityVolumeRendering;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;

public class DirectVolumeRendering_step4 : MonoBehaviour
{
    // Static folder path
    private string folderPath = "Assets/Assignment_7_Assets/PATIENT_DICOM";

    // Step 4: Transfer function path (assign in inspector or hardcode)
    public string colorMapPath = "Assets/Assignment_7_Assets/erdc_rainbow_dark.tf";

    // Step 4: Visibility window
    public Vector2 visibilityWindow = new Vector2(0.6f, 1f);

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
        // Read image buffer
        int nx = (int)volumeImage.GetWidth();
        int ny = (int)volumeImage.GetHeight();
        int nz = (int)volumeImage.GetDepth();
        int numPixels = nx * ny * nz;
        float[] voxelData = new float[numPixels];
        Vector3Int dimension = new Vector3Int(nx, ny, nz);    

        IntPtr bufferImg = volumeImage.GetBufferAsFloat();        
        Marshal.Copy(bufferImg, voxelData, 0, numPixels);
        Debug.Log(voxelData.Length); // Debug

        // Create dataset
        VolumeDataset dataset = new VolumeDataset();
        dataset.data = voxelData;
        dataset.dimX = dimension.x;
        dataset.dimY = dimension.y;
        dataset.dimZ = dimension.z;

        // Create volume rendered object
        if (dataset != null)
        {
            Debug.Log("Loading dataset");
            var volRendObject = await VolumeObjectFactory.CreateObjectAsync(dataset);

            // Step 4: Transfer Function & Lighting 

            // Load transfer function preset
            var newTF = LoadTransferFunction(colorMapPath);
            if (newTF != null)
                volRendObject.SetTransferFunction(newTF);

            // Enable lighting asynchronously
            await volRendObject.SetLightingEnabledAsync(true);

            // Set visibility window
            volRendObject.SetVisibilityWindow(visibilityWindow);

            return volRendObject;
        }
        else
        {
            Debug.Log("Dataset not found!");
            return null;
        }
    }

    // Step 4: Load transfer function from JSON-style preset
    public UnityVolumeRendering.TransferFunction LoadTransferFunction(string colorMapPath)
    {
        if (!File.Exists(colorMapPath))
        {
            Debug.LogError(string.Format("File does not exist: {0}", colorMapPath));
            return null;
        }

        string colorMap = File.ReadAllText(colorMapPath);

        TF1DSerialisationData data = JsonUtility.FromJson<TF1DSerialisationData>(colorMap);

        // Debug
        Debug.Log(data.colourPoints.ToString());
        Debug.Log(data.alphaPoints.ToString());

        // Create TransferFunction
        var tf = new UnityVolumeRendering.TransferFunction();
        tf.colourControlPoints = data.colourPoints;
        tf.alphaControlPoints = data.alphaPoints;

        return tf;
    }
}
