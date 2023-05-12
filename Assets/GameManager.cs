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
    [SerializeField]
    private bool _legDetect_forTesting = false;

    public bool _LegDetect {
        get { return _legDetect_forTesting; }
        set {
            if(value == _bodyAnimator.LegDetect)
                return ;
            
            _bodyAnimator.LegDetect = value;
        }
    }

    private void Start()
    {
	_bodyAnimator.BodyOutput = _bodyOutput;
	_bodyAnimator.FaceOutput = _faceOutput;
	_bodyAnimator.FingerOutput = _fingerOutput;
    }


    private void Update() {
        if(_LegDetect != _legDetect_forTesting)
            _LegDetect = _legDetect_forTesting;
    }

}
