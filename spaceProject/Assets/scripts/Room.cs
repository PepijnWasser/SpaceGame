using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Room : MonoBehaviour
{
	public Doorway[] doorways;
	public MeshCollider[] meshcolliders;

    [HideInInspector]
	public List<Bounds> RoomBounds
    {
        get
        {
            List<Bounds> myBounds = new List<Bounds>();
            foreach (MeshCollider meshCollider in meshcolliders)
            {
                myBounds.Add(meshCollider.bounds);
            }
            return myBounds;
        }
    }

}



