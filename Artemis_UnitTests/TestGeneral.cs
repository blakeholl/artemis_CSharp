#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestGeneral.cs" company="GAMADU.COM">
//     Copyright � 2013 GAMADU.COM. All rights reserved.
//
//     Redistribution and use in source and binary forms, with or without modification, are
//     permitted provided that the following conditions are met:
//
//        1. Redistributions of source code must retain the above copyright notice, this list of
//           conditions and the following disclaimer.
//
//        2. Redistributions in binary form must reproduce the above copyright notice, this list
//           of conditions and the following disclaimer in the documentation and/or other materials
//           provided with the distribution.
//
//     THIS SOFTWARE IS PROVIDED BY GAMADU.COM 'AS IS' AND ANY EXPRESS OR IMPLIED
//     WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
//     FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL GAMADU.COM OR
//     CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
//     CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
//     SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
//     ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//     NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
//     ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//     The views and conclusions contained in the software and documentation are those of the
//     authors and should not be interpreted as representing official policies, either expressed
//     or implied, of GAMADU.COM.
// </copyright>
// <summary>
//   The general test.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
#endregion File description

namespace UnitTests
{
    #region Using statements

    using global::System;
    using global::System.Collections.Generic;
    using global::System.Diagnostics;
    using global::System.Linq;

    using Artemis;
    using Artemis.Interface;
    using Artemis.Manager;
    using Artemis.System;
    using Artemis.Utils;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using UnitTests.Component;
    using UnitTests.System;

    #endregion Using statements

    /// <summary>The general test.</summary>
    [TestClass]
    public class TestGeneral
    {
        /// <summary>The load.</summary>
        ////private const int Load = 4096;
        private const int Load = 16384;
        ////private const int Load = 65535;

        /// <summary>The component pool.</summary>
        private static readonly Dictionary<Type, Bag<IComponent>> ComponentPool = new Dictionary<Type, Bag<IComponent>>();

        /// <summary>The health bag.</summary>
        private static readonly Bag<IComponent> HealthBag = new Bag<IComponent>();

        /// <summary>Tests the attributes.</summary>
        [TestMethod]
        public void TestAttributes()
        {
            Console.Write("Initialize EntityWorld: ");
            EntityWorld entityWorld = new EntityWorld { PoolCleanupDelay = 1 };
            entityWorld.InitializeAll();
            Console.WriteLine("OK");

            const int ExpectedNumberOfSystems = 2;
            int actualNumberOfSystems = entityWorld.SystemManager.Systems.Count;
            Assert.AreEqual(ExpectedNumberOfSystems, actualNumberOfSystems, "Number of initial systems does not fit.");
            Console.WriteLine("Number of Systems: {0} OK", actualNumberOfSystems);

            Console.Write("Build up entity with component from pool manually: ");
            Entity entityWithPooledComponent = TestEntityFactory.CreateTestPowerEntityWithPooledComponent(entityWorld);
            Console.WriteLine("OK");

            Console.Write("Build up entity from template: ");
            Entity entityFromTemplate = entityWorld.CreateEntityFromTemplate("test");
            Assert.IsNotNull(entityFromTemplate, "Entity from test template is null.");
            Console.WriteLine("OK");

            entityWorld.Update(ExecutionType.Asynchronous);
            entityWorld.Draw();

            Console.Write("Remove component from entity: ");
            entityWithPooledComponent.RemoveComponent<TestPowerComponentPoolable>();
            entityWithPooledComponent.Refresh();

            entityWorld.Update();
            entityWorld.Draw();

            Assert.IsFalse(entityWithPooledComponent.HasComponent<TestPowerComponentPoolable>(), "Entity has still deleted component.");
            Console.WriteLine("OK");

            Console.Write("Add component to entity: ");
            entityWithPooledComponent.AddComponentFromPool<TestPowerComponentPoolable>();
            entityWithPooledComponent.GetComponent<TestPowerComponentPoolable>().Power = 100;
            entityWithPooledComponent.Refresh();

            entityWorld.Update(ExecutionType.Asynchronous);
            entityWorld.Draw();

            Assert.IsTrue(entityWithPooledComponent.HasComponent<TestPowerComponentPoolable>(), "Could not add component to entity.");
            Console.WriteLine("OK");
        }

