﻿using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class StartMenuRoad : MonoBehaviour
{
    // Simplified RoadMaker for start menu track demo

    const float WAYPOINT_DIFF = 16.0f; //22.5f; // difference in angle to place next waypoint WEIRD ERRORS AT 15.0F

    public float radius /*= 90.0f*/; // can be pretty much anything (better to be positive), just says how big the circle is N.B. bigger radius smooths waviness (probably cap this variable)
    private float segments = 300.0f; // number of extrusions, helps calculate number of points in track by determining degrees between each point (300 appears to be best number, may as well make consistent)

    // Road dimentions
    private float lineWidth = 0.3f; // width central line
    private float roadWidth = 15.0f;
    private float edgeWidth = 0.5f; // walls
    private float edgeHeight = 2.5f;

    public float waviness /*= 80.0f*/; // max:100 can really be any positive number, major errors only occur by start (make fixed starting straight? or just keep this value steady)
    private float waveScale = 0.1f; // keep steady 0.1

    public Vector2 waveOffset /*= new Vector2(256.0f, 0.0f)*/; // max:360 (have both numbers set to same thing?) sets up adding waviness, both numbers can be anything?! maybe make this the user adjust
    private Vector2 waveStep = new Vector2(0.3f, 0.3f); // keep steady

    private bool stripeCheck = true; // for the edge walls (to make stripey)

    void Update()
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

        meshFilter.mesh = mb.CreateMesh();
        meshCollider.sharedMesh = meshFilter.mesh;
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

    public void SetRadius(float value)
    {
        radius = value;
    }

    public void SetWaviness(float value)
    {
        waviness = value;
    }

    public void SetWaveOffsetX(float value)
    {
        waveOffset.x = value;
    }

    public void SetWaveOffsetY(float value)
    {
        waveOffset.y = value;
    }
}
