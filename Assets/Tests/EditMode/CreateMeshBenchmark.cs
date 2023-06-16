using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using Unity.Physics;
using UnityEditor;
using UnityEngine;
using Collider = Unity.Physics.Collider;

public class ColliderSystemTests : ECSTestsFixture
{
    [Test]
    [Performance]
    public void CreateMeshColliderBenchmark()
    {
        // GenerateMesh(out var vertices, out var triangles);
        LoadMesh(out var vertices, out var triangles);

        Measure.Method(() =>
        {
            var numColliders = triangles.Length / 2;
            var compoundColliderBlobInstances = new NativeArray<CompoundCollider.ColliderBlobInstance>(numColliders, Allocator.TempJob);
            var createMeshColliderJob = new CreateMeshColliderJob
            {
                Vertices = vertices,
                Triangles = triangles,
                ColliderBlobInstances = compoundColliderBlobInstances
            };
            createMeshColliderJob.Run();
            CompoundCollider.Create(compoundColliderBlobInstances);
            compoundColliderBlobInstances.Dispose();
        }).MeasurementCount(50).Run();

        
        vertices.Dispose();
        triangles.Dispose();
    }

    private static void LoadMesh(out NativeArray<float3> vertices, out NativeArray<int3> triangles)
    {
        const string meshFilePath = "Assets/Resources/chunk_mesh.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshFilePath);
        vertices = new NativeArray<float3>(mesh.vertices.Length, Allocator.TempJob);
        for (var i = 0; i < mesh.vertices.Length; i++)
            vertices[i] = mesh.vertices[i];
        triangles = new NativeArray<int3>(mesh.triangles.Length / 3, Allocator.TempJob);
        for (var i = 0; i < mesh.triangles.Length; i += 3)
            triangles[i / 3] = new int3(mesh.triangles[i], mesh.triangles[i + 1], mesh.triangles[i + 2]);
    }

    private static void GenerateMesh(out NativeArray<float3> vertices, out NativeArray<int3> triangles)
    {
        var mesh = new Mesh();
        var meshVertices = new Vector3[8]
        {
            new(-0.5f, -0.5f, -0.5f),
            new(0.5f, -0.5f, -0.5f),
            new(-0.5f, 0.5f, -0.5f),
            new(0.5f, 0.5f, -0.5f),
            new(-0.5f, -0.5f, 0.5f),
            new(0.5f, -0.5f, 0.5f),
            new(-0.5f, 0.5f, 0.5f),
            new(0.5f, 0.5f, 0.5f)
        };
        var meshTriangles = new int[36]
        {
            0, 2, 1, // Front
            1, 2, 3,
            1, 3, 7, // Right
            1, 7, 5,
            5, 7, 6, // Back
            5, 6, 4,
            4, 6, 2, // Left
            4, 2, 0,
            2, 6, 3, // Top
            3, 6, 7,
            4, 0, 1, // Bottom
            4, 1, 5
        };

        mesh.vertices = meshVertices;
        mesh.triangles = meshTriangles;
        mesh.RecalculateNormals();

        vertices = new NativeArray<float3>(meshVertices.Length, Allocator.TempJob);
        for (var i = 0; i < meshVertices.Length; i++)
            vertices[i] = meshVertices[i];

        triangles = new NativeArray<int3>(meshTriangles.Length / 3, Allocator.TempJob);
        for (var i = 0; i < meshTriangles.Length; i += 3)
            triangles[i / 3] = new int3(meshTriangles[i], meshTriangles[i + 1], meshTriangles[i + 2]);
    }
}

[BurstCompile]
public struct CreateMeshColliderJob : IJob
{
    public NativeArray<float3> Vertices;
    public NativeArray<int3> Triangles;
    public NativeArray<CompoundCollider.ColliderBlobInstance> ColliderBlobInstances;

    public void Execute()
    {
        var filter = new CollisionFilter();
        for (var i = 0; i < Triangles.Length / 2; i++)
        {
            var triangleIndex = i * 2; // Two triangles per quad
            var triangle0 = Triangles[triangleIndex];
            var triangle1 = Triangles[triangleIndex + 1];
            var vertex0 = Vertices[triangle0.x];
            var vertex1 = Vertices[triangle0.y];
            var vertex2 = Vertices[triangle0.z];
            var vertex3 = Vertices[triangle1.z];

            ColliderBlobInstances[i] = new CompoundCollider.ColliderBlobInstance
            {
                Collider = PolygonCollider.CreateQuad(vertex0, vertex1, vertex2, vertex3, filter),
                CompoundFromChild = RigidTransform.identity
            };
        }
    }
}