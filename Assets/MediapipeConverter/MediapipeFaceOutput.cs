using Mediapipe;
using Mediapipe.Unity;
using Mediapipe.Unity.CoordinateSystem;
using Mediapipe.Unity.FaceMesh;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;
using Rect = UnityEngine.Rect;

public class MediapipeFaceOutput : MediapipeOutput, IFaceOutput
{
    //[SerializeField]
    //private NNModel _mouthModelSource;
    private FaceMeshSolution _solution;
    private readonly List<(int, int)> _connections = new List<(int, int)> {
      // Face Oval
      (10, 338),
      (338, 297),
      (297, 332),
      (332, 284),
      (284, 251),
      (251, 389),
      (389, 356),
      (356, 454),
      (454, 323),
      (323, 361),
      (361, 288),
      (288, 397),
      (397, 365),
      (365, 379),
      (379, 378),
      (378, 400),
      (400, 377),
      (377, 152),
      (152, 148),
      (148, 176),
      (176, 149),
      (149, 150),
      (150, 136),
      (136, 172),
      (172, 58),
      (58, 132),
      (132, 93),
      (93, 234),
      (234, 127),
      (127, 162),
      (162, 21),
      (21, 54),
      (54, 103),
      (103, 67),
      (67, 109),
      (109, 10),
      // Left Eye
      (33, 7),
      (7, 163),
      (163, 144),
      (144, 145),
      (145, 153),
      (153, 154),
      (154, 155),
      (155, 133),
      (33, 246),
      (246, 161),
      (161, 160),
      (160, 159),
      (159, 158),
      (158, 157),
      (157, 173),
      (173, 133),
      // Left Eyebrow
      (46, 53),
      (53, 52),
      (52, 65),
      (65, 55),
      (70, 63),
      (63, 105),
      (105, 66),
      (66, 107),
      // Right Eye
      (263, 249),
      (249, 390),
      (390, 373),
      (373, 374),
      (374, 380),
      (380, 381),
      (381, 382),
      (382, 362),
      (263, 466),
      (466, 388),
      (388, 387),
      (387, 386),
      (386, 385),
      (385, 384),
      (384, 398),
      (398, 362),
      // Right Eyebrow
      (276, 283),
      (283, 282),
      (282, 295),
      (295, 285),
      (300, 293),
      (293, 334),
      (334, 296),
      (296, 336),
      // Lips (Inner)
      (78, 95),
      (95, 88),
      (88, 178),
      (178, 87),
      (87, 14),
      (14, 317),
      (317, 402),
      (402, 318),
      (318, 324),
      (324, 308),
      (78, 191),
      (191, 80),
      (80, 81),
      (81, 82),
      (82, 13),
      (13, 312),
      (312, 311),
      (311, 310),
      (310, 415),
      (415, 308),
      // Lips (Outer)
      (61, 146),
      (146, 91),
      (91, 181),
      (181, 84),
      (84, 17),
      (17, 314),
      (314, 405),
      (405, 321),
      (321, 375),
      (375, 291),
      (61, 185),
      (185, 40),
      (40, 39),
      (39, 37),
      (37, 0),
      (0, 267),
      (267, 269),
      (269, 270),
      (270, 409),
      (409, 291),
    };
    private LandmarkConverter _faceLandmarkConverter;
    private LandmarkConverter _leftIrislandmarkConverter;
    private LandmarkConverter _rightIrislandmarkConverter;
    private WaitUntil _waitUtilAvatarUpdated;
    private bool _isAvatarUpdated = false;
    private float[] _mouthSourceData;
    private Vector3[] _blendshapeOriginPoints;
    private Vector3[] _blendshapeDestinationPoints;
    private Rect _blendshapeAdjustedRect = new Rect(-960, -540, 1920, 1080);
    private OneEuroFilter<Quaternion> _filter;

    public override bool IsMirror
    {
	get => _isMirror;
	set
	{
	    _isMirror = value;
	    _faceLandmarkConverter.IsMirror = value;
	    _leftIrislandmarkConverter.IsMirror = value;
	    _rightIrislandmarkConverter.IsMirror = value;
	}
    }

    public bool UseHead { get; set; } = true;
    public RotateSpace Space { get; set; }
    public Quaternion HeadRotation { get; private set; }
    public Dictionary<int, float> Blendshapes { get; private set; } = new Dictionary<int, float>();
    public override List<(int, int)> Connections => _connections;

