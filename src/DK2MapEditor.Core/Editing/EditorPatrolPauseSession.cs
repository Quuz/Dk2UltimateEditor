using System.Globalization;

namespace DK2MapEditor.Core.Editing;

public enum EditorPatrolPauseRange { Valid, BelowMinimum, AboveMaximum }

/// <summary>Source numeric pause request: independent literal text, initial
/// source LONG value, range checked on OK, Cancel without map/list changes.</summary>
public sealed class EditorPatrolPauseSession : IDisposable
{
    private readonly EditorPatrolManagerSession manager;
    private readonly EditorPatrolManagerRoute route;
    private readonly int occurrence;
    private bool closed,applying;
    internal EditorPatrolPauseSession(EditorPatrolManagerSession manager,EditorPatrolManagerRoute route,int occurrence)
    {
        this.manager=manager;this.route=route;this.occurrence=occurrence;
        var point=manager.GuardPause(route,occurrence);
        InitialValue=unchecked((int)point.WaitDelay);Text=InitialValue.ToString(CultureInfo.InvariantCulture);
    }
    public int InitialValue {get;}
    public string Text {get;private set;}
    public bool IsClosed=>closed;
    public bool IsApplying=>applying;
    public bool IsAccepted {get;private set;}
    public bool IsCurrent
    {
        get
        {
            if(closed||applying)return false;
            try{manager.GuardPause(route,occurrence);return true;}
            catch(Exception error)when(error is ArgumentException or InvalidOperationException or System.IO.InvalidDataException or FormatException or OverflowException){return false;}
        }
    }
    public int Value=>ParseSourceNumber(Text);
    public EditorPatrolPauseRange Range=>Value<0?EditorPatrolPauseRange.BelowMinimum:Value>250?EditorPatrolPauseRange.AboveMaximum:EditorPatrolPauseRange.Valid;
    private void Guard(){if(!IsCurrent)throw new InvalidOperationException("The pause request is no longer current.");}
    public void SetText(string value){ArgumentNullException.ThrowIfNull(value);Guard();Text=value;}
    /// <summary>Documented GetDlgItemInt signed decimal-prefix contract:
    /// leading whitespace, optional sign, stop at first nondecimal character,
    /// failed/overflow conversion returns0. No range clamp or text rewrite.</summary>
    public static int ParseSourceNumber(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var start=0;while(start<value.Length&&value[start] is ' ' or '\t' or '\r' or '\n' or '\v' or '\f')start++;
        var end=start;if(end<value.Length&&value[end] is '+' or '-')end++;
        var digits=end;while(end<value.Length&&value[end] is >= '0' and <= '9')end++;
        if(end==digits)return 0;
        return int.TryParse(value.AsSpan(start,end-start),NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out var number)?number:0;
    }
    public EditorPatrolManagerRoute Accept()
    {
        Guard();var value=Value;
        if(value is <0 or >250)throw new ArgumentOutOfRangeException(nameof(Text),"Original pause values must be0–250.");
        var revision=manager.Document.EditRevision;applying=true;
        try{var result=manager.SetPause(route,occurrence,value);IsAccepted=true;closed=true;return result;}
        catch{if(manager.Document.EditRevision!=revision){IsAccepted=true;closed=true;}if(manager.IsClosed)closed=true;throw;}
        finally{applying=false;}
    }
    public void Dispose(){closed=true;}
}

public sealed partial class EditorPatrolManagerSession
{
    public EditorPatrolPauseSession SelectPause(EditorPatrolManagerRoute route,int occurrence)=>new(this,route,occurrence);
    internal PatrolPoint GuardPause(EditorPatrolManagerRoute requested,int occurrence)
    {
        var route=Guard(requested);
        if(occurrence<0||occurrence>=route.IDs.Length)throw new ArgumentOutOfRangeException(nameof(occurrence));
        return Required(route.IDs[occurrence]);
    }
}
