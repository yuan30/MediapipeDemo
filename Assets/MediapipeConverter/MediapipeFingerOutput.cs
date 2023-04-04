using Mediapipe;
using Mediapipe.Unity.HandTracking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

public class MediapipeFingerOutput : MediapipeOutput, IFingerOutput
{
    private HandTrackingSolution _solution;
    private readonly List<(int, int)> _connections = new List<(int, int)>()
    {
	// thumb
	(0, 1), // 0
	(1, 2), // 1
	(2, 3), // 2
	(3, 4), // 3
	// palm
	(0, 5), // 4
	(5, 9), // 5
	(9, 13), // 6
	(13, 17), // 7
	(0, 17), // 8
	// index
	(5, 6), // 9
	(6, 7), // 10
	(7, 8), // 11
	// middle
	(9, 10), // 12
	(10, 11), // 13
	(11, 12), // 14
	// ring
	(13, 14), // 15
	(14, 15), // 16
	(15, 16), // 17
	// pinky
	(17, 18), // 18
	(18, 19), // 19
	(19, 20), // 20
    };
    private LandmarkConverter _leftLandmarkConverter;
    private LandmarkConverter _rightLandmarkConverter;

    private IList<NormalizedLandmarkList> _landmarks;
    private IList<ClassificationList> _handedness;

    private static readonly int HASH_LEFT = "Left".GetHashCode();
    private static readonly int HASH_RIGHT = "Right".GetHashCode();
    private static readonly Quaternion LEFT_HAND_ROTATION = Quaternion.Euler(0, 90, 0);
    private static readonly Quaternion RIGHT_HAND_ROTATION = Quaternion.Euler(0, -90, 0);
    private static readonly Quaternion LEFT_THUMB_ROTATION = Quaternion.Euler(0, 45, 0);
    private static readonly Quaternion RIGHT_THUMB_ROTATION = Quaternion.Euler(0, -45, 0);
    private static readonly int[] PALM_CONNECTION_INDICES = { 4, 8 };
    private static readonly int[] FINGER_THUMB_INDICES = { 1, 2, 3 };
    private static readonly int[] FINGER_INDEX_INDICES = { 9, 10, 11 };
    private static readonly int[] FINGER_MIDDLE_INDICES = { 12, 13, 14 };
    private static readonly int[] FINGER_RING_INDICES = { 15, 16, 17 };
    private static readonly int[] FINGER_PINKY_INDICES = { 18, 19, 20 };
    private static readonly HumanBodyBones[] RIGHT_HAND_BONES = {  HumanBodyBones.RightHand,
	HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal,
	HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal,
	HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal,
	HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal,
	HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal
    };
    private static readonly HumanBodyBones[] LEFT_HAND_BONES = { HumanBodyBones.LeftHand,
	HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbDistal,
	HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal,
	HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal,
	HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal,
	HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal
    };
    private const int FINGER_BONE_COUNT = 3;
    public enum FingerType { Thumb, Index, Middle, Ring, Pinky }

    public override bool IsMirror
    {
	get => _isMirror;
	set
	{
	    _isMirror = value;
	    _leftLandmarkConverter.IsMirror = value;
	    _rightLandmarkConverter.IsMirror = value;
	}
    }

    public bool UseWrist { get; set; } = true;

    private bool _foundLeftHand;
    private bool _foundRightHand;
    public bool FoundLeftHand => IsMirror ? _foundRightHand : _foundLeftHand;
    public bool FoundRightHand => IsMirror ? _foundLeftHand : _foundRightHand;
    public RotateSpace Space => RotateSpace.World;

    public Dictionary<HumanBodyBones, Quaternion> FingerRotation { get; private set; }
	= new Dictionary<HumanBodyBones, Quaternion>();

    private Dictionary<HumanBodyBones, OneEuroFilter<Quaternion>> _filters = new Dictionary<HumanBodyBones, OneEuroFilter<Quaternion>>();

    public override List<(int, int)> Connections => _connections;

    private object _handnessLock = new object();

    private void Awake()
    {
	_leftLandmarkConverter = new LandmarkConverter();
	_rightLandmarkConverter = new LandmarkConverter();
	IsMirror = _isMirror;
	_solution = GetComponent<HandTrackingSolution>();
    }

    private void OnEnable()
    {
	_solution.OnHandLandmarkUpdated += OnMultiLandmarkListUpdate;
	_solution.OnHandednessUpdated += OnMultiHandednessUpdate;
    }

    private void OnDisable()
    {
	_solution.OnHandLandmarkUpdated -= OnMultiLandmarkListUpdate;
	_solution.OnHandednessUpdated -= OnMultiHandednessUpdate;
    }

