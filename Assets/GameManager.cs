using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private MediapipeBodyOutput _bodyOutput;
    [SerializeField]
    private MediapipeFaceOutput _faceOutput;
    [SerializeField]
    private MediapipeFingerOutput _fingerOutput;
    [SerializeField]
    private BodyAnimator _bodyAnimator;

    private void Start()
    {
	_bodyAnimator.BodyOutput = _bodyOutput;
	_bodyAnimator.FaceOutput = _faceOutput;
	_bodyAnimator.FingerOutput = _fingerOutput;
    }

}
