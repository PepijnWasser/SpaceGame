using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

public class LevelBuilder : MonoBehaviour
{
	public bool spawnStartRoom = true;
	public Doorway startdoor;
	public Room startRoomPrefab;
	public List<Room> roomPrefabs = new List<Room>();


	public Vector2 minMaxRooms = new Vector2(3, 10);
	public Vector2 spawnableArea = new Vector2(20, 20);

	public List<Doorway> availableDoorways = new List<Doorway>();

	public Dictionary<Doorway, Doorway> removedDoorDoorDictionary = new Dictionary<Doorway, Doorway>();

	ConsoleRoom consoleRoom;
	List<Room> placedRooms = new List<Room>();

	LayerMask roomLayerMask;
	Color roomColor;

	Room NewestRoom;

	void Start()
	{
		roomLayerMask = LayerMask.GetMask("Room");
		StartCoroutine("GenerateLevel");
	}

	IEnumerator GenerateLevel()
	{
		roomColor = Random.ColorHSV();
		WaitForFixedUpdate interval = new WaitForFixedUpdate();

		Debug.Log("\n\n");
        if (spawnStartRoom)
        {
			// Place start room
			PlaceConsoleRoom();
		}
        else
        {
			AddDoorwayToList(startdoor, ref availableDoorways);
        }
		yield return interval;

		// Random iterations
		int iterations = Random.Range((int)minMaxRooms.x, (int)minMaxRooms.y);

		for (int i = 0; i < iterations; i++)
		{
			// Place random room from list
			SpawnRoom();
			RemoveDoorsInSameSpace();
			yield return interval;
		}

		//yield return new WaitForSeconds(3);
		//ResetLevelGenerator ();
		StopCoroutine("GenerateLevel");
	}

	void PlaceConsoleRoom()
	{
		// Instantiate room
		consoleRoom = Instantiate(startRoomPrefab) as ConsoleRoom;
		consoleRoom.transform.parent = this.transform;

		// Get doorways from current room and add them randomly to the list of available doorways
		AddDoorwaysToList(consoleRoom, ref availableDoorways);

		// Position room
		consoleRoom.transform.position = Vector3.zero;
		consoleRoom.transform.rotation = Quaternion.identity;
	}

	void SpawnRoom()
	{
		bool roomPlaced = false;
		
		Room roomToTest = roomPrefabs[Random.Range(0, roomPrefabs.Count)];
		List<Room> roomsCheckedList = new List<Room>();

		//check for availible position and rotation
		while(roomsCheckedList.Count < roomPrefabs.Count)
        {
			while (roomsCheckedList.Contains(roomToTest))
			{
				roomToTest = roomPrefabs[Random.Range(0, roomPrefabs.Count)];
			}

			Room currentRoom = Instantiate(roomPrefabs[Random.Range(0, roomPrefabs.Count)]) as Room;
			currentRoom.transform.parent = this.transform;

			//give it a color
			currentRoom.GetComponentInChildren<Renderer>().sharedMaterial.color = roomColor;

			List<Doorway> allAvailableDoorways = new List<Doorway>(availableDoorways);
			AddDoorwaysToList(currentRoom, ref availableDoorways);

			Doorway existingDoorwayToTest = allAvailableDoorways[Random.Range(0, allAvailableDoorways.Count)];
			List<Doorway> existingDoorsCheckedList = new List<Doorway>();

			while (existingDoorsCheckedList.Count < allAvailableDoorways.Count)
			{
				while (existingDoorsCheckedList.Contains(existingDoorwayToTest))
				{
					existingDoorwayToTest = allAvailableDoorways[Random.Range(0, allAvailableDoorways.Count)];
				}

				List<Doorway> currentRoomDoorways = new List<Doorway>();
				AddDoorwaysToList(currentRoom, ref currentRoomDoorways);

				Doorway newDoorwayToTest = currentRoomDoorways[Random.Range(0, currentRoomDoorways.Count)];
				List<Doorway> newDoorsCheckedList = new List<Doorway>();

				while (newDoorsCheckedList.Count < currentRoomDoorways.Count)
				{
					while (newDoorsCheckedList.Contains(newDoorwayToTest))
					{
						newDoorwayToTest = currentRoomDoorways[Random.Range(0, currentRoomDoorways.Count)];
					}
					// Position room
					PositionRoomAtDoorway(ref currentRoom, newDoorwayToTest, existingDoorwayToTest);

					// Check room overlaps
					if (!CheckRoomOverlap(currentRoom) && CheckRoomWithinBounds(existingDoorwayToTest))
					{
						roomPlaced = true;

						// Add room to list
						placedRooms.Add(currentRoom);
						NewestRoom = currentRoom;

						// Remove occupied doorways
						newDoorwayToTest.gameObject.SetActive(false);
						availableDoorways.Remove(newDoorwayToTest);

						existingDoorwayToTest.gameObject.SetActive(false);
						availableDoorways.Remove(existingDoorwayToTest);

						removedDoorDoorDictionary.Add(existingDoorwayToTest, newDoorwayToTest);

						// Exit loop if room has been placed
					}
					if (roomPlaced)
					{
						break;
					}
					else
					{
						newDoorsCheckedList.Add(newDoorwayToTest);
					}
				}
				if (roomPlaced)
				{
					break;
				}
				else
				{
					existingDoorsCheckedList.Add(existingDoorwayToTest);
				}
			}
			if (roomPlaced)
			{
				break;
			}
			else
			{
				roomsCheckedList.Add(roomToTest);
				Destroy(currentRoom.gameObject);
				RemoveDoorwaysToList(currentRoom, ref availableDoorways);
			}
		}
        if (!roomPlaced)
        {
			ResetLevelGenerator();
        }
	}

