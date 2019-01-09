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

		// Token: 0x06000D08 RID: 3336 RVA: 0x00066F20 File Offset: 0x00065320
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

		// Token: 0x04000BCF RID: 3023
		[RequiredField]
		[PublicAPI]
		public Func<GameObject> gameObject;

		// Token: 0x04000BD0 RID: 3024
		[PublicAPI]
		public FsmGameObject spawnPoint;

		// Token: 0x04000BD1 RID: 3025
		[PublicAPI]
		public FsmVector3 position;

		// Token: 0x04000BD2 RID: 3026
		[PublicAPI]
		public FsmFloat frequency;

		// Token: 0x04000BD3 RID: 3027
		[PublicAPI]
		public FsmInt spawnMin;

		// Token: 0x04000BD4 RID: 3028
		[PublicAPI]
		public FsmInt spawnMax;

		// Token: 0x04000BD5 RID: 3029
		[PublicAPI]
		public FsmFloat speedMin;

		// Token: 0x04000BD6 RID: 3030
		[PublicAPI]
		public FsmFloat speedMax;

		// Token: 0x04000BD7 RID: 3031
		[PublicAPI]
		public FsmFloat angleMin;

		// Token: 0x04000BD8 RID: 3032
		[PublicAPI]
		public FsmFloat angleMax;

		// Token: 0x04000BD9 RID: 3033
		[PublicAPI]
		public FsmFloat originVariationX;

		// Token: 0x04000BDA RID: 3034
		[PublicAPI]
		public FsmFloat originVariationY;

		// Token: 0x04000BDD RID: 3037
		private float timer;

		// Token: 0x04000BDE RID: 3038
		private bool originAdjusted;
	}
}
