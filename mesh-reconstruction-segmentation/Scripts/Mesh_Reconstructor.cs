// Step 2
// Reconstruct the 3D surface mesh from the volume data

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using itk.simple;
using g3; // Geometry3Sharp namespace

public class Mesh_Reconstructor : MonoBehaviour
{
    public string datasetFile = "Assets/Liver_Mask_01.tif"; // dataset file path

    private Image _volumeImage;

    void Start()
    {
        // Load volume image using SimpleITK ImageFileReader
        var reader = new ImageFileReader();
        reader.SetFileName(datasetFile);
        _volumeImage = reader.Execute();

        // Cast volume image to 32-bit float needed for buffer extraction
        var volumeImage = SimpleITK.Cast(_volumeImage, PixelIDValueEnum.sitkFloat32);

        int depth = (int)volumeImage.GetDepth();
        int height = (int)volumeImage.GetHeight();
        int width = (int)volumeImage.GetWidth();
        int length = width * height * depth;

        // Get pointer to image buffer as float array
        IntPtr bufferImg = volumeImage.GetBufferAsFloat();

        float[] bufferAsArrayImg = new float[length];
        Marshal.Copy(bufferImg, bufferAsArrayImg, 0, length);

        // Create dense grid for marching cubes
        DenseGrid3f grid = new DenseGrid3f(width, height, depth, 1);

        // Fill the grid with negated values for pixels > 0
        for (int k = 0; k < depth; k++)
        {
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    int idx = i + width * (j + height * k);
                    float pixel = bufferAsArrayImg[idx];

                    if (pixel > 0)
                    {
                        grid[idx] = -pixel; // assign value to liver grid
                    }
                }
            }
        }

        // Setup and run Marching Cubes
        double cellsize = volumeImage.GetSpacing()[0];
        double numcells = 64;
        var iso = new DenseGridTrilinearImplicit(grid, Vector3f.Zero, cellsize);

        MarchingCubes c = new MarchingCubes();
        c.Implicit = iso;
        c.RootMode = MarchingCubes.RootfindingModes.Bisection;
        c.RootModeSteps = 5;
        c.Bounds = iso.Bounds();
        c.CubeSize = c.Bounds.MaxDim / numcells;
        c.Bounds.Expand(3 * c.CubeSize);
        c.Generate();

        var meshResult = c.Mesh;

        // Flip mesh for Unity's left-hand coordinate system
        MeshTransforms.FlipLeftRightCoordSystems(meshResult);
        // Compute normals for shading
        MeshNormals.QuickCompute(meshResult);

        // Create GameObject with mesh components to display
        GameObject liverObject = new GameObject("Liver");
        liverObject.transform.parent = this.gameObject.transform;

        MeshFilter mf = liverObject.AddComponent<MeshFilter>();
        MeshRenderer mr = liverObject.AddComponent<MeshRenderer>();

        mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        // Assign mesh to GameObject
        g3UnityUtils.SetGOMesh(liverObject, meshResult);
    }
}