	void AddDoorwaysToList(Room room, ref List<Doorway> list)
	{
		foreach (Doorway doorway in room.doorways)
		{
			int r = Random.Range(0, list.Count);
			list.Insert(r, doorway);
		}
	}

	void AddDoorwayToList(Doorway doorway, ref List<Doorway> list)
	{
		int r = Random.Range(0, list.Count);
		list.Insert(r, doorway);		
	}

	void RemoveDoorwaysToList(Room room, ref List<Doorway> list)
	{
		foreach (Doorway doorway in room.doorways)
		{
			list.Remove(doorway);
		}
	}

	void PositionRoomAtDoorway(ref Room room, Doorway roomDoorway, Doorway targetDoorway)
	{

		// Reset room position and rotation
		room.transform.position = Vector3.zero;
		room.transform.rotation = Quaternion.identity;

		// Rotate room to match previous doorway orientation
		Vector3 targetDoorwayEuler = targetDoorway.transform.eulerAngles;
		targetDoorwayEuler.y = Mathf.Round(targetDoorwayEuler.y * 100.0f) * 0.01f;

		Vector3 roomDoorwayEuler = roomDoorway.transform.eulerAngles;
		float deltaAngle = Mathf.DeltaAngle(roomDoorwayEuler.y, targetDoorwayEuler.y);
		Quaternion RotationToCompensateDeltaAngle = Quaternion.AngleAxis(deltaAngle, Vector3.up);
		room.transform.rotation = (RotationToCompensateDeltaAngle * Quaternion.Euler(0, 180f, 0));

		Quaternion roomRotation = room.transform.rotation;
		roomRotation = Quaternion.Euler(0.0f, Mathf.Round(roomRotation.eulerAngles.y * 100.0f) * 0.01f, 0.0f);
		room.transform.rotation = roomRotation;


		// Position room
		Vector3 roomPositionOffset = roomDoorway.transform.position - room.transform.position;
		Vector3 newVec = targetDoorway.transform.position - roomPositionOffset;
		newVec = new Vector3(Mathf.Round(newVec.x * 100.0f) * 0.01f, Mathf.Round(newVec.y * 100.0f) * 0.01f, Mathf.Round(newVec.z * 100.0f) * 0.01f);

		room.transform.position = newVec;
	}
	

	bool CheckRoomOverlap(Room room)
	{
		Bounds[] bounds = room.RoomBounds.ToArray();
		for(int i = 0; i < bounds.Count(); i++)
        {
			bounds[i].center = room.transform.position;
			bounds[i].Expand(-0.1f);
		}
		for(int i = 0; i < bounds.Count(); i++)
        {
			Collider[] colliders = Physics.OverlapBox(bounds[i].center, bounds[i].size / 2, room.transform.rotation, roomLayerMask);
			if (colliders.Length > 0)
			{
				// Ignore collisions with current room
				foreach (Collider c in colliders)
				{
					if (c.transform.parent.gameObject.Equals(room.gameObject))
					{
						continue;
					}
					else
					{
						return true;
					}
				}
			}

		}
		return false;
	}

