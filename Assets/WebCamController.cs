using Mediapipe.Unity;
using Mediapipe.Unity.FaceMesh;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WebCamSourceCounter
{
    public ImageSource Source;
    public string SourceName;
    public int Count;
}

public class WebCamController : MonoBehaviour
{
    [SerializeField]
    //private WebCamSource _webCamSourcePrefab;
	private ImageSource _webCamSourcePrefab;
    [SerializeField]
    private bool _needPreview;
    [SerializeField]
    private RawImage _previewPrefab;
    [SerializeField]
    private Transform _previewParent;
	[SerializeField]
    private int _clips = 0;

    private bool _isBackgroundOpen;
    private WebCamSourceCounter _mediaSourceCounter;
    private Dictionary<Solution, WebCamSourceCounter> _solutionToSourceMapping = new Dictionary<Solution, WebCamSourceCounter>();
    private List<WebCamSourceCounter> _sourceCounterList = new List<WebCamSourceCounter>();
    private Dictionary<WebCamSourceCounter, RawImage> _sourceCouterPreviewMapping = new Dictionary<WebCamSourceCounter, RawImage>();
    //private Subject<Texture> _onTextureUpdate = new Subject<Texture>();
    //public IObservable<Texture> OnTextureUpdate => _onTextureUpdate;

    private void Start()
    {

    }

    public void ToggleMedia(bool isOn, IObserver<Texture> observer)
    {
	if (isOn)
	    OpenMedia(observer);
	else
	    CloseMedia();
    }

    public void OpenMedia(IObserver<Texture> observer)
    {
	//Debug.Log(_backgroundSourceCounter == null);
	if (!TryGetDeviceSourceCounter(out var sourceCounter))
	{
	    _mediaSourceCounter = CreateSourceCounter();
	}
	else
	    _mediaSourceCounter = sourceCounter;

	_isBackgroundOpen = true;
	StartCoroutine(Play(_mediaSourceCounter, observer));
	//_isBackgroundOpen = true;
	//StartCoroutine(Play(force));
    }

    public void CloseMedia()
    {
	Stop(_mediaSourceCounter);
	//Debug.Log(_backgroundSourceCounter == null);
	if (_mediaSourceCounter.Count == 0)
	{
	    _mediaSourceCounter = null;
	    _isBackgroundOpen = false;
	}
    }

    public IEnumerator Play(Solution solution)
    {Debug.Log("---===START MP: " + solution.toString() + " ===---");
	WebCamSourceCounter sourceCounter = _solutionToSourceMapping[solution];
	yield return Play(sourceCounter);
    }

    public void Pause(Solution solution)
    {
	Pause(_solutionToSourceMapping[solution]);
    }

    public IEnumerator Resume(Solution solution)
    {
	yield return Resume(_solutionToSourceMapping[solution]);
    }

    public void Stop(Solution solution)
    {
	Stop(_solutionToSourceMapping[solution]);
    }

    public void Remove(Solution solution)
    {
	_solutionToSourceMapping.Remove(solution);
    }

    private IEnumerator Play(WebCamSourceCounter sourceCounter, IObserver<Texture> observer = null)
    {
	if (!sourceCounter.Source.isPrepared)
	{
	    yield return sourceCounter.Source.Play();
	}

	if (!_sourceCouterPreviewMapping.ContainsKey(sourceCounter))
	{
	    var preview = Instantiate(_previewPrefab, _previewParent);
	    preview.texture = sourceCounter.Source.GetCurrentTexture();
	    _sourceCouterPreviewMapping.Add(sourceCounter, preview);
	}

	observer?.OnNext(sourceCounter.Source.GetCurrentTexture());

	sourceCounter.Count++;
	//Debug.Log(sourceCounter.Count);
    }

    private void Pause(WebCamSourceCounter sourceCounter)
    {
	sourceCounter.Source.Pause();
    }

    private IEnumerator Resume(WebCamSourceCounter sourceCounter)
    {
	yield return sourceCounter.Source.Resume();
    }

