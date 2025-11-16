using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Code From: https://github.com/daniel-ilett/shaders-wall-cutout/blob/main/Assets/Scripts/CutoutObject.cs

public class CutoutObject : MonoBehaviour
{
    [SerializeField]
    private Transform targetObject;

    [SerializeField]
    private LayerMask furnitureMask;

    private Camera mainCamera;
	private readonly HashSet<Renderer> previouslyAffected = new HashSet<Renderer>();

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
		// Default mask to Furniture layer if not set
		if (furnitureMask.value == 0)
		{
			int furnitureLayer = LayerMask.NameToLayer("Furniture");
			if (furnitureLayer >= 0) furnitureMask = 1 << furnitureLayer;
		}
		// Auto-assign the main tower if not set in inspector
		if (targetObject == null)
		{
			GameObject mainTower = GameObject.FindGameObjectWithTag("MainTower");
			if (mainTower != null) targetObject = mainTower.transform;
		}
    }

    private void Update()
    {
		// Lazy-assign in case the MainTower spawns later
		if (targetObject == null)
		{
			GameObject mainTower = GameObject.FindGameObjectWithTag("MainTower");
			if (mainTower != null) targetObject = mainTower.transform;
			if (targetObject == null) return;
		}

        Vector2 cutoutPos = mainCamera.WorldToViewportPoint(targetObject.position);

        Vector3 offset = targetObject.position - transform.position;
        RaycastHit[] hitObjects = Physics.RaycastAll(transform.position, offset, offset.magnitude, furnitureMask);
		HashSet<Renderer> affectedThisFrame = new HashSet<Renderer>();

        for (int i = 0; i < hitObjects.Length; ++i)
        {
			var rend = hitObjects[i].transform.GetComponent<Renderer>();
			if (rend == null) continue;
			affectedThisFrame.Add(rend);
            Material[] materials = rend.materials;

            for (int m = 0; m < materials.Length; ++m)
            {
				// Match Shader Graph property names
				materials[m].SetVector("_CutoutPosition", cutoutPos);
                materials[m].SetFloat("_CutoutSize", 0.2f);
                materials[m].SetFloat("_FalloffSize", 0.05f);
            }
        }
		// Disable cutout on previously affected renderers that are no longer hit
		foreach (var rend in previouslyAffected)
		{
			if (rend == null) continue;
			if (affectedThisFrame.Contains(rend)) continue;
			var mats = rend.materials;
			for (int m = 0; m < mats.Length; m++)
			{
				mats[m].SetFloat("_CutoutSize", 0f);
			}
		}
		previouslyAffected.Clear();
		foreach (var r in affectedThisFrame) previouslyAffected.Add(r);
    }
}

/*
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Code From: https://github.com/daniel-ilett/shaders-wall-cutout/blob/main/Assets/Scripts/CutoutObject.cs

public class CutoutObject : MonoBehaviour
{
    [SerializeField]
    private Transform targetObject;
		[SerializeField]
		private Transform[] additionalTargets;

    [SerializeField]
    private LayerMask furnitureMask;

		[SerializeField] private float baseCutoutSize = 0.1f;
		[SerializeField] private float baseFalloffSize = 0.05f;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
        targetObject = GameObject.FindGameObjectWithTag("MainTower").transform;
    }

    private void Update()
    {
			// Build targets list
			List<Transform> targets = new List<Transform>();
			if (targetObject != null) targets.Add(targetObject);
			if (additionalTargets != null && additionalTargets.Length > 0)
			{
				for (int i = 0; i < additionalTargets.Length; i++)
				{
					if (additionalTargets[i] != null) targets.Add(additionalTargets[i]);
				}
			}
			if (targets.Count == 0) return;

			// Centroid and viewport coverage radius
			Vector3 centroidWorld = Vector3.zero;
			for (int i = 0; i < targets.Count; i++) centroidWorld += targets[i].position;
			centroidWorld /= targets.Count;

			Vector2 centroidViewport = mainCamera.WorldToViewportPoint(centroidWorld);
			centroidViewport.y /= (Screen.width / Screen.height);

			float maxViewportRadius = 0f;
			for (int i = 0; i < targets.Count; i++)
			{
				Vector2 vp = mainCamera.WorldToViewportPoint(targets[i].position);
				vp.y /= (Screen.width / Screen.height);
				float d = Vector2.Distance(vp, centroidViewport);
				if (d > maxViewportRadius) maxViewportRadius = d;
			}
			float cutoutSize = baseCutoutSize + maxViewportRadius;
			float falloffSize = baseFalloffSize;

			Vector3 offset = centroidWorld - transform.position;
        RaycastHit[] hitObjects = Physics.RaycastAll(transform.position, offset, offset.magnitude, furnitureMask);

        for (int i = 0; i < hitObjects.Length; ++i)
        {
            Material[] materials = hitObjects[i].transform.GetComponent<Renderer>().materials;

            for (int m = 0; m < materials.Length; ++m)
            {
					materials[m].SetVector("_CutoutPos", centroidViewport);
					materials[m].SetFloat("_CutoutSize", cutoutSize);
					materials[m].SetFloat("_FalloffSize", falloffSize);
            }
        }
    }
}
*/