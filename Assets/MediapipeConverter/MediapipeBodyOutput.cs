using Mediapipe.Unity.PoseTracking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MediapipeBodyOutput : MediapipeOutput, IBodyOutput
{
    private PoseTrackingSolution _solution;
    private readonly List<(int, int)> _connections = new List<(int, int)>()
    {
	// Right Eye
	(0, 1), // 0
	(1, 2), // 1
	(2, 3), // 2
	(3, 7), // 3
	// Left Eye
	(0, 4), // 4
	(4, 5), // 5
	(5, 6), // 6
	(6, 8), // 7
	// Lips
	(9, 10), // 8
	// Right Arm
	(11, 13), // 9
	(13, 15), // 10
	// Right Hand
	(15, 17), // 11
	(15, 19), // 12
	(15, 21), // 13
	(17, 19), // 14
	// Left Arm
	(12, 14), // 15
	(14, 16), // 16
	// Left Hand
	(16, 18), // 17
	(16, 20), // 18
	(16, 22), // 19
	(18, 20), // 20
	// Torso
	(11, 12), // 21
	(12, 24), // 22
	(24, 23), // 23
	(23, 11), // 24
	// Right Leg
	(23, 25), // 25
	(25, 27), // 26
	(27, 29), // 27
	(27, 31), // 28
	(29, 31), // 29
	// Left Leg
	(24, 26), // 30
	(26, 28), // 31
	(28, 30), // 32
	(28, 32), // 33
	(30, 32), // 34
    };
    private LandmarkConverter _landmarkConverter;

    public override bool IsMirror
    {
	get => _isMirror;
	set
	{
	    _isMirror = value;
	    _landmarkConverter.IsMirror = value;
	}
    }

    public bool BodyFollowHead => true;
    public bool LeftHandFound { get; private set; }
    public bool RightHandFound { get; private set; }

    public RotateSpace Space => RotateSpace.World;
    public Dictionary<HumanBodyBones, Quaternion> BodyRotation { get; private set; } = new Dictionary<HumanBodyBones, Quaternion>();
    public override List<(int, int)> Connections => _connections;

    private Quaternion LEFT_ARM_ADJUSTED_ROTATION = Quaternion.AngleAxis(90, Vector3.up);
    private Quaternion RIGHT_ARM_ADJUSTED_ROTATION = Quaternion.AngleAxis(-90, Vector3.up);
	//NEW
	private Quaternion LEG_ADJUSTED_ROTATION = Quaternion.AngleAxis(90, Vector3.forward);

    private static readonly HumanBodyBones[] LEFT_ARM_BONES = { HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm };
    private static readonly HumanBodyBones[] RIGHT_ARM_BONES = { HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm };

	// NEW LEG
	public bool LeftToesFound { get; private set; }
    public bool RightToesFound { get; private set; }
	private static readonly HumanBodyBones[] LEFT_LEG_BONES = { HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg };
	private static readonly HumanBodyBones[] RIGHT_LEG_BONES = { HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg };
	private static readonly int LEFT_TOES_INDEX = 32;
    private static readonly int RIGHT_TOES_INDEX = 31;

    private static readonly int[] LEFT_ARM_CONN_INDICES = { 15, 16 };
    private static readonly int[] RIGHT_ARM_CONN_INDICES = { 9, 10 };
    private static readonly int LEFT_HAND_INDEX = 16;
    private static readonly int RIGHT_HAND_INDEX = 15;
    private static readonly int[] BODY_CONN_INDICES = { 21, 22, 23, 24 };
    private static readonly int[] LEFT_LEG_CONN_INDICES = { 30, 31 };
    private static readonly int[] RIGHT_LEG_CONN_INDICES = { 25, 26 };
    private Dictionary<HumanBodyBones, OneEuroFilter<Quaternion>> _filters = new Dictionary<HumanBodyBones, OneEuroFilter<Quaternion>>();
    private void Awake()
    {
	_landmarkConverter = new LandmarkConverter();
	IsMirror = _isMirror;
	_solution = GetComponent<PoseTrackingSolution>();
	//Debug.Log("value" + HumanBodyBones.LeftUpperArm.value);
	Debug.Log("id:" + HumanBodyBones.LeftUpperLeg);
    }

    private void OnEnable()
    {
	_solution.OnPoseLandmarkUpdated += _landmarkConverter.OnLandmarkListUpdate;
	//_solution.OnPoseWorldLandmarkUpdated += _landmarkConverter.OnWorldLandmarkListUpdate;
    }

    private void OnDisable()
    {
	_solution.OnPoseLandmarkUpdated -= _landmarkConverter.OnLandmarkListUpdate;
	//_solution.OnPoseWorldLandmarkUpdated -= _landmarkConverter.OnWorldLandmarkListUpdate;
    }

    private void Update()
    {
	Vector3[] positions = _landmarkConverter.Positions;
	if (positions == null) return;
	bool leftHandFound = InViewport(positions[LEFT_HAND_INDEX]);
	bool rightHandFound = InViewport(positions[RIGHT_HAND_INDEX]);
	LeftHandFound = IsMirror ? rightHandFound : leftHandFound;
	RightHandFound = IsMirror ? leftHandFound : rightHandFound;
	var leftArmBone = IsMirror ? RIGHT_ARM_BONES : LEFT_ARM_BONES;
	var rightArmBone = IsMirror ? LEFT_ARM_BONES : RIGHT_ARM_BONES;

	bool leftToesFound = InViewport(positions[LEFT_TOES_INDEX]);
	bool rightToesFound = InViewport(positions[RIGHT_TOES_INDEX]);
	LeftToesFound = IsMirror ? rightToesFound : leftToesFound;
	RightToesFound = IsMirror ? leftToesFound : rightToesFound;
	var leftLegBone = IsMirror ? RIGHT_LEG_BONES : LEFT_LEG_BONES;
	var rightLegBone = IsMirror ? LEFT_LEG_BONES : RIGHT_LEG_BONES;
	UpdateArmData(positions, leftArmBone, LEFT_ARM_CONN_INDICES, LEFT_ARM_ADJUSTED_ROTATION);
	UpdateArmData(positions, rightArmBone, RIGHT_ARM_CONN_INDICES, RIGHT_ARM_ADJUSTED_ROTATION);

	UpdateLegData(positions, leftLegBone, LEFT_LEG_CONN_INDICES, LEG_ADJUSTED_ROTATION);
	UpdateLegData(positions, rightLegBone, RIGHT_LEG_CONN_INDICES, LEG_ADJUSTED_ROTATION);
    }

    private void UpdateArmData(Vector3[] positions, HumanBodyBones[] bones, int[] connIndices, Quaternion adjustedRot)
    {
	if (bones.Length != connIndices.Length)
	    throw new System.Exception("body input and output count is different");

	Vector3 upwards = Vector3.up;
	for (int i = 0; i < connIndices.Length; i++)
	{
	    Quaternion rotation = GetRotation(positions, connIndices[i], adjustedRot, upwards);
	    upwards = rotation * Vector3.up;
	    Filter(bones[i], rotation);
	}
    }

	private void UpdateLegData(Vector3[] positions, HumanBodyBones[] bones, int[] connIndices, Quaternion adjustedRot)
    {
	if (bones.Length != connIndices.Length)
	    throw new System.Exception("body input and output count is different");

	Vector3 upwards = Vector3.right;
	for (int i = 0; i < connIndices.Length; i++)
	{
	    Quaternion rotation = GetRotation(positions, connIndices[i], adjustedRot, upwards);
	    upwards = rotation * Vector3.right;
	    Filter(bones[i], rotation);
	}
    }

    private void Filter(HumanBodyBones bone, Quaternion rotation)
    {
	if (!_filters.TryGetValue(bone, out OneEuroFilter<Quaternion> f))
	{
	    f = new OneEuroFilter<Quaternion>(_filterFrequency);
	    _filters.Add(bone, f);
	}
	BodyRotation[bone] = f.Filter(rotation);
    }

    public override void SetupFilterFrequency(float frequency)
    {
	base.SetupFilterFrequency(frequency);
	foreach(var f in _filters)
	{
	    f.Value?.UpdateParams(_filterFrequency);
	}
    }

    private bool InViewport(Vector3 position)
    {
	return position.x < 0 && position.x > -1 && position.y < 0 && position.y > -1;
    }

    [SerializeField]
    private int highlightIndex;
    private void OnDrawGizmos()
    {
	if (!_showGizmos || _landmarkConverter == null || _landmarkConverter.Positions == null) return;
	Vector3[] positions = _landmarkConverter.Positions;
	for (int i = 0; i < positions.Length; i++)
	{
	    Gizmos.color = Color.blue;
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
		Gizmos.color = Color.blue;
		Gizmos.DrawLine(positions[_connections[i].Item1] * _gizmosScaleFactor, positions[_connections[i].Item2] * _gizmosScaleFactor);
	    }
	}
    }
}
