using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Agora.Rtc;
using TMPro;
using System;


#if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
using UnityEngine.Android;
#endif

public class VideoCallHandler : MonoBehaviour
{
#if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
private ArrayList permissionList = new ArrayList() { Permission.Camera, Permission.Microphone };
#endif

    // Fill in your app ID.
    private string _appID = "254940b7abbb4352b36edfda47f726af";
    // Fill in your channel name.
    private string _channelName = "TestChannel";
    // Fill in the temporary token you obtained from Agora Console.
    private string _token = "";
    // A variable to save the remote user uid.
    private uint remoteUid;
    private uint personalid;

    public bool sharingScreen = false;

    internal VideoSurface LocalView;
    internal VideoSurface RemoteView;
    internal IRtcEngineEx RtcEngine;
    //internal IRtcEngine RtcEngine;

    public VideoSurface clientView;
    //public VideoSurface remoteView;

    public VideoSurface remoteViewPrefab;

    public VideoSurface localScreenShare;
    public VideoSurface RemoteScreenShare;


    void Start()
    {
        SetupVideoSDKEngine();
        InitEventHandler();

        LocalView = clientView;
        //RemoteView = remoteViewPrefab;

    }

    // Update is called once per frame
    void Update()
    {
        CheckPermissions();
    }
    private void SetupVideoSDKEngine()
    {
        // Create an instance of the video SDK.
        RtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngineEx();
        // Specify the context configuration to initialize the created instance.
        RtcEngineContext context = new RtcEngineContext(_appID, 0,
        CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_COMMUNICATION,
        AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT, AREA_CODE.AREA_CODE_GLOB, null);
        // Initialize the instance.
        RtcEngine.Initialize(context);
    }
   
    public void Join()
    {
        //LocalView = Instantiate(prefab);
        //UserEntered();
        // Enable the video module.
        RtcEngine.EnableVideo();
        // Set the user role as broadcaster.
        RtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        // Set the local video view.
        LocalView.SetForUser(0, "", VIDEO_SOURCE_TYPE.VIDEO_SOURCE_CAMERA);
        // Start rendering local video.
        LocalView.SetEnable(true);
        // Join a channel.
        RtcEngine.JoinChannel(_token, _channelName);
    }



    public void Leave()
    {
        // Leaves the channel.
        RtcEngine.LeaveChannel();
        // Disable the video modules.
        RtcEngine.DisableVideo();
        // Stops rendering the remote video.
        RemoteView.SetEnable(false);
        // Stops rendering the local video.
        LocalView.SetEnable(false);
    }

    private void CheckPermissions()
    {
#if (UNITY_2018_3_OR_NEWER && UNITY_ANDROID)
    foreach (string permission in permissionList)
    {
        if (!Permission.HasUserAuthorizedPermission(permission))
        {
            Permission.RequestUserPermission(permission);
        }
    }
#endif
    }
    void OnApplicationQuit()
    {
        if (RtcEngine != null)
        {
            Leave();
            RtcEngine.Dispose();
            RtcEngine = null;

        }
    }
    private void InitEventHandler()
    {
        // Creates a UserEventHandler instance.
        UserEventHandler handler = new UserEventHandler(this);
        RtcEngine.InitEventHandler(handler);
    }

    public VideoSurface UserEntered()
    {
       VideoSurface s = Instantiate(remoteViewPrefab,remoteViewPrefab.transform);
        Debug.Log("Hahahha");

        return s;
    }

    internal class UserEventHandler : IRtcEngineEventHandler
    {
        private readonly VideoCallHandler _videoSample;

        internal UserEventHandler(VideoCallHandler videoSample)
        {
            _videoSample = videoSample;
        }
        // This callback is triggered when the local user joins the channel.
        public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed )
        {
            _videoSample.personalid = connection.localUid;
            Debug.Log("You joined channel: " + connection.channelId + connection.localUid);
        }

        public override void OnLeaveChannel(RtcConnection connection, RtcStats stats)
        {
            Debug.Log("You left channel: " + connection.channelId);
            
            Destroy(_videoSample.RemoteView.gameObject);
        }

        public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
        {
            _videoSample.RemoteView = _videoSample.UserEntered();
            _videoSample.RemoteView.SetForUser(uid, connection.channelId, VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE);
            // Save the remote user ID in a variable.
            _videoSample.remoteUid = uid;
            // Destroy(_videoSample.RemoteView);
            Debug.Log(uid);
            _videoSample.RemoteView.SetEnable(true);

        }
        // This callback is triggered when a remote user leaves the channel or drops offline.
        public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
        {
            Destroy(_videoSample.RemoteView.gameObject);

        }


      

    }

    #region Share Screen
    private void muteRemoteAudio(bool value)
    {
        // Pass the uid of the remote user you want to mute.
        RtcEngine.MuteRemoteAudioStream(System.Convert.ToUInt32(remoteUid), value);
    }

    private void updateChannelPublishOptions(bool publishMediaPlayer)
    {
        ChannelMediaOptions channelOptions = new ChannelMediaOptions();
        channelOptions.publishScreenTrack.SetValue(publishMediaPlayer);
        //channelOptions.publishAudioTrack.SetValue(true);
        channelOptions.publishScreenCaptureAudio.SetValue(publishMediaPlayer);
        channelOptions.publishSecondaryScreenTrack.SetValue(publishMediaPlayer);
        channelOptions.publishCameraTrack.SetValue(!publishMediaPlayer);
        channelOptions.clientRoleType.SetValue(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        RtcEngine.UpdateChannelMediaOptions(channelOptions);

        RtcEngine.JoinChannelEx(_token, new RtcConnection(_channelName, personalid),channelOptions);
    }

    private void setupLocalVideo(bool isScreenSharing)
    {
        if (isScreenSharing)
        {
            // Render the screen sharing track on the local view.
            LocalView.SetForUser(0, "", VIDEO_SOURCE_TYPE.VIDEO_SOURCE_SCREEN_PRIMARY);

        }
        else
        {
            LocalView.SetForUser(0, "", VIDEO_SOURCE_TYPE.VIDEO_SOURCE_CAMERA_PRIMARY);

        }

    }

    public void shareScreen()
    {
        if (!sharingScreen)
        {
            // The target size of the screen or window thumbnail (the width and height are in pixels).
            SIZE t = new SIZE(360, 240);
            // The target size of the icon corresponding to the application program (the width and height are in pixels)
            SIZE s = new SIZE(360, 240);
            // Get a list of shareable screens and windows
            var info = RtcEngine.GetScreenCaptureSources(t, s, true);
            Debug.Log(info.Length);
            // Get the first source id to share the whole screen.
            long dispId = info[1].sourceId;

            Debug.Log(dispId);


            // To share a part of the screen, specify the screen width and size using the Rectangle class.
            RtcEngine.StartScreenCaptureByWindowId((long)0, default (Rectangle),
                    default(ScreenCaptureParameters));
            // Publish the screen track and unpublish the local video track.
            updateChannelPublishOptions(true);
            // Display the screen track in the local view.
            setupLocalVideo(true);
            // Change the screen sharing button text.
            // Update the screen sharing state.
            sharingScreen = true;
        }
        else
        {
            // Stop sharing.
            RtcEngine.StopScreenCapture();
            // Publish the local video track when you stop sharing your screen.
            updateChannelPublishOptions(false);
            // Display the local video in the local view.
            setupLocalVideo(false);
            // Update the screen sharing state.
            sharingScreen = false;
            // Change to the default text of the button when you stop sharing your screen.
        }
    }


    #endregion


}
