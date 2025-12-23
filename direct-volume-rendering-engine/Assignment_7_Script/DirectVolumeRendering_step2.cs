// Assignment 7: Direct Volume Rendering of DICOM Data
// Step 2: Config Volume Rendering Object from loaded DICOM data

using itk.simple;
using System.Threading.Tasks;
using UnityEngine;
using UnityVolumeRendering;
using System;
using System.Runtime.InteropServices;
using System.IO;

public class DirectVolumeRendering_step2 : MonoBehaviour
{
    // Static folder path
    private string folderPath = "Assets/Assignment_7_Assets/PATIENT_DICOM";
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

        // Fire-and-forget async call (optional to await in async Start)
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
        // First read the image buffer and store in a float array
        int nx = (int)volumeImage.GetWidth();
        int ny = (int)volumeImage.GetHeight();
        int nz = (int)volumeImage.GetDepth();
        int numPixels = nx * ny * nz;
        float[] voxelData = new float[numPixels];
        Vector3Int dimension = new Vector3Int(nx, ny, nz);    

        IntPtr bufferImg = volumeImage.GetBufferAsFloat();        
        Marshal.Copy(bufferImg, voxelData, 0, numPixels);
        Debug.Log(voxelData.Length); //Print the length to debug   

        // Import voxel data based on the float array to VolumeDataset to work with UnityVolumeRendering
        VolumeDataset dataset = new VolumeDataset();
        dataset.data = voxelData;
        dataset.dimX = dimension.x;
        dataset.dimY = dimension.y;
        dataset.dimZ = dimension.z;

        // Based on this volume dataset, you then now can load and create Unity GameObject to render it.
        if (dataset != null)
        {
            // Load Volume Data and Create GameObject
            Debug.Log("Loading dataset");
            var volRendObject = await VolumeObjectFactory.CreateObjectAsync(dataset);

            return volRendObject;
        }
        else
        {
            Debug.Log("Dataset not found!");
            return null;
        }
    }
}
