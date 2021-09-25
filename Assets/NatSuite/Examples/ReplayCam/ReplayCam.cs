/* 
*   NatCorder
*   Copyright (c) 2021 Yusuf Olokoba
*/

namespace NatSuite.Examples
{


    using System.Threading.Tasks;

    using UnityEngine;
    using Recorders;
    using Recorders.Clocks;
    using Recorders.Inputs;

    public class ReplayCam : MonoBehaviour
    {

        [Header(@"Recording")]
        public int videoWidth = 1280;
        public int videoHeight = 720;
        public bool recordMicrophone;

        private MP4Recorder recorder;
        private CameraInput cameraInput;
        private AudioInput audioInput;
        public AudioSource microphoneSource;

        public string path = "";

        // private IEnumerator Start()
        // {
        //     // Start microphone
        //     microphoneSource = gameObject.AddComponent<AudioSource>();
        //     microphoneSource.mute =
        //     microphoneSource.loop = true;
        //     microphoneSource.bypassEffects =
        //     microphoneSource.bypassListenerEffects = false;
        //     microphoneSource.clip = Microphone.Start(null, true, 1, AudioSettings.outputSampleRate);
        //     yield return new WaitUntil(() => Microphone.GetPosition(null) > 0);
        //     microphoneSource.Play();
        // }

        private void OnDestroy()
        {
            // Stop microphone
            microphoneSource.Stop();
            Microphone.End(null);
        }

        public void StartRecording()
        {
            Debug.Log("ReplayCam StartRecordint");
            // Start recording
            var frameRate = 30;
            var sampleRate = recordMicrophone ? AudioSettings.outputSampleRate : 0;
            var channelCount = recordMicrophone ? (int)AudioSettings.speakerMode : 0;
            var clock = new RealtimeClock();
            recorder = new MP4Recorder(videoWidth, videoHeight, frameRate, sampleRate, channelCount, audioBitRate: 96_000);
            // Create recording inputs
            cameraInput = new CameraInput(recorder, clock, Camera.main);
            audioInput = recordMicrophone ? new AudioInput(recorder, clock, microphoneSource, true) : null;
            // Unmute microphone
            microphoneSource.mute = audioInput == null;
        }

        public async Task<string> StopRecording()
        {
            Debug.Log("ReplayCam StorpRecordint");
            // Mute microphone
            microphoneSource.mute = true;
            // Stop recording
            audioInput?.Dispose();
            cameraInput.Dispose();
            path = await recorder.FinishWriting();
            // Playback recording
            Debug.Log($"Saved recording to: {path}");

            return path;
            //Handheld.PlayFullScreenMovie($"file://{path}");
        }
    }
}