	bool CheckRoomWithinBounds(Doorway doorway)
    {
		Vector3 parentPosition = doorway.transform.parent.transform.parent.position;
		float doorX = doorway.transform.position.x;
		float doorZ = doorway.transform.position.z;
		
		float maxX = (spawnableArea.x / 2) + parentPosition.x;
		float minX = -(spawnableArea.x / 2) + parentPosition.x;

		float maxZ = (spawnableArea.y / 2) + parentPosition.z;
		float minZ = -(spawnableArea.y / 2) + parentPosition.z;

		if (doorX <  maxX && doorX > minX && doorZ < maxZ && doorZ > minZ)
        {
			return true;
        }
        else
        {
			return false;
        }
    }

	void RemoveDoorsInSameSpace()
    {
		List<Doorway> allAvailableDoorways = new List<Doorway>(availableDoorways);

		foreach (Doorway doorway in allAvailableDoorways)
        {
			foreach(Doorway doorway2 in allAvailableDoorways)
            {
				//remove doorways if position is the same
				if ((doorway.transform.position - doorway2.transform.position).magnitude < 0.001 && doorway != doorway2)
                {
					doorway.gameObject.SetActive(false);
					availableDoorways.Remove(doorway);

					doorway2.gameObject.SetActive(false);
					availableDoorways.Remove(doorway2);
				}
            }
        }
    }

	void ResetLevelGenerator()
	{
		Debug.Log("Reset level generator");

		StopCoroutine("GenerateLevel");

		// Delete all rooms
		if (consoleRoom)
		{
			DestroyImmediate(consoleRoom.gameObject);
		}

		foreach (Room room in placedRooms)
		{
			if(room != null)
            {
				 DestroyImmediate(room.gameObject);
			}
		}

		// Clear lists
		placedRooms.Clear();
		availableDoorways.Clear();
		removedDoorDoorDictionary.Clear();

		// Restart coroutine
		EditorCoroutineUtility.StartCoroutineOwnerless(GenerateLevel());
	}

	void TestRoomRemoval()
	{
		Dictionary<Doorway, Doorway> removedDoorDoorCopy = new Dictionary<Doorway, Doorway>(removedDoorDoorDictionary);
		foreach (KeyValuePair<Doorway, Doorway> keyValue in removedDoorDoorCopy)
		{
			Doorway doorKey = null;
			Doorway doorValue = null;

			if (keyValue.Key != null)
            {
				doorKey = keyValue.Key;
			}
			
			if(keyValue.Value != null)
            {
				doorValue = keyValue.Value;
			}

			if(doorValue == null || doorKey == null)
            {
				if(doorValue == null)
                {
					Debug.Log("doorValue was nul");
					doorKey.gameObject.SetActive(true);
					availableDoorways.Add(doorKey);
                }
                else
                {
					Debug.Log("doorKey was nul");
					doorValue.gameObject.SetActive(true);
					availableDoorways.Add(doorValue);
                }
				removedDoorDoorDictionary.Remove(doorKey);
            }
		}
		List<Doorway> doorwaysToRemove = new List<Doorway>();
		foreach(Doorway doorway in availableDoorways)
        {
			if(doorway == null)
            {
				doorwaysToRemove.Add(doorway);
            }
        }
		foreach(Doorway doorway in doorwaysToRemove)
        {
			availableDoorways.Remove(doorway);
        }
		doorwaysToRemove.Clear();
	}

	public void GenerateNewLevel()
    {
		roomLayerMask = LayerMask.GetMask("Room");
		ResetLevelGenerator();
    }

    private void Update()
    {
		TestRoomRemoval();
        if (Input.GetKeyDown(KeyCode.E))
        {
			SpawnRoom();
			RemoveDoorsInSameSpace();
        }
		if (Input.GetKeyDown(KeyCode.Z))
		{
			if(NewestRoom != null)
            {
				Destroy(NewestRoom.gameObject);
			}
		}
	}
}
