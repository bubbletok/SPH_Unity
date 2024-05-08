using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ParticleSpawner : MonoBehaviour
{
    public float gravity;
    public float collisionDamping;
    
    public Vector2 boundsSize;
    
    public float particleSize;
    public int numParticles;
    public float particleSpacing;

    public GameObject refParticle;
    public GameObject[] particles;

    public Color particleColor = new Color(0.3011303f, 0.7340082f, 1);
    
    private Vector2[] velocities;
    private Vector2[] positions;
    private void Start()
    {
    }

    void Init()
    {
        if (numParticles <= 0)
        {
            numParticles = 1;
        }
        // Create particle arrays
        positions = new Vector2[numParticles];
        velocities = new Vector2[numParticles];
        // Place particles in a grid formation
        int particlesPerRow = (int)Mathf.Sqrt(numParticles);
        int particlesPerCol = (numParticles - 1) / particlesPerRow + 1;
        float spacing = particleSize + particleSpacing;

        for (int i = 0; i < numParticles; i++)
        {
            float x = (i % particlesPerRow - particlesPerRow / 2f + 0.5f) * spacing;
            float y = (i / particlesPerRow - particlesPerCol / 2f + 0.5f) * spacing;
            positions[i] = new Vector2(x, y);
            //particles[i] = Instantiate(refParticle, positions[i], Quaternion.identity);
        }
    }

    void Update()
    {
        for (int i = 0; i < positions.Length; i++)
        {
            velocities[i] += Vector2.down * gravity * Time.deltaTime;
            positions[i] += velocities[i] * Time.deltaTime;
            ResolveCollisions(ref positions[i], ref velocities[i]);

            //DrawCircle(positions[i], particleSize, particleColor);
            //articles[i].transform.position = positions[i];
        }
    }
    
    
    void DrawCircle(Vector2 pos, float size, Color color)
    {
        /*particle.transform.position = pos;
        //particle.transform.localScale = new Vector2(size, size);
        SpriteRenderer sprite = particle.GetComponent<SpriteRenderer>();
        sprite.color = color;*/
    }
    
    void ResolveCollisions(ref Vector2 position, ref Vector2 velocity)
    {
        Vector2 halfBoundsSize = boundsSize / 2 - Vector2.one * particleSize;

        if (Mathf.Abs(position.x) > halfBoundsSize.x)
        {
            position.x = halfBoundsSize.x * Mathf.Sign(position.x);
            velocity.x *= -1 * collisionDamping;
        }

        if (Mathf.Abs(position.y) > halfBoundsSize.y)
        {
            position.y = halfBoundsSize.y * Mathf.Sign(position.y);
            velocity.y *= -1 * collisionDamping;
        }
    }

    private void OnValidate()
    {
        Init();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = particleColor;
        for (int i = 0; i < positions.Length; i++)
        {
            //Gizmos.DrawSphere(positions[i], particleSize);
            Vector2 scale = new Vector2(particleSize, particleSize);
            Gizmos.DrawMesh(refParticle.GetComponent<MeshFilter>().sharedMesh, positions[i], quaternion.identity,
                scale);
        }
        
        Gizmos.color = new Color(0, 1, 0.5f);
        Gizmos.DrawWireCube(Vector3.zero, boundsSize);
    }
}
