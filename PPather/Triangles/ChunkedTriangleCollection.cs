﻿/*
 *  Part of PPather
 *  Copyright Pontus Borg 2008
 *
 */

using System;
using System.Numerics;
using System.Collections.Generic;
using Wmo;
using Microsoft.Extensions.Logging;
using PPather.Graph;
using static System.MathF;
using static WowTriangles.Utils;
using PPather.Triangles.Data;
using PPather;

namespace WowTriangles;

/// <summary>
/// A chunked collection of triangles
/// </summary>
public sealed class ChunkedTriangleCollection
{
    private readonly ILogger logger;
    private readonly MPQTriangleSupplier supplier;
    private readonly SparseMatrix2D<TriangleCollection> chunks;

    private const int maxCache = 128;
    public Action<ChunkEventArgs> NotifyChunkAdded;

    public ChunkedTriangleCollection(ILogger logger, int initCapacity, MPQTriangleSupplier supplier)
    {
        this.logger = logger;
        this.supplier = supplier;
        chunks = new SparseMatrix2D<TriangleCollection>(initCapacity);
    }

    public void Close()
    {
        supplier.Clear();
        EvictAll();
    }

    public void EvictAll()
    {
        foreach (TriangleCollection chunk in chunks.GetAllElements())
        {
            chunk.Clear();
        }

        chunks.Clear();
    }

    public static void GetGridStartAt(float x, float y, out int grid_x, out int grid_y)
    {
        x = ChunkReader.ZEROPOINT - x;
        grid_x = (int)(x / ChunkReader.TILESIZE);
        y = ChunkReader.ZEROPOINT - y;
        grid_y = (int)(y / ChunkReader.TILESIZE);
    }

    private static void GetGridLimits(int grid_x, int grid_y,
                                out float min_x, out float min_y,
                                out float max_x, out float max_y)
    {
        max_x = ChunkReader.ZEROPOINT - (grid_x * ChunkReader.TILESIZE);
        min_x = max_x - ChunkReader.TILESIZE;
        max_y = ChunkReader.ZEROPOINT - (grid_y * ChunkReader.TILESIZE);
        min_y = max_y - ChunkReader.TILESIZE;
    }

    private void LoadChunkAt(float x, float y)
    {
        GetGridStartAt(x, y, out int grid_x, out int grid_y);

        if (chunks.ContainsKey(grid_x, grid_y))
            return;

        GetGridLimits(grid_x, grid_y, out float min_x, out float min_y, out float max_x, out float max_y);

        TriangleCollection tc = new(logger);
        tc.SetLimits(min_x - 1, min_y - 1, -1E30f, max_x + 1, max_y + 1, 1E30f);

        supplier.GetTriangles(tc, min_x, min_y, max_x, max_y);

        chunks.Add(grid_x, grid_y, tc);

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace($"Grid [{grid_x},{grid_y}] Bounds: [{min_x:F4}, {min_y:F4}] [{max_x:F4}, {max_y:F4}] [{x}, {y}] - Count: {chunks.Count}");
        }

