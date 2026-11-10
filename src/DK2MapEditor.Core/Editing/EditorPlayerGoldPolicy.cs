namespace DK2MapEditor.Core.Editing;

public sealed record EditorPlayerGoldCapacity(uint Player,int? Capacity,int TreasuryTiles,bool HeartPresent);

/// <summary>One validated general-player change, independent of whether its
/// informational notice was acknowledged or merely closed.</summary>
public sealed class EditorPlayerSettingsPlan
{
    internal EditorPlayerSettingsPlan(NativeMapDocument document,PlayerSettingsChange change,EditorPlayerGoldCapacity capacity,long revision,byte[] fingerprint,
        NativeMapDocument capacitySource,long capacityRevision,byte[] capacityFingerprint)
    { Document=document;Change=change;Capacity=capacity;Revision=revision;Fingerprint=fingerprint;CapacitySource=capacitySource;CapacityRevision=capacityRevision;CapacityFingerprint=capacityFingerprint; }
    internal NativeMapDocument Document {get;}
    internal long Revision {get;}
    internal byte[] Fingerprint {get;}
    internal NativeMapDocument CapacitySource {get;}
    internal long CapacityRevision {get;}
    internal byte[] CapacityFingerprint {get;}
    public PlayerSettingsChange Change {get;}
    public EditorPlayerGoldCapacity Capacity {get;}
    public bool NeedsGoldNotice=>Change.ID is >=3 and <=7 && Capacity.Capacity is { } limit && Change.Gold>limit;
    public bool IsApplied {get;internal set;}
    public bool IsCurrent=>Document.IsEditorPlayerSettingsCurrent(this);
}

/// <summary>Source informational OK-only request. Dispose/close does not veto
/// saving the plan; only stale caller/data invalidates that independent plan.</summary>
public sealed class EditorPlayerGoldNotice : IDisposable
{
    private readonly Func<bool> callerCurrent;
    internal EditorPlayerGoldNotice(EditorPlayerGoldPolicy policy,EditorPlayerSettingsPlan plan,Func<bool> callerCurrent)
    {Policy=policy;Plan=plan;this.callerCurrent=callerCurrent;}
    internal EditorPlayerGoldPolicy Policy {get;}
    internal bool TargetCurrent=>callerCurrent()&&Plan.IsCurrent;
    public EditorPlayerSettingsPlan Plan {get;}
    public bool RequiresNotice=>!Policy.Suppressed;
    public bool IsCurrent=>Policy.IsCurrent(this);
    public bool IsClosed {get;internal set;}
    public bool IsAcknowledged {get;private set;}
    public bool Acknowledge(bool remember=false)
    {
        if(!IsCurrent)throw new InvalidOperationException("The player-gold notice is no longer current.");
        Policy.Remember(this,remember);IsAcknowledged=true;Policy.Release(this);return true;
    }
    public void Dispose()=>Policy.Release(this);
}

public sealed class EditorPlayerGoldPolicy : IDisposable
{
    public const uint NoticeBit=0x00000040;
    private readonly EditorRequestPreferences preferences;
    private EditorPlayerGoldNotice? active;
    private bool disposed;
    public EditorPlayerGoldPolicy(uint initialFlags=0):this(new EditorRequestPreferences(initialFlags)) { }
    public EditorPlayerGoldPolicy(EditorRequestPreferences preferences)=>this.preferences=preferences??throw new ArgumentNullException(nameof(preferences));
    public uint Flags=>preferences.Flags;
    public bool Suppressed=>(Flags&NoticeBit)!=0;
    public EditorPlayerGoldNotice? Begin(EditorPlayerSettingsPlan plan,Func<bool> callerCurrent)
    {
        ObjectDisposedException.ThrowIf(disposed,this);ArgumentNullException.ThrowIfNull(plan);ArgumentNullException.ThrowIfNull(callerCurrent);
        if(!plan.IsCurrent)throw new InvalidOperationException("Refresh the player settings before showing the notice.");
        if(!plan.NeedsGoldNotice)return null;
        if(active is not null)throw new InvalidOperationException("Finish the current player-gold notice.");
        var notice=new EditorPlayerGoldNotice(this,plan,callerCurrent);preferences.Acquire(notice);active=notice;
        if(!IsCurrent(notice)){Release(notice);throw new InvalidOperationException("The notice caller changed during preparation.");}
        return notice;
    }
    private bool Owns(EditorPlayerGoldNotice notice)=>!disposed&&!notice.IsClosed&&ReferenceEquals(notice.Policy,this)&&ReferenceEquals(active,notice);
    internal bool IsCurrent(EditorPlayerGoldNotice notice)
    {
        if(!Owns(notice))return false;
        try{return notice.TargetCurrent&&Owns(notice);}
        catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.IOException or System.IO.InvalidDataException or FormatException or OverflowException){return false;}
    }
    internal void Remember(EditorPlayerGoldNotice notice,bool remember)
    {if(notice.RequiresNotice)preferences.Remember(notice,NoticeBit,remember);}
    internal void Release(EditorPlayerGoldNotice notice)
    {if(!ReferenceEquals(notice.Policy,this))return;notice.IsClosed=true;preferences.Release(notice);if(ReferenceEquals(active,notice))active=null;}
    public void ResetSuppression(){ObjectDisposedException.ThrowIf(disposed,this);active?.Dispose();preferences.Reset(NoticeBit);}
    public void Dispose(){if(disposed)return;active?.Dispose();disposed=true;}
}

