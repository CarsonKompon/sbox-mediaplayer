using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Editor;
using MediaHelpers;

namespace CarsonK;

/// <summary>
/// A placeable TV that you can queue media on
/// </summary>
[EditorModel("models/items/electronics/tv_medium_onstand.vmdl")]
[Library("carson_mediaplayer"), HammerEntity]
public partial class MediaPlayer : ModelEntity, IUse
{

    public virtual bool IsUsable( Entity user ) => true;
    [Net] public List<MediaVideo> Queue {get; set;} = new();
    [Net] public MediaVideo CurrentlyPlaying { get; set; }
    [Net] public float CurrentLength { get; set; } = 5;
    [Net] public RealTimeSince VideoStart { get; set; } = 0;
    [Net] public bool IsPlaying { get; set; } = false;
    private bool LoadingVideo { get; set; } = false;

    /// <summary>
    /// Physics motion type.
    /// </summary>
    [Property( Title = "Physics Type" )]
    protected PhysicsMotionType MotionType {get; set;} = PhysicsMotionType.Dynamic;

    public VideoPlayer Video { get; set; }
    public Material ScreenMaterial { get; set; }

    public MediaPlayer()
    {
        ScreenMaterial = Material.Load("materials/mediaplayer_screen.vmat").CreateCopy();
    }

    public override void Spawn()
    {
        base.Spawn();
        SetModel("models/items/electronics/tv_medium_onstand.vmdl");
        SetupPhysicsFromModel(MotionType);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Video?.Dispose();
    }

    [GameEvent.Tick.Server]
    public void ServerTick()
    {
        if(IsPlaying && Video.PlaybackTime > 0 && Video.PlaybackTime > Video.Duration)
        {
            SkipCurrentAll();
        }
        if(!LoadingVideo && !IsPlaying && Queue.Count() > 0 && Video == null)
        {
            CurrentlyPlaying = Queue[0];
            Queue.RemoveAt(0);
            PlayVideoForAll(CurrentlyPlaying.Url);
        }
    }

    [GameEvent.Client.Frame]
    public void OnFrame()
    {
        if(Video == null) return;
        Video?.Present();
        ScreenMaterial.Set("Color", Video.Texture);
        SetMaterialOverride(ScreenMaterial, "screen");
    }

    public virtual bool OnUse(Entity user)
    {
        Game.AssertServer();

        MediaBrowser.Open(To.Single(user), NetworkIdent);
        
        return false;
    }

    [ConCmd.Server]
    public static async void QueueMedia(int networkIdent, string url)
    {
        var entity = Entity.FindByIndex(networkIdent);
        if(entity is not MediaPlayer mediaPlayer)
        {
            Log.Error("📺: Tried to queue media on a TV that doesn't exist!");
            return;
        }

        MediaVideo video = await MediaVideo.CreateFromUrl(url);

        // Queue the media
        mediaPlayer.Queue.Add(video);
    }

    [ConCmd.Server]
    public static void RemoveMedia(int networkIdent, string url)
    {
        var entity = Entity.FindByIndex(networkIdent);
        if(entity is not MediaPlayer mediaPlayer)
        {
            Log.Error("📺: Tried to remove media from a TV that doesn't exist!");
            return;
        }

        var where = mediaPlayer.Queue.Where(x => x.Url == url);

        if(mediaPlayer.CurrentlyPlaying.Url == url)
        {
            mediaPlayer.SkipCurrentAll();
        }
        else if(where.Count() > 0)
        {
            mediaPlayer.Queue.Remove(where.First());
        }
    }

    public void SkipCurrentAll()
    {
        
        SkipCurrent();
        SkipCurrentRpc();
    }

    public void SkipCurrent()
    {
        
        Video.Dispose();
        Video = null;
        IsPlaying = false;
        LoadingVideo = false;
        CurrentlyPlaying = null;
    }

    [ClientRpc]
    public void SkipCurrentRpc()
    {
        SkipCurrent();
    }

    public async void PlayVideoForAll(string url)
    {
        LoadingVideo = true;
        VideoStart = 0f;

        if(MediaHelper.IsYoutubeUrl(url))
        {
            YoutubePlayerResponse youtube = await MediaHelper.GetYoutubePlayerResponseFromUrl(url);
            CurrentLength = youtube.Duration.Seconds;
            string streamUrl = youtube.GetStreamUrl();
            PlayVideoRpc(streamUrl);
            return;
        }

        
        PlayVideoRpc(url);
        IsPlaying = true;
    }


    public async void PlayVideo(string url)
    {
        Video = new VideoPlayer();
        Video.OnAudioReady = () => Video.PlayAudio(this);

        if(MediaHelper.IsYoutubeUrl(url))
        {
            YoutubePlayerResponse youtube = await MediaHelper.GetYoutubePlayerResponseFromUrl(url);
            string streamUrl = youtube.GetStreamUrl();
            Log.Info(streamUrl);
            Video.Play(streamUrl);
        }
        else
        {
            Video.Play(url);
        }

        CurrentLength = Video.Duration;
    }

    [ClientRpc]
    public void PlayVideoRpc(string url)
    {
        PlayVideo(url);
    }
}