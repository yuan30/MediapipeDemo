using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MediapipeOutput : MonoBehaviour
{
    [SerializeField]
    protected bool _showGizmos;
    [SerializeField]
    protected float _gizmosScaleFactor = 50;
    [SerializeField]
    protected bool _isMirror;
    public virtual bool IsMirror { get => _isMirror; set => _isMirror = value; }

    public abstract List<(int, int)> Connections { get; }
    protected float _filterFrequency = 35;
    private Transform _model;

    public void Init(Transform model)
    {
	_model = model;
    }

    public virtual void SetupFilterFrequency(float frequency)
    {
	if (frequency <= 0)
	    _filterFrequency = 0.01f;
	else
	    _filterFrequency = frequency;
    }

    //   private void OnValidate()
    //   {
    //if (!Application.isPlaying) return;
    //IsMirror = _isMirror;
    //   }

    protected Quaternion GetRotation(Vector3[] positions, int connIndex, Quaternion adjustedRotation, Vector3 upwards)
    {
	Vector3 connDir = GetVector(positions, connIndex, IsMirror);

	Quaternion desiredRotation = Quaternion.LookRotation(connDir, upwards) * adjustedRotation;
	return desiredRotation;
    }

    protected Quaternion GetRotation(Vector3[] positions, int connIndex, Vector3 upwards)
    {
	Vector3 connDir = GetVector(positions, connIndex, IsMirror);

	Quaternion desiredRotation = Quaternion.LookRotation(connDir, upwards);
	return desiredRotation;
    }

    protected Vector3 GetVector(Vector3[] positions, int connectionIndex, bool isMirror = false)
    {
	(int, int) connection = Connections[connectionIndex];

	return GetVector(positions, connection.Item1, connection.Item2, isMirror);
    }

    protected Vector3 GetVector(Vector3[] positions, int index1, int index2, bool isMirror = false)
    {
        var startPos = positions[index1];
        var endPos = positions[index2];
        // Debug.Log("_Test_vec_start_point: " + index1 + ", end_point: " + index2);
        // Debug.Log("_Test_vec_start: " + startPos + ", end: " + endPos + "| mirror is " + isMirror);
        // Debug.Log("_Test_vec_: " + (endPos - startPos).normalized);
        return (endPos - startPos).normalized;
        //return isMirror ? (startPos - endPos).normalized : (endPos - startPos).normalized;
    }
}
