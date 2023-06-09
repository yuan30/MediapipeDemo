using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyAnimator : MonoBehaviour
{
	[SerializeField]
    private bool _legDetect_forTesting = false;
	public bool LegDetect { get { return _legDetect_forTesting; } set { _legDetect_forTesting = value; } }

    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private float _delayTime = 0.5f;
    public float NotFoundBufferTime { get { return _delayTime; } set { _delayTime = value; } }

    [SerializeField]
    private bool _useSmooth = true;
    public bool UseSmooth { get { return _useSmooth; } set { _useSmooth = value; } }

    [SerializeField]
    private float _smoothSpeed = 5f;
    public float SmoothSpeed { get { return _smoothSpeed; } set { _smoothSpeed = value; } }

    private float _notFoundLeftHandScheduledTime;
    private float _notFoundRightHandScheduledTime;

    private float _leftArmDetectedLerpValue;
    private float _rightArmDetectedLerpValue;
    private float _leftForearmDetectedLerpValue;
    private float _rightForearmDetectedLerpValue;
    private float _leftHandDetectedLerpValue;
    private float _rightHandDetectedLerpValue;

	private float _RightThigh, _RightLeg, _LeftThigh, _LeftLeg, _Hips;

    private IBodyOutput _bodyOutput;
    public IBodyOutput BodyOutput { get => _bodyOutput; set { _bodyOutput = value; } }

    private IFingerOutput _fingerOutput;
    public IFingerOutput FingerOutput { get => _fingerOutput; set { _fingerOutput = value; } }

    private IFaceOutput _faceOutput;
    public IFaceOutput FaceOutput { get => _faceOutput; set { _faceOutput = value; } }

    private const int LOWER_ARM_OFFSET = 2;
    private const int HAND_OFFSET = 4;
    private const int FINGER_COUNT = 15;

    private static readonly Quaternion LEFT_HAND_IDLE_ROTATION = Quaternion.Euler(0, 0, 10);
    private static readonly Quaternion RIGHT_HAND_IDLE_ROTATION = Quaternion.Euler(0, 0, -10);
    private static readonly Quaternion LEFT_ARM_IDLE_ROTATION = Quaternion.Euler(0, 0, 35);
    private static readonly Quaternion RIGHT_ARM_IDLE_ROTATION = Quaternion.Euler(0, 0, -35);
    private static readonly Quaternion LEFT_FOREARM_IDLE_ROTATION = Quaternion.Euler(0, 0, 10);
    private static readonly Quaternion RIGHT_FOREARM_IDLE_ROTATION = Quaternion.Euler(0, 0, -10);

	private static readonly Quaternion L_LEG_IDLE_ROTATION = Quaternion.Euler(0, 0, -30); //畫面看上去的左
	private static readonly Quaternion R_LEG_IDLE_ROTATION = Quaternion.Euler(0, 0, 30); //畫面看上去的右

	private static readonly Quaternion IDLE_ROTATION = Quaternion.Euler(0, 0, 0);

	private Vector3 hipOffest;
    public void Awake()
    {
		_animator = GetComponent<Animator>();
		hipOffest = _animator.GetBoneTransform(HumanBodyBones.Hips).position;
    }

    private void Update()
    {
	if (_bodyOutput != null)
	{
	    _leftArmDetectedLerpValue = GetLerpValue(_leftArmDetectedLerpValue, _bodyOutput.LeftHandFound);
	    UpdateAnimatorArm(_bodyOutput, HumanBodyBones.LeftUpperArm, LEFT_ARM_IDLE_ROTATION, _leftArmDetectedLerpValue);
	    _leftForearmDetectedLerpValue = GetLerpValue(_leftForearmDetectedLerpValue, _bodyOutput.LeftHandFound);
	    UpdateAnimatorArm(_bodyOutput, HumanBodyBones.LeftLowerArm, LEFT_FOREARM_IDLE_ROTATION, _leftForearmDetectedLerpValue);
	    _rightArmDetectedLerpValue = GetLerpValue(_rightArmDetectedLerpValue, _bodyOutput.RightHandFound);
	    UpdateAnimatorArm(_bodyOutput, HumanBodyBones.RightUpperArm, RIGHT_ARM_IDLE_ROTATION, _rightArmDetectedLerpValue);
	    _rightForearmDetectedLerpValue = GetLerpValue(_rightForearmDetectedLerpValue, _bodyOutput.RightHandFound);
	    UpdateAnimatorArm(_bodyOutput, HumanBodyBones.RightLowerArm, RIGHT_FOREARM_IDLE_ROTATION, _rightForearmDetectedLerpValue);

		//NEW
		if (_legDetect_forTesting) {
			_RightThigh = GetLerpValue(_RightThigh, _bodyOutput.RightToesFound);
			UpdateAnimatorLeg(_bodyOutput, HumanBodyBones.RightUpperLeg, R_LEG_IDLE_ROTATION, _RightThigh);
			_RightLeg = GetLerpValue(_RightLeg, _bodyOutput.RightToesFound);
			UpdateAnimatorLeg(_bodyOutput, HumanBodyBones.RightLowerLeg, R_LEG_IDLE_ROTATION, _RightLeg);
			_LeftThigh = GetLerpValue(_LeftThigh, _bodyOutput.LeftToesFound);
			UpdateAnimatorLeg(_bodyOutput, HumanBodyBones.LeftUpperLeg, L_LEG_IDLE_ROTATION, _LeftThigh);
			_LeftLeg = GetLerpValue(_LeftLeg, _bodyOutput.LeftToesFound);
			UpdateAnimatorLeg(_bodyOutput, HumanBodyBones.LeftLowerLeg, L_LEG_IDLE_ROTATION, _LeftLeg);
		}

		_Hips = GetLerpValue(_LeftLeg, true);
		UpdateAnimatorHip(_bodyOutput, HumanBodyBones.Hips, IDLE_ROTATION, hipOffest);
		UpdateAnimatorHip(_bodyOutput, HumanBodyBones.Spine, IDLE_ROTATION, hipOffest);
		//UpdateAnimatorArm(_bodyOutput, HumanBodyBones.Hips, IDLE_ROTATION, _Hips);
		//UpdateAnimatorArm(_bodyOutput, HumanBodyBones.Spine, IDLE_ROTATION, _Hips);


	    //UpdateAnimatorBones(_bodyOutput.BodyRotation, _bodyOutput.Space);
	    /*if (_bodyOutput.BodyFollowHead)
	    {
		Transform head = _animator.GetBoneTransform(HumanBodyBones.Head);
		Transform spine = _animator.GetBoneTransform(HumanBodyBones.Spine);
		spine.rotation = Quaternion.Slerp(Quaternion.LookRotation(transform.forward), head.rotation, 0.3f);
	    }*/
	}

	if (_fingerOutput != null)
	{
	    //bool found = GetBooleanFalseWithDelay(_fingerOutput.FoundLeftHand, ref _notFoundLeftHandScheduledTime);
	    _leftHandDetectedLerpValue = GetLerpValue(_leftHandDetectedLerpValue, _fingerOutput.FoundLeftHand);
	    UpdateAnimatorArm(_fingerOutput, (int)HumanBodyBones.LeftHand, (int)HumanBodyBones.LeftThumbProximal, LEFT_HAND_IDLE_ROTATION, _leftHandDetectedLerpValue);

	    //found = GetBooleanFalseWithDelay(_fingerOutput.FoundRightHand, ref _notFoundRightHandScheduledTime);
	    _rightHandDetectedLerpValue = GetLerpValue(_rightHandDetectedLerpValue, _fingerOutput.FoundRightHand);
	    UpdateAnimatorArm(_fingerOutput, (int)HumanBodyBones.RightHand, (int)HumanBodyBones.RightThumbProximal, RIGHT_HAND_IDLE_ROTATION, _rightHandDetectedLerpValue);

	}

	if (_faceOutput != null && _faceOutput.UseHead)
	{
	    Transform head = _animator.GetBoneTransform(HumanBodyBones.Head);
	    head.localRotation = _faceOutput.HeadRotation;
	}
    }

    private bool GetBooleanFalseWithDelay(bool boolean, ref float scheduledTime)
    {
	if (!boolean)
	{
	    if (Time.time > scheduledTime)
		return false;
	}
	else
	    scheduledTime = Time.time + _delayTime;

	return true;
    }

    private float GetLerpValue(float source, bool found)
    {
		float result;
		if (_useSmooth)
		{
			float speed = found ? _smoothSpeed : -_smoothSpeed;
			result = Mathf.Clamp01(source + speed * Time.deltaTime);
		}
		else
			result = found ? 1 : 0;
		return result;
    }

	/*private void UpdateAnimatorArm(IBodyOutput output, HumanBodyBones bone, Quaternion idleRotation, float detectedLerpValue)
    {
		if (output == null) return;

		if (!output.BodyRotation.TryGetValue(bone, out Quaternion outputHandRot)) return;


		Quaternion originHandRot = Quaternion.Inverse(_animator.transform.rotation) * _animator.GetBoneTransform(bone).parent.rotation * idleRotation;
		Quaternion resultHandRot = Quaternion.Slerp(originHandRot, outputHandRot, detectedLerpValue);
		//Debug.Log("hand"+resultHandRot);
		UpdateAnimatorBone(bone, resultHandRot, output.Space);
    }*/
	Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 d1 = a - b;
        Vector3 d2 = a - c;

        Vector3 dd = Vector3.Cross(d1, d2);
        dd.Normalize();

        return dd;
    }
	private void UpdateAnimatorHip(IBodyOutput output, HumanBodyBones bone, Quaternion rotation, Vector3 positionOffest)
    {
		if (output == null) return;
		if (!output.BodyRotation.TryGetValue(bone, out Quaternion outputHipRot)) return;

		Transform boneTransform = _animator.GetBoneTransform(bone);
		if (boneTransform == null) return;

		var forward = TriangleNormal(_animator.GetBoneTransform(bone).position, _animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position, 
						_animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position);
		var originInverseRotation = Quaternion.Inverse(Quaternion.LookRotation(forward)) * _animator.GetBoneTransform(bone).parent.rotation;

		var pos = 1 * output.Hip2D +  new Vector3(positionOffest.x, positionOffest.y, positionOffest.z);
		//boneTransform.position = pos;
		//Debug.Log("_test_Hip2D: " + boneTransform.position);

		//new Vector3(boneTransform.position.x, boneTransform.position.y, boneTransform.position.z);
		//boneTransform.rotation = Quaternion.LookRotation(forward) * inverseRotation;
		
		_Hips = GetLerpValue(_Hips, true);
		Quaternion resultHandRot = Quaternion.Slerp(_animator.GetBoneTransform(bone).rotation, outputHipRot, _Hips);
		if (bone == HumanBodyBones.Hips)
			//_animator.GetBoneTransform(bone).parent.rotation = outputHipRot;
			boneTransform.rotation = resultHandRot ;
		else if (bone == HumanBodyBones.Spine) {
			/*var sf = TriangleNormal(_animator.GetBoneTransform(bone).position, _animator.GetBoneTransform(HumanBodyBones.LeftShoulder).position, 
						_animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position);
			originInverseRotation = */
			boneTransform.rotation = resultHandRot ;//* originInverseRotation;
		}
		/*if (space == RotateSpace.Local)
		{
			boneTransform.localRotation = rotation;
		}
		else if (space == RotateSpace.World)
		{
			var rot = _animator.transform.rotation * rotation;
			boneTransform.rotation = rot;
		}
		else if (space == RotateSpace.Absolute)
		{
			boneTransform.rotation = rotation;
		}*/
    }

    private void UpdateAnimatorArm(IFingerOutput output, int handIndex, int thumbProximalIndex, Quaternion idleRotation, float detectedLerpValue)
    {
	if (output == null) return;

	if (!output.FingerRotation.TryGetValue((HumanBodyBones)handIndex, out Quaternion outputHandRot)) return;

	Quaternion originHandRot = Quaternion.Inverse(_animator.transform.rotation) * _animator.GetBoneTransform((HumanBodyBones)handIndex).parent.rotation * idleRotation;
	Quaternion resultHandRot = Quaternion.Slerp(originHandRot, outputHandRot, detectedLerpValue);
	UpdateAnimatorBone((HumanBodyBones)handIndex, resultHandRot, output.Space);

	for (int i = 0; i < FINGER_COUNT; i++)
	{
	    var bone = (HumanBodyBones)(thumbProximalIndex + i);
	    if (!output.FingerRotation.TryGetValue(bone, out Quaternion outputRotation)) continue;

	    Quaternion originRot = originHandRot * idleRotation;
	    Quaternion resultRot = Quaternion.Slerp(originRot, outputRotation, detectedLerpValue);
		Debug.Log(resultRot);
	    UpdateAnimatorBone(bone, resultRot, output.Space);
	}
    }

    private void UpdateAnimatorArm(IBodyOutput output, HumanBodyBones bone, Quaternion idleRotation, float detectedLerpValue)
    {
		if (output == null) return;

		if (!output.BodyRotation.TryGetValue(bone, out Quaternion outputHandRot)) return;


		Quaternion originHandRot = Quaternion.Inverse(_animator.transform.rotation) * _animator.GetBoneTransform(bone).parent.rotation * idleRotation;
		Quaternion resultHandRot = Quaternion.Slerp(originHandRot, outputHandRot, detectedLerpValue);
		//Debug.Log("hand"+resultHandRot);
		UpdateAnimatorBone(bone, outputHandRot, output.Space);
    }

	private void UpdateAnimatorLeg(IBodyOutput output, HumanBodyBones bone, Quaternion idleRotation, float detectedLerpValue)
    {
		if (output == null) return;

		if (!output.BodyRotation.TryGetValue(bone, out Quaternion outputHandRot)) return;
		//Debug.Log(outputHandRot);
		// Quaternion characterRotation = _animator.transform.rotation;
		// Quaternion avatarRotation = _animator.GetBoneTransform(bone).parent.rotation;
		// Quaternion boneMappingQuaternion = Quaternion.Inverse(avatarRotation) * characterRotation;
		// Debug.Log("bonemapping: " + boneMappingQuaternion);
		Quaternion originHandRot = Quaternion.Inverse(_animator.transform.rotation) * _animator.GetBoneTransform(bone).parent.rotation;// * idleRotation;
		Quaternion resultHandRot = Quaternion.Slerp(originHandRot, outputHandRot, detectedLerpValue);

		//Debug.Log("_Test_Leg_origin" + originHandRot.eulerAngles);
		Debug.Log("_Test_Leg_output" + outputHandRot.eulerAngles);
		Debug.Log("_Test_Leg_output" + resultHandRot.eulerAngles);
		//UpdateAnimatorBone(bone, resultHandRot, output.Space);
		UpdateAnimatorBone(bone, outputHandRot, output.Space);

	}

    private void UpdateAnimatorBones(Dictionary<HumanBodyBones, Quaternion> outputMapping, RotateSpace space, params HumanBodyBones[] skipBones)
    {
	if (skipBones.Length > 0)
	    for (int i = 0; i < skipBones.Length; i++)
	    {
		if (outputMapping.ContainsKey(skipBones[i]))
		    outputMapping.Remove(skipBones[i]);
	    }

	foreach (var outputRotation in outputMapping)
	{
	    if (_faceOutput != null && _faceOutput.UseHead && outputRotation.Key == HumanBodyBones.Head) continue;
	    if (_fingerOutput != null && _fingerOutput.UseWrist &&
		(outputRotation.Key == HumanBodyBones.LeftHand || outputRotation.Key == HumanBodyBones.RightHand)) continue;
	    UpdateAnimatorBone(outputRotation.Key, outputRotation.Value, space);
	}
    }

    private void UpdateAnimatorBone(HumanBodyBones bone, Quaternion rotation, RotateSpace space = RotateSpace.Local)
    {
		Transform boneTransform = _animator.GetBoneTransform(bone);
		if (boneTransform == null) return;
		if (space == RotateSpace.Local)
		{
			boneTransform.localRotation = rotation;
		}
		else if (space == RotateSpace.World)
		{
			var rot = _animator.transform.rotation * rotation;
			boneTransform.rotation = rot;
		}
		else if (space == RotateSpace.Absolute)
		{
			boneTransform.rotation = rotation;
		}
    }

    private void ResetAnimatorBone(HumanBodyBones bone)
    {
	UpdateAnimatorBone(bone, Quaternion.identity);
    }

    private void ResetAnimatorArm(int upperArmBoneIndex, Quaternion idleRotation)
    {
	HumanBodyBones upperArmBone = (HumanBodyBones)upperArmBoneIndex;
	HumanBodyBones lowerArmBone = (HumanBodyBones)upperArmBoneIndex + LOWER_ARM_OFFSET;
	HumanBodyBones handBone = (HumanBodyBones)upperArmBoneIndex + HAND_OFFSET;

	UpdateAnimatorBone(upperArmBone, idleRotation);
	ResetAnimatorBone(lowerArmBone);
	ResetAnimatorBone(handBone);
    }

    private void ResetAnimatorFinger(int thumbIndex)
    {
	for (int i = 0; i < FINGER_COUNT; i++)
	{
	    var bone = (HumanBodyBones)(thumbIndex + i);
	    ResetAnimatorBone(bone);
	}
    }

    private void OnDrawGizmos()
    {
	if (_bodyOutput == null || _bodyOutput.BodyRotation == null) return;
	foreach (var bodyRotation in _bodyOutput.BodyRotation)
	{
	    var boneTransform = _animator.GetBoneTransform(bodyRotation.Key);
	    Gizmos.DrawRay(boneTransform.position, boneTransform.up * 1);
	}
    }
}