public sealed partial class NativeMapDocument
{
    public EditorPlayerGoldCapacity CalculateEditorPlayerGoldCapacity(uint player)
    {
        if(player is <1 or >7)throw new ArgumentOutOfRangeException(nameof(player));
        var records=Records();var variables=MapVariableCells().ToDictionary(cell=>cell.Definition.ID);
        int? Variable(uint id)
        {
            if(!IsLegacyProject&&MapVariableFile is { } file&&file.UInt32(4)!=4
                ||IsLegacyProject&&records.Any(record=>record.File.Signature==LegacyInfo&&record.Category=="Project.Opaque"))return null;
            var cell=variables[id];return unchecked((int)(cell.Bits??cell.Definition.DefaultBits));
        }
        var tiles=0;var known=true;
        for(var y=0;y<Height;y++)for(var x=0;x<Width;x++)
        {
            var tile=GetTile(x,y);if(tile.Owner!=player)continue;
            if(!statisticsTileRules.Value.TryGetValue((tile.Terrain,tile.Variant,tile.Owner),out var rule)){known=false;continue;}
            if(rule.Group==1&&tile.Terrain==10)tiles++;
        }
        var heart=HeartPosition((byte)player)!=new TilePoint(0,0);
        var treasury=Variable(0x18);var heartGold=Variable(0xB2);
        int? capacity=!known||tiles!=0&&treasury is null||heart&&heartGold is null?null:
            unchecked((tiles==0?0:tiles*treasury!.Value)+(heart?16*heartGold!.Value:0));
        return new(player,capacity,tiles,heart);
    }
    public EditorPlayerSettingsPlan PrepareEditorPlayerSettings(PlayerSettingsChange change,NativeMapDocument? capacitySource=null)
    {
        ArgumentNullException.ThrowIfNull(change);
        capacitySource??=this;
        if(IsSaveLocked||capacitySource.IsSaveLocked)throw new InvalidOperationException("Finish the current edit before preparing player settings.");
        var revision=EditRevision;var fingerprint=RegionRemovalFingerprint();
        var capacityRevision=capacitySource.EditRevision;
        var capacityFingerprint=ReferenceEquals(capacitySource,this)?fingerprint:capacitySource.RegionRemovalFingerprint();
        _=ValidatePlayerSettingsChanges([change]);
        var capacity=change.ID is >=3 and <=7?capacitySource.CalculateEditorPlayerGoldCapacity(change.ID):new EditorPlayerGoldCapacity(change.ID,0,0,false);
        if(EditRevision!=revision||!fingerprint.AsSpan().SequenceEqual(RegionRemovalFingerprint())
            ||capacitySource.EditRevision!=capacityRevision||!capacityFingerprint.AsSpan().SequenceEqual(capacitySource.RegionRemovalFingerprint()))throw new InvalidOperationException("The map changed while preparing player settings.");
        return new(this,change with{},capacity,revision,fingerprint,capacitySource,capacityRevision,capacityFingerprint);
    }
    internal bool IsEditorPlayerSettingsCurrent(EditorPlayerSettingsPlan plan)=>!plan.IsApplied&&ReferenceEquals(plan.Document,this)&&!IsSaveLocked&&EditRevision==plan.Revision
        &&plan.Fingerprint.AsSpan().SequenceEqual(RegionRemovalFingerprint())&&!plan.CapacitySource.IsSaveLocked
        &&plan.CapacitySource.EditRevision==plan.CapacityRevision&&plan.CapacityFingerprint.AsSpan().SequenceEqual(plan.CapacitySource.RegionRemovalFingerprint());
    public void ApplyEditorPlayerSettings(EditorPlayerSettingsPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if(!IsEditorPlayerSettingsCurrent(plan))throw new InvalidOperationException("Refresh player settings after map changes.");
        var revision=EditRevision;
        try{SetPlayerSettings([plan.Change]);plan.IsApplied=true;}
        catch{plan.IsApplied=EditRevision!=revision;throw;}
    }
}
