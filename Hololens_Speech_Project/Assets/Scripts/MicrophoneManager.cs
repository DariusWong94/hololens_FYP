// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;

public class MicrophoneManager : MonoBehaviour
{
    [Tooltip("A text area for the recognizer to display the recognized strings.")]
    [SerializeField]
    private Text dictationDisplay;
    private DictationRecognizer dictationRecognizer;

    // Use this string to cache the text currently displayed in the text box.
    private StringBuilder textSoFar;

    // Using an empty string specifies the default microphone. 
    private static string deviceName = string.Empty;
    private int samplingRate;
    private const int messageLength = 10;

    // Use this to reset the UI once the Microphone is done recording after it was started.
    private bool hasRecordingStarted;

    public Button StartBtn, StopBtn;
    void Awake()
    {

        Button Start = StartBtn.GetComponent<Button>();
        Button Stop = StopBtn.GetComponent<Button>();

        Start.onClick.AddListener(startOnClick);
        Stop.onClick.AddListener(stopOnClick);

        /* TODO: DEVELOPER CODING EXERCISE 3.a */

        // 3.a: Create a new DictationRecognizer and assign it to dictationRecognizer variable.
        dictationRecognizer = new DictationRecognizer();

        // 3.a: Register for dictationRecognizer.DictationHypothesis and implement DictationHypothesis below
        // This event is fired while the user is talking. As the recognizer listens, it provides text of what it's heard so far.
        dictationRecognizer.DictationHypothesis += DictationRecognizer_DictationHypothesis;

        // 3.a: Register for dictationRecognizer.DictationResult and implement DictationResult below
        // This event is fired after the user pauses, typically at the end of a sentence. The full recognized string is returned here.
        dictationRecognizer.DictationResult += DictationRecognizer_DictationResult;

        // 3.a: Register for dictationRecognizer.DictationComplete and implement DictationComplete below
        // This event is fired when the recognizer stops, whether from Stop() being called, a timeout occurring, or some other error.
        dictationRecognizer.DictationComplete += DictationRecognizer_DictationComplete;

        // 3.a: Register for dictationRecognizer.DictationError and implement DictationError below
        // This event is fired when an error occurs.
        dictationRecognizer.DictationError += DictationRecognizer_DictationError;

        // Query the maximum frequency of the default microphone. Use 'unused' to ignore the minimum frequency.
        int unused;
        Microphone.GetDeviceCaps(deviceName, out unused, out samplingRate);

        // Use this string to cache the text currently displayed in the text box.
        textSoFar = new StringBuilder();

        // Use this to reset the UI once the Microphone is done recording after it was started.
        hasRecordingStarted = false;
           
    }

    private void startOnClick()
    {
        StartRecording();
        Debug.Log("Starting");
    }

    private void stopOnClick()
    {
        StopRecording();

        Debug.Log("Stopping");
    }

    void Update()
    {
        // 3.a: Add condition to check if dictationRecognizer.Status is Running
        if (hasRecordingStarted && !Microphone.IsRecording(deviceName) && dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            // Reset the flag now that we're cleaning up the UI.
            hasRecordingStarted = false;
            
        }
    }

    /// Turns on the dictation recognizer and begins recording audio from the default microphone.
    public AudioClip StartRecording()
    {
        //Shutdown the PhraseRecognitionSystem. This controls the KeywordRecognizers
        PhraseRecognitionSystem.Shutdown();
        //Start dictationRecognizer
        dictationRecognizer.Start();
        //Uncomment this line
        dictationDisplay.text = "Now Listening...";
        //Set the flag that we've started recording.
        hasRecordingStarted = true;
        //Start recording from the microphone for 10 seconds.
        return Microphone.Start(deviceName, false, messageLength, samplingRate);
    }

    /// Ends the recording session.
    public void StopRecording()
    {
        //Check if dictationRecognizer.Status is Running and stop it if so
        if (dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            dictationRecognizer.Stop();
            dictationDisplay.text = "Stopped Listening...";

        }
        Microphone.End(deviceName);
    }

    /// <summary>
    /// This event is fired while the user is talking. As the recognizer listens, it provides text of what it's heard so far.
 
    private void DictationRecognizer_DictationHypothesis(string text)
    {
        // 3.a: Set DictationDisplay text to be textSoFar and new hypothesized text
        // We don't want to append to textSoFar yet, because the hypothesis may have changed on the next event
        dictationDisplay.text = textSoFar.ToString() + " " + text + "...";
    }

    /// <summary>
   
    private void DictationRecognizer_DictationResult(string text, ConfidenceLevel confidence)
    {
        //Append textSoFar with latest text
        //textSoFar.Append(text + ". ");
           
        //Set DictationDisplay text to be textSoFar
        dictationDisplay.text = text + ". ";
   
    }


    /// This event is fired when the recognizer stops, whether from Stop() being called, a timeout occurring, or some other error.
    /// Typically, this will simply return "Complete". In this case, we check to see if the recognizer timed out.

    private void DictationRecognizer_DictationComplete(DictationCompletionCause cause)
    {
        // If Timeout occurs, the user has been silent for too long.
        // With dictation, the default timeout after a recognition is 20 seconds.
        // The default timeout with initial silence is 5 seconds.
        if (cause == DictationCompletionCause.TimeoutExceeded)
        {
            Microphone.End(deviceName);
            dictationDisplay.text = "Timed out, Press record to start listening again";
              
        }
    }

    /// This event is fired when an error occurs.
    private void DictationRecognizer_DictationError(string error, int hresult)
    {
        // 3.a: Set DictationDisplay text to be the error string
        dictationDisplay.text = error + "\nHRESULT: " + hresult;
    }

    /// The dictation recognizer may not turn off immediately, so this call blocks on
    /// the recognizer reporting that it has actually stopped.
    public IEnumerator WaitForDictationToStop()
    {
        while (dictationRecognizer != null && dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            yield return null;
        }
    }
}
