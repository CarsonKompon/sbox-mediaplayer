using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Editor;
using System.Linq;
using MediaHelpers;

namespace CarsonK;

/// <summary>
/// A placeable TV that you can queue media on
/// </summary>
[EditorModel("models/mediaplayer_dev.vmdl")]
[Library]
public partial class MediaPlayer : ModelEntity, IUse
{

    public virtual bool IsUsable( Entity user ) => true;
    [Net] public List<string> Queue {get; set;} = new();
    [Net] public string CurrentlyPlaying { get; set; } = "";
    [Net] public float CurrentLength { get; set; } = 5;
    [Net] public RealTimeSince VideoStart { get; set; } = 0;
    [Net] public bool IsPlaying { get; set; } = false;
    private bool LoadingVideo { get; set; } = false;

    public VideoPlayer Video { get; set; }
    public Material ScreenMaterial { get; set; }

    public MediaPlayer()
    {
        ScreenMaterial = Material.Load("materials/mediaplayer_screen.vmat").CreateCopy();
    }

    public override void Spawn()
    {
        base.Spawn();
        SetModel("models/mediaplayer_dev.vmdl");
        SetupPhysicsFromModel(PhysicsMotionType.Dynamic);
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
            Video.Dispose();
            Video = null;
            IsPlaying = false;
        }
        if(!LoadingVideo && !IsPlaying && Queue.Count() > 0 && VideoStart >= CurrentLength)
        {
            CurrentlyPlaying = Queue[0];
            PlayVideoForAll(CurrentlyPlaying);
            Queue.RemoveAt(0);
            VideoStart = 0f;
            LoadingVideo = true;
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
    public static void QueueMedia(int networkIdent, string url)
    {
        var entity = Entity.FindByIndex(networkIdent);
        if(entity is not MediaPlayer mediaPlayer)
        {
            Log.Error("📺: Tried to queue media on a TV that doesn't exist!");
            return;
        }

        // Queue the media
        mediaPlayer.Queue.Add(url);
    }

    public void PlayVideoForAll(string url)
    {
        PlayVideo(CurrentlyPlaying);
        PlayVideoRpc(CurrentlyPlaying);
        IsPlaying = true;
    }

    public async void PlayVideo(string url)
    {
        Video = new VideoPlayer();
        Video.OnAudioReady = () => Video.PlayAudio(this);
        Video.OnLoaded = () => {
            if(Game.IsServer)
            {
                LoadingVideo = false;
                CurrentLength = Video.Duration;
            }
        };

        if(MediaHelper.IsYoutubeUrl(url))
        {
            var streamUrl = await MediaHelper.GetUrlFromYoutubeUrl(url);
            Video.Play(streamUrl);
        }
        else if(MediaHelper.IsKickUrl(url))
        {
            var streamUrl = await MediaHelper.GetUrlFromKickUrl(url);
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