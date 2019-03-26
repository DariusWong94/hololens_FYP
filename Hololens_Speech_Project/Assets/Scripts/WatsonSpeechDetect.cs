using UnityEngine;
using UnityEngine.UI;

using System;
using System.Collections;
using System.Collections.Generic;

using IBM.Watson.DeveloperCloud.Logging;
using IBM.Watson.DeveloperCloud.Services.SpeechToText.v1;
using IBM.Watson.DeveloperCloud.Utilities;
using IBM.Watson.DeveloperCloud.DataTypes;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;


class AppCache
{
    public static string API { get; } = @"trnsl.1.1.20190108T061753Z.b053ec266ea463e9.a45153a1b639847d4b144a1c93d61cec8d7d18db";

    public static string UrlDetectSrcLanguage { get; } = @"https://translate.yandex.net/api/v1.5/tr.json/detect?key={0}&text={1}";
    public static string UrlGetAvailableLanguages { get; } = @"https://translate.yandex.net/api/v1.5/tr.json/getLangs?key={0}&ui={1}";
    public static string UrlTranslateLanguage { get; } = @"https://translate.yandex.net/api/v1.5/tr.json/translate?key={0}&text={1}&lang={2}";
}


public class WatsonSpeechDetect : MonoBehaviour {

    #region PLEASE SET THESE VARIABLES IN THE INSPECTOR
    [Space(10)]
    [Tooltip("The service URL (optional). This defaults to \"https://stream.watsonplatform.net/speech-to-text/api\"")]
    [SerializeField]
    private string _serviceUrl = "https://stream.watsonplatform.net/speech-to-text/api";
    [Tooltip("Text field to display the results of streaming.")]
    public Text ResultsField;

    [Tooltip("Text field to display text buffer")]
    public Text TextBuffer;

    [Tooltip("Text field to display number of Speakers.")]
    public Text speakerCount;

    [Header("CF Authentication")]
    [Tooltip("The authentication username.")]
    [SerializeField]
    public string _username = "55976d01-df06-4cfd-89d5-e069322be964";
    [Tooltip("The authentication password.")]
    [SerializeField]
    public string _password = "dbvWRaUcLcuV";

    [Header("IAM Authentication")]
    [Tooltip("The IAM apikey.")]
    [SerializeField]
    public string _iamApikey = "c03oWta-w6CJS73F8i_Bsx3Zwin6WtfI_QMSKuksLbSY";
    [Tooltip("The IAM url used to authenticate the apikey (optional). This defaults to \"https://iam.bluemix.net/identity/token\".")]
    [SerializeField]
    public string _iamUrl = "https://gateway-wdc.watsonplatform.net/speech-to-text/api";
    #endregion

    private int _recordingRoutine = 0;
    private static string _microphoneID = string.Empty;
    private AudioClip _recording = null;
    private int _recordingBufferSize = 1;
    private int _recordingHZ = 22050;

    private List<string> AvailLangs;

    private SpeechToText _service;

    void Start()
    {
        LogSystem.InstallDefaultReactors();
        
    }
    void Awake()
    {
        onEnglishClick();
        //int unused;
        //Microphone.GetDeviceCaps(_microphoneID, out unused, out _recordingHZ);

    }
    public void startOnClick()
    {
        Runnable.Run(CreateService());
        ResultsField.text = "Listening...";
        Debug.Log("Starting");
    }

    public void stopOnClick()
    {
        Active = false;
        StopRecording();
        ResultsField.text = "Stopped...";
        Debug.Log("Stopped");
    }

    public void onEnglishClick()
    {

        Globals.defaultLang = true;
        Globals.lang = "en";
        Debug.Log("English Selected");
        TextBuffer.text = Globals.originalText;

    }

    public void onChineseClick()
    {
        Globals.defaultLang = false;
        Globals.lang = "zh";
        Debug.Log("Chinese Selected");
        textTranslate();
    }


    private IEnumerator CreateService()
    {
        //  Create credential and instantiate service
        Credentials credentials = null;
        if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
        {
            //  Authenticate using username and password
            credentials = new Credentials(_username, _password, _serviceUrl);
        }
        else if (!string.IsNullOrEmpty(_iamApikey))
        {
            //  Authenticate using iamApikey
            TokenOptions tokenOptions = new TokenOptions()
            {
                IamApiKey = _iamApikey,
                IamUrl = _iamUrl
            };

            credentials = new Credentials(tokenOptions, _serviceUrl);

            //  Wait for tokendata
            while (!credentials.HasIamTokenData())
                yield return null;
        }
        else
        {
            throw new WatsonException("Please provide either username and password or IAM apikey to authenticate the service.");
        }

        _service = new SpeechToText(credentials);
        _service.StreamMultipart = true;

        Active = true;
        StartRecording();
    }