        /// <summary>Tests the dummies.</summary>
        [TestMethod]
        public void TestDummies()
        {
            Console.Write("Initialize EntityWorld: ");
            EntityWorld entityWorld = new EntityWorld();
            entityWorld.SystemManager.SetSystem(new TestCommunicationSystem(), GameLoopType.Update);
            entityWorld.InitializeAll(false);
            Console.WriteLine("OK");

            Console.Write("Fill EntityWorld with " + Load + " grouped entities: ");
            for (int index = Load - 1; index >= 0; --index)
            {
                TestEntityFactory.CreateTestHealthEntity(entityWorld, "test");
            }

            Console.WriteLine("OK");

            Console.Write("Add a tagged entity to EntityWorld: ");
            TestEntityFactory.CreateTestHealthEntity(entityWorld, null, "tag");
            Console.WriteLine("OK");

            Console.Write("Update EntityWorld: ");
            Stopwatch stopwatch = Stopwatch.StartNew();
            entityWorld.Update();
            entityWorld.Draw();
            stopwatch.Stop();
            Console.Write("duration " + FastDateTime.ToString(stopwatch.Elapsed) + " ");

            Console.WriteLine("OK");

            int actualNumberOfSystems = entityWorld.SystemManager.Systems.Count;
            const int ExpectedNumberOfSystems = 1;
            Console.Write("Number of Systems: {0} ", actualNumberOfSystems);
            Assert.AreEqual(ExpectedNumberOfSystems, actualNumberOfSystems);
            Console.WriteLine("OK");

            Entity actualTaggedEntity = entityWorld.TagManager.GetEntity("tag");
            Console.Write("Is tagged entity present: {0} ", actualTaggedEntity != null);
            Assert.IsNotNull(actualTaggedEntity);
            Console.WriteLine("OK");

            int actualNumberOfGroupedEntities = entityWorld.GroupManager.GetEntities("test").Count;
            const int ExpectedNumberOfGroupedEntities = Load;
            Console.Write("Number of grouped entities: {0} ", actualNumberOfGroupedEntities);
            Assert.AreEqual(ExpectedNumberOfGroupedEntities, actualNumberOfGroupedEntities);
            Console.WriteLine("OK");
#if DEBUG
            int actualNumberOfActiveEntities = entityWorld.EntityManager.ActiveEntitiesCount;
            const int ExpectedNumberOfActiveEntities = ExpectedNumberOfGroupedEntities + ExpectedNumberOfSystems;
            Console.Write("Number of active entities: {0} ", actualNumberOfActiveEntities);
            Assert.AreEqual(ExpectedNumberOfActiveEntities, actualNumberOfActiveEntities);
            Console.WriteLine("OK");
#endif
        }

        /// <summary>Tests the hybrid queue system.</summary>
        [TestMethod]
        public void TestHybridQueueSystem()
        {
            Console.Write("Initialize EntityWorld: ");
            EntityWorld entityWorld = new EntityWorld();
            TestQueueHybridSystem testQueueHybridSystem = entityWorld.SystemManager.SetSystem(new TestQueueHybridSystem(), GameLoopType.Update);
            entityWorld.InitializeAll(false);
            Console.WriteLine("OK");

            const int Chunk = 500;

            Console.Write("Fill EntityWorld with first  chunk of " + Chunk + " entities: ");
            List<Entity> entities = new List<Entity>();
            for (int index = Chunk; index > 0; --index)
            {
                entities.Add(TestEntityFactory.CreateTestHealthEntity(entityWorld));
            }

            Console.WriteLine("OK");
            Console.Write("Fill EntityWorld with second chunk of " + Chunk + " entities: ");

            for (int index = Chunk; index > 0; --index)
            {
                Entity entity = TestEntityFactory.CreateTestHealthEntity(entityWorld);

                testQueueHybridSystem.AddToQueue(entity);
                entities.Add(entity);
            }

            Console.WriteLine("OK");

            Stopwatch stopwatch = Stopwatch.StartNew();
            int numberOfQueues = 0;
            while (testQueueHybridSystem.QueueCount > 0)
            {
                ++numberOfQueues;
                entityWorld.Update();
                entityWorld.Draw();
            }

            stopwatch.Stop();
            Console.WriteLine("Processed {0} hybrid queues with duration {1}", numberOfQueues,  FastDateTime.ToString(stopwatch.Elapsed));

            Console.Write("Test first  chunk: ");
            float expectedPointsFirstChunk = 100.0f - (10 * numberOfQueues);
            if (expectedPointsFirstChunk < 0.0f)
            {
                Console.Write("Results may be inaccurate. Please lower chunk size. ");
                expectedPointsFirstChunk = 0.0f;
            }

            for (int index = Chunk - 1; index >= 0; --index)
            {
                Assert.AreEqual(expectedPointsFirstChunk, entities[index].GetComponent<TestHealthComponent>().Points, "Index:<" + index + ">.");
            }

            Console.WriteLine("OK");

            Console.Write("Test second chunk: ");
            float expectedPointsSecondChunk = 90.0f - (10 * numberOfQueues);
            if (expectedPointsSecondChunk < 0.0f)
            {
                Console.Write("Results may be inaccurate. Please lower chunk size. ");
                expectedPointsSecondChunk = 0.0f;
            }

            for (int index = (Chunk * 2) - 1; index >= Chunk; --index)
            {
                Assert.AreEqual(expectedPointsSecondChunk, entities[index].GetComponent<TestHealthComponent>().Points, "Index:<" + index + ">.");
            }

            Console.WriteLine("OK");
        }

