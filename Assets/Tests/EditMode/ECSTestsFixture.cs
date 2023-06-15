// using Assert = FastAssert;
using System;
using NUnit.Framework;
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;
#if !UNITY_DOTSRUNTIME
using UnityEngine.LowLevel;
#endif

// If ENABLE_UNITY_COLLECTIONS_CHECKS is not defined we will ignore the test
// When using this attribute, consider it to logically AND with any other TestRequiresxxxx attrubute
#if ENABLE_UNITY_COLLECTIONS_CHECKS
internal class TestRequiresCollectionChecks : Attribute
{
    public TestRequiresCollectionChecks(string msg = null)
    {
    }
}
#else
    internal class TestRequiresCollectionChecks : IgnoreAttribute
    {
        public TestRequiresCollectionChecks(string msg =
 null) : base($"Test requires ENABLE_UNITY_COLLECTION_CHECKS which is not defined{(msg == null ? "." : $": {msg}")}") { }
    }
#endif

// If ENABLE_UNITY_COLLECTIONS_CHECKS and UNITY_DOTS_DEBUG is not defined we will ignore the test
// conversely if either of them are defined the test will be run.
// When using this attribute, consider it to logically AND with any other TestRequiresxxxx attrubute
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
internal class TestRequiresDotsDebugOrCollectionChecks : Attribute
{
    public TestRequiresDotsDebugOrCollectionChecks(string msg = null)
    {
    }
}
#else
    internal class TestRequiresDotsDebugOrCollectionChecks : IgnoreAttribute
    {
        public TestRequiresDotsDebugOrCollectionChecks(string msg =
 null) : base($"Test requires UNITY_DOTS_DEBUG || ENABLE_UNITY_COLLECTION_CHECKS which neither are defined{(msg == null ? "." : $": {msg}")}") { }
    }
#endif

// Ignores te test when in an il2cpp build only. Please make use of the 'msg' string
// to tell others why this test should be ignored
#if !ENABLE_IL2CPP
internal class IgnoreTest_IL2CPP : Attribute
{
    public IgnoreTest_IL2CPP(string msg = null)
    {
    }
}
#else
    internal class IgnoreTest_IL2CPP : IgnoreAttribute
    {
        public IgnoreTest_IL2CPP(string msg = null) : base($"Test ignored on IL2CPP builds{(msg == null ? "." : $": {msg}")}") { }
    }
#endif

public partial class EmptySystem : SystemBase
{
    protected override void OnUpdate()
    {
    }

    public EntityQuery GetEntityQuery(params EntityQueryDesc[] queriesDesc)
    {
        return base.GetEntityQuery(queriesDesc);
    }

    public EntityQuery GetEntityQuery(params ComponentType[] componentTypes)
    {
        return base.GetEntityQuery(componentTypes);
    }

    public EntityQuery GetEntityQuery(NativeArray<ComponentType> componentTypes)
    {
        return base.GetEntityQuery(componentTypes);
    }
}

[BurstCompile(CompileSynchronously = true)]
public class ECSTestsCommonBase
{
    [SetUp]
    public virtual void Setup()
    {
#if UNITY_DOTSRUNTIME
            Unity.Runtime.TempMemoryScope.EnterScope();
            UnityEngine.TestTools.LogAssert.ExpectReset();
#endif
    }

    [TearDown]
    public virtual void TearDown()
    {
#if UNITY_DOTSRUNTIME
            Unity.Runtime.TempMemoryScope.ExitScope();
#endif
    }

    [BurstDiscard]
    public static void TestBurstCompiled(ref bool falseIfNot)
    {
        falseIfNot = false;
    }

    [BurstCompile(CompileSynchronously = true)]
    public static bool IsBurstEnabled()
    {
        var burstCompiled = true;
        TestBurstCompiled(ref burstCompiled);
        return burstCompiled;
    }
}

public abstract class ECSTestsFixture : ECSTestsCommonBase
{
    private bool JobsDebuggerWasEnabled;
    protected EntityManager m_Manager;
    protected EntityManager.EntityManagerDebug m_ManagerDebug;
#if !UNITY_DOTSRUNTIME
    protected PlayerLoopSystem m_PreviousPlayerLoop;
#endif
    protected World m_PreviousWorld;

    protected int StressTestEntityCount = 1000;
    protected World World;

    public EmptySystem EmptySystem => World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<EmptySystem>();

    [SetUp]
    public override void Setup()
    {
        base.Setup();

#if !UNITY_DOTSRUNTIME
        // unit tests preserve the current player loop to restore later, and start from a blank slate.
        m_PreviousPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
        PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());
#endif

        m_PreviousWorld = World.DefaultGameObjectInjectionWorld;
        World = World.DefaultGameObjectInjectionWorld = new World("Test World");
        World.UpdateAllocatorEnableBlockFree = true;
        m_Manager = World.EntityManager;
        m_ManagerDebug = new EntityManager.EntityManagerDebug(m_Manager);