    private void Update()
    {
	if (_landmarks == null) return;

	_foundLeftHand = _foundRightHand = false;
	lock (_handnessLock)
	{
	    if (_handedness != null)
	    {
		for (int i = 0; i < _handedness.Count; i++)
		{
		    var h = _handedness[i];
		    var classfication = h.Classification;

		    if (classfication == null || classfication.Count == 0 || classfication[0] == null) continue;

		    var foundLeftHand = classfication[0].Label.Trim().GetHashCode() == HASH_LEFT;
		    var foundRightHand = classfication[0].Label.Trim().GetHashCode() == HASH_RIGHT;

		    if (i >= _landmarks.Count) continue;
		    if (foundLeftHand)
		    {
			_leftLandmarkConverter.OnLandmarkListUpdate(_landmarks[i]);
			_foundLeftHand = true;
		    }
		    else if (foundRightHand)
		    {
			_rightLandmarkConverter.OnLandmarkListUpdate(_landmarks[i]);
			_foundRightHand = true;
		    }
		}
	    }
	}

	if (_leftLandmarkConverter.Positions != null)
	    UpdateLeftHandData(_leftLandmarkConverter.Positions);

	if (_rightLandmarkConverter.Positions != null)
	    UpdateRightHandData(_rightLandmarkConverter.Positions);
    }

    private void OnMultiLandmarkListUpdate(IList<NormalizedLandmarkList> landmarks)
    {
	if (landmarks == null || landmarks.Count < 0) return;
	_landmarks = landmarks;
    }

    private void OnMultiHandednessUpdate(IList<ClassificationList> handedness)
    {
	lock (_handnessLock)
	    _handedness = handedness;
    }

    private void UpdateLeftHandData(Vector3[] positions)
    {
	var bones = IsMirror ? RIGHT_HAND_BONES : LEFT_HAND_BONES;
	Quaternion thumbRot = IsMirror ? Quaternion.AngleAxis(90, Vector3.up) * LEFT_THUMB_ROTATION : LEFT_THUMB_ROTATION;
	UpdateHandData(positions, bones, !IsMirror, LEFT_HAND_ROTATION, thumbRot);
    }

    private void UpdateRightHandData(Vector3[] positions)
    {
	var bones = IsMirror ? LEFT_HAND_BONES : RIGHT_HAND_BONES;
	Quaternion thumbRot = IsMirror ? Quaternion.AngleAxis(-90, Vector3.up) * RIGHT_THUMB_ROTATION : RIGHT_THUMB_ROTATION;
	UpdateHandData(positions, bones, IsMirror, RIGHT_HAND_ROTATION, thumbRot);
    }

    private void UpdateHandData(Vector3[] positions, HumanBodyBones[] bones, bool vectorChange, Quaternion handRot, Quaternion thumbRot)
    {
	if (UseWrist)
	    UpdatePalmData(positions, GetPalmBone(bones), vectorChange);
	UpdateFingerData(positions, GetFingerBones(bones, FingerType.Index), FINGER_INDEX_INDICES, handRot, vectorChange);
	UpdateFingerData(positions, GetFingerBones(bones, FingerType.Middle), FINGER_MIDDLE_INDICES, handRot, vectorChange);
	UpdateFingerData(positions, GetFingerBones(bones, FingerType.Ring), FINGER_RING_INDICES, handRot, vectorChange);
	UpdateFingerData(positions, GetFingerBones(bones, FingerType.Pinky), FINGER_PINKY_INDICES, handRot, vectorChange);
	UpdateFingerData(positions, GetFingerBones(bones, FingerType.Thumb), FINGER_THUMB_INDICES, thumbRot, vectorChange);
    }

    public HumanBodyBones GetPalmBone(HumanBodyBones[] handBones) => handBones[0];

    public HumanBodyBones[] GetFingerBones(HumanBodyBones[] handBones, FingerType type)
    {
	HumanBodyBones[] bones = new HumanBodyBones[FINGER_BONE_COUNT];
	int startIndex = -1;
	switch (type)
	{
	    case FingerType.Thumb:
		startIndex = 1;
		break;
	    case FingerType.Index:
		startIndex = 4;
		break;
	    case FingerType.Middle:
		startIndex = 7;
		break;
	    case FingerType.Ring:
		startIndex = 10;
		break;
	    case FingerType.Pinky:
		startIndex = 13;
		break;
	    default:
		break;
	}
	if (startIndex == -1)
	    throw new System.Exception("finger type is not found");

	System.Array.Copy(handBones, startIndex, bones, 0, FINGER_BONE_COUNT);
	return bones;
    }

