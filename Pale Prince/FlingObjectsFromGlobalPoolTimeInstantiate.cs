using System;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Pale_Prince
{
	public class FlingObjectsFromGlobalPoolTimeInstantiate : RigidBody2dActionBase
	{
		public override void Reset()
		{
			gameObject = null;
			spawnPoint = null;
			position = new FsmVector3
			{
				UseVariable = true
			};
			spawnMin = null;
			spawnMax = null;
			speedMin = null;
			speedMax = null;
			angleMin = null;
			angleMax = null;
			originVariationX = null;
			originVariationY = null;
		}

		public override void OnUpdate()
		{
			timer += Time.deltaTime;
			
			if (timer < frequency.Value) return;

			timer = 0f;
			
			if (gameObject == null) return;
			
			Vector3 spawnLocation = Vector3.zero;
			
			if (spawnPoint.Value != null)
			{
				spawnLocation = spawnPoint.Value.transform.position;
			}
			
			int spawnAmount = Random.Range(spawnMin.Value, spawnMax.Value + 1);
			
			for (int i = 1; i <= spawnAmount; i++)
			{
				GameObject go = Object.Instantiate(gameObject?.Invoke(), spawnLocation, Quaternion.Euler(Vector3.zero));
				
				float x = go.transform.position.x;
				float y = go.transform.position.y;
				float z = go.transform.position.z;
				
				if (originVariationX != null)
				{
					x = go.transform.position.x + Random.Range(-originVariationX.Value, originVariationX.Value);
					originAdjusted = true;
				}
				
				if (originVariationY != null)
				{
					y = go.transform.position.y + Random.Range(-originVariationY.Value, originVariationY.Value);
					originAdjusted = true;
				}
				
				if (originAdjusted)
				{
					go.transform.position = new Vector3(x, y, z);
				}
				
				float speed = Random.Range(speedMin.Value, speedMax.Value);
				float angle = Random.Range(angleMin.Value, angleMax.Value);
				
				go.GetComponent<Rigidbody2D>().velocity = new Vector2
				(
					speed * Mathf.Cos(angle * (Mathf.PI / 180f)),
					speed * Mathf.Sin(angle * (Mathf.PI / 180f))
				);
				
				go.SetActive(true);
			}
		}

		[RequiredField]
		[PublicAPI]
		public Func<GameObject> gameObject;

		[PublicAPI]
		public FsmGameObject spawnPoint;

		[PublicAPI]
		public FsmVector3 position;

		[PublicAPI]
		public FsmFloat frequency;

		[PublicAPI]
		public FsmInt spawnMin;

		[PublicAPI]
		public FsmInt spawnMax;

		[PublicAPI]
		public FsmFloat speedMin;

		[PublicAPI]
		public FsmFloat speedMax;

		[PublicAPI]
		public FsmFloat angleMin;

		[PublicAPI]
		public FsmFloat angleMax;

		[PublicAPI]
		public FsmFloat originVariationX;

		[PublicAPI]
		public FsmFloat originVariationY;

		private float timer;

		private bool originAdjusted;
	}
}
