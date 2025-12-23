// Assignment 7: Direct Volume Rendering of DICOM Data
// Step 1: Load DICOM patient data and quick debug check

using UnityEngine;
using itk.simple;

public class DirectVolumeRendering : MonoBehaviour
{
    // Static folder path
    private string folderPath = "Assets/Assignment_7_Assets/PATIENT_DICOM";
    void Start()
    {
        LoadVolumeData();
    }

    public void LoadVolumeData()
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError("Folder path is empty! Please check the hardcoded path.");
            return;
        }

        // Load DICOM series
        var imageFileReader = new ImageSeriesReader();
        VectorString dicomNames = ImageSeriesReader.GetGDCMSeriesFileNames(folderPath);
        imageFileReader.SetFileNames(dicomNames);
        var volImage = imageFileReader.Execute();

        // Cast to Float32
        var volumeImage = SimpleITK.Cast(volImage, PixelIDValueEnum.sitkFloat32);

        // Print total number of pixels
        Debug.Log("Total number of pixels: " + volumeImage.GetNumberOfPixels());
    }
}
