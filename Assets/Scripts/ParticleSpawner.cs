using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using static System.MathF;
using Random = UnityEngine.Random;

public class ParticleSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public bool randomPosition;
    [Range(1, 100000)]
    public int maxNumParticles;
    [Range(1, 100000)]
    public int numParticles;
    public Vector2 spawnCenter;
    public Vector2 spawnSize;
    public float spacing;
    public Vector2 initialVelocity;

    [System.Serializable]
    public struct ParticlesData
    {
        public List<Vector2> positions;
        public List<Vector2> velocities;

        public ParticlesData(int num)
        {
            positions = new List<Vector2>();
            velocities = new List<Vector2>();
        }
        
        /*public ParticlesData(int num)
        {
            positions = new Vector2[num];
            velocities = new Vector2[num];
        }*/
    }

    public ParticlesData GetParticlesData()
    {
        numParticles = numParticles > maxNumParticles ? maxNumParticles : numParticles;
        ParticlesData particlesData = new ParticlesData(numParticles);
        if (randomPosition)
        {
            GetRandomPosition(ref particlesData);
        }
        else
        {
            GetPosition(ref particlesData);
        }
        for (int i = 0; i < numParticles; i++)
        {
            float x = (Random.value - 0.5f);
            float y = (Random.value - 0.5f);
            particlesData.velocities.Add(initialVelocity);
            //particlesData.velocities[i] = new Vector2(x, y);;
        }
        return particlesData;
    }

    void GetPosition(ref ParticlesData particlesData)
    {
        for (int idx = 0; idx < numParticles; idx++)
        {
            float particlesPerRow = (int)Sqrt(numParticles);
            float particlesPerCol = (numParticles - 1) / particlesPerRow + 1;
            float x = (idx % particlesPerRow - particlesPerRow / 2f + 0.5f) * spacing % spawnSize.x;
            float y = (idx / particlesPerRow - particlesPerCol / 2f + 0.5f) * spacing % spawnSize.y;
            particlesData.positions.Add(new Vector2(x, y) + spawnCenter);
        }
    }

    void GetRandomPosition(ref ParticlesData particlesData)
    {
        for (int idx = 0; idx < numParticles; idx++)
        {
            float x = (Random.value - 0.5f) * spawnSize.x;
            float y = (Random.value - 0.5f) * spawnSize.y;
            particlesData.positions.Add(new Vector2(x, y) + spawnCenter);
        }
    }
    
    private void OnDrawGizmos()
    {
        // Draw Bounds
        Gizmos.color = new Color(1, 1, 0.5f);
        Gizmos.DrawWireCube(spawnCenter, spawnSize);
    }
}
