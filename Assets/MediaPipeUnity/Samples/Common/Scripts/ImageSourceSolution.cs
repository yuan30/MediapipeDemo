// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using UnityEngine;

namespace Mediapipe.Unity
{
    public abstract class ImageSourceSolution<T> : Solution where T : GraphRunner
    {
	[SerializeField] protected Screen screen;
	[SerializeField] protected T graphRunner;
	[SerializeField] protected TextureFramePool textureFramePool;
	protected WebCamController _webCamController;
	protected WebCamController WebCamController
	{
	    get
	    {
		if (_webCamController == null)
		    _webCamController = FindObjectOfType<WebCamController>();
		return _webCamController;
	    }
	}

	private Coroutine _coroutine;
	private bool _wasDestroy;

	public RunningMode runningMode;
	public bool IsPlaying { get; private set; }

	public long timeoutMillisec
	{
	    get => graphRunner.timeoutMillisec;
	    set => graphRunner.timeoutMillisec = value;
	}

	public override void Play()
	{
	    if (_coroutine != null)
	    {
		Stop();
	    }
	    base.Play();
	    _coroutine = StartCoroutine(Run());
	}

	public override void Pause()
	{
	    base.Pause();
	    WebCamController.Pause(this);
	    IsPlaying = false;
	}

	public override void Resume()
	{
	    base.Resume();
	    var _ = StartCoroutine(WebCamController.Resume(this));
	    IsPlaying = true;
	}

	public override void Stop()
	{
	    base.Stop();
	    if (_coroutine != null && !_wasDestroy)
	    {
		StopCoroutine(_coroutine);
		_coroutine = null;
	    }
	    WebCamController?.Stop(this);
	    graphRunner?.Stop();
	    IsPlaying = false;
	}

	private IEnumerator Run()
	{
	    var graphInitRequest = graphRunner.WaitForInit(runningMode);
	    var imageSource = WebCamController.GetImageSource(this);


	    yield return WebCamController.Play(this);

	    if (!imageSource.isPrepared)
	    {
		Debug.LogError(TAG + " Failed to start ImageSource, exiting...");
		yield break;
	    }


	    // Use RGBA32 as the input format.
	    // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so the following code must be fixed.
	    textureFramePool.ResizeTexture(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32);
	    //SetupScreen(imageSource);

	    yield return graphInitRequest;
	    if (graphInitRequest.isError)
	    {
		Debug.LogError(TAG + " " + graphInitRequest.error);
		yield break;
	    }

	    OnStartRun();
	    graphRunner.StartRun(imageSource);

	    var waitWhilePausing = new WaitWhile(() => isPaused);

	    IsPlaying = true;

	    while (true)
	    {
		if (isPaused)
		{
		    yield return waitWhilePausing;
		}

		if (!textureFramePool.TryGetTextureFrame(out var textureFrame))
		{
		    yield return new WaitForEndOfFrame();
		    continue;
		}

		// Copy current image to TextureFrame
		ReadFromImageSource(imageSource, textureFrame);
		AddTextureFrameToInputStream(textureFrame);
		yield return new WaitForEndOfFrame();

		if (runningMode.IsSynchronous())
		{
		    RenderCurrentFrame(textureFrame);
		    yield return WaitForNextValue();
		}
	    }
	}

	public void SelectSource(int index)
	{
	    WebCamController.SelectSource(this, index);
	}

	protected virtual void SetupScreen(ImageSource imageSource)
	{
	    // NOTE: The screen will be resized later, keeping the aspect ratio.
	    screen.Initialize(imageSource);
	}

	protected virtual void RenderCurrentFrame(TextureFrame textureFrame)
	{
	    screen.ReadSync(textureFrame);
	}

	protected abstract void OnStartRun();

	protected abstract void AddTextureFrameToInputStream(TextureFrame textureFrame);

	protected abstract IEnumerator WaitForNextValue();

	protected virtual void OnDestroy()
	{
	    if (WebCamController != null)
	    {
		WebCamController.Stop(this);
		WebCamController.Remove(this);
	    }
	    _wasDestroy = true;
	}
		
    }

}
