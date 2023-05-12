using Mediapipe;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandmarkConverter
{
    public struct LandmarkPoint
    {
	public Vector3 position;
	public float visibility;
    }
    private LandmarkPoint[] _points;
    public LandmarkPoint[] Points
    {
	get
	{
	    lock (_lock)
		return _points;
	}
    }

    private Vector3[] _positions;
    public Vector3[] Positions
    {
	get
	{
	    lock (_lock)
	    {
		if (_points == null) return null;

		if (_positions == null)
		    _positions = new Vector3[_points.Length];
		for (int i = 0; i < _positions.Length; i++)
		{
		    _positions[i] = _points[i].position;
		}
		return _positions;
	    }
	}
    }

	private LandmarkPoint[] _wpoints;
    public LandmarkPoint[] WPoints
    {
	get
	{
	    lock (_lock)
		return _wpoints;
	}
    }
	private Vector3[] _worldpositions;
    public Vector3[] WorldPositions
    {
	get
	{
	    lock (_lock)
	    {
		if (_wpoints == null) return null;

		if (_worldpositions == null)
		    _worldpositions = new Vector3[_wpoints.Length];
		for (int i = 0; i < _worldpositions.Length; i++)
		{
		    _worldpositions[i] = _wpoints[i].position;
		}
		return _worldpositions;
	    }
	}
    }

    public bool IsMirror { get; set; }
    public int Count => _points.Length;


    private object _lock = new object();

    public void OnLandmarkListUpdate(LandmarkList landmarkList)
    {
	if (landmarkList == null || landmarkList.Landmark == null) return;
	lock (_lock)
	{
	    if (_points == null)
		_points = new LandmarkPoint[landmarkList.Landmark.Count];

	    for (int i = 0; i < _points.Length; i++)
	    {
		var point = _points[i];
		var landmark = landmarkList.Landmark[i];
		point.position = Convert(landmark);
		point.visibility = landmark.HasVisibility ? landmark.Visibility : 0;
		_points[i] = point;
	    }
	}
    }

    public void OnLandmarkListUpdate(NormalizedLandmarkList landmarkList)
    {
	if (landmarkList == null || landmarkList.Landmark == null) return;
	OnLandmarkListUpdate(landmarkList.Landmark);
    }

    public void OnLandmarkListUpdate(IList<NormalizedLandmark> landmarkList)
    {
	if (landmarkList == null || landmarkList.Count <= 0) return;
	lock (_lock)
	{
	    if (_points == null)
		_points = new LandmarkPoint[landmarkList.Count];

	    for (int i = 0; i < _points.Length; i++)
	    {
		var point = _points[i];
		var landmark = landmarkList[i];
		point.position = Convert(landmark);
		point.visibility = landmark.HasVisibility ? landmark.Visibility : 0;
		_points[i] = point;
	    }
	}
    }

	public void OnWorldLandmarkListUpdate(LandmarkList landmarkList)
    {
	if (landmarkList == null || landmarkList.Landmark == null) return;
	OnWorldLandmarkListUpdate(landmarkList.Landmark);
    }

	public void OnWorldLandmarkListUpdate(IList<Landmark> landmarkList)
    {
	if (landmarkList == null || landmarkList.Count <= 0) return;
	lock (_lock)
	{
	    if (_wpoints == null)
		_wpoints = new LandmarkPoint[landmarkList.Count];

	    for (int i = 0; i < _wpoints.Length; i++)
	    {
		var point = _wpoints[i];
		var landmark = landmarkList[i];
		point.position = Convert(landmark);
		point.visibility = landmark.HasVisibility ? landmark.Visibility : 0;
		_wpoints[i] = point;
	    }
	}
    }

    public Vector3 Convert(Landmark landmark)
    {
	return Convert(landmark, IsMirror);
    }

    public Vector3 Convert(NormalizedLandmark landmark)
    {
	return Convert(landmark, IsMirror);
    }

    public static Vector3 Convert(Landmark landmark, bool isMirror)
    {
	return new Vector3(isMirror ? -landmark.X : landmark.X, -landmark.Y, -landmark.Z);
    }

    public static Vector3 Convert(NormalizedLandmark landmark, bool isMirror)
    {
	return new Vector3(isMirror ? -landmark.X : landmark.X, -landmark.Y, -landmark.Z);
    }
}
