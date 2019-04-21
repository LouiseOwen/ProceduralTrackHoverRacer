using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class RoadMaker : MonoBehaviour
{
    const float WAYPOINT_DIFF = 22.5f; // difference in angle to place next waypoint

    public float radius = 90.0f; // can be pretty much anything, just says how big the circle is N.B. bigger radius smooths waviness (probably cap this variable)
    private float segments = 300.0f; // number of extrusions, helps calculate number of points in track by determining degrees between each point (300 appears to be best number, may as well make consistent)

    [SerializeField] private GameObject car;
    [SerializeField] private GameObject finishLine;
    [SerializeField] private GameObject lapChecker;

    // Road dimentions
    private float lineWidth = 0.3f; // width central line
    private float roadWidth = 15.0f;
    private float edgeWidth = 0.5f; // walls
    private float edgeHeight = 2.5f;

    public float waviness = 80.0f; // can really be any positive number, major errors only occur by start (make fixed starting straight? or just keep this value steady)
    private float waveScale = 0.1f; // keep steady 0.1

    public Vector2 waveOffset = new Vector2(256.0f, 0.0f); // sets up adding waviness, both numbers can be anything?! maybe make this the user adjust
    private Vector2 waveStep = new Vector2(0.3f, 0.3f); // keep steady

    private bool stripeCheck = true; // for the edge walls (to make stripey)

    [SerializeField] private GameObject waypoint;
    [SerializeField] private Transform waypoints;
    [SerializeField] private WaypointCircuit waypointCircuit;
 
    void Start ()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        MeshBuilder mb = new MeshBuilder(6);

        float segmentDegrees = 360.0f / segments;

        List<Vector3> points = new List<Vector3>();

        // Makes circle out of points
        for (float degrees = 0.0f; degrees < 360.0f; degrees += segmentDegrees)
        {
            Vector3 point = Quaternion.AngleAxis(degrees, Vector3.up) * Vector3.forward * radius;
            // could add height varience here?
            points.Add(point);
        }

        Vector2 wave = waveOffset;

        // Adds waviness to the circle (push/pull point out or inwards, based on perlin noise)
        for (int i = 0; i < points.Count; i++)
        {
            wave += waveStep;

            Vector3 centerDir = points[i].normalized; // essentially scales down the point position so we can easily apply waviness

            float sample = Mathf.PerlinNoise(wave.x * waveScale, wave.y * waveScale); // remember PerlinNoise is between 0.0 and 1.0
            sample *= waviness; // scales sample up

            float control = Mathf.PingPong(i, points.Count / 2.0f) / (points.Count / 2.0f); // to stop the start/end join from being jaggered

            points[i] += centerDir * sample * control;
        }

        // Gets the centre points on which to build the road (then sends to ExtrudeRoad())
        for (int i = 1; i < points.Count + 1; i++)
        {
            Vector3 pPrev = points[i - 1]; // prev point
            Vector3 p0 = points[i % points.Count]; // current point
            Vector3 p1 = points[(i + 1) % points.Count]; // next point
            // modulus to keep the index within the bounds of the list

            ExtrudeRoad(mb, pPrev, p0, p1);
        }

        car.transform.position = points[0];
        car.transform.LookAt(points[1]);

        finishLine.transform.position = points[2];
        finishLine.transform.LookAt(points[3]);
        finishLine.transform.position += new Vector3(0.0f, 5.0f, 0.0f);
        finishLine.transform.localScale = new Vector3(roadWidth * 2, finishLine.transform.localScale.y, finishLine.transform.localScale.z);

        int lapCheckerWaypoint = points.Count - 10;
        lapChecker.transform.position = points[lapCheckerWaypoint];
        lapChecker.transform.LookAt(points[lapCheckerWaypoint + 1]);
        lapChecker.transform.position += new Vector3(0.0f, 5.0f, 0.0f);
        //finishLine.transform.localScale = new Vector3(roadWidth * 2, finishLine.transform.localScale.y, finishLine.transform.localScale.z);

        meshFilter.mesh = mb.CreateMesh();
        meshCollider.sharedMesh = meshFilter.mesh;

        Vector3 current = points[0];
        for (int i = 1; i < points.Count; i++)
        {
            if (Vector3.Angle(current, points[i]) > WAYPOINT_DIFF)
            {
                Vector3 forward = (points[i + 1] - points[i]).normalized;
                Quaternion lookRot = Quaternion.LookRotation(forward);
                GameObject newWaypoint = Instantiate(waypoint, points[i], lookRot, waypoints);
                newWaypoint.name = "Waypoint " + waypoints.childCount.ToString("000");
                Vector3 scale = newWaypoint.transform.localScale;
                scale.x = roadWidth * 2;
                newWaypoint.transform.localScale = scale;
                current = points[i];
            }
        }
        waypointCircuit.CreateRouteUsingChildObjects();
        waypointCircuit.CachePositionsAndDistances(); // might not need (test this)
    }

    private void ExtrudeRoad(MeshBuilder mb, Vector3 pPrev, Vector3 p0, Vector3 p1)
    {
        // Roadline
        Vector3 offset = Vector3.zero; // the position to put the feature relative to the centre points
        Vector3 target = Vector3.forward * lineWidth; // how far the mesh shoud be extruded

        MakeRoadQuad(mb, pPrev, p0, p1, offset, target, 0);

        // Road
        offset += target;
        target = Vector3.forward * roadWidth;

        MakeRoadQuad(mb, pPrev, p0, p1, offset, target, 1);

        // Edge wall meshes (makes stripey)
        int stripeSubmesh = 2;

        if (stripeCheck)
        {
            stripeSubmesh = 3;
        }

        stripeCheck = !stripeCheck;

        // Edge
        offset += target;
        target = Vector3.up * edgeHeight;

        MakeRoadQuad(mb, pPrev, p0, p1, offset, target, stripeSubmesh);

        // Edge top
        offset += target;
        target = Vector3.forward * edgeWidth;

        MakeRoadQuad(mb, pPrev, p0, p1, offset, target, stripeSubmesh);

        // Edge outer
        offset += target;
        target = -Vector3.up * edgeHeight;

        MakeRoadQuad(mb, pPrev, p0, p1, offset, target, stripeSubmesh);
    }

    private void MakeRoadQuad(MeshBuilder mb, Vector3 pPrev, Vector3 p0, Vector3 p1, Vector3 offset, Vector3 targetOffset, int submesh)
    {
        Vector3 forward = (p1 - p0).normalized; // the direction the current road point is facing
        Vector3 forwardPrev = (p0 - pPrev).normalized; // the direction the previous road point was facing

        // Build outer (left of center line points)
        Quaternion perp = Quaternion.LookRotation(Vector3.Cross(forward, Vector3.up)); // (perpendicular) cross product gives the perpendicular axis
        Quaternion perpPrev = Quaternion.LookRotation(Vector3.Cross(forwardPrev, Vector3.up)); // also need the perpendicular axis of the previous point (I think these are world-space and that Unity is right-handed axis)
        // Creates quads to make up road
        Vector3 br = p0 + (perpPrev * offset); // perp rotates the offset to face outwards
        Vector3 bl = p0 + (perpPrev * (offset + targetOffset));

        Vector3 tr = p1 + (perp * offset);
        Vector3 tl = p1 + (perp * (offset + targetOffset));

        mb.BuildTriangle(br, bl, tr, submesh);
        mb.BuildTriangle(bl, tl, tr, submesh);

        // Build inner (right of center line points)
        perp = Quaternion.LookRotation(Vector3.Cross(-forward, Vector3.up)); // remember that Unity is right-handed axis
        perpPrev = Quaternion.LookRotation(Vector3.Cross(-forwardPrev, Vector3.up));

        bl = p0 + (perpPrev * offset); // perp values are now flipped so must restructure quad points
        br = p0 + (perpPrev * (offset + targetOffset));

        tl = p1 + (perp * offset);
        tr = p1 + (perp * (offset + targetOffset));

        mb.BuildTriangle(br, bl, tr, submesh);
        mb.BuildTriangle(bl, tl, tr, submesh);
    }
}
