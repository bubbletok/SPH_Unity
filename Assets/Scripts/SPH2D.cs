using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class SPH2D : MonoBehaviour
{
    [FormerlySerializedAs("runSimulationByFrame")] [FormerlySerializedAs("runSimulationByStep")] [Header("SPH2D Settings")]
    public bool runSimulationByFixedFrame;
    public int iterationsPerFrame;
    public int timeScale;
    [Range(0, 50)]
    public float gravity;
    [Range(0, 1)]
    public float collisionDamping;
    [Range(0, 1000)]
    public float smoothingRadius;
    [Range(0, 1000)]
    public float targetDensity;
    [Range(0, 1000)]
    public float pressureMultiplier;
    [Range(0, 100)]
    public float particleMass;
    public Vector2 boundsSize;

    [Header("References")] public ComputeShader shader;
    public ParticleSpawner particleSpawner;
    public ParticleDisplay particleDisplay;
    public DensityDisplay densityDisplay;
    
    private ParticleSpawner.ParticlesData particles;
    private int numParticles;
    
    public ComputeBuffer positionBuffer { get; private set; }
    public ComputeBuffer predictedPositoinBuffer { get; private set; }
    public ComputeBuffer velocityBuffer { get; private set; }
    public ComputeBuffer densityBuffer { get; private set; }
    public ComputeBuffer pressureForceBuffer { get; private set; }
    public ComputeBuffer spatialLookupBuffer { get; private set; }
    public ComputeBuffer spatialIndicesBuffer { get; private set; }

    //public UnityEvent onReset;
    
    private const int CalculateExternalForces = 0;
    private const int UpdateSpatialHash = 1;
    private const int CalculateDensity = 2;
    private const int CalculatePressureForce = 3;
    private const int UpdatePosition = 4;

    private bool isPaused;
    private bool isNextFrame;
    
    public Vector2[] positions;
    public Vector2[] velocities;

    [Header("Debug variables")]
    /*public float[] densities;
    public Vector2[] forces;*/
    public Vector2[] spatialLookup;
    public int[] spatialIndices;

    private void Awake()
    {
        print($"Space bar: Play/Stop, Right Arrow: Next Frame, R: Reset");
    }

    private void Start()
    {
        Init();
    }
    
    void Init()
    {
        isPaused = true;
        particles = particleSpawner.GetParticlesData();
        /*positions = particles.positions;
        velocities = particles.velocities;*/
        
        SetProperty(1/60f);
        CreateInitialBuffer();
        ResetBufferData();
        SetBuffers();
        
        //onReset.AddListener(SetProperty);
        //onReset.AddListener(SetInitialBuffer);
        //onReset.Invoke();
        
        particleDisplay.Init(this);
        densityDisplay.Init(this);
    }

    void SetProperty(float deltaTime)
    {   
        numParticles = particles.positions.Count;
        shader.SetInt("numParticles", numParticles);
        shader.SetFloat("smoothingRadius", smoothingRadius);
        shader.SetFloat("gravity", gravity);
        shader.SetFloat("collisionDamping", collisionDamping);
        shader.SetFloat("deltaTime", deltaTime);
        shader.SetFloat("targetDensity",targetDensity);
        shader.SetFloat("pressureMultiplier",pressureMultiplier);
        shader.SetFloat("particleMass",particleMass);
        shader.SetVector("boundsSize", boundsSize);
    }

    void CreateInitialBuffer()
    {
        positionBuffer = new ComputeBuffer(numParticles, sizeof(float) * 2);
        predictedPositoinBuffer = new ComputeBuffer(numParticles, sizeof(float) * 2);
        velocityBuffer = new ComputeBuffer(numParticles, sizeof(float) * 2);
        densityBuffer = new ComputeBuffer(numParticles, sizeof(float));
        pressureForceBuffer = new ComputeBuffer(numParticles, sizeof(float) * 2);
        spatialLookupBuffer = new ComputeBuffer(numParticles, sizeof(int) * 2);
        spatialIndicesBuffer = new ComputeBuffer(numParticles, sizeof(int));
    }

    void ResetBufferData()
    {
        positionBuffer.SetData(particles.positions);
        predictedPositoinBuffer.SetData(particles.positions);
        velocityBuffer.SetData(particles.velocities);
        
        positions = new Vector2[numParticles];
        /*velocities = new Vector2[numParticles];
        densities = new float[numParticles];
        forces = new Vector2[numParticles];*/
        /*spatialLookup = new Vector2[numParticles];
        spatialIndices = new int[numParticles];*/
    }

    void SetBuffers()
    {
        SetComputeShaderBuffers(shader, positionBuffer, "positions", CalculateExternalForces, UpdatePosition);
        SetComputeShaderBuffers(shader, predictedPositoinBuffer, "predictedPositions",  UpdateSpatialHash, CalculateExternalForces, CalculateDensity, CalculatePressureForce);
        SetComputeShaderBuffers(shader, velocityBuffer, "velocities", CalculateExternalForces, CalculatePressureForce, UpdatePosition);
        SetComputeShaderBuffers(shader, densityBuffer, "densities", CalculateDensity, CalculatePressureForce);
        SetComputeShaderBuffers(shader, pressureForceBuffer,"pressureForces",CalculatePressureForce);
        SetComputeShaderBuffers(shader, spatialLookupBuffer, "SpatialLookup", UpdateSpatialHash);
        SetComputeShaderBuffers(shader, spatialIndicesBuffer, "SpatialIndices", UpdateSpatialHash);
    }

    void SetComputeShaderBuffers(ComputeShader computeShader, ComputeBuffer buffer, string id, params int[] kernels)
    {
        for (int i = 0; i < kernels.Length; i++)
        {
            computeShader.SetBuffer(kernels[i], id, buffer);
        }
    }

    private void FixedUpdate()
    {
        if (runSimulationByFixedFrame)
        {
            SimulationFrame(Time.fixedDeltaTime);
        }
        if (isNextFrame)
        {
            isPaused = true;
            isNextFrame = false;
        }
    }

    void Update()
    {
        if (!runSimulationByFixedFrame)
        {
            SimulationFrame(Time.deltaTime);
        }
        HandleSimulationInput();
    }

    void SimulationFrame(float frame)
    {
        if (isPaused) return;
        
        float timeStep = frame / iterationsPerFrame * timeScale;
        SetProperty(timeStep);
        for (int i = 0; i < iterationsPerFrame; i++)
        {
            SimulationStep();
        }
    }

    void SimulationStep()
    {
        shader.Dispatch(CalculateExternalForces, numParticles, 1, 1);
        shader.Dispatch(UpdateSpatialHash, numParticles, 1, 1);
        shader.Dispatch(CalculateDensity, numParticles, 1, 1);
        shader.Dispatch(CalculatePressureForce, numParticles, 1, 1);
        shader.Dispatch(UpdatePosition, numParticles, 1, 1);
        
        positionBuffer.GetData(positions);
        
        //predictedPositoinBuffer.GetData(positions);
        /*velocityBuffer.GetData(velocities);
        densityBuffer.GetData(densities);
        pressureForceBuffer.GetData(forces);*/
        /*spatialLookupBuffer.GetData(spatialLookup);
        spatialIndicesBuffer.GetData(spatialIndices);*/
    }

    void HandleSimulationInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPaused = !isPaused;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            isPaused = false;
            isNextFrame = true;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            //particles.positions.Add(GetNewParticle());
            /*SetProperty(1/60f);
            ReleaseBuffers();
            CreateInitialBuffer();*/
            ResetBufferData();
            //SetBuffers();
            SimulationStep();
            ResetBufferData();
            //particleDisplay.Reset();
            //particleDisplay.Init(this);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetBufferData();
            SimulationStep();
            ResetBufferData();
        }

        /*if (Input.GetKeyDown(KeyCode.Q))
        {
            bRunSimulation = false;
            /*particles.positions.Append(new Vector2(0, 0));
            numParticles = particles.positions.Length;#1#
            onReset.Invoke();
            bRunSimulation = true;
        }*/
    }

    Vector2 GetNewParticle()
    {
        float x = (Random.value - 0.5f) * boundsSize.x;
        float y = (Random.value - 0.5f) * boundsSize.y;
        return new Vector2(x, y);
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
        print("buffers released");
    }

    void ReleaseBuffers()
    {
        if(positionBuffer!=null) positionBuffer.Release();
        if(predictedPositoinBuffer!=null) predictedPositoinBuffer.Release();
        if(velocityBuffer!=null) velocityBuffer.Release();
        if(densityBuffer!=null) densityBuffer.Release();
        if(pressureForceBuffer!=null) pressureForceBuffer.Release();
        if(spatialLookupBuffer!=null) spatialLookupBuffer.Release();
        if(spatialIndicesBuffer!=null) spatialIndicesBuffer.Release();
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 1, 0.2f);
        Gizmos.DrawWireCube(Vector3.zero, boundsSize);

        /*Gizmos.color = new Color(0.5f, 0,1f);
        for (int i = 0; i < numParticles; i++)
        {
            Gizmos.DrawSphere(positions[i],0.01f);
        }*/
    }
}
