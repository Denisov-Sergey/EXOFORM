#if UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using System.Reflection;
using Exoform.Scripts.Spawning;
using Exoform.Scripts.Ecs.Components.UnitLogicComponents;

namespace Exoform.Tests.EditMode
{
    public class SpawnPrefabManagerTests
    {
        private World world;
        private EntityManager entityManager;

        [SetUp]
        public void Setup()
        {
            world = new World("TestWorld");
            entityManager = world.EntityManager;
            World.DefaultGameObjectInjectionWorld = world;
        }

        [TearDown]
        public void TearDown()
        {
            world.Dispose();
            World.DefaultGameObjectInjectionWorld = null;
            typeof(SpawnPrefabManager)
                .GetField("cachedConfig", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, null);
        }

        private SpawnPrefabConfiguration CreateConfig(int limit)
        {
            var config = ScriptableObject.CreateInstance<SpawnPrefabConfiguration>();
            config.playerUnits.Add(new SpawnPrefabConfiguration.UnitPrefabEntry
            {
                unitName = "Player",
                unitType = UnitType.Infantry,
                prefab = new GameObject("p"),
                teamId = 1,
                maxSimultaneous = limit
            });
            return config;
        }

        private void SetConfig(SpawnPrefabConfiguration config)
        {
            typeof(SpawnPrefabManager)
                .GetField("cachedConfig", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, config);
        }

        [Test]
        public void CanSpawnUnit_False_When_Limit_Reached()
        {
            SetConfig(CreateConfig(2));

            for (int i = 0; i < 2; i++)
            {
                var e = entityManager.CreateEntity(typeof(UnitLogicComponent), typeof(CombatComponent));
                entityManager.SetComponentData(e, new UnitLogicComponent { TeamId = 1, UnitType = UnitType.Infantry });
                entityManager.SetComponentData(e, new CombatComponent { IsDead = false });
            }

            Assert.IsFalse(SpawnPrefabManager.CanSpawnUnit(1, UnitType.Infantry));
        }

        [Test]
        public void CanSpawnUnit_True_When_Under_Limit()
        {
            SetConfig(CreateConfig(2));

            var e = entityManager.CreateEntity(typeof(UnitLogicComponent), typeof(CombatComponent));
            entityManager.SetComponentData(e, new UnitLogicComponent { TeamId = 1, UnitType = UnitType.Infantry });
            entityManager.SetComponentData(e, new CombatComponent { IsDead = false });

            Assert.IsTrue(SpawnPrefabManager.CanSpawnUnit(1, UnitType.Infantry));
        }
    }
}
#endif
