// Step 4 : Mesh Decimation to reduce triangle count

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using itk.simple;
using g3; // Geometry3Sharp namespace

public class Mesh_Reconstructor_With_Decimation : MonoBehaviour
{
    public string datasetFile = "Assets/Liver_Mask_01.tif"; // Dataset file path

    private Image _volumeImage;

    void Start()
    {
        // Load volume image using SimpleITK
        var reader = new ImageFileReader();
        reader.SetFileName(datasetFile);
        _volumeImage = reader.Execute();

        // Cast volume image to 32-bit float for buffer extraction
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

        // Fill grid with negated values for pixels > 0
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
                        grid[idx] = -pixel;
                    }
                }
            }
        }

        // Setup and run Marching Cubes
        double cellsize = volumeImage.GetSpacing()[0];
        double numcells = 64;
        var iso = new DenseGridTrilinearImplicit(grid, Vector3f.Zero, cellsize);

        MarchingCubes mc = new MarchingCubes();
        mc.Implicit = iso;
        mc.RootMode = MarchingCubes.RootfindingModes.Bisection;
        mc.RootModeSteps = 5;
        mc.Bounds = iso.Bounds();
        mc.CubeSize = mc.Bounds.MaxDim / numcells;
        mc.Bounds.Expand(3 * mc.CubeSize);
        mc.Generate();

        var meshResult = mc.Mesh;

        // Mesh smoothing
        float EdgeLengthMultiplier = 5.5f;
        int remeshPasses = 20;
        float smoothSpeedT = 1.0f;

        Remesher remesh = new Remesher(meshResult);
        remesh.PreventNormalFlips = true;
        remesh.SetTargetEdgeLength(EdgeLengthMultiplier);
        remesh.SmoothSpeedT = smoothSpeedT;
        remesh.SetProjectionTarget(MeshProjectionTarget.Auto(meshResult));

        for (int i = 0; i < remeshPasses; ++i)
        {
            remesh.BasicRemeshPass();
        }

        // Newly introducing Mesh Decimation step 
        // Mesh decimation - reduce triangle count to target (try adjusting target count)
        Reducer reducer = new Reducer(meshResult);
        reducer.ReduceToTriangleCount(2000);

        // Flip and compute normals
        MeshTransforms.FlipLeftRightCoordSystems(meshResult);
        MeshNormals.QuickCompute(meshResult);

        // Create and setup GameObject to display mesh
        GameObject liverObject = new GameObject("Liver");
        liverObject.transform.parent = this.gameObject.transform;

        MeshFilter mf = liverObject.AddComponent<MeshFilter>();
        MeshRenderer mr = liverObject.AddComponent<MeshRenderer>();

        mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        // Assign mesh to GameObject
        g3UnityUtils.SetGOMesh(liverObject, meshResult);
    }
}