        NotifyChunkAdded?.Invoke(new ChunkEventArgs(grid_x, grid_y));
    }

    public TriangleCollection GetChunkAt(float x, float y)
    {
        LoadChunkAt(x, y);
        GetGridStartAt(x, y, out int grid_x, out int grid_y);

        return GetChunkAt(grid_x, grid_y);
    }

    public TriangleCollection GetChunkAt(int grid_x, int grid_y)
    {
        return chunks.TryGetValue(grid_x, grid_y, out TriangleCollection tc)
            ? tc
            : default;
    }

    public bool IsSpotBlocked(float x, float y, float z,
                              float toonHeight, float toonSize)
    {
        TriangleCollection tc = GetChunkAt(x, y);

        TriangleMatrix tm = tc.GetTriangleMatrix();
        ArraySegment<int> ts = tm.GetAllCloseTo(x, y, toonSize);

        Vector3 toon = new(x, y, z + toonHeight - toonSize);

        for (int i = 0; i < ts.Count; i++)
        {
            int t = ts[i];

            Vector3 vertex0;
            Vector3 vertex1;
            Vector3 vertex2;

            tc.GetTriangleVertices(t,
                    out vertex0.X, out vertex0.Y, out vertex0.Z,
                    out vertex1.X, out vertex1.Y, out vertex1.Z,
                    out vertex2.X, out vertex2.Y, out vertex2.Z, out _);

            float d = PointDistanceToTriangle(toon, vertex0, vertex1, vertex2);
            if (d < toonSize)
                return true;
        }

        return false;
    }

    public bool IsStepBlocked(float x0, float y0, float z0,
                              float x1, float y1, float z1,
                              float toonHeight, float toonSize)
    {
        TriangleCollection tc = GetChunkAt(x0, y0);

        float dx = x0 - x1;
        float dy = y0 - y1;
        float dz = z0 - z1;
        float stepLength = Sqrt((dx * dx) + (dy * dy) + (dz * dz));
        // 1: check steepness

        // TODO

        // 2: check is there is a big step

        float mid_x = (x0 + x1) / 2.0f;
        float mid_y = (y0 + y1) / 2.0f;
        float mid_z = (z0 + z1) / 2.0f;
        float mid_z_hit;
        float mid_dz = Abs(stepLength);
        //if (mid_dz < 1.0f) mid_dz = 1.0f;
        if (FindStandableAt(mid_x, mid_y, mid_z - mid_dz, mid_z + mid_dz, out mid_z_hit, out _, toonHeight, toonSize))
        {
            float dz0 = Abs(z0 - mid_z_hit);
            float dz1 = Abs(z1 - mid_z_hit);

            // Console.WriteLine("z0 " + z0 + " z1 " + z1 + " dz0 " + dz0+ " dz1 " + dz1 );
            if (dz0 > stepLength / 2.0 && dz0 > 1.0)
                return true; // too steep

            if (dz1 > stepLength / 2.0 && dz1 > 1.0)
                return true; // too steep
        }
        else
        {
            // bad!
            return true;
        }

        TriangleMatrix tm = tc.GetTriangleMatrix();
        ArraySegment<int> ts = tm.GetAllInSquare(Min(x0, x1), Min(y0, y1), Max(x0, x1), Max(y0, y1));

        // 3: check collision with objects

        Vector3 from, from_up, from_low;
        Vector3 to, to_up, to_low;

        from.X = x0;
        from.Y = y0;
        from.Z = z0 + toonSize; //+0.5

        to.X = x1;
        to.Y = y1;
        to.Z = z1 + toonSize;

        from_up = new Vector3(from.X, from.Y, from.Z);
        from_up.Z = z0 + toonHeight - toonSize;

        to_up = new Vector3(to.X, from.Y, from.Z);
        to_up.Z = z1 + toonHeight - toonSize;

        //diagonal
        if (CheckForCollision(tc, ts, from, to_up)) { return true; }

        //diagonal
        if (CheckForCollision(tc, ts, from_up, to)) { return true; }

        //head height
        // if (CheckForCollision(tc, ts, ref from_up, ref to_up)) { return true; }

        //close to the ground
        from_low = new Vector3(from.X, from.Y, from.Z);
        from_low.Z = z0 + 0.2f;
        to_low = new Vector3(to.X, to.Y, to.Z);
        to_low.Z = z1 + 0.2f;
        if (CheckForCollision(tc, ts, from_low, to_low)) { return true; }

        GetNormal(x0, y0, x1, y1, out float ddx, out float ddy, 0.2f);

        from_low.X += ddy;
        from_low.Y += ddx;
        to_low.X += ddy;
        to_low.Y += ddx;
        if (CheckForCollision(tc, ts, from_low, to_low)) { return true; }

        from_low.X -= 2 * ddy;
        from_low.Y -= 2 * ddx;
        to_low.X -= 2 * ddy;
        to_low.Y -= 2 * ddx;

        return CheckForCollision(tc, ts, from_low, to_low);
    }

    public static void GetNormal(float x1, float y1, float x2, float y2, out float dx, out float dy, float factor)
    {
        dx = x2 - x1;
        dy = y2 - y1;

        if (Abs(dx) > Abs(dy))
        {
            dy /= dx;
            dx = 1;
        }
        else
        {
            dx /= dy;
            dy = 1;
        }

        dx *= factor;
        dy *= factor;
    }

    private static bool CheckForCollision(TriangleCollection tc, ArraySegment<int> ts, in Vector3 from, in Vector3 to)
    {
        for (int i = 0; i < ts.Count; i++)
        {
            int t = ts[i];

            Vector3 vertex0;
            Vector3 vertex1;
            Vector3 vertex2;

            tc.GetTriangleVertices(t,
                    out vertex0.X, out vertex0.Y, out vertex0.Z,
                    out vertex1.X, out vertex1.Y, out vertex1.Z,
                    out vertex2.X, out vertex2.Y, out vertex2.Z);

            if (SegmentTriangleIntersect(from, to, vertex0, vertex1, vertex2, out _))
            {
                return true;
            }
        }
        return false;
    }

    public bool FindStandableAt(float x, float y, float min_z, float max_z,
                               out float z0, out TriangleType flags, float toonHeight, float toonSize)
    {
        return FindStandableAt1(x, y, min_z, max_z, out z0, out flags, toonHeight, toonSize, false, TriangleType.Terrain);
    }

    public bool IsInWater(float x, float y, float min_z, float max_z)
    {
        TriangleCollection tc = GetChunkAt(x, y);
        TriangleMatrix tm = tc.GetTriangleMatrix();
        ArraySegment<int> ts = tm.GetAllCloseTo(x, y, 1.0f);

        Vector3 s0 = new(x, y, min_z);
        Vector3 s1 = new(x, y, max_z);

        for (int i = 0; i < ts.Count; i++)
        {
            int t = ts[i];

            Vector3 vertex0;
            Vector3 vertex1;
            Vector3 vertex2;

            tc.GetTriangleVertices(t,
                    out vertex0.X, out vertex0.Y, out vertex0.Z,
                    out vertex1.X, out vertex1.Y, out vertex1.Z,
                    out vertex2.X, out vertex2.Y, out vertex2.Z, out TriangleType t_flags);

            GetTriangleNormal(vertex0, vertex1, vertex2, out _);

            if (SegmentTriangleIntersect(s0, s1, vertex0, vertex1, vertex2, out _))
            {
                if ((t_flags & TriangleType.Water) != 0)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public int GradiantScore(float x, float y, float z, float range)
    {
        TriangleCollection tc = GetChunkAt(x, y);
        TriangleMatrix tm = tc.GetTriangleMatrix();

        float maxZ = float.MinValue;
        float minZ = float.MaxValue;

        ArraySegment<int> array = tm.GetAllCloseTo(x, y, range);
        for (int i = 0; i < array.Count; i++)
        {
            int t = array[i];

            Vector3 vertex0;
            Vector3 vertex1;
            Vector3 vertex2;

            tc.GetTriangleVertices(t,
                    out vertex0.X, out vertex0.Y, out vertex0.Z,
                    out vertex1.X, out vertex1.Y, out vertex1.Z,
                    out vertex2.X, out vertex2.Y, out vertex2.Z, out TriangleType flags);

            if (flags == TriangleType.Terrain)
            {
                if (vertex0.Z > maxZ) { maxZ = vertex0.Z; }
                if (vertex2.Z > maxZ) { maxZ = vertex1.Z; }
                if (vertex1.Z > maxZ) { maxZ = vertex2.Z; }

                if (vertex0.Z < minZ) { minZ = vertex0.Z; }
                if (vertex2.Z < minZ) { minZ = vertex1.Z; }
                if (vertex1.Z < minZ) { minZ = vertex2.Z; }
            }
        }
        int g = (int)(maxZ - minZ);
        if (g > 10)
        {
            g = 10;
        }
        return g;
    }

    public bool IsCloseToModel(float x, float y, float z, float range)
    {
        TriangleCollection tc = GetChunkAt(x, y);
        TriangleMatrix tm = tc.GetTriangleMatrix();

        ArraySegment<int> array = tm.GetAllCloseTo(x, y, range);
        for (int i = 0; i < array.Count; i++)
        {
            int t = array[i];

            Vector3 vertex0;
            Vector3 vertex1;
            Vector3 vertex2;

            tc.GetTriangleVertices(t,
                    out vertex0.X, out vertex0.Y, out vertex0.Z,
                    out vertex1.X, out vertex1.Y, out vertex1.Z,
                    out vertex2.X, out vertex2.Y, out vertex2.Z, out TriangleType flags);

            //check triangle is part of a model
            if ((flags & TriangleType.Object) != 0 || (flags & TriangleType.Model) != 0)
            {
                float minHeight = 0.75f;
                float height = 2;

                //and the vertex is close to the char
                if ((vertex0.Z > z + minHeight && vertex0.Z < z + height) ||
                    (vertex1.Z > z + minHeight && vertex1.Z < z + height) ||
                    (vertex2.Z > z + minHeight && vertex2.Z < z + height))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool LineOfSightExists(Spot a, Spot b)
    {
        TriangleCollection tc = GetChunkAt(a.Loc.X, a.Loc.Y);
        TriangleMatrix tm = tc.GetTriangleMatrix();
        ICollection<int> ts = tm.GetAllCloseTo(a.Loc.X, a.Loc.Y, a.GetDistanceTo(b) + 1);

        Vector3 s0 = a.Loc;
        Vector3 s1 = b.Loc;

        foreach (int t in ts)
        {
            Vector3 vertex0;
            Vector3 vertex1;
            Vector3 vertex2;

            tc.GetTriangleVertices(t,
                    out vertex0.X, out vertex0.Y, out vertex0.Z,
                    out vertex1.X, out vertex1.Y, out vertex1.Z,
                    out vertex2.X, out vertex2.Y, out vertex2.Z, out _);

            if (SegmentTriangleIntersect(s0, s1, vertex0, vertex1, vertex2, out _))
            {
                return false;
            }
        }
        return true;
    }

    public bool FindStandableAt1(float x, float y, float min_z, float max_z,
                               out float z0, out TriangleType flags,
                               float toonHeight, float toonSize,
                               bool IgnoreGradient, TriangleType allowedFlags)
    {
        const float minCliffD = 0.5f;

        TriangleCollection tc = GetChunkAt(x, y);
        TriangleMatrix tm = tc.GetTriangleMatrix();
        ArraySegment<int> ts = tm.GetAllCloseTo(x, y, 1.0f);

        Vector3 s0 = new(x, y, min_z);
        Vector3 s1 = new(x, y, max_z);

        float best_z = -1E30f;
        TriangleType best_flags = TriangleType.Terrain;
        bool found = false;

        for (int i = 0; i < ts.Count; i++)
        {
            int t = ts[i];

            Vector3 vertex0;
            Vector3 vertex1;
            Vector3 vertex2;

            tc.GetTriangleVertices(t,
                    out vertex0.X, out vertex0.Y, out vertex0.Z,
                    out vertex1.X, out vertex1.Y, out vertex1.Z,
                    out vertex2.X, out vertex2.Y, out vertex2.Z, out TriangleType t_flags);

            if (allowedFlags == TriangleType.Terrain || allowedFlags.Has(t_flags))
            {
                GetTriangleNormal(vertex0, vertex1, vertex2, out Vector3 normal);
                float angle_z = Sin(30f / 360.0f * PI * 2f); // 45f -> 40 degree || 60f -> 50 degree || 30f -> 28.6 degree
                if (Abs(normal.Z) > angle_z)
                {
                    if (SegmentTriangleIntersect(s0, s1, vertex0, vertex1, vertex2, out Vector3 intersect))
                    {
                        if (intersect.Z > best_z &&
                            !IsSpotBlocked(intersect.X, intersect.Y, intersect.Z, toonHeight, toonSize))
                        {
                            best_z = intersect.Z;
                            best_flags = t_flags;
                            found = true;
                        }
                    }
                }
            }
        }
        if (found)
        {
            Vector3 up, dn;
            up.Z = best_z + 2;
            dn.Z = best_z - 5;

            const int size = 4;
            Span<bool> nearCliff = stackalloc bool[size] { true, true, true, true };
            Span<float> dx = stackalloc float[size] { minCliffD, -minCliffD, 0, 0 };
            Span<float> dy = stackalloc float[size] { 0, 0, minCliffD, -minCliffD };

            bool allGood;
            for (int j = 0; j < ts.Count; j++)
            {
                int t = ts[j];
                Vector3 vertex0;
                Vector3 vertex1;
                Vector3 vertex2;

                tc.GetTriangleVertices(t,
                        out vertex0.X, out vertex0.Y, out vertex0.Z,
                        out vertex1.X, out vertex1.Y, out vertex1.Z,
                        out vertex2.X, out vertex2.Y, out vertex2.Z);

                allGood = true;
                for (int i = 0; i < size; i++)
                {
                    if (nearCliff[i])
                    {
                        up.X = dn.X = x + dx[i];
                        up.Y = dn.Y = y + dy[i];
                        if (SegmentTriangleIntersect(up, dn, vertex0, vertex1, vertex2, out _))
                            nearCliff[i] = false;
                    }
                    allGood &= !nearCliff[i];
                }
                if (allGood)
                    break;
            }

            allGood = true;
            for (int i = 0; i < size; i++)
                allGood &= !nearCliff[i];
            if (!allGood)
            {
                z0 = best_z;
                flags = best_flags;
                return false; // too close to cliff
            }
        }
        z0 = best_z;
        flags = best_flags;
        return found;
    }
}