    private const int FACE_LANDMARK_COUNT = 468;
    private const int IRIS_LANDMARK_COUNT = 5;

    private static readonly int HEAD_TOP_INDEX = 10;
    private static readonly int HEAD_BOTTOM_INDEX = 152;
    private static readonly int HEAD_LEFT_INDEX = 234;
    private static readonly int HEAD_RIGHT_INDEX = 454;
    private static readonly int HEAD_FRONT_INDEX = 1;
    private static readonly int HEAD_BACK_INDEX = 2;
    private static readonly string[] BLENDSHAPE_NAMES = { "A", "E", "I", "O", "U" };
    private static readonly int[] MOUTH_INDICES = { 13, 14, 78, 80, 81, 82, 87, 88, 95, 178, 191, 308, 310, 311, 312, 317, 318, 324, 402, 415 };
    public string[] BlendshapeNames => BLENDSHAPE_NAMES;
    private IWorker _worker;

    private void Awake()
    {
	_faceLandmarkConverter = new LandmarkConverter();
	_leftIrislandmarkConverter = new LandmarkConverter();
	_rightIrislandmarkConverter = new LandmarkConverter();
	IsMirror = _isMirror;
	_solution = GetComponent<FaceMeshSolution>();
	_waitUtilAvatarUpdated = new WaitUntil(() => _isAvatarUpdated);
	//_worker = _mouthModelSource.CreateWorker();
    }

    private void OnEnable()
    {
	_solution.OnFaceLandmarkListUpdated += OnLandmarkUpdated;
    }

    private void OnDisable()
    {
	_solution.OnFaceLandmarkListUpdated -= OnLandmarkUpdated;
    }

    private void Update()
    {
	var facePositions = _faceLandmarkConverter.Positions;

	if (_filter == null)
	    _filter = new OneEuroFilter<Quaternion>(_filterFrequency);
	if (UseHead)
	    HeadRotation = _filter.Filter(GetHeadRotation(facePositions, IsMirror));
	UpdateBlendshapes(facePositions);
    }

    private Vector3 GetBlendshapePoint(NormalizedLandmark landmark)
    {
	Rect rect = _blendshapeAdjustedRect;
	float zScale = rect.width;
	float x = Mathf.LerpUnclamped(rect.xMax, rect.xMin, landmark.X);
	float y = Mathf.LerpUnclamped(rect.yMax, rect.yMin, landmark.Y);
	float z = zScale * landmark.Z;
	return new Vector3(x, y, z);
    }

    private Quaternion GetHeadRotation(Vector3[] positions, bool isMirror)
    {
	if (positions == null) return Quaternion.identity;
	Vector3 right = GetVector(positions, HEAD_LEFT_INDEX, HEAD_RIGHT_INDEX, isMirror);
	Vector3 up = GetVector(positions, HEAD_BOTTOM_INDEX, HEAD_TOP_INDEX);
	Vector3 forwards = Vector3.Cross(right, up);
	return Quaternion.LookRotation(forwards, up);
    }

    private void UpdateBlendshapes(Vector3[] positions)
    {
	if (positions == null) return;
	//UpdateHeadRotaiton(positions, true);
	//aafterPosition = CenterNormalizeFlipRotate(positions, HeadRotation, false);
	lock (_lock)
	{
	    var rotation = GetHeadRotation(_blendshapeOriginPoints, true);
	    _blendshapeDestinationPoints = CenterNormalizeFlipRotate(_blendshapeOriginPoints, rotation, true);
	}

	_mouthSourceData = ConvertVector3ToFloatArr(_blendshapeDestinationPoints, MOUTH_INDICES);

	_isAvatarUpdated = true;
	//StartCoroutine(ExecuteBlendshape(_worker, new int[] { 1, MOUTH_INDICES.Length * 3 }, _mouthSourceData, OnMouthBlendshapeUpdate));

    }

