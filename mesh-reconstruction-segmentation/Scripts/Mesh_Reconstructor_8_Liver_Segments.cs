// Step 5 : Mesh reconstruction for 8 liver segments + 1 Gallbladder segment

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using itk.simple;
using g3; // Geometry3Sharp namespace

public class Mesh_Reconstructor_8_Liver_Segments : MonoBehaviour
{
    public string datasetFile = "Assets/Liver_Mask_01.tif"; // Path to dataset file

    private Image _volumeImage;
    private int depth, height, width, length;
    private float[] bufferAsArrayImg;

    void Start()
    {
        // Load volume image using SimpleITK
        var reader = new ImageFileReader();
        reader.SetFileName(datasetFile);
        _volumeImage = reader.Execute();

        // Cast volume image to 32-bit float for buffer extraction
        var volumeImage = SimpleITK.Cast(_volumeImage, PixelIDValueEnum.sitkFloat32);

        depth = (int)volumeImage.GetDepth();
        height = (int)volumeImage.GetHeight();
        width = (int)volumeImage.GetWidth();
        length = width * height * depth;

        // Get volume image buffer array
        IntPtr bufferImg = volumeImage.GetBufferAsFloat();
        bufferAsArrayImg = new float[length];
        Marshal.Copy(bufferImg, bufferAsArrayImg, 0, length);

        // Create a dense grid for each segment (1 through 8)
        // [ADDED] Include 9th segment for gallbladder
        int numSegments = 9; // 8 liver segments + 1 gallbladder
        List<DenseGrid3f> segmentGrids = new List<DenseGrid3f>();
        for (int seg = 1; seg <= numSegments; seg++)
        {
            segmentGrids.Add(new DenseGrid3f(width, height, depth, 1));
        }

        // For each voxel, assign pixel value to corresponding segment grid
        for (int k = 0; k < depth; k++)
        {
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    int idx = i + width * (j + height * k);
                    float pixel = bufferAsArrayImg[idx];
                    int segmentIndex = (int)pixel - 1; // segment 1 maps to index 0, etc.

                    if (segmentIndex >= 0 && segmentIndex < numSegments) // [ADDED] updated to include gallbladder
                    {
                        // Assign negated pixel to segment's grid for marching cubes
                        segmentGrids[segmentIndex][idx] = -pixel;
                    }
                }
            }
        }

        double cellsize = volumeImage.GetSpacing()[0];
        double numcells = 64;

        // Loop through segment grids, reconstruct meshes, create objects
        for (int seg = 0; seg < numSegments; seg++) // [ADDED] loop up to 9 instead of 8
        {
            var grid = segmentGrids[seg];
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

            MeshTransforms.FlipLeftRightCoordSystems(meshResult);
            MeshNormals.QuickCompute(meshResult);

            // Create GameObject per segment
            // [ADDED] Give gallbladder a specific name
            string segmentName = (seg == 8) ? "Gallbladder" : $"LiverSegment_{seg + 1}";
            GameObject segObject = new GameObject(segmentName);
            segObject.transform.parent = this.gameObject.transform;

            MeshFilter mf = segObject.AddComponent<MeshFilter>();
            MeshRenderer mr = segObject.AddComponent<MeshRenderer>();

            // Assign mesh
            g3UnityUtils.SetGOMesh(segObject, meshResult);

            // Assign random color material for each segment - so we can differentiate
            // [ADDED] Give gallbladder a distinct color (green for visibility)
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = (seg == 8) ? Color.green : UnityEngine.Random.ColorHSV();
            mr.material = mat;
        }

        // [ADDED] Confirmation log
        Debug.Log("✅ Mesh reconstruction complete: 8 liver segments + 1 gallbladder segment.");
    }
}
