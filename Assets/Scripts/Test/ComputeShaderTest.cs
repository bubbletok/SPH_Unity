using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ComputeShaderTest : MonoBehaviour
{
    public ComputeShader shader;

    public RenderTexture texture;
    // Start is called before the first frame update
    public int nums;
    public float scale;
    public Vector2 bounds;
    private float[] randoms;

    private Vector2[] positions;
    void Start()
    {
        /*
        texture = new RenderTexture(256, 256, 24);
        texture.enableRandomWrite = true;
        texture.Create();

        shader.SetTexture(0, "Result", texture);
        shader.Dispatch(0,texture.width/8,texture.height/8,1);
    */
        
    }

    private void Update()
    {
        //UpdatePosWithComputeShader();
        if (Input.GetMouseButtonDown(0))
        {
            UpdatePosCPU();
        }
        if (Input.GetMouseButtonDown(1))
        {
            UpdatePosGPU();
        }
    }
    void Init()
    {
        positions = new Vector2[nums];
        for (int i = 0; i < nums; i++)
        {
            float x = (Random.value - 0.5f) * bounds.x;
            float y = (Random.value - 0.5f) * bounds.y;
            positions[i] = new Vector2(x, y);
        }

        /*randoms = new float[nums*2];*/
    }
    
    void UpdatePosCPU()
    {
        for (int i = 0; i < nums; i++)
        {
            float x = (Random.value - 0.5f) * bounds.x;
            float y = (Random.value - 0.5f) * bounds.y;
            positions[i] = new Vector2(x, y);
        }
    }

    void UpdatePosGPU()
    {
        /*for (int i = 0; i < nums*2; i++)
        {
            randoms[i] = Random.value;
        }*/
        int vectorSize = sizeof(float) * 2;
        ComputeBuffer positionBuffer = new ComputeBuffer(positions.Length, vectorSize);
        positionBuffer.SetData(positions);
        shader.SetBuffer(0,"positions",positionBuffer);

        /*ComputeBuffer randomBuffer = new ComputeBuffer(randoms.Length, sizeof(float));
        randomBuffer.SetData(randoms);
        shader.SetBuffer(0, "random", randomBuffer);*/
        shader.SetFloat("time",Time.deltaTime);
        
        shader.SetInt("nums",nums);
        shader.SetVector("bounds", bounds);
        
        shader.Dispatch(0, positions.Length/32, 1, 1);
        
        positionBuffer.GetData(positions);
    }

    private void OnValidate()
    {
        Init();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        for (int i = 0; i < nums; i++)
        {
            Gizmos.DrawSphere(positions[i],scale);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector2.zero,bounds);
    }
}