    private void OnMouthBlendshapeUpdate(float[] blendshapeValues)
    {
	for (int i = 0; i < blendshapeValues.Length; i++)
	{
	    if (i == 4)
		blendshapeValues[i] *= 0.3f;
	    Blendshapes[i] = blendshapeValues[i] * 100;
	}
    }
    private object _lock = new object();
    private void OnLandmarkUpdated(IList<NormalizedLandmarkList> landmarks)
    {
	if (landmarks == null || landmarks.Count == 0 || landmarks[0] == null) return;
	var enumerator = landmarks[0].Landmark.GetEnumerator();

	var faceLandmarks = EnumerateLandmark(enumerator, FACE_LANDMARK_COUNT);
	if (faceLandmarks != null)
	{
	    lock (_lock)
	    {
		if (_blendshapeOriginPoints == null)
		    _blendshapeOriginPoints = new Vector3[faceLandmarks.Count];
		for (int i = 0; i < faceLandmarks.Count; i++)
		{
		    //_blendshapeOriginPoints[i] = _blendshapeAdjustedRect.GetLocalPosition(faceLandmarks[i]);
		    _blendshapeOriginPoints[i] = GetBlendshapePoint(faceLandmarks[i]);
		}
	    }
	}

	_faceLandmarkConverter.OnLandmarkListUpdate(faceLandmarks);
	_leftIrislandmarkConverter.OnLandmarkListUpdate(EnumerateLandmark(enumerator, IRIS_LANDMARK_COUNT));
	_rightIrislandmarkConverter.OnLandmarkListUpdate(EnumerateLandmark(enumerator, IRIS_LANDMARK_COUNT));

    }

    private static float[] ConvertVector3ToFloatArr(Vector3[] input, int[] indices = null)
    {
	if (indices == null)
	{
	    var result = new float[3 * input.Length];
	    for (int i = 0; i < input.Length; i += 3)
	    {
		result[i] = input[i].x;
		result[i + 1] = input[i].y;
		result[i + 2] = input[i].z;
	    }
	    return result;
	}
	else
	{
	    var result = new float[3 * indices.Length];
	    for (int i = 0; i < result.Length; i += 3)
	    {
		//if (i ==507) Debug.Log("copying indice " + i.ToString());
		result[i] = input[indices[i / 3]].x;
		result[i + 1] = input[indices[i / 3]].y;
		result[i + 2] = input[indices[i / 3]].z;
	    }

	    return result;
	}
    }


    protected static bool IsHigher(Vector3 a, Vector3 b) => a.y > b.y;
    protected static bool IsToTheLeft(Vector3 a, Vector3 b) => a.x < b.x;
    protected static bool IsFrontToBack(Vector3 a, Vector3 b) => a.z < b.z;

    private Vector3[] NormalizeFlip(Vector3[] positions, Quaternion headRotation)
    {

	float maxDistance = (positions[HEAD_TOP_INDEX] - positions[HEAD_BOTTOM_INDEX]).magnitude;
	float right = (positions[HEAD_LEFT_INDEX] - positions[HEAD_RIGHT_INDEX]).magnitude;
	Vector3 v0 = positions[0];
	for (int i = 0; i < positions.Length; i++)
	{
	    positions[i] -= v0;
	    positions[i] = headRotation * positions[i];
	    positions[i] /= maxDistance;
	}

	if (IsHigher(positions[HEAD_BOTTOM_INDEX], positions[HEAD_TOP_INDEX]))
	    for (int i = 0; i < positions.Length; i++)
	    {
		positions[i] = Vector3.Scale(positions[i], new Vector3(1, -1, 1));
	    }

	if (!IsToTheLeft(positions[HEAD_LEFT_INDEX], positions[HEAD_RIGHT_INDEX]))
	    for (int i = 0; i < positions.Length; i++)
	    {
		positions[i] = Quaternion.AngleAxis(180, Vector3.up) * positions[i];
	    }

	if (IsToTheLeft(positions[HEAD_LEFT_INDEX], positions[HEAD_RIGHT_INDEX]) && IsFrontToBack(positions[HEAD_BACK_INDEX], positions[HEAD_FRONT_INDEX]))
	    for (int i = 0; i < positions.Length; i++)
	    {
		positions[i] = Vector3.Scale(positions[i], new Vector3(-1, 1, 1));
	    }

	return positions;
    }

