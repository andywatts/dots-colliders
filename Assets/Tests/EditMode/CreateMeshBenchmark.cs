using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using Unity.Burst;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.PerformanceTesting;
using Collider = Unity.Physics.Collider;
using MeshCollider = Unity.Physics.MeshCollider;

public class ColliderSystemTests : ECSTestsFixture
{
    [Test, Performance]
    public void CreateMeshColliderBenchmark()
    {
        LoadMesh(out var vertices, out var triangles);

        Measure.Method(() =>
        {
            var blobCollider = new NativeArray<BlobAssetReference<Collider>>(1, Allocator.TempJob);
            var createMeshColliderJob = new CreateMeshColliderJob
            {
                Vertices = vertices,
                Triangles = triangles,
                BlobCollider = blobCollider
            };
            createMeshColliderJob.Schedule().Complete();
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
}

[BurstCompile]
public struct CreateMeshColliderJob : IJob
{
    public NativeArray<float3> Vertices;
    public NativeArray<int3> Triangles;
    public NativeArray<BlobAssetReference<Collider>> BlobCollider;

    public void Execute()
    {
        var filter = new CollisionFilter();
        BlobCollider[0] = MeshCollider.Create(Vertices, Triangles, filter);
    }
}