    private void Stop(WebCamSourceCounter sourceCounter)
    {
	sourceCounter.Count--;
	//Debug.Log(sourceCounter.Count);
	if (sourceCounter.Count <= 0)
	{
	    sourceCounter.Source.Stop();
	    //Destroy(sourceCounter.Source.gameObject);
	    if (_sourceCouterPreviewMapping.TryGetValue(sourceCounter, out RawImage preview))
	    {
		if (preview != null)
		    Destroy(preview.gameObject);
		_sourceCouterPreviewMapping.Remove(sourceCounter);
	    }
	}

	//if (!_isBackgroundOpen)
	//    _webCamSource.Stop();
    }

    public ImageSource GetImageSource(Solution solution)
    {
	if (!_solutionToSourceMapping.TryGetValue(solution, out var sourceCounter))
	{
	    if (TryGetDeviceSourceCounter(out var firstSourceCounter))
	    {
		sourceCounter = firstSourceCounter;
	    }
	    else
	    {
		sourceCounter = CreateSourceCounter();
	    }
	    _solutionToSourceMapping.Add(solution, sourceCounter);
	}
	return sourceCounter.Source;
	//return _webCamSource;
    }

    public void SelectSource(Solution solution, int index)
    {
	bool hasCurrentSourceCounter = _solutionToSourceMapping.TryGetValue(solution, out var currentSourceCounter);
	bool hasTargetSourceCounter = TryGetDeviceSourceCounter(out var targetSourceCounter, index);

	if (hasCurrentSourceCounter && hasTargetSourceCounter && currentSourceCounter == targetSourceCounter) return;

	if (hasCurrentSourceCounter)
	    solution.Stop();

	if (!hasTargetSourceCounter)
	{
	    targetSourceCounter = CreateSourceCounter(index);
	}
	Debug.Log(targetSourceCounter.Source.sourceName);
	_solutionToSourceMapping[solution] = targetSourceCounter;
	Debug.Log(_solutionToSourceMapping[solution].Source.sourceName);
	solution.Play();
	//_webCamSource.SelectSource(index);
	//StartCoroutine(Play(true));
    }

    public void SelectBackgroundSource(int index, IObserver<Texture> observer)
    {
	bool hasCurrentSourceCounter = _mediaSourceCounter != null;
	bool hasTargetSourceCounter = TryGetDeviceSourceCounter(out var targetSourceCounter, index);

	if (hasCurrentSourceCounter && hasTargetSourceCounter && _mediaSourceCounter == targetSourceCounter)
	{
	    observer.OnNext(_mediaSourceCounter.Source.GetCurrentTexture());
	    _mediaSourceCounter.Count++;
	    return;
	}

	if (hasCurrentSourceCounter)
	    Stop(_mediaSourceCounter);

	if (!hasTargetSourceCounter)
	{
	    targetSourceCounter = CreateSourceCounter(index);
	}

	_mediaSourceCounter = targetSourceCounter;
	StartCoroutine(Play(targetSourceCounter, observer));
    }

    private void SetBackground()
    {
	//for (int i = 0; i < _materialProperties.Length; i++)
	//{
	//    _backgrounds[i].gameObject.SetActive(true);
	//    var materialProperty = _materialProperties[i];
	//    materialProperty.SetTexture("_UnlitColorMap", _backgroundSourceCounter.Source.GetCurrentTexture());
	//    _backgrounds[i].SetPropertyBlock(materialProperty);
	//}
    }

    private WebCamSourceCounter CreateSourceCounter(int index = 0)
    {
		index = _clips;
		//Debug.Log("vidop clips:"+_webCamSourcePrefab.videoClips);
	ImageSource source = Instantiate(_webCamSourcePrefab, transform);
	source.SelectSource(index);
	WebCamSourceCounter sourceCounter = new WebCamSourceCounter()
	{
	    Source = source,
	    SourceName = source.sourceName,
	    Count = 0
	};
	_sourceCounterList.Add(sourceCounter);
	return sourceCounter;
    }

    private bool TryGetDeviceSourceCounter(out WebCamSourceCounter sourceCounter, int index = 0)
    {
	string deviceName = WebCamTexture.devices[index].name;
	int deviceHash = deviceName.GetHashCode();
	for (int i = 0; i < _sourceCounterList.Count; i++)
	{
	    if (deviceHash == _sourceCounterList[i].SourceName.GetHashCode())
	    {
		sourceCounter = _sourceCounterList[i];
		return true;
	    }
	}
	sourceCounter = null;
	return false;
    }
}
