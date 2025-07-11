using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using Exoform.Scripts.Spawning;
using Exoform.Scripts.Ecs.Components.UnitLogicComponents;

public class SpawnPrefabConfigurationTests
{
    private World _world;
    private EntityManager _manager;

    [SetUp]
    public void Setup()
    {
        _world = new World("TestWorld");
        World.DefaultGameObjectInjectionWorld = _world;
        _manager = _world.EntityManager;

        var config = ScriptableObject.CreateInstance<SpawnPrefabConfiguration>();
        config.playerUnits = new List<SpawnPrefabConfiguration.UnitPrefabEntry>
        {
            new SpawnPrefabConfiguration.UnitPrefabEntry
            {
                unitName = "Test Unit",
                unitType = UnitType.Infantry,
                spawnWeight = 1f,
                teamId = 1,
                maxSimultaneous = 1
            }
        };

        var field = typeof(SpawnPrefabManager).GetField("cachedConfig", BindingFlags.Static | BindingFlags.NonPublic);
        field.SetValue(null, config);
    }

    [TearDown]
    public void Teardown()
    {
        var field = typeof(SpawnPrefabManager).GetField("cachedConfig", BindingFlags.Static | BindingFlags.NonPublic);
        field.SetValue(null, null);
        _world.Dispose();
        World.DefaultGameObjectInjectionWorld = null;
    }

    [Test]
    public void CanSpawnUnit_ReturnsFalse_WhenLimitReached()
    {
        Assert.IsTrue(SpawnPrefabManager.CanSpawnUnit(1, UnitType.Infantry));

        var entity = _manager.CreateEntity();
        _manager.AddComponentData(entity, new UnitLogicComponent { TeamId = 1, UnitType = UnitType.Infantry });
        _manager.AddComponentData(entity, new CombatComponent { IsDead = false });

        Assert.IsFalse(SpawnPrefabManager.CanSpawnUnit(1, UnitType.Infantry));
    }
}
