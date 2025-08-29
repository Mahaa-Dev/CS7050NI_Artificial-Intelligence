using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Add this line
public class PathfindingTester : MonoBehaviour
{
    // The A* manager.
    private AStarManager AStarManager = new AStarManager();
    // List of possible waypoints.
    private List<GameObject> Waypoints = new List<GameObject>();
    // List of waypoint map connections. Represents a path.
    private List<Connection> ConnectionArray = new List<Connection>();
    // The start and end nodes.
    [SerializeField]
    private GameObject start;
    [SerializeField]
    private GameObject end;


    // Debug line offset.
    Vector3 OffSet = new Vector3(0, 0.3f, 0);
    // Start is called before the first frame update
    // Movement variables.
    [SerializeField]
    public float currentSpeed = 80;
    [SerializeField]
    private int noOfParcel;

    private int currentTarget = 0;
    private Vector3 currentTargetPos;
    private int moveDirection = 1;
    public bool agentMove = true;

    private string speedText;
    private string distanceText;
    // private string distanceNode;
    private string strStart;

    private AudioSource audioSource;
    [SerializeField]
    private float rotationSpeed = 5f; // Add this line for rotation speed

    [SerializeField] private GameObject medicalParcel; // Reference to the parcel prefab
    private Rigidbody parcelRigidbody; // Rigidbody of the instantiated medical parcel


    private Quaternion initialRotation; // Initial rotation of the drone

    public float updateInterval = 0.1f; // Interval for updating UI information