    private void UpdatePalmData(Vector3[] positions, HumanBodyBones bone, bool vectorChange)
    {
	Vector3 palmDir1 = GetVector(positions, PALM_CONNECTION_INDICES[0]);
	Vector3 palmDir2 = GetVector(positions, PALM_CONNECTION_INDICES[1]);
	// up direction should inverse between left and right hand, and be effected by mirror.
	Vector3 handUpDir = Vector3.Cross(vectorChange ? -palmDir1 : palmDir1, palmDir2);
	Vector3 handForwardDir = palmDir1 + palmDir2;

	Quaternion adjustedRot = vectorChange ? LEFT_HAND_ROTATION : RIGHT_HAND_ROTATION;
	Quaternion desiredRot = Quaternion.LookRotation(handForwardDir, handUpDir) * adjustedRot;
	FingerRotation[bone] = desiredRot;
	//FingerRotation[bone] = Filter(bone, desiredRot);
    }

    private void UpdateFingerData(Vector3[] positions, HumanBodyBones[] bones, int[] connIndices, Quaternion adjustedRot, bool vectorChange)
    {
	if (bones.Length != connIndices.Length)
	    throw new System.Exception("finger input and output count is different");

	Vector3 parentDirection = GetVector(positions, 0, _connections[connIndices[0]].Item1);
	Vector3 palmDir1 = GetVector(positions, PALM_CONNECTION_INDICES[0]);
	Vector3 palmDir2 = GetVector(positions, PALM_CONNECTION_INDICES[1]);
	// up direction should inverse between left and right hand, and be effected by mirror.
	Vector3 parentUpDirection = Vector3.Cross(vectorChange ? -palmDir1 : palmDir1, palmDir2);

	//Vector3 upwards = Vector3.up;
	for (int i = 0; i < connIndices.Length; i++)
	{
	    Vector3 currentDirection = GetVector(positions, connIndices[i]);
	    float dot = Vector3.Dot(parentDirection.normalized, currentDirection.normalized);
	    Vector3 upwards = Vector3.Lerp(parentDirection, parentUpDirection, dot);
	    Quaternion rotation = GetRotation(positions, connIndices[i], adjustedRot, upwards);
	    FingerRotation[bones[i]] = rotation;
	    //FingerRotation[bones[i]] = Filter(bones[i], rotation);
	    parentDirection = currentDirection;
	    parentUpDirection = upwards;
	}
    }

    [SerializeField]
    private int highlightIndex = -1;
    private void OnDrawGizmos()
    {
	if (!_showGizmos) return;
	DrawGizmosHand(_leftLandmarkConverter, Color.cyan);
	DrawGizmosHand(_rightLandmarkConverter, Color.yellow);
    }

    private void DrawGizmosHand(LandmarkConverter landmarkConverter, Color color)
    {
	if (landmarkConverter == null || landmarkConverter.Positions == null) return;
	Vector3[] positions = landmarkConverter.Positions;
	for (int i = 0; i < positions.Length; i++)
	{
	    Gizmos.color = color;
	    Gizmos.DrawSphere(positions[i] * _gizmosScaleFactor, 0.5f);
	}

	for (int i = 0; i < _connections.Count; i++)
	{
	    if (i == highlightIndex)
	    {
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(positions[_connections[i].Item1] * _gizmosScaleFactor, 1.0f);
		Gizmos.DrawSphere(positions[_connections[i].Item2] * _gizmosScaleFactor, 1.0f);
		Gizmos.DrawLine(positions[_connections[i].Item1] * _gizmosScaleFactor, positions[_connections[i].Item2] * _gizmosScaleFactor);
	    }
	    else
	    {
		Gizmos.color = color;
		Gizmos.DrawLine(positions[_connections[i].Item1] * _gizmosScaleFactor, positions[_connections[i].Item2] * _gizmosScaleFactor);
	    }
	}
    }

    private Quaternion Filter(HumanBodyBones bone, Quaternion rotation)
    {
	if (!_filters.TryGetValue(bone, out var filter))
	{
	    filter = new OneEuroFilter<Quaternion>(_filterFrequency);
	    _filters.Add(bone, filter);
	}
	return filter.Filter(rotation);
    }

    public override void SetupFilterFrequency(float frequency)
    {
	base.SetupFilterFrequency(frequency);
	foreach (var f in _filters)
	{
	    f.Value?.UpdateParams(_filterFrequency);
	}
    }
}