        // Many ECS tests will only pass if the Jobs Debugger enabled;
        // force it enabled for all tests, and restore the original value at teardown.
        JobsDebuggerWasEnabled = JobsUtility.JobDebuggerEnabled;
        JobsUtility.JobDebuggerEnabled = true;

#if !UNITY_DOTSRUNTIME
        // JobsUtility.ClearSystemIds();
#endif

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
        // In case entities journaling is initialized, clear it
        EntitiesJournaling.Clear();
#endif
    }

    [TearDown]
    public override void TearDown()
    {
        if (World != null && World.IsCreated)
        {
            // Clean up systems before calling CheckInternalConsistency because we might have filters etc
            // holding on SharedComponentData making checks fail
            while (World.Systems.Count > 0) World.DestroySystemManaged(World.Systems[0]);

            m_ManagerDebug.CheckInternalConsistency();

            World.Dispose();
            World = null;

            World.DefaultGameObjectInjectionWorld = m_PreviousWorld;
            m_PreviousWorld = null;
            m_Manager = default;
        }

        JobsUtility.JobDebuggerEnabled = JobsDebuggerWasEnabled;

#if !UNITY_DOTSRUNTIME
        // JobsUtility.ClearSystemIds();
#endif

#if !UNITY_DOTSRUNTIME
        PlayerLoop.SetPlayerLoop(m_PreviousPlayerLoop);
#endif

        base.TearDown();
    }

    public void AssertDoesNotExist(Entity entity)
    {
        Assert.IsFalse(m_Manager.HasComponent<EcsTestData>(entity));
        Assert.IsFalse(m_Manager.HasComponent<EcsTestData2>(entity));
        Assert.IsFalse(m_Manager.HasComponent<EcsTestData3>(entity));
        Assert.IsFalse(m_Manager.Exists(entity));
    }

    public void AssertComponentData(Entity entity, int index)
    {
        Assert.IsTrue(m_Manager.HasComponent<EcsTestData>(entity));
        Assert.IsTrue(m_Manager.HasComponent<EcsTestData2>(entity));
        Assert.IsFalse(m_Manager.HasComponent<EcsTestData3>(entity));
        Assert.IsTrue(m_Manager.Exists(entity));

        Assert.AreEqual(-index, m_Manager.GetComponentData<EcsTestData2>(entity).value0);
        Assert.AreEqual(-index, m_Manager.GetComponentData<EcsTestData2>(entity).value1);
        Assert.AreEqual(index, m_Manager.GetComponentData<EcsTestData>(entity).value);
    }

    public Entity CreateEntityWithDefaultData(int index)
    {
        var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));

        // HasComponent & Exists setup correctly
        Assert.IsTrue(m_Manager.HasComponent<EcsTestData>(entity));
        Assert.IsTrue(m_Manager.HasComponent<EcsTestData2>(entity));
        Assert.IsFalse(m_Manager.HasComponent<EcsTestData3>(entity));
        Assert.IsTrue(m_Manager.Exists(entity));

        // Create must initialize values to zero
        Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData2>(entity).value0);
        Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData2>(entity).value1);
        Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData>(entity).value);

        // Setup some non zero default values
        m_Manager.SetComponentData(entity, new EcsTestData2(-index));
        m_Manager.SetComponentData(entity, new EcsTestData(index));

        AssertComponentData(entity, index);

        return entity;
    }

    public void AssertSameChunk(Entity e0, Entity e1)
    {
        Assert.AreEqual(m_Manager.GetChunk(e0), m_Manager.GetChunk(e1));
    }

    public void AssetHasChangeVersion<T>(Entity e, uint version) where T :
#if UNITY_DISABLE_MANAGED_COMPONENTS
        struct,
#endif
        IComponentData
    {
        var type = m_Manager.GetComponentTypeHandle<T>(true);
        var chunk = m_Manager.GetChunk(e);
        Assert.AreEqual(version, chunk.GetChangeVersion(ref type));
        Assert.IsFalse(chunk.DidChange(ref type, version));
        Assert.IsTrue(chunk.DidChange(ref type, version - 1));
    }

    public void AssetHasChunkOrderVersion(Entity e, uint version)
    {
        var chunk = m_Manager.GetChunk(e);
        Assert.AreEqual(version, chunk.GetOrderVersion());
    }

    public void AssetHasBufferChangeVersion<T>(Entity e, uint version) where T : unmanaged, IBufferElementData
    {
        var type = m_Manager.GetBufferTypeHandle<T>(true);
        var chunk = m_Manager.GetChunk(e);
        Assert.AreEqual(version, chunk.GetChangeVersion(ref type));
        Assert.IsFalse(chunk.DidChange(ref type, version));
        Assert.IsTrue(chunk.DidChange(ref type, version - 1));
    }

    public void AssetHasSharedChangeVersion<T>(Entity e, uint version) where T : unmanaged, ISharedComponentData
    {
        var type = m_Manager.GetSharedComponentTypeHandle<T>();
        var chunk = m_Manager.GetChunk(e);
        Assert.AreEqual(version, chunk.GetChangeVersion(type));
        Assert.IsFalse(chunk.DidChange(type, version));
        Assert.IsTrue(chunk.DidChange(type, version - 1));
    }

    private partial class EntityForEachSystem : SystemBase
    {
        protected override void OnUpdate()
        {
        }
    }
}