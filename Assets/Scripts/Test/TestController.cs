﻿using ECS.Storage;
using ECS.Tasks;
using ECS.Tasks.Runner;
using Test.Systems;
using UnityEngine;
using Utils;
using Utils.Random;
using Utils.Rendering;

namespace Test
{
	public class TestController : MonoBehaviour
	{
		[SerializeField] private int executorCount = 1;
		[SerializeField] private int spaceshipCount = 1000;
		[SerializeField] private GraphicAssetLibrary assetLibrary;
		[SerializeField] private Profiler.Timeline timeline;

		private Utils.Logger logger;
		private SubtaskRunner subtaskRunner;
		private EntityContext entityContext;
		private DeltaTimeHandle deltaTime;
		private IRandomProvider random;
		private RenderManager renderManager;
		private TaskManager systemManager;

		private Profiler.TimelineTrack blockMainTrack;
		private Profiler.TimelineTrack renderTrack;

		protected void Awake()
		{
			if(assetLibrary == null)
			{
				Debug.LogError($"[{nameof(TestController)}] No '{nameof(GraphicAssetLibrary)}' provided!");
				return;
			}

			logger = new Utils.Logger(UnityEngine.Debug.Log);
			subtaskRunner = new SubtaskRunner(executorCount);
			entityContext = new EntityContext();
			deltaTime = new DeltaTimeHandle();
			random = new ShiftRandomProvider();
			renderManager = new RenderManager(executorCount, assetLibrary);
			systemManager = new TaskManager(subtaskRunner, new ECS.Tasks.ITask[]
			{
				new SpawnSpaceshipSystem(spaceshipCount, random, entityContext),
				new ApplyVelocitySystem(deltaTime, entityContext),
				new ApplyGravitySystem(deltaTime, entityContext),
				new AgeSystem(deltaTime, entityContext),
				new RegisterRenderObjectsSystem(renderManager, entityContext),
				new LifetimeSystem(entityContext)
			}, logger, timeline);

			blockMainTrack = timeline?.CreateTrack<Profiler.TimelineTrack>("Finishing systems on main");
			renderTrack = timeline?.CreateTrack<Profiler.TimelineTrack>("Rendering");
			timeline?.StartTimers();
		}
		
		protected void Update()
		{
			blockMainTrack?.LogStartWork();
			{
				//Wait for the systems to be complete
				systemManager.Complete();
			}
			blockMainTrack?.LogEndWork();

			renderTrack?.LogStartWork();
			{
				//Render the results of the systems
				renderManager.Render();
			}
			renderTrack?.LogEndWork();

			//Log any messages that where recorded on other threads
			logger?.Print();

			//Setup the systems
			renderManager.Clear();
			deltaTime.Update(Time.deltaTime);

			//Start the systems
			systemManager.Run();
		}

		protected void OnDestroy()
		{
			subtaskRunner?.Dispose();
			renderManager?.Dispose();
		}
	}
}