        /// <summary>Tests a simple system.</summary>
        [TestMethod]
        public void TestSimpleSystem()
        {
            Console.Write("Initialize EntityWorld: ");
            EntityWorld entityWorld = new EntityWorld();
            entityWorld.SystemManager.SetSystem(new TestNormalEntityProcessingSystem1(), GameLoopType.Update);
            entityWorld.InitializeAll(false);
            Console.WriteLine("OK");

            Entity entity1 = TestEntityFactory.CreateTestHealthEntity(entityWorld);
            Assert.IsNotNull(entity1);

            Entity entity2 = TestEntityFactory.CreateTestPowerEntity(entityWorld);
            Assert.IsNotNull(entity2);

            Stopwatch stopwatch = Stopwatch.StartNew();
            entityWorld.Update();
            entityWorld.Draw();
            stopwatch.Stop();
#if DEBUG
            Console.WriteLine("Processed update and draw with duration {0} for {1} elements", FastDateTime.ToString(stopwatch.Elapsed), entityWorld.EntityManager.ActiveEntitiesCount);
#else
            Console.WriteLine("Processed update and draw with duration {0} for {1} elements", FastDateTime.ToString(stopwatch.Elapsed), entityWorld.EntityManager.ActiveEntities.Count);
#endif
            const float Expected1 = 90.0f;
            Assert.AreEqual(Expected1, entity1.GetComponent<TestHealthComponent>().Points);

            const float Expected2 = 100.0f;
            Assert.AreEqual(Expected2, entity2.GetComponent<TestHealthComponent>().Points);
            Assert.AreEqual(Expected2, entity2.GetComponent<TestPowerComponent>().Power);
        }

