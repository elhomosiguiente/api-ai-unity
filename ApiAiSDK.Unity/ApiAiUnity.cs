using System;
using UnityEngine;
using ApiAiSDK;
using ApiAiSDK.Model;

namespace ApiAiSDK.Unity
{
	public class AIResponseEventArgs : EventArgs
	{
		private readonly AIResponse response;

		public AIResponse Response {
			get {
				return response;
			}
		}

		public AIResponseEventArgs(AIResponse response)
		{
			this.response = response;
		}
	}

	public class AIErrorEventArgs : EventArgs
	{

		private readonly Exception exception;

		public Exception Exception {
			get {
				return exception;
			}
		}

		public AIErrorEventArgs(Exception ex)
		{
			exception = ex;
		}
	}

	public class ApiAiUnity
	{
		private ApiAi apiAi;
		AudioSource audioSource;
		volatile bool recordingActive;
		private object thisLock = new object();

		public event EventHandler<AIResponseEventArgs> OnResult;
		public event EventHandler<AIErrorEventArgs> OnError;
		public event EventHandler<EventArgs> OnListeningStarted;
		public event EventHandler<EventArgs> OnListeningFinished;

		public ApiAiUnity()
		{

		}

		public void Initialize(AIConfiguration config)
		{
			apiAi = new ApiAi(config);
		}

		public void StartListening(AudioSource audioSource)
		{
			lock (thisLock) {
				if (!recordingActive) {
					this.audioSource = audioSource;
					StartRecording();
				} else {
					throw new InvalidOperationException("Can't start ne recording session while another recording session active");
				}
			}
		}

		public void StopListening()
		{
			if (recordingActive) {

				float[] samples = null;
	
				lock (thisLock) {
					if (recordingActive) {
						StopRecording();
						samples = new float[audioSource.clip.samples];
						audioSource.clip.GetData(samples, 0);
						audioSource = null;
					}
				}

				if (samples != null) {
					try {
						var aiResponse = apiAi.voiceRequest(samples);
						ProcessResult(aiResponse);	
					} catch (Exception ex) {
						if (OnError != null) {
							OnError(this, new AIErrorEventArgs(ex));
						}
					}


				}
			}
		}

		private void ProcessResult(AIResponse aiResponse)
		{
			if (aiResponse != null) {
				if (OnResult != null) {
					OnResult(this, new AIResponseEventArgs(aiResponse));
				}
			} else {
				if (OnError != null) {
					OnError(this, new AIErrorEventArgs(new Exception("API.AI Service returns null")));
				}
			}
		}

		private void StartRecording()
		{
			audioSource.clip = Microphone.Start(null, true, 20, 16000);
			recordingActive = true;
			if (OnListeningStarted != null) {
				OnListeningStarted(this, EventArgs.Empty);
			}
		}

		private void StopRecording()
		{
			Microphone.End(null);
			recordingActive = false;
			if (OnListeningFinished != null) {
				OnListeningFinished(this, EventArgs.Empty);
			}
		}

		public AIResponse TextRequest(string request)
		{
			return apiAi.textRequest(request);
		}

		public void ResetContexts()
		{
			// TODO
		}


	}
}

