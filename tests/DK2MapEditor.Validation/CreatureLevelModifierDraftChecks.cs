using DK2MapEditor.Core.Editing;

internal static class CreatureLevelModifierDraftChecks
{
    public static void Run()
    {
        void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Reject(Action action)
        {try{action();}catch(Exception error)when(error is ArgumentException or InvalidDataException or ObjectDisposedException){return;}throw new Exception("Invalid level draft accepted.");}
        uint[] visible=[0,1,2,3,4,5,6,7,9,10,13,15,16,17,18,19,21,22,24,27,28,30,31];
        Check(CreatureLevelModifierControls.Statistics.Select(c=>uint.Parse(c.Value)).SequenceEqual(visible),"Official visible statistic ID/order changed.");
        Check(CreatureLevelModifierControls.Statistics.All(c=>!string.IsNullOrWhiteSpace(c.English)&&!string.IsNullOrWhiteSpace(c.Russian)),"A visible statistic has no EN/RU name.");
        int[] controls=[1847,1851,1853,1855,1857,1859,1861,1863,1865];
        Check(Enumerable.Range(2,9).Select(CreatureLevelModifierControls.ControlForLevel).SequenceEqual(controls)
            &&CreatureLevelModifierControls.FirstPersonControl==1867,"Official level2/10/FirstPerson control mapping was shifted.");
        Check(CreatureLevelModifierControls.DisplayValue(0xffffffff)==-1&&CreatureLevelModifierControls.DisplayValue(0x0000ffff)==-1
            &&CreatureLevelModifierControls.DisplayValue(0xffff0001)==1&&CreatureLevelModifierControls.DisplayValue(0x00008000)==-32768,"Official SHORT load/sign extension differs.");
        CreatureLevelModifierRecord[] Seed()=>Enumerable.Range(0,32).SelectMany(stat=>Enumerable.Range(2,9)
            .Select(level=>new CreatureLevelModifierRecord(65,(uint)stat,(uint)(100+stat+level),(uint)level,0))
            .Append(new(224,(uint)stat,(uint)(50+stat),10,0))).ToArray();
        var source=Seed();var original=source.ToArray();
        using(var draft=new CreatureLevelModifierDraft(source))
        {
            Check(draft.SelectedStatistic==0&&draft.Current.Levels.SequenceEqual(Enumerable.Range(2,9).Select(l=>100+l))&&draft.Changes.Count==0,"Opening a draft rewrote original values.");
            source[0]=new(65,0,9999,2,0);Check(draft.Current.Levels[0]==102,"Draft retained mutable caller array storage.");source=original.ToArray();
            foreach(var stat in visible)
            {
                draft.SelectStatistic(stat,draft.Current);
                var levels=Enumerable.Range(0,9).Select(i=>i%2==0?-10000+(int)stat+i:10000-(int)stat-i).ToArray();
                var values=new CreatureLevelModifierValues(levels,-10000+(int)stat);
                draft.Store(values);levels[0]=0;
                Check(draft.Current.Levels[0]==-10000+stat&&draft.Current.FirstPerson==-10000+stat,"Private page did not copy/retain all ten signed values.");
            }
            Check(draft.Changes.Count==230&&draft.Changes.All(c=>visible.Contains(c.Key.Statistic))&&source.SequenceEqual(original),"Draft touched hidden source IDs or failed to retain all visible pages.");
            foreach(var stat in visible)
            {
                draft.SelectStatistic(stat,draft.Current);
                Check(draft.Current.Levels[0]==-10000+stat&&draft.Current.Levels[8]==-9992+stat&&draft.Current.FirstPerson==-10000+stat,"Changing selection lost an earlier page.");
            }
            var selected=draft.SelectedStatistic;var changes=draft.Changes.ToArray();var current=draft.Current;
            foreach(var index in Enumerable.Range(0,10))foreach(var invalid in new[]{-10001,10001})
            {
                var levels=current.Levels.ToArray();var first=current.FirstPerson;
                if(index==9)first=invalid;else levels[index]=invalid;
                Reject(()=>draft.Store(new(levels,first)));
                Reject(()=>draft.SelectStatistic(0,new(levels,first)));
                Check(draft.SelectedStatistic==selected&&draft.Changes.SequenceEqual(changes),"Invalid ten-value page partially changed values/selection.");
            }
            foreach(uint hidden in new uint[]{8,11,12,14,20,23,25,26,29,32,uint.MaxValue})Reject(()=>draft.SelectStatistic(hidden,current));
            Reject(()=>draft.Store(new(new int[8],0)));Check(draft.Changes.SequenceEqual(changes),"Invalid shape/ID changed the draft.");
            draft.Dispose();Check(draft.IsClosed,"Draft was not closed.");
            Reject(()=>draft.Store(current));Reject(()=>draft.SelectStatistic(0,current));Reject(()=>{_=draft.Current;});Reject(()=>{_=draft.Changes;});
        }
        var duplicated=Seed().Append(new CreatureLevelModifierRecord(65,0,0x0000ffff,2,999)).ToArray();
        using(var draft=new CreatureLevelModifierDraft(duplicated))
        {
            Check(draft.Current.Levels[0]==-1&&draft.Changes.Count==0,"Ordered native occurrences did not load the last SHORT.");
            draft.Store(draft.Current);Check(draft.Changes.Count==0,"Equal signed input normalized imported DWORD bits.");
            var levels=draft.Current.Levels.ToArray();levels[0]=3;draft.Store(new(levels,draft.Current.FirstPerson));
            var change=draft.Changes.Single();Check(change.OriginalBits==0x0000ffff&&change.Value==3&&change.Key==new CreatureLevelModifierKey(65,0,2),"Delta lost original bits or targeted a wrong level.");
        }
        var bad=Seed();bad[0]=bad[0] with{PercentBits=0x8000};
        using(var draft=new CreatureLevelModifierDraft(bad))
        {Check(draft.Current.Levels[0]==-32768&&draft.Changes.Count==0,"Imported out-of-range data was repaired on load.");Reject(()=>draft.Store(draft.Current));Check(draft.Changes.Count==0,"Rejected imported value mutated the draft.");}
        Reject(()=>{using var draft=new CreatureLevelModifierDraft(Seed().Where(r=>!(r.RecordType==224&&r.Statistic==31)));});
        Console.WriteLine("CREATURE LEVEL DRAFT DETAIL official23 IDs/order, EN/RU names, corrected9 normal/FirstPerson controls, signed SHORT display and -10000..10000 DDX limits;230 private cells, selection retention, all-slot validation before publication, exact original bits/no normalization, hidden/missing/closed guards; no writer/GUI/archive changes");
    }
}