        /// <summary>Tests the queue systems.</summary>
        [TestMethod]
        public void TestQueueSystems()
        {
            Console.Write("Initialize EntityWorld: ");
            EntityWorld entityWorld = new EntityWorld();
            TestQueueSystem testQueueSystem1 = entityWorld.SystemManager.SetSystem(new TestQueueSystem(10), GameLoopType.Update);
            TestQueueSystem testQueueSystem2 = entityWorld.SystemManager.SetSystem(new TestQueueSystem(10), GameLoopType.Update);
            TestQueueSystemCopy testQueueSystem3 = entityWorld.SystemManager.SetSystem(new TestQueueSystemCopy(20), GameLoopType.Update);
            entityWorld.InitializeAll(false);
            Console.WriteLine("OK");

            QueueSystemProcessingThreadSafe.SetQueueProcessingLimit(20, testQueueSystem2.Id);

            int expectedLimit = QueueSystemProcessingThreadSafe.GetQueueProcessingLimit(testQueueSystem2.Id);
            Assert.AreEqual(expectedLimit, QueueSystemProcessingThreadSafe.GetQueueProcessingLimit(testQueueSystem1.Id));
            Assert.AreNotEqual(expectedLimit, QueueSystemProcessingThreadSafe.GetQueueProcessingLimit(testQueueSystem3.Id));

            QueueSystemProcessingThreadSafe.SetQueueProcessingLimit(1024, testQueueSystem1.Id);
            QueueSystemProcessingThreadSafe.SetQueueProcessingLimit(4096, testQueueSystem3.Id);

            Console.Write("Fill EntityWorld with first  chunk of " + Load + " entities: ");
            List<Entity> entities1 = new List<Entity>();
            for (int index = Load; index >= 0; --index)
            {
                Entity entity = TestEntityFactory.CreateTestHealthEntity(entityWorld);

                QueueSystemProcessingThreadSafe.AddToQueue(entity, testQueueSystem1.Id);
                entities1.Add(entity);
            }

            Console.WriteLine("OK");
            Console.Write("Fill EntityWorld with second chunk of " + Load + " entities: ");
            List<Entity> entities2 = new List<Entity>();
            for (int index = Load; index >= 0; --index)
            {
                Entity entity = TestEntityFactory.CreateTestHealthEntity(entityWorld);

                QueueSystemProcessingThreadSafe.AddToQueue(entity, testQueueSystem3.Id);
                entities2.Add(entity);
            }

            Console.WriteLine("OK");
            Console.WriteLine("Begin down tearing of queues...");
            Stopwatch stopwatch = Stopwatch.StartNew();
            int loopCount = 0;
            while (QueueSystemProcessingThreadSafe.QueueCount(testQueueSystem1.Id) > 0 || QueueSystemProcessingThreadSafe.QueueCount(testQueueSystem3.Id) > 0)
            {
                entityWorld.Update(ExecutionType.Asynchronous);
                entityWorld.Draw();
                ++loopCount;
#if DEBUG
                Console.WriteLine("Queue size thread A: {0} B: {1}", QueueSystemProcessingThreadSafe.QueueCount(testQueueSystem1.Id), QueueSystemProcessingThreadSafe.QueueCount(testQueueSystem3.Id));
#endif
            }

            stopwatch.Stop();
            Console.WriteLine("End OK. Loops: {0} Time: {1}", loopCount, FastDateTime.ToString(stopwatch.Elapsed));

            Console.Write("Test entities 1: ");
            const float Expected1 = 90.0f;
            foreach (Entity entity in entities1)
            {
                Assert.AreEqual(Expected1, entity.GetComponent<TestHealthComponent>().Points);
            }

            Console.WriteLine("OK");
            Console.Write("Test entities 2: ");
            const float Expected2 = 80.0f;
            foreach (Entity entity in entities2)
            {
                Assert.AreEqual(Expected2, entity.GetComponent<TestHealthComponent>().Points);
            }

            Console.WriteLine("OK");
        }

        /// <summary>Systems the communication test.</summary>
        [TestMethod]
        public void TestSystemCommunication()
        {
            Console.Write("Initialize EntityWorld: ");
            EntitySystem.BlackBoard.SetEntry("Damage", 5);
            EntityWorld entityWorld = new EntityWorld();
            entityWorld.SystemManager.SetSystem(new TestCommunicationSystem(), GameLoopType.Update);
            entityWorld.InitializeAll(false);
            Console.WriteLine("OK");

            Console.Write("Fill EntityWorld with " + Load + " entities: ");
            List<Entity> entities = new List<Entity>();
            for (int index = Load; index >= 0; --index)
            {
                Entity entity = TestEntityFactory.CreateTestHealthEntity(entityWorld);
                entities.Add(entity);
            }

            Console.WriteLine("OK");

            Stopwatch stopwatch = Stopwatch.StartNew();
            entityWorld.Update();
            entityWorld.Draw();
            stopwatch.Stop();
            Console.WriteLine("Update 1 duration: {0}", FastDateTime.ToString(stopwatch.Elapsed));

            EntitySystem.BlackBoard.SetEntry("Damage", 10);

            stopwatch.Restart();
            entityWorld.Update();
            entityWorld.Draw();
            stopwatch.Stop();
            Console.WriteLine("Update 2 duration: {0}", FastDateTime.ToString(stopwatch.Elapsed));

            Console.Write("Test entities: ");
            const float Expected = 85.0f;
            foreach (Entity item in entities)
            {
                Assert.AreEqual(Expected, item.GetComponent<TestHealthComponent>().Points);
            }

            Console.WriteLine("OK");
            EntitySystem.BlackBoard.RemoveEntry("Damage");
        }