    public bool Active
    {
        get { return _service.IsListening; }
        set
        {
            if (value && !_service.IsListening)
            {
                _service.DetectSilence = true;
                _service.EnableWordConfidence = true;
                _service.EnableTimestamps = true;
                _service.SilenceThreshold = 0.05f;
                _service.MaxAlternatives = 0;
                _service.EnableInterimResults = true;
                _service.OnError = OnError;
                _service.InactivityTimeout = -1;
                _service.ProfanityFilter = false;
                _service.SmartFormatting = true;
                _service.SpeakerLabels = true;
                _service.WordAlternativesThreshold = null;
                _service.StartListening(OnRecognize, OnRecognizeSpeaker);
            }
            else if (!value && _service.IsListening)
            {
                _service.StopListening();
            }
        }
    }

    private void StartRecording()
    {
        if (_recordingRoutine == 0)
        {
            UnityObjectUtil.StartDestroyQueue();
            _recordingRoutine = Runnable.Run(RecordingHandler());
        }
    }

    private void StopRecording()
    {
        if (_recordingRoutine != 0)
        {
            Microphone.End(_microphoneID);
            Runnable.Stop(_recordingRoutine);
            _recordingRoutine = 0;
        }
    }

    private void OnError(string error)
    {
        Active = false;

        //Log.Debug("OnError()", "Error! {0}", error);
    }

    private IEnumerator RecordingHandler()
    {
        //Log.Debug("RecordingHandler()", "devices: {0}", Microphone.devices);
        //Log.Debug("RecordingHandler()", "Recording Hertz: {0}", _recordingHZ);

        _recording = Microphone.Start(_microphoneID, true, _recordingBufferSize, _recordingHZ);
        yield return null;      // let _recordingRoutine get set..

        if (_recording == null)
        {
            StopRecording();
            yield break;
        }

        bool bFirstBlock = true;
        int midPoint = _recording.samples / 2;
        float[] samples = null;

        while (_recordingRoutine != 0 && _recording != null)
        {
            int writePos = Microphone.GetPosition(_microphoneID);
            //Log.Debug("RecordingHandler()", "writePos: {0}", writePos);
            //Log.Debug("RecordingHandler()", "_recording.samples: {0}", _recording.samples);

            if (writePos > _recording.samples || !Microphone.IsRecording(_microphoneID))
            {
                Log.Error("RecordingHandler()", "Microphone disconnected.");
                StopRecording();
                yield break;
            }

            if ((bFirstBlock && writePos >= midPoint)
              || (!bFirstBlock && writePos < midPoint))
            {
                // front block is recorded, make a RecordClip and pass it onto our callback.
                samples = new float[midPoint];
                _recording.GetData(samples, bFirstBlock ? 0 : midPoint);

                AudioData record = new AudioData();
                record.MaxLevel = Mathf.Max(Mathf.Abs(Mathf.Min(samples)), Mathf.Max(samples));
                record.Clip = AudioClip.Create("Recording", midPoint, _recording.channels, _recordingHZ, false);
                record.Clip.SetData(samples, 0);

                _service.OnListen(record);

                bFirstBlock = !bFirstBlock;
            }
            else
            {
                // calculate the number of samples remaining until we ready for a block of audio, 
                // and wait that amount of time it will take to record.
                int remaining = bFirstBlock ? (midPoint - writePos) : (_recording.samples - writePos);
                float timeRemaining = (float)remaining / (float)_recordingHZ;

                yield return new WaitForSeconds(timeRemaining);
            }
        }
        yield break;
    }

    //Translate
    
    private void textTranslate()
    {
        var strUrl = string.Format(AppCache.UrlTranslateLanguage, AppCache.API, Globals.originalText, Globals.lang);
        Log.Debug("URL STRING ", strUrl);
        WWW request = new WWW(strUrl);
        StartCoroutine(OnResponse(request));
    }
    private IEnumerator OnResponse(WWW req) {
        yield return req;
        
        var dict = JsonConvert.DeserializeObject<JObject>(req.text);

        var statusCode = dict["code"].ToString();

        if (statusCode.Equals("200"))
        {
            var translatedText = dict["text"][0].ToString();
            Globals.translatedText = translatedText;
            TextBuffer.text = Globals.translatedText;
        }
        else
        {
            var statusMessage = dict["message"].ToString();
            Log.Debug("Error ","{0}: | {1}", statusCode, statusMessage);
        }
    }
    
    public static class Globals
    {
        public static int bufferCount = 0;
        public static string Speaker = "0";

        public static string[] textArr;
        public static string SpeakerText = "";

        public static string originalText = "";

        public static bool defaultLang = true;
        public static string translatedText;
        public static string lang = "en";

        public static long sCount = 0;

        public static string Text = "";

    
        public static double[][] fullArr = new double[][] { };
    }