    private string avoidCollisionText;
    void Start()
    {
        strStart = "Delivering";
        avoidCollisionText = "No Collision";
        distanceText = "0000";

        StartCoroutine(UpdateAgentInfo());
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(PauseAgentForSeconds(1f));

        initialRotation = transform.rotation;



        if (start == null || end == null)
        {
            Debug.Log("No start or end waypoints.");
            return;
        }
        VisGraphWaypointManager tmpWpM = start.GetComponent<VisGraphWaypointManager>();
        if (tmpWpM == null)
        {
            Debug.Log("Start is not a waypoint.");
            return;
        }
        tmpWpM = end.GetComponent<VisGraphWaypointManager>();
        if (tmpWpM == null)
        {
            Debug.Log("End is not a waypoint.");
            return;
        }
        // Find all the waypoints in the level.
        GameObject[] GameObjectsWithWaypointTag;
        GameObjectsWithWaypointTag = GameObject.FindGameObjectsWithTag("Waypoint");
        foreach (GameObject waypoint in GameObjectsWithWaypointTag)
        {
            VisGraphWaypointManager tmpWaypointMan = waypoint.GetComponent<VisGraphWaypointManager>();
            if (tmpWaypointMan)
            {
                Waypoints.Add(waypoint);
            }
        }
        // Go through the waypoints and create connections.
        foreach (GameObject waypoint in Waypoints)
        {
            VisGraphWaypointManager tmpWaypointMan = waypoint.GetComponent<VisGraphWaypointManager>();
            // Loop through a waypoints connections.
            foreach (VisGraphConnection aVisGraphConnection in tmpWaypointMan.Connections)
            {
                if (aVisGraphConnection.ToNode != null)
                {
                    Connection aConnection = new Connection();
                    aConnection.FromNode = waypoint;
                    aConnection.ToNode = aVisGraphConnection.ToNode;
                    AStarManager.AddConnection(aConnection);
                }
                else
                {
                    Debug.Log("Warning, " + waypoint.name + " has a missing to node for a connection!");
                }
            }
        }
        // Run A Star...
        // ConnectionArray stores all the connections in the route to the goal / end node.
        ConnectionArray = AStarManager.PathfindAStar(start, end);
        if (ConnectionArray.Count == 0)
        {
            Debug.Log("Warning, A* did not return a path between the start and end node.");
        }
    }
    // Draws debug objects in the editor and during editor play (if option set).
    void OnDrawGizmos()
    {
        // Draw path.
        foreach (Connection aConnection in ConnectionArray)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine((aConnection.FromNode.transform.position + OffSet), (aConnection.ToNode.transform.position + OffSet));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (agentMove)
        {

            // Determine the direction to first node in the array.
            if (moveDirection > 0)
            {
                currentTargetPos = ConnectionArray[currentTarget].ToNode.transform.position;
            }
            else
            {
                currentTargetPos = ConnectionArray[currentTarget].FromNode.transform.position;
            }
            // Clear y to avoid up/down movement. Assumes flat surface.
            currentTargetPos.y = transform.position.y;
            Vector3 direction = currentTargetPos - transform.position;
            // Calculate the length of the relative position vector
            float distance = direction.magnitude;

            // Calculate the normalised direction to the target from a game object.
            Vector3 normDirection = direction.normalized;
            // Face in the right direction.
            direction.y = 0;
            //total distance of the all nodes
            float totalDistance = 0;

            for (int i = 0; i < ConnectionArray.Count; i++)
            {
                totalDistance += Vector3.Distance(ConnectionArray[i].FromNode.transform.position, ConnectionArray[i].ToNode.transform.position);
            }

            distanceText = (totalDistance * 2).ToString("F0");

            if (currentTarget == 0)
            {

                if (moveDirection > 0)
                {
                    transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, 100, transform.position.z), currentSpeed * Time.deltaTime);

                }
                else if (moveDirection < 0 && distance < 100)
                {
                    transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, 10, transform.position.z), currentSpeed * Time.deltaTime);

                }

            }
            //if last node in the path, fly down before moving towards target
            if (currentTarget == ConnectionArray.Count - 1)
            {

                if (moveDirection > 0 && distance < 100)
                {
                    transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, 10, transform.position.z), currentSpeed * Time.deltaTime);
                }
                else if (moveDirection < 0)
                {
                    transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, 100, transform.position.z), currentSpeed * Time.deltaTime);
                }
            }


            if (direction.magnitude > 0)
            {
                Quaternion rotation = Quaternion.LookRotation(normDirection, Vector3.up); // Change this line
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * rotationSpeed);
            }

            // Move the game object.
            transform.position = Vector3.MoveTowards(transform.position, currentTargetPos, currentSpeed * Time.deltaTime);
            // Check if close to current target.
            if (distance < 1)
            {
                if (moveDirection > 0)
                {

                    currentTarget++;
                    if (currentTarget >= ConnectionArray.Count)
                    {
                        // Reached the end of the path.
                        // return to start
                        strStart = "Delivered & Returning";
                        currentTarget = ConnectionArray.Count - 1;
                        moveDirection = -1;

                        // Drop parcels and spawn multiple based on noOfParcel
                        DropParcels();

                        //increase the speed of the drone by linear function 10% of the speed times noOfParcel
                        while (noOfParcel > 0)
                        {
                           currentSpeed = currentSpeed + (currentSpeed * 0.1f);
                            noOfParcel--;
                        }

                    }
                }
                else
                {
                    currentTarget--;
                    if (currentTarget < 0)
                    {
                        transform.position = new Vector3(transform.position.x, 6, transform.position.z);
                        StartCoroutine(RotateBackToInitialRotation(2f));
                        strStart = "Reached Hospital";
                        agentMove = false;
                        audioSource.Stop();
                    }
                }
            }


        }
        else
        {
        }
    }


    //drone with less speed needs to change or wait and give a way fro another drone to pass.
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Drone")
        {

            PathfindingTester pathfinder = other.gameObject.GetComponent<PathfindingTester>();
            //get the current speed of the drone
            float speed = pathfinder.currentSpeed;
            string name = other.gameObject.name;
            //get the gameobject name of the drone which is in the collision
            string name1 = this.gameObject.name;
            //get the speedof name 1 drone
            PathfindingTester pathfinder1 = this.gameObject.GetComponent<PathfindingTester>();

            float speed1 = pathfinder1.currentSpeed;
            if (speed >= speed1)
            {
                avoidCollisionText = name1 + " avoided collision with " + name;
                StartCoroutine(avoidCollision(0.5f, pathfinder1));
            }
            else
            {
                avoidCollisionText = name + " avoided collision with " + name1;
                StartCoroutine(avoidCollision(0.5f, pathfinder));
            }
        }
    }
    //when the drone is out of the collision
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Drone")
        {
            strStart = "Delivering";
        }
    }

    // Pause the agent for a specified duration
    private IEnumerator PauseAgentForSeconds(float seconds)
    {
        // Stop the agent
        agentMove = false;
        // Wait for the specified duration
        yield return new WaitForSeconds(seconds);
        // Resume the agent after the wait
        agentMove = true;
    }

    // Rotate the agent back to the initial rotation
    IEnumerator RotateBackToInitialRotation(float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            // Interpolate between the current rotation and the initial rotation
            transform.rotation = Quaternion.Lerp(transform.rotation, initialRotation, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // Ensure the final rotation is set to the initial rotation
        transform.rotation = initialRotation;
    }



    // Update the agent information on the UI
    IEnumerator UpdateAgentInfo()
    {
        while (true)
        {
            string infoString = UpdateUI();
            UpdateTextComponent(infoString);
            yield return new WaitForSeconds(updateInterval);
        }
    }
    // Build the information string for the UI
    string UpdateUI()
    {
        // Find all game objects with the "Drone" tag
        GameObject[] drones = GameObject.FindGameObjectsWithTag("Drone");

        // Build the information string header
        string infoText = "Agent Information:\n\n\n";
        infoText += "Name:\t\t\t\t\t\tSpeed:\t\t\t\tFlight Distance:\t\t\tParcels:\t\t\tDrone Status:\n\n";

        // Build the information string for each drone
        foreach (GameObject drone in drones)
        {
            PathfindingTester pathfinder = drone.GetComponent<PathfindingTester>();

            if (pathfinder != null)
            {
                infoText += $"{drone.name}\t\t\t\t{pathfinder.currentSpeed:F2} m/s\t\t\t\t{pathfinder.distanceText} m\t\t\t\t\t\t{pathfinder.noOfParcel}\t\t\t\t\t{pathfinder.strStart}\n\n";
            }
        }

        return infoText;
    }

    // Update the Text component with the information string
    void UpdateTextComponent(string infoString)
    {
        // Find the Text component by name
        Text agentInfoText = GameObject.Find("agentInfoText")?.GetComponent<Text>();

        Text agentCollisionText = GameObject.Find("agentCollision")?.GetComponent<Text>();

        // Update the Text component with the information string
        if (agentInfoText != null)
        {

            agentInfoText.text = infoString;
        }
        if (agentCollisionText != null)
        {

            agentCollisionText.text = "Agent Collision Information: \n\n" + avoidCollisionText;
        }
    }
    // Drop parcels and spawn multiple based on noOfParcel
    void DropParcels()
    {
        // Check if the parcel prefab is assigned
        if (medicalParcel == null)
        {
            Debug.LogError("Parcel prefab not assigned.");
            return;
        }

        // Drop parcels and spawn multiples based on noOfParcel
        for (int i = 0; i < noOfParcel; i++)
        {
            // Instantiate a parcel at the drone's position
            GameObject parcel = Instantiate(medicalParcel, transform.position, Quaternion.identity);

            // Access the Rigidbody component attached to the drone
            Rigidbody parcelRigidbody = parcel.GetComponent<Rigidbody>();

            // If Rigidbody exists on the parcel prefab, enable gravity-like effect
            if (parcelRigidbody != null)
            {
                parcelRigidbody.useGravity = true; // Disable default gravity
                parcelRigidbody.isKinematic = false; // Set isKinematic to false to allow physics interactions

                // Set initial velocity to simulate gravity-like effect
                float gravityStrength = 10f; // You can adjust this value
                parcelRigidbody.velocity = Vector3.down * gravityStrength; // Simulate gravity by setting initial downward velocity
            }

            // Customize the spawned parcel's Transform directly
            CustomizeParcelTransform(parcel.transform, i);
        }
        StartCoroutine(PauseAgentForSeconds(2f));
        Destroy(medicalParcel);
        StartCoroutine(PauseAgentForSeconds(2f));
    }

    // Customize the spawned parcel's Transform directly
    void CustomizeParcelTransform(Transform parcelTransform, int index)
    {
        parcelTransform.position += Vector3.up * index;
        // // Offset each parcel vertically
        // parcelTransform.position += Vector3.up * index;

        // // Introduce randomness for scattering
        // float scatterRange = 0.5f; // Adjust this value based on how scattered you want the parcels
        // float randomX = Random.Range(-scatterRange, scatterRange);
        // float randomZ = Random.Range(-scatterRange, scatterRange);

        // // Apply random offsets to X and Z axes
        // parcelTransform.position += new Vector3(randomX, 0f, randomZ);

        // // Scale the parcel
        float scaleMultiplier = Random.Range(0.8f, 1.2f);
        parcelTransform.localScale = new Vector3(2, 2, 2);
    }

    // Avoid collision with another drone
    private IEnumerator avoidCollision(float seconds, PathfindingTester pathfinder)
    {
        // Store the initial position of the pathfinder
        Vector3 initialPosition = pathfinder.transform.position;

        float elapsedTime = 0f;

        while (elapsedTime < seconds)
        {
            pathfinder.transform.position = Vector3.Lerp(initialPosition, new Vector3(initialPosition.x, 130, initialPosition.z), elapsedTime / seconds);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        initialPosition = pathfinder.transform.position;
        elapsedTime = 0;
        while (elapsedTime < 1)
        {
            pathfinder.transform.position = Vector3.Lerp(initialPosition, new Vector3(initialPosition.x, 100, initialPosition.z), elapsedTime / seconds);

            elapsedTime += Time.deltaTime;
            yield return null;
        }


    }


}
