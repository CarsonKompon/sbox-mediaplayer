using System;
using System.Threading.Tasks;
using Sandbox;
using MediaHelpers;

namespace CarsonK;

public partial class MediaVideo : BaseNetworkable
{
    [Net] public string Url {get; set;}
    [Net] public string Title {get; set;}
    [Net] public string Author {get; set;}
    [Net] public string Description {get; set;} = "";
    public Texture Thumbnail
    {
        get
        {
            if(_thumbnail == null) _thumbnail = Texture.Load(ThumbnailUrl);
            return _thumbnail;
        }
        set
        {
            _thumbnail = value;
        }
    }
    [Net] public string ThumbnailUrl {get; set;} = "";
    private Texture _thumbnail = null;

    public MediaVideo()
    {
    }

    public async void LoadFromYoutube(string url)
    {
        var youtubePlayer = await MediaHelper.GetYoutubePlayerResponseFromUrl(url);
        Title = youtubePlayer.Title;
        Author = youtubePlayer.Author;
        Description = youtubePlayer.Description;
        if(youtubePlayer.Thumbnails.Count > 0) ThumbnailUrl = youtubePlayer.Thumbnails[0].Url;
    }

    public static async Task<MediaVideo> CreateFromUrl(string url)
    {
        MediaVideo video = new MediaVideo();
        video.Url = url;
        video.Title = url;

        if(MediaHelper.IsYoutubeUrl(url))
        {
            video.LoadFromYoutube(url);
        }

        return video;
    }
}