    private void OnRecognize(SpeechRecognitionEvent result, Dictionary<string, object> customData)
    {

        if (result != null && result.results.Length > 0)
        {
            foreach (var res in result.results)
            {
                string text = "";

                foreach (var alt in res.alternatives)
                {
                    text = string.Format("{0} ", alt.transcript);
                    Globals.Text = text;
                    ResultsField.text = "Detecting Speakers...";
                }

                if (res.final == true) {
                    Log.Debug("Final Text----------------", text);
                    Globals.textArr = text.Split(' ');
                    
                    //for (var i = 0; i < Globals.textArr.Length; i++)
                    //{
                    //    Log.Debug("Final Text", i + " " + Globals.textArr[i]);
                    //}

                }
                if (res.keywords_result != null && res.keywords_result.keyword != null)
                {
                    foreach (var keyword in res.keywords_result.keyword)
                    {
                        string start = string.Format("{0} ", keyword.start_time);
                        string end = string.Format("{0} ", keyword.end_time);
                        Log.Debug("Keyword Start time: ", start);
                        Log.Debug("Keyword End time: ", end);
                        //Log.Debug("ExampleStreaming.OnRecognize()", "keyword: {0}, confidence: {1}, start time: {2}, end time: {3}", keyword.normalized_text, keyword.confidence, keyword.start_time, keyword.end_time);
                    }
                }

                if (res.word_alternatives != null)
                {
                    foreach (var wordAlternative in res.word_alternatives)
                    {
                        Log.Debug("ExampleStreaming.OnRecognize()", "Word alternatives found. Start time: {0} | EndTime: {1}", wordAlternative.start_time, wordAlternative.end_time);
                        foreach (var alternative in wordAlternative.alternatives)
                        {
                            Log.Debug("ExampleStreaming.OnRecognize()", "\t word: {0} | confidence: {1}", alternative.word, alternative.confidence);
                        }

                    }
                }
            }
        }
    }

    private void OnRecognizeSpeaker(SpeakerRecognitionEvent result, Dictionary<string, object> customData)
    {
        
        if (result != null)
        {
            var i = 0;
            var resLen = result.speaker_labels.Length;
            foreach (SpeakerLabelsResult labelResult in result.speaker_labels)
            {
                
                Log.Debug("ExampleStreaming.OnRecognize()", string.Format("speaker result: {0} | confidence: {3} | from: {1} | to: {2} | final: {4}", labelResult.speaker, labelResult.from, labelResult.to, labelResult.confidence, labelResult.final));
                //string text = string.Format("speaker result: {0} | confidence: {3} | from: {1} | to: {2}", labelResult.speaker, labelResult.from, labelResult.to, labelResult.confidence);
                
                // Counting Number of Speakers
                if (labelResult.speaker == 0 && Globals.sCount == 0)
                {
                    Globals.sCount = 1;
                    speakerCount.text = "Speaker Count: 1";
                }
                else if (labelResult.speaker == Globals.sCount) {
                    Globals.sCount++;
                    speakerCount.text = "Speaker Count: " + Globals.sCount.ToString();
                }

                //Globals.Speaker = labelResult.speaker.ToString();
              

                if (i < Globals.textArr.Length)
                {
                    ResultsField.text = "Speaker " + labelResult.speaker.ToString() + ": " + "is speaking..." + "\n";
                    Debug.Log(Globals.textArr[i]);  
                    // Refresh text buffer 
                    if (Globals.bufferCount == 0 || Globals.bufferCount > 30)
                    {
                        Globals.bufferCount = 0;
                        Globals.originalText = "";
                        TextBuffer.text = "";
                    }

                    // If same speaker, concat speaker text .. if last iteration, print 
                    if (Globals.Speaker == labelResult.speaker.ToString())
                    {
                        Globals.SpeakerText = Globals.SpeakerText + " " + Globals.textArr[i];
                        Debug.Log("Same Speaker -----------------");
                        if (i == resLen - 1 && Globals.textArr[i] != null)
                        {
                            Debug.Log("Current i +++++++++++++++++" + i);
                            updateBuffer();
                            Globals.SpeakerText = "";
                        }
                        continue;
                    }
                    else
                    {
                        updateBuffer();
                        Globals.Speaker = labelResult.speaker.ToString();
                        Globals.SpeakerText = Globals.textArr[i];
                    }
                }
                i++;
            }
            Array.Clear(Globals.textArr, 0, Globals.textArr.Length);
        }
        //ConcatSubs();
    }

    private void updateBuffer() {
        Log.Debug("Current Printed Word", Globals.SpeakerText);

        if (Globals.SpeakerText != "" || Globals.SpeakerText != null)
        {
            Globals.originalText += "Speaker " + Globals.Speaker + ": " + Globals.SpeakerText + "\n";
            
            if (Globals.defaultLang == true)
            {
               TextBuffer.text = Globals.originalText;
            }
            else {
                textTranslate();
            }
            
            Globals.bufferCount++;
        }
        ResultsField.text = "Detecting Speakers...";
    }
   
}
