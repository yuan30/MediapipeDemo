using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RotateSpace { World, Local, Absolute }
public interface IOutput
{
    public RotateSpace Space { get; }
}

public interface IBodyOutput : IOutput
{
    public bool BodyFollowHead { get; }
    public bool LeftHandFound { get; }
    public bool RightHandFound { get; }
    public bool LeftToesFound { get; }
    public bool RightToesFound { get; }
    public Dictionary<HumanBodyBones, Quaternion> BodyRotation { get; }
}

public interface IFaceOutput : IOutput
{
    public bool UseHead { get; set;  }
    public Quaternion HeadRotation { get; }
    public string[] BlendshapeNames { get; }
    public Dictionary<int, float> Blendshapes { get; }
}

public interface IFingerOutput : IOutput
{
    public bool UseWrist { get; set; }
    public bool FoundLeftHand { get; }
    public bool FoundRightHand { get; }
    public Dictionary<HumanBodyBones, Quaternion> FingerRotation { get; }
}