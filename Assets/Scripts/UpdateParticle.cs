using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using static System.MathF;
using UnityEngine;

public class UpdateParticle : MonoBehaviour
{
    public float gravity;
    [Range(0.0f, 2.0f)]
    public float collisionDamping;
    public Vector2 boundsSize;

    [Header("Smoothing Radius For Density")]
    [Range(0.0f, 100.0f)]
    public float smoothingRadius;

    public Vector2 densityPoistion;

    public TMP_Text densityText;
    private ParticleSpawner _particleSpawner;
    private void Awake()
    {
        _particleSpawner = GetComponent<ParticleSpawner>();
    }
    
    
    void Update()
    {
        var spwanData = _particleSpawner.GetSpawnData();
        Vector2[] particlePosition = spwanData.Positions;
        Vector2[] particleVelocity = spwanData.Velocities;
        float size = spwanData.Size;
        for (int i = 0; i < particlePosition.Length; i++)
        {
            particleVelocity[i] += Vector2.down * gravity * Time.deltaTime;
            particlePosition[i] += particleVelocity[i] * Time.deltaTime;
            ResolveCollisions(ref particlePosition[i], ref particleVelocity[i], size);
        }

        if (Input.GetMouseButton(0))
        {
            densityPoistion = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        var density = CalculateDensity(densityPoistion);
        densityText.text = "Density: " + density;
    }

    static float Smoothingkernel(float radius, float dist)
    {
        float volume = PI * Pow(radius, 8) / 4;
        float value = Max(0, radius * radius - dist * dist);
        return value * value * value / volume;
    }

    float CalculateDensity(Vector2 samplePoint)
    {
        float density = 0;
        const float mass = 1;
        var spwanData = _particleSpawner.GetSpawnData();
        foreach (Vector2 position in spwanData.Positions)
        {
            float dist = (position - samplePoint).magnitude;
            float influence = Smoothingkernel(smoothingRadius, dist);
            density += mass * influence;
        }
        return density;
    }
    
    void ResolveCollisions(ref Vector2 position, ref Vector2 velocity, float particleSize)
    {
        Vector2 halfBoundsSize = boundsSize / 2 - Vector2.one * particleSize;

        if (Abs(position.x) > halfBoundsSize.x)
        {
            position.x = halfBoundsSize.x * Sign(position.x);
            velocity.x *= -1 * collisionDamping;
        }

        if (Abs(position.y) > halfBoundsSize.y)
        {
            position.y = halfBoundsSize.y * Sign(position.y);
            velocity.y *= -1 * collisionDamping;
        }
    }

    private void OnDrawGizmos()
    {
        // Draw Bounds
        Gizmos.color = new Color(0, 1, 0.5f);
        Gizmos.DrawWireCube(Vector3.zero, boundsSize);
        
        // Draw Density Circle
        Gizmos.color = new Color(0, 0.7f, .7f);
        Gizmos.DrawWireSphere(densityPoistion, smoothingRadius);
    }
}