using UnityEngine;

public class SPH2D : MonoBehaviour
{
    private static readonly int NumParticles = Shader.PropertyToID("numParticles");
    private static readonly int SmoothingRadius = Shader.PropertyToID("smoothingRadius");
    private static readonly int Gravity = Shader.PropertyToID("gravity");
    private static readonly int CollisionDamping = Shader.PropertyToID("collisionDamping");
    private static readonly int DeltaTime = Shader.PropertyToID("deltaTime");
    private static readonly int TargetDensity = Shader.PropertyToID("targetDensity");
    private static readonly int PressureMultiplier = Shader.PropertyToID("pressureMultiplier");
    private static readonly int NearPressureMultiplier = Shader.PropertyToID("nearPressureMultiplier");
    private static readonly int ParticleMass = Shader.PropertyToID("particleMass");
    private static readonly int BoundsSize = Shader.PropertyToID("boundsSize");

    [Header("SPH2D Settings")]
    public bool runSimulationByFixedFrame;
    public int iterationsPerFrame;
    public int timeScale;
    public int maxNumParticles;
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
    [Range(0, 1000)]
    public float nearPressureMultiplier;
    [Range(0, 100)]
    public float particleMass;
    public Vector2 boundsSize;

    [Header("References")] public ComputeShader shader;
    public ParticleSpawner particleSpawner;
    public ParticleDisplay particleDisplay;
    public DensityDisplay densityDisplay;
    
    private ParticleSpawner.ParticlesData particles;
    [HideInInspector]
    public int numParticles;
    
    public ComputeBuffer PositionBuffer { get; private set; }
    public ComputeBuffer PredictedPositoinBuffer { get; private set; }
    public ComputeBuffer VelocityBuffer { get; private set; }
    public ComputeBuffer DensityBuffer { get; private set; }
    public ComputeBuffer PressureForceBuffer { get; private set; }
    public ComputeBuffer SpatialIndicesBuffer { get; private set; }
    public ComputeBuffer SpatialOffsetsBuffer { get; private set; }

    private GPUSort gpuSort;
    

    //public UnityEvent onReset;
    
    private const int CalculateExternalForces = 0;
    private const int UpdateSpatialHash = 1;
    private const int CalculateDensity = 2;
    private const int CalculatePressureForce = 3;
    private const int UpdatePosition = 4;

    private bool isPaused;
    private bool isNextFrame;
    
    /*public Vector2[] positions;
    public Vector2[] velocities;*/

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
        
        SpawnParticle();
        
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
        
        gpuSort = new();
        gpuSort.SetBuffers(SpatialIndicesBuffer, SpatialOffsetsBuffer);
    }

    void SpawnParticle()
    {
        particles = particleSpawner.GetParticlesData();
        numParticles = particles.positions.Count;
        numParticles = numParticles > maxNumParticles ? maxNumParticles : numParticles;
    }

    void SetProperty(float deltaTime)
    {   
        numParticles = particles.positions.Count;
        shader.SetInt(NumParticles, numParticles);
        shader.SetFloat(SmoothingRadius, smoothingRadius);
        shader.SetFloat(Gravity, gravity);
        shader.SetFloat(CollisionDamping, collisionDamping);
        shader.SetFloat(DeltaTime, deltaTime);
        shader.SetFloat(TargetDensity,targetDensity);
        shader.SetFloat(PressureMultiplier,pressureMultiplier);
        shader.SetFloat(NearPressureMultiplier,nearPressureMultiplier);
        shader.SetFloat(ParticleMass,particleMass);
        shader.SetVector(BoundsSize, boundsSize);
    }

    void CreateInitialBuffer()
    {
        PositionBuffer = new ComputeBuffer(maxNumParticles, sizeof(float) * 2);
        PredictedPositoinBuffer = new ComputeBuffer(maxNumParticles, sizeof(float) * 2);
        VelocityBuffer = new ComputeBuffer(maxNumParticles, sizeof(float) * 2);
        DensityBuffer = new ComputeBuffer(maxNumParticles, sizeof(float));
        PressureForceBuffer = new ComputeBuffer(maxNumParticles, sizeof(float) * 2);
        SpatialIndicesBuffer = new ComputeBuffer(maxNumParticles, sizeof(int) * 3);
        SpatialOffsetsBuffer = new ComputeBuffer(maxNumParticles, sizeof(int));
    }

    void ResetBufferData()
    {
        PositionBuffer.SetData(particles.positions);
        PredictedPositoinBuffer.SetData(particles.positions);
        VelocityBuffer.SetData(particles.velocities);
        
        //positions = new Vector2[numParticles];
        /*velocities = new Vector2[numParticles];
        densities = new float[numParticles];
        forces = new Vector2[numParticles];*/
        /*spatialLookup = new Vector2[numParticles];
        spatialIndices = new int[numParticles];*/
    }

    void SetBuffers()
    {
        SetComputeShaderBuffers(shader, PositionBuffer, "positions", CalculateExternalForces, UpdatePosition);
        SetComputeShaderBuffers(shader, PredictedPositoinBuffer, "predictedPositions",  UpdateSpatialHash, CalculateExternalForces, CalculateDensity, CalculatePressureForce);
        SetComputeShaderBuffers(shader, VelocityBuffer, "velocities", CalculateExternalForces, CalculatePressureForce, UpdatePosition);
        SetComputeShaderBuffers(shader, DensityBuffer, "densities", CalculateDensity, CalculatePressureForce);
        SetComputeShaderBuffers(shader, PressureForceBuffer,"pressureForces",CalculatePressureForce);
        SetComputeShaderBuffers(shader, SpatialIndicesBuffer, "SpatialIndices", UpdateSpatialHash);
        //SetComputeShaderBuffers(shader, SpatialOffsetsBuffer, "SpatialOffsets", UpdateSpatialHash);
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
        //gpuSort.SortAndCalculateOffsets();
        
        shader.Dispatch(CalculateDensity, numParticles, 1, 1);
        shader.Dispatch(CalculatePressureForce, numParticles, 1, 1);
        shader.Dispatch(UpdatePosition, numParticles, 1, 1);
        
        //positionBuffer.GetData(positions);
        
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
            SpawnParticle();
            SetProperty(1/60f);
            particleDisplay.Reset(this);
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
        PositionBuffer?.Release();
        PredictedPositoinBuffer?.Release();
        VelocityBuffer?.Release();
        DensityBuffer?.Release();
        PressureForceBuffer?.Release();
        SpatialIndicesBuffer?.Release();
        SpatialOffsetsBuffer?.Release();
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
