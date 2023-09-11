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
[Library("carson_mediaplayer_wall"), HammerEntity]
public partial class MediaPlayerWall : MediaPlayer
{
    public override void Spawn()
    {
        base.Spawn();
        Model = Cloud.Model("luke.tv_flatscreen");
        SetupPhysicsFromModel(MotionType);
    }
}