﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkArea : MonoBehaviour {
	
	public Vector3 center;
	public Vector3 area;

	void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, area);
    }
}
