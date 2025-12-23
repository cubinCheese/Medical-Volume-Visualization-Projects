using UnityEngine;
using itk.simple; 
public class Step_1_Loading_Volume_Imaging : MonoBehaviour
{
    // Path to your dataset file - update this to your actual file path
    public string datasetFile = "Assets/Liver_Mask_01.tif";

    void Start()
    {
        // create the SimpleITK ImageFileReader
        ImageFileReader imageFileReader = new ImageFileReader();

        // set the file name to load
        imageFileReader.SetFileName(datasetFile);

        // execute reading the volume image
        Image volImage = imageFileReader.Execute();

        // print the total number of pixels to the Unity console
        Debug.Log("Total number of pixels: " + volImage.GetNumberOfPixels());
    }
}