        /// <summary>Tests the render multi health bar system.</summary>
        [TestMethod]
        public void TestRenderMultiHealthBarSystem()
        {
            Console.Write("Initialize EntityWorld: ");
            HealthBag.Clear();
            ComponentPool.Clear();

            HealthBag.Add(new TestHealthComponent());
            HealthBag.Add(new TestHealthComponent());
            ComponentPool.Add(typeof(TestHealthComponent), HealthBag);

            EntityWorld entityWorld = new EntityWorld();
            entityWorld.EntityManager.RemovedComponentEvent += RemovedComponent;
            entityWorld.EntityManager.RemovedEntityEvent += RemovedEntity;
            entityWorld.SystemManager.SetSystem(new TestRenderHealthBarMultiSystem(), GameLoopType.Update);
            entityWorld.InitializeAll(false);
            Console.WriteLine("OK");

            Console.Write("Fill EntityWorld with " + Load + " entities: ");
            List<Entity> entities = new List<Entity>();
            for (int index = Load - 1; index >= 0; --index)
            {
                Entity entity = TestEntityFactory.CreateTestHealthEntity(entityWorld);
                entities.Add(entity);
            }

            Console.WriteLine("OK");

            const int Passes = 9;
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int index = 0; index < Passes; ++index)
            {
                entityWorld.Update();
                entityWorld.Draw();
            }

            stopwatch.Stop();
            Console.WriteLine("Update (" + Passes + " passes) duration: {0}", FastDateTime.ToString(stopwatch.Elapsed));

            int expectedPoints = 100 - (Passes * 10);
            if (expectedPoints < 0)
            {
                expectedPoints = 0;
            }

            int df = entities.Count(item => Math.Abs((int)(item.GetComponent<TestHealthComponent>().Points - expectedPoints)) < float.Epsilon);

            Assert.AreEqual(Load, df);

            Console.WriteLine("Found {0} entities with health of {1}.", df, expectedPoints);
        }

        /// <summary>Tests multiple systems.</summary>
        [TestMethod]
        public void TestMultipleSystems()
        {
            Console.Write("Initialize EntityWorld: ");
            HealthBag.Clear();
            ComponentPool.Clear();

            HealthBag.Add(new TestHealthComponent());
            HealthBag.Add(new TestHealthComponent());
            ComponentPool.Add(typeof(TestHealthComponent), HealthBag);

            EntityWorld entityWorld = new EntityWorld();
            entityWorld.EntityManager.RemovedComponentEvent += RemovedComponent;
            entityWorld.EntityManager.RemovedEntityEvent += RemovedEntity;
            entityWorld.SystemManager.SetSystem(new TestRenderHealthBarSingleSystem(), GameLoopType.Update);
            entityWorld.SystemManager.SetSystem(new TestEntityProcessingSystem1(), GameLoopType.Update);
            entityWorld.SystemManager.SetSystem(new TestEntityProcessingSystem2(), GameLoopType.Update);
            entityWorld.SystemManager.SetSystem(new TestEntityProcessingSystem3(), GameLoopType.Update);
            entityWorld.InitializeAll(false);
            Console.WriteLine("OK");

            Console.Write("Fill EntityWorld with " + Load + " entities: ");
            List<Entity> entities = new List<Entity>();
            for (int index = Load - 1; index >= 0; --index)
            {
                Entity entity = TestEntityFactory.CreateTestHealthEntity(entityWorld);
                entities.Add(entity);
            }

            Console.WriteLine("OK");

            const int Passes = 3;
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int index = 0; index < Passes; ++index)
            {
                entityWorld.Update();
                entityWorld.Draw();
            }

            stopwatch.Stop();
            Console.WriteLine("Update (" + Passes + " passes) duration: {0}", FastDateTime.ToString(stopwatch.Elapsed));

            /*
            int df = 0;
            foreach (Entity entity in entities)
            {
                if (Math.Abs(entity.GetComponent<TestHealthComponent>().Points - 90) < float.Epsilon)
                {
                    df++;
                }
                else
                {
                    Console.WriteLine("Error " + df);
                }
            }
            */
        }

        /// <summary>The removed component.</summary>
        /// <param name="entity">The entity.</param>
        /// <param name="component">The component.</param>
        private static void RemovedComponent(Entity entity, IComponent component)
        {
            Console.WriteLine("This was the component removed: " + component.GetType());
            Bag<IComponent> tempBag;
            if (ComponentPool.TryGetValue(component.GetType(), out tempBag))
            {
                Console.WriteLine("Health Component Pool has " + tempBag.Count + " objects");
                tempBag.Add(component);
            }

            if (ComponentPool.TryGetValue(component.GetType(), out tempBag))
            {
                Console.WriteLine("Health Component Pool now has " + tempBag.Count + " objects");
            }
        }

        /// <summary>The removed entity.</summary>
        /// <param name="entity">The entity.</param>
        private static void RemovedEntity(Entity entity)
        {
            Console.WriteLine("The entity {0} was removed successfully.", entity.UniqueId);
        }
    }
}