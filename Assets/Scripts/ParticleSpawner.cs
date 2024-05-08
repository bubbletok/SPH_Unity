using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class ParticleSpawner : MonoBehaviour
{
    [Range(0, 100000)]
    public int numParticles;
    [Range(0.0f, 1.0f)]
    public float particleSpacing;
    public Color particleColor = new Color(0.3011303f, 0.7340082f, 1);
    public GameObject refParticle;

    [System.Serializable]
    public struct SpawnData
    {
        private Vector2[] positions;
        private Vector2[] velocities;
        [SerializeField] private float size;
        public void InitPositions(int value)
        {
            positions = new Vector2[value];
        }

        public void InitVelocities(int value)
        {
            velocities = new Vector2[value];
        }

        public void SetSize(float value)
        {
            size = value;
        }
        
        public Vector2[] Positions => positions;
        public Vector2[] Velocities => velocities;
        public float Size => size;
    };
    
    [SerializeField]
    private SpawnData _particles;

    public SpawnData GetSpawnData()
    {
        return _particles;
    }

    void Init()
    {
        if (numParticles <= 0)
        {
            numParticles = 1;
        }

        if (particleSpacing <= 0)
        {
            particleSpacing = 0;
        }
        // Create particle arrays
        _particles.InitPositions(numParticles);
        _particles.InitVelocities(numParticles);
        // Place particles in a grid formation
        int particlesPerRow = (int)Mathf.Sqrt(numParticles);
        int particlesPerCol = (numParticles - 1) / particlesPerRow + 1;
        float spacing = _particles.Size + particleSpacing;

        for (int i = 0; i < numParticles; i++)
        {
            float x = (i % particlesPerRow - particlesPerRow / 2f + 0.5f) * spacing;
            float y = (i / particlesPerRow - particlesPerCol / 2f + 0.5f) * spacing;
            _particles.Positions[i] = new Vector2(x, y);
            //particles[i] = Instantiate(refParticle, positions[i], Quaternion.identity);
        }
    }

    private void OnValidate()
    {
        Init();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = particleColor;
        for (int i = 0; i < _particles.Positions.Length; i++)
        {
            //Gizmos.DrawSphere(positions[i], particleSize);
            Vector2 scale = new Vector2(_particles.Size, _particles.Size);
            Gizmos.DrawMesh(refParticle.GetComponent<MeshFilter>().sharedMesh, _particles.Positions[i], quaternion.identity,
                scale);
        }
    }
}