    protected Vector3[] CenterNormalizeFlipRotate(Vector3[] verts, Quaternion rotation, bool t)
    {
	var v = new Vector3[verts.Length];
	float magnitude = (verts[HEAD_TOP_INDEX] - verts[HEAD_BOTTOM_INDEX]).magnitude;

	Vector3 v0 = verts[0];

	for (int vi = 0; vi < verts.Length; vi++)
	{
	    v[vi] = verts[vi] - v0;
	    v[vi] = (t ? Quaternion.Euler(0, 180, 0) : Quaternion.identity) * Quaternion.Inverse(rotation) * v[vi];
	    v[vi] = v[vi] / magnitude;
	}

	if (IsHigher(v[HEAD_BOTTOM_INDEX], v[HEAD_TOP_INDEX]))
	    for (int vi = 0; vi < v.Length; vi++)
		v[vi] = Vector3.Scale(v[vi], new Vector3(1, -1, 1));

	if (!IsToTheLeft(v[HEAD_LEFT_INDEX], v[HEAD_RIGHT_INDEX]))
	    for (var vi = 0; vi < verts.Length; vi++)
		v[vi] = Quaternion.Euler(0.0f, 180.0f, 0.0f) * v[vi];

	if (IsToTheLeft(v[HEAD_LEFT_INDEX], v[HEAD_RIGHT_INDEX]) && IsFrontToBack(v[HEAD_BACK_INDEX], v[HEAD_FRONT_INDEX]))
	    for (var vi = 0; vi < v.Length; vi++)
		v[vi] = Vector3.Scale(v[vi], new Vector3(-1, 1, 1));

	return v;
    }

    private static IList<NormalizedLandmark> EnumerateLandmark(IEnumerator<NormalizedLandmark> enumerator, int count)
    {
	IList<NormalizedLandmark> landmarks = new List<NormalizedLandmark>();
	for (int i = 0; i < count; i++)
	{
	    if (enumerator.MoveNext())
		landmarks.Add(enumerator.Current);
	}

	if (landmarks.Count < count)
	    return null;
	return landmarks;
    }

    private IEnumerator ExecuteBlendshape(IWorker worker, int[] shape, float[] data, Action<float[]> callback)
    {
	var input = new Tensor(shape, data);
	Tensor output = worker.Execute(input).PeekOutput();

	callback?.Invoke(output.ToReadOnlyArray());
	input.Dispose();
	output.Dispose();
	yield return null;
    }

    public override void SetupFilterFrequency(float frequency)
    {
	base.SetupFilterFrequency(frequency);
	_filter?.UpdateParams(_filterFrequency);
    }

    private void OnDrawGizmos()
    {
	if (!_showGizmos) return;
	DrawGizmosFace(_faceLandmarkConverter);
	DrawGizmosIris(_leftIrislandmarkConverter);
	DrawGizmosIris(_rightIrislandmarkConverter);
    }

    private void DrawGizmosFace(LandmarkConverter landmarkConverter)
    {
	lock (_lock)
	{
	    if (_blendshapeOriginPoints == null || _blendshapeOriginPoints.Length == 0) return;
	    Gizmos.color = UnityEngine.Color.white;
	    for (int i = 0; i < _blendshapeOriginPoints.Length; i++)
	    {
		Gizmos.DrawSphere(_blendshapeOriginPoints[i] * _gizmosScaleFactor, 0.5f);
	    }

	    for (int i = 0; i < _connections.Count; i++)
	    {
		Gizmos.DrawLine(_blendshapeOriginPoints[_connections[i].Item1] * _gizmosScaleFactor, _blendshapeOriginPoints[_connections[i].Item2] * _gizmosScaleFactor);
	    }
	}

	if (_blendshapeDestinationPoints == null || _blendshapeDestinationPoints.Length == 0) return;
	Gizmos.color = UnityEngine.Color.red;
	for (int i = 0; i < _blendshapeDestinationPoints.Length; i++)
	{
	    Gizmos.DrawSphere(_blendshapeDestinationPoints[i] * _gizmosScaleFactor, 0.5f);
	}

	for (int i = 0; i < _connections.Count; i++)
	{
	    Gizmos.DrawLine(_blendshapeDestinationPoints[_connections[i].Item1] * _gizmosScaleFactor, _blendshapeDestinationPoints[_connections[i].Item2] * _gizmosScaleFactor);
	}


    }

    private void DrawGizmosIris(LandmarkConverter landmarkConverter)
    {
	if (landmarkConverter == null || landmarkConverter.Positions == null) return;
	Vector3[] positions = landmarkConverter.Positions;
	for (int i = 0; i < positions.Length; i++)
	{
	    Gizmos.color = UnityEngine.Color.yellow;
	    Gizmos.DrawSphere(positions[i] * _gizmosScaleFactor, 0.2f);
	}
    }

 //   private void OnDestroy()
 //   {
	//_worker?.Dispose();
 //   }
}
