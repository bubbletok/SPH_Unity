using System;
using UnityEngine;


public class ParticleDisplay : MonoBehaviour
{
    public Mesh mesh;
    public Shader shader;
    public float scale;
    public Gradient colourMap;
    public int gradientResolution;
    public float velocityDisplayMax;
    
    public Material material;
    private Bounds bounds;
    Texture2D gradientTexture;

    private ComputeBuffer argsBuffer;

    private bool bNeedUpdate;
    private bool bCanDraw;
    public void Init(SPH2D sph)
    {
        InitData(sph);
        
        bounds = new Bounds(Vector3.zero, Vector3.one * 10000);

        bCanDraw = true;

        //sph.onReset.AddListener(ReleaseBuffers);
    }

    void InitData(SPH2D sph)
    {
        material = new Material(shader);

        ResetBufferData(sph);

        argsBuffer = CreateArgBuffer(mesh, sph.numParticles);
    }
    
    public void Update()
    {
        if (bNeedUpdate)
        {
            UpdateSettings();
            bNeedUpdate = false;
        }
        if (bCanDraw)
        {
            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
        }
    }

    void UpdateSettings()
    {
        if (material == null) return;
        
        TextureFromGradient(ref gradientTexture, gradientResolution, colourMap);
        material.SetTexture("ColourMap", gradientTexture);

        material.SetFloat("scale", scale);
        material.SetFloat("velocityMax", velocityDisplayMax);
    }
    
    public static void TextureFromGradient(ref Texture2D texture, int width, Gradient gradient, FilterMode filterMode = FilterMode.Bilinear)
    {
        if (texture == null)
        {
            texture = new Texture2D(width, 1);
        }
        else if (texture.width != width)
        {
            texture.Reinitialize(width, 1);
        }
        if (gradient == null)
        {
            gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.black, 1) },
                new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
            );
        }
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = filterMode;

        Color[] cols = new Color[width];
        for (int i = 0; i < cols.Length; i++)
        {
            float t = i / (cols.Length - 1f);
            cols[i] = gradient.Evaluate(t);
        }
        texture.SetPixels(cols);
        texture.Apply();
    }
    
    ComputeBuffer CreateArgBuffer(Mesh drawMesh, int instanceCount)
    {
        int subMeshIndex = 0;
        uint[] argsData = new uint[5];

        argsData[0] = (uint)drawMesh.GetIndexCount(subMeshIndex);
        argsData[1] = (uint)instanceCount;
        argsData[2] = (uint)drawMesh.GetIndexStart(subMeshIndex);
        argsData[3] = (uint)drawMesh.GetBaseVertex(subMeshIndex);
        argsData[4] = 0;

        ComputeBuffer argBuffer = new ComputeBuffer(1, sizeof(int) * 5, ComputeBufferType.IndirectArguments);
        argBuffer.SetData(argsData);

        return argBuffer;
    }

    public void Reset(SPH2D sph)
    {
        bCanDraw = false;
        ReleaseBuffers();
        material = null;
        InitData(sph);
        UpdateSettings();
        bCanDraw = true;
    }
    
    public void ResetBufferData(SPH2D sph)
    {
        material.SetBuffer("Positions2D", sph.PositionBuffer);
        material.SetBuffer("Velocities", sph.VelocityBuffer);
        material.SetBuffer("Densities", sph.DensityBuffer);
    }

    
    void OnDestroy()
    {
        ReleaseBuffers();
    }

    void ReleaseBuffers()
    {
        if(argsBuffer!=null) argsBuffer.Release();
    }

    private void OnValidate()
    {
        bNeedUpdate = true;
    }

    /*private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < positions.Length; i++)
        {
            Gizmos.DrawSphere(positions[i],0.1f);
        }
    }*/
}
