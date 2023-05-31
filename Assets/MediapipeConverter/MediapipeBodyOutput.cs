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
	public Dictionary<HumanBodyBones, Vector3> BodyRotationVec3 { get; private set; } = new Dictionary<HumanBodyBones, Vector3>();
    public override List<(int, int)> Connections => _connections;

	public Vector3[] Positions2D { get; private set; }
	public Vector3 Hip2D { get; private set; }
	//public Vector3 hip2D = new Vector3();
    private Quaternion LEFT_ARM_ADJUSTED_ROTATION = Quaternion.AngleAxis(-90, Vector3.up);
    private Quaternion RIGHT_ARM_ADJUSTED_ROTATION = Quaternion.AngleAxis(90, Vector3.up);
	//NEW
	//private Quaternion LEG_ADJUSTED_ROTATION = Quaternion.Euler(90, 0, 0);
	private Quaternion LEG_ADJUSTED_ROTATION = Quaternion.AngleAxis(90, Vector3.right);
	private Quaternion HIP_ADJUSTED_ROTATION = Quaternion.AngleAxis(-90, Vector3.up);

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
	// new ? spine  right <-hip-> left 這邊直接套能行?
	private static readonly int LEFT_SHOULDER_INDEX = 12;
	private static readonly int RIGHT_SHOULDER_INDEX = 11;
	private static readonly int LEFT_HIP_INDEX = 24;
	private static readonly int RIGHT_HIP_INDEX = 23;
	private static readonly int LEFT_KNEE_INDEX = 26;
	private static readonly int RIGHT_KNEE_INDEX = 25;
	private static readonly int[] HIP_CONN_INDEX = { 23 };
	
	[SerializeField] public bool isTest2DPose = true;
	//[SerializeField] public bool isTestWorldPose = false;
    private Dictionary<HumanBodyBones, OneEuroFilter<Quaternion>> _filters = new Dictionary<HumanBodyBones, OneEuroFilter<Quaternion>>();
	private Dictionary<HumanBodyBones, OneEuroFilter<Vector3>> _vec3Filters = new Dictionary<HumanBodyBones, OneEuroFilter<Vector3>>();
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
		_solution.OnPoseWorldLandmarkUpdated += _landmarkConverter.OnWorldLandmarkListUpdate;
    }

    private void OnDisable()
    {
		_solution.OnPoseLandmarkUpdated -= _landmarkConverter.OnLandmarkListUpdate;
		_solution.OnPoseWorldLandmarkUpdated -= _landmarkConverter.OnWorldLandmarkListUpdate;
    }

    private void Update()
    {
		Vector3[] positions = _landmarkConverter.Positions;
		Vector3[] worldPositions = _landmarkConverter.WorldPositions;
		if (positions == null || worldPositions == null) return;
		//------------Test positions values-----------
		Positions2D = positions;
		Debug.LogWarning("Image plane position" + positions[LEFT_HAND_INDEX]);
		Debug.LogWarning("world(human) position" + worldPositions[LEFT_HAND_INDEX]);
		//--------------------------------------------
		//-----Calculate hip and spine positions------
		Vector3 _hip2D = new Vector3(positions[LEFT_HIP_INDEX].x + positions[RIGHT_HIP_INDEX].x, positions[LEFT_HIP_INDEX].y + positions[RIGHT_HIP_INDEX].y
		    , positions[LEFT_HIP_INDEX].z + positions[RIGHT_HIP_INDEX].z) / 2.0F;
		Vector3 _hip3D = new Vector3(worldPositions[LEFT_HIP_INDEX].x + worldPositions[RIGHT_HIP_INDEX].x, worldPositions[LEFT_HIP_INDEX].y + worldPositions[RIGHT_HIP_INDEX].y
		    , worldPositions[LEFT_HIP_INDEX].z + worldPositions[RIGHT_HIP_INDEX].z) / 2.0F;
		Vector3 _sternum = new Vector3(worldPositions[LEFT_SHOULDER_INDEX].x + worldPositions[RIGHT_SHOULDER_INDEX].x
										, worldPositions[LEFT_SHOULDER_INDEX].y + worldPositions[RIGHT_SHOULDER_INDEX].y
										, worldPositions[LEFT_SHOULDER_INDEX].z + worldPositions[RIGHT_SHOULDER_INDEX].z) / 2.0F;
		//Vector3 spine = (positions[LEFT_SHOULDER_INDEX] + positions[RIGHT_SHOULDER_INDEX] + positions[LEFT_HIP_INDEX] + positions[RIGHT_HIP_INDEX]) / 4.0;
		Vector3 _spine = (_hip3D + _sternum) / 2.0F;
		//--------------Arm settings------------------
		bool leftHandFound = InViewport(positions[LEFT_HAND_INDEX]);
		bool rightHandFound = InViewport(positions[RIGHT_HAND_INDEX]);
		LeftHandFound = IsMirror ? rightHandFound : leftHandFound;
		RightHandFound = IsMirror ? leftHandFound : rightHandFound;
		var leftArmBone = IsMirror ? RIGHT_ARM_BONES : LEFT_ARM_BONES;
		var rightArmBone = IsMirror ? LEFT_ARM_BONES : RIGHT_ARM_BONES;
		//--------------Leg settings------------------
		bool leftToesFound = InViewport(positions[LEFT_TOES_INDEX]);
		bool rightToesFound = InViewport(positions[RIGHT_TOES_INDEX]);
		LeftToesFound = IsMirror ? rightToesFound : leftToesFound;
		RightToesFound = IsMirror ? leftToesFound : rightToesFound;
		var leftLegBone = IsMirror ? RIGHT_LEG_BONES : LEFT_LEG_BONES;
		var rightLegBone = IsMirror ? LEFT_LEG_BONES : RIGHT_LEG_BONES;
		
		// initPosition = jointPoints[PositionIndex.hip.Int()].Transform.position;
        // hip.Inverse = Quaternion.Inverse(Quaternion.LookRotation(forward));
        // hip.InverseRotation = hip.Inverse * hip.InitRotation;
		// var forward = TriangleNormal(jointPoints[PositionIndex.hip.Int()].Pos3D, jointPoints[PositionIndex.lThighBend.Int()].Pos3D, jointPoints[PositionIndex.rThighBend.Int()].Pos3D);
		// jointPoints[PositionIndex.hip.Int()].Transform.position = jointPoints[PositionIndex.hip.Int()].Pos3D * 0.005f + new Vector3(initPosition.x, initPosition.y, initPosition.z + dz);
		// jointPoints[PositionIndex.hip.Int()].Transform.rotation = Quaternion.LookRotation(forward) * jointPoints[PositionIndex.hip.Int()].InverseRotation;
		/* -------The solution------
		var forward = TriangleNormal(hip2D, positions[LEFT_HIP_INDEX], positions[RIGHT_HIP_INDEX]); // Either 2D or 3D the direction are same.
		var inverse = Quaternion.Inverse(Quaternion.LookRotation(forward));
		var inverseRotation = inverse * _anim[hip].Transform.position;
		_anim[hip].Transform.position = hip2D + new Vector3(_anim[hip].Transform.position);
		_anim[hip].Transform.rotation = Quaternion.LookRotation(forward) * inverseRotation;
		*/
		//var forward = TriangleNormal(_hip2D, positions[LEFT_HIP_INDEX], positions[RIGHT_HIP_INDEX]); // Either 2D or 3D the direction are same.
		var forward = TriangleNormal(_hip3D, worldPositions[LEFT_HIP_INDEX], worldPositions[RIGHT_HIP_INDEX]);
		//forward = TriangleNormal(_hip3D, worldPositions[RIGHT_KNEE_INDEX], worldPositions[LEFT_KNEE_INDEX]); //方向相反180度
		//forward = TriangleNormal(_hip3D, worldPositions[LEFT_KNEE_INDEX], worldPositions[RIGHT_KNEE_INDEX]);
		// way 2
		forward = TriangleNormal(_spine, worldPositions[RIGHT_HIP_INDEX], worldPositions[LEFT_HIP_INDEX]);
		// way 1
		//forward = TriangleNormal(_spine, worldPositions[LEFT_HIP_INDEX], worldPositions[RIGHT_HIP_INDEX]);
		Hip2D = _hip2D;
		//hip2D = _hip2D;
		
		if (isTest2DPose){
			// UpdateArmData(positions, leftArmBone, LEFT_ARM_CONN_INDICES, LEFT_ARM_ADJUSTED_ROTATION);
			// UpdateArmData(positions, rightArmBone, RIGHT_ARM_CONN_INDICES, RIGHT_ARM_ADJUSTED_ROTATION);

			// UpdateLegData(positions, leftLegBone, LEFT_LEG_CONN_INDICES, LEG_ADJUSTED_ROTATION, forward);
			// UpdateLegData(positions, rightLegBone, RIGHT_LEG_CONN_INDICES, LEG_ADJUSTED_ROTATION, forward);

			//UpdateTorso(positions, HumanBodyBones.Hips, HIP_CONN_INDEX, HIP_ADJUSTED_ROTATION);
			//UpdateTorso(positions, HumanBodyBones.Spine, HIP_CONN_INDEX, HIP_ADJUSTED_ROTATION);
		} else {
			UpdateArmData(worldPositions, leftArmBone, LEFT_ARM_CONN_INDICES, LEFT_ARM_ADJUSTED_ROTATION, forward);
			UpdateArmData(worldPositions, rightArmBone, RIGHT_ARM_CONN_INDICES, RIGHT_ARM_ADJUSTED_ROTATION, forward);

			
			UpdateLegData(worldPositions, leftLegBone, LEFT_LEG_CONN_INDICES, LEG_ADJUSTED_ROTATION, forward);
			UpdateLegData(worldPositions, rightLegBone, RIGHT_LEG_CONN_INDICES, LEG_ADJUSTED_ROTATION, forward);

			// Test hip
			// start (right 畫面上的右) and  end (left 畫面上的左) -> get a vector //HIP_CONN_INDEX
			UpdateTorso(worldPositions, HumanBodyBones.Hips, HIP_CONN_INDEX, HIP_ADJUSTED_ROTATION, forward);
			// way 1
			var spineForward = TriangleNormal(_spine, worldPositions[RIGHT_SHOULDER_INDEX], worldPositions[LEFT_SHOULDER_INDEX]);
			// way 2
			spineForward = TriangleNormal(_spine, worldPositions[LEFT_SHOULDER_INDEX], worldPositions[RIGHT_SHOULDER_INDEX]);
			UpdateTorso(worldPositions, HumanBodyBones.Spine, HIP_CONN_INDEX, HIP_ADJUSTED_ROTATION, spineForward);
		}
    }

	Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 d1 = a - b;
        Vector3 d2 = a - c;

        Vector3 dd = Vector3.Cross(d1, d2);
        dd.Normalize();

        return dd;
    }

    /*private Quaternion GetInverse(JointPoint p1, JointPoint p2, Vector3 forward)
    {
        return Quaternion.Inverse(Quaternion.LookRotation(p1.Transform.position - p2.Transform.position, forward));
    }*/

	private void UpdateTorso(Vector3[] positions, HumanBodyBones bone, int[] connIndices, Quaternion adjustedRot, Vector3 forward) 
	{
		//if (bones.Length != connIndices.Length)
	        //throw new System.Exception("body input and output count is different");
		//Vector3 upwards = Vector3.up;
		//Quaternion rotation = GetRotation(positions, connIndices[0], adjustedRot, upwards);
		var inverse = Quaternion.Inverse(Quaternion.LookRotation(forward));
		var inverseRotation = inverse; // * InitRotation (= Bone.Transform.rotation);
		Filter(bone, Quaternion.LookRotation(forward));

		
		 //* _anim[hip].Transform.rotation;
		//_anim[hip].Transform.position = hip2D + new Vector3(_anim[hip].Transform.position);
		//_anim[hip].Transform.rotation = Quaternion.LookRotation(forward) * inverseRotation;

	}

    private void UpdateArmData(Vector3[] positions, HumanBodyBones[] bones, int[] connIndices, Quaternion adjustedRot, Vector3 forward)
    {
		if (bones.Length != connIndices.Length)
			throw new System.Exception("body input and output count is different");

		//Vector3 upwards = Vector3.up;
		for (int i = 0; i < connIndices.Length; i++)
		{
			Quaternion rotation = GetRotation(positions, connIndices[i], adjustedRot, forward);
			//upwards = rotation * Vector3.up;
			Filter(bones[i], rotation);
		}
    }

	private void UpdateLegData(Vector3[] positions, HumanBodyBones[] bones, int[] connIndices, Quaternion adjustedRot, Vector3 forward)
    {
		if (bones.Length != connIndices.Length)
			throw new System.Exception("body input and output count is different");

		//Vector3 upwards = Vector3.up;
		for (int i = 0; i < connIndices.Length; i++)
		{
			//Quaternion rotation = i == 0 ? GetRotation(positions, connIndices[i], adjustedRot, upwards): GetRotation(positions, connIndices[i], upwards);
			Quaternion rotation = GetRotation(positions, connIndices[i], adjustedRot, forward);
			Debug.Log("_Test_Leg_" + connIndices[i] + ", " + rotation.eulerAngles);
			//upwards = rotation * Vector3.up;
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
	/*
	private void Filter(HumanBodyBones bone, Vector3 rotation)
    {
		if (!_vec3Filters.TryGetValue(bone, out OneEuroFilter<Vector3> f))
		{
			f = new OneEuroFilter<Vector3>(_filterFrequency);
			_vec3Filters.Add(bone, f);
		}
		BodyRotationVec3[bone] = f.Filter(rotation);
    }*/

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
	//Vector3[] positions = _landmarkConverter.Positions;
	Vector3[] positions = _landmarkConverter.WorldPositions;
	//Vector3[] worldPositions = _landmarkConverter.WorldPositions;
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
