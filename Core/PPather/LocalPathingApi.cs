﻿using PPather.Data;
using Microsoft.Extensions.Logging;
using PPather;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using WowTriangles;
using System;
using PPather.Graph;

#pragma warning disable 162

namespace Core;

public sealed class LocalPathingApi : IPPather
{
    private const bool debug = false;

    private readonly ILogger logger;

    private readonly PPatherService service;

    private DateTime lastSave;

    public LocalPathingApi(ILogger logger, PPatherService service, DataConfig dataConfig)
    {
        this.logger = logger;
        this.service = service;
    }

    public ValueTask DrawLines(List<LineArgs> lineArgs)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask DrawSphere(SphereArgs args)
    {
        return ValueTask.CompletedTask;
    }

    public Vector3[] FindMapRoute(int uiMap, Vector3 mapFrom, Vector3 mapTo)
    {
        long timestamp = Stopwatch.GetTimestamp();

        service.SetLocations(
            service.ToWorld(uiMap, mapFrom.X, mapFrom.Y, mapFrom.Z),
            service.ToWorld(uiMap, mapTo.X, mapTo.Y));

        Path path = service.DoSearch(PathGraph.eSearchScoreSpot.A_Star_With_Model_Avoidance);
        if (path == null)
        {
            if (debug)
                LogWarning($"Failed to find a path from {mapFrom} to {mapTo} took {Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds} ms.");

            return Array.Empty<Vector3>();
        }

        if (debug)
            LogDebug($"Finding route from {mapFrom} map {uiMap} to {mapTo} took {Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds} ms.");

        if ((DateTime.UtcNow - lastSave).TotalMinutes >= 1)
        {
            service.Save();
            lastSave = DateTime.UtcNow;
        }

        Vector3[] array = new Vector3[path.locations.Count];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = service.ToLocal(path.locations[i], (int)service.SearchFrom.W, uiMap);
        }
        return array;
    }

    public Vector3[] FindWorldRoute(int uiMap, Vector3 worldFrom, Vector3 worldTo)
    {
        long timestamp = Stopwatch.GetTimestamp();

        service.SetLocations(
            service.ToWorldZ(uiMap, worldFrom.X, worldFrom.Y, worldFrom.Z),
            service.ToWorldZ(uiMap, worldTo.X, worldTo.Y, worldTo.Z));

        Path path = service.DoSearch(PathGraph.eSearchScoreSpot.A_Star_With_Model_Avoidance);
        if (path == null)
        {
            if (debug)
                LogWarning($"Failed to find a path from {worldFrom} to {worldTo} took {Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds} ms.");

            return Array.Empty<Vector3>();
        }

        if (debug)
            LogDebug($"Finding route from {worldFrom} map {uiMap} to {worldTo} took {Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds} ms.");

        if ((DateTime.UtcNow - lastSave).TotalMinutes >= 1)
        {
            service.Save();
            lastSave = DateTime.UtcNow;
        }

        return path.locations.ToArray();
    }


    #region Logging

    private void LogDebug(string text)
    {
        logger.LogDebug($"{nameof(LocalPathingApi)}: {text}");
    }

    private void LogWarning(string text)
    {
        logger.LogWarning($"{nameof(LocalPathingApi)}: {text}");
    }

    #endregion
}