using Mediapipe.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WebCamView : MonoBehaviour
{
    [SerializeField]
    private Dropdown _cameraDropdown;
    [SerializeField]
    private WebCamController _controller;
    [SerializeField]
    private Solution[] _solutions;
	[SerializeField]
	private bool _enabledVideoAsInput = false;
	[SerializeField]
    private VideoSource _videoSourcePrefab;

    private void Start()
    {
	InitDropdownOptions();

	_cameraDropdown.onValueChanged.AddListener(index =>
	{
	    foreach (var solution in _solutions)
	    {
		_controller.SelectSource(solution, index);
	    }
	});
    }

    private void InitDropdownOptions()
    {
	_cameraDropdown.ClearOptions();
	List<string> deviceNames = new List<string>();
		if (_enabledVideoAsInput){
			
			for (int i = 0; i < _videoSourcePrefab.videoClips; i++)
			{
				var device = _videoSourcePrefab.videoSources[i];
				deviceNames.Add(device.name);
			}

			_cameraDropdown.AddOptions(deviceNames);
			return ;
		}
	//_cameraDropdown.ClearOptions();
	//List<string> deviceNames = new List<string>();
	for (int i = 0; i < WebCamTexture.devices.Length; i++)
	{
	    var device = WebCamTexture.devices[i];
	    deviceNames.Add(device.name);
	}

	_cameraDropdown.AddOptions(deviceNames);
    }
}
