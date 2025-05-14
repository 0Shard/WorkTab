// PriorityTracker.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkTab {
    public class PriorityTracker: IExposable, ILoadReferenceable {
        // caches for ever/partially scheduled
        private readonly Dictionary<WorkGiverDef, bool> _everScheduledWorkGiver = new Dictionary<WorkGiverDef, bool>();
        private readonly Dictionary<WorkTypeDef, bool>  _partScheduledWorkType  = new Dictionary<WorkTypeDef, bool>();
        private readonly Dictionary<WorkGiverDef, bool> _timeScheduledWorkGiver = new Dictionary<WorkGiverDef, bool>();

        private readonly Dictionary<WorkGiverDef, string> _timeScheduledWorkGiverTip = new Dictionary<WorkGiverDef, string>();

        private readonly Dictionary<WorkTypeDef, bool> _timeScheduledWorkType = new Dictionary<WorkTypeDef, bool>();

        private readonly Dictionary<WorkTypeDef, string> _timeScheduledWorkTypeTip = new Dictionary<WorkTypeDef, string>();

        protected Dictionary<WorkGiverDef, WorkPriority> priorities = new Dictionary<WorkGiverDef, WorkPriority>();
        protected List<WorkPriority>                     workPriorityTrackersScribe;
        private   int                                    _loadId;

        public PriorityTracker() {
            SetLoadId();
        }

        public virtual WorkPriority this[WorkGiverDef workgiver] {
            get {
                if (priorities.TryGetValue(workgiver, out var item))
                {
                    return item;
                };

                Logger.Debug(
                    $"requested {workgiver.defName} priorities for {Pawn?.LabelShort ?? "<no pawn>"}, which did not yet exist.");

                item = new WorkPriority(this, workgiver);
                priorities.Add(workgiver, item);
                return item;
            }
            set => priorities[workgiver] = value;
        }

        public virtual Pawn Pawn { get; set; }

        public virtual void ExposeData() {
            if (Scribe.mode == LoadSaveMode.Saving) {
                workPriorityTrackersScribe = priorities.Values.ToList();
            }

            Scribe_Collections.Look(ref workPriorityTrackersScribe, "Priorities", LookMode.Deep, this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                priorities = workPriorityTrackersScribe
                            // check if any workgivers were removed midgame (don't try this at home, kids!)
                            .Where(k => k.Workgiver != null)
                            // reinstate the dictionary
                            .ToDictionary(k => k.Workgiver);
            }

            Scribe_Values.Look(ref _loadId, "loadId");
        }

        public string GetUniqueLoadID() {
            return $"Fluffy.PriorityTracker.{_loadId}";
        }

        // accessors
        public bool EverScheduled(WorkGiverDef workgiver) {
            if (!_everScheduledWorkGiver.ContainsKey(workgiver)) {
                Recache(workgiver);
            }

            return _everScheduledWorkGiver[workgiver];
        }

        public int[] GetPriorities(WorkGiverDef workgiver) {
            return TimeUtilities.WholeDay.Select(h => GetPriority(workgiver, h)).ToArray();
        }

        public int[] GetPriorities(WorkTypeDef worktype) {
            return TimeUtilities.WholeDay.Select(h => GetPriority(worktype, h)).ToArray();
        }

        public int GetPriority(WorkGiverDef workGiver, int hour)
        {
            return Pawn.AllowedToDo(workGiver) ? this[workGiver][hour] : 0;
        }

        public int GetPriority(WorkTypeDef workType, int hour) {
            if (workType == null) {
                return 0;
            }

            var minPriority = int.MaxValue;
            var found = false;
            foreach (var wg in workType.workGiversByPriority)
            {
                if (!Pawn.AllowedToDo(wg))
                {
                    continue;
                }

                var p = this[wg][hour];
                if (p <= 0)
                {
                    continue;
                }
                minPriority = Math.Min(minPriority, p);
                found = true;
            }

            return found ? minPriority : 0;
        }

        public void InvalidateCache(WorkGiverDef workgiver, bool bubble = true) {
            _everScheduledWorkGiver.Remove(workgiver);
            _timeScheduledWorkGiver.Remove(workgiver);
            _timeScheduledWorkGiverTip.Remove(workgiver);

            if (bubble) {
                InvalidateCache(workgiver.workType, false);
            }
        }

        public void InvalidateCache(WorkTypeDef worktype, bool bubble = true) {
            if (worktype == null) {
                return;
            }

            _timeScheduledWorkType.Remove(worktype);
            _timeScheduledWorkTypeTip.Remove(worktype);

            if (bubble) {
                worktype.workGiversByPriority.ForEach(wg => InvalidateCache(wg, false));
            }
        }

        public bool PartScheduled(WorkTypeDef worktype) {
            if (!_partScheduledWorkType.ContainsKey(worktype)) {
                Recache(worktype);
            }

            return _partScheduledWorkType[worktype];
        }

        public void Recache(WorkGiverDef workgiver) {
            // recache workgiver stuff
            int[] priorities = this[workgiver].Priorities;
            _everScheduledWorkGiver[workgiver] = priorities.Any(p => p > 0);
            _timeScheduledWorkGiver[workgiver] = priorities.Distinct().Count() > 1;
            _timeScheduledWorkGiverTip[workgiver] = DrawUtilities.TimeScheduledTip(priorities, workgiver.label);
        }

        public void Recache(WorkTypeDef worktype) {
            if (worktype == null) {
                return;
            }

            List<WorkGiverDef> workgivers = worktype.workGiversByPriority;
            int[] priorities = GetPriorities( worktype );

            // first make sure all workgivers are cached
            foreach (var workgiver in workgivers.Where(workgiver => !_everScheduledWorkGiver.ContainsKey(workgiver)))
            {
                Recache(workgiver);
            }

            // recache worktype stuff
            _timeScheduledWorkType[worktype] = workgivers.Any(wg => _timeScheduledWorkGiver[wg]);
            _timeScheduledWorkTypeTip[worktype] = DrawUtilities.TimeScheduledTip(priorities, worktype.gerundLabel);

            // is any workgiver different from the whole at any time during the day?
            _partScheduledWorkType[worktype] = TimeUtilities.WholeDay
                                                            .Any(hour =>
                                                                worktype.workGiversByPriority
                                                                        .Any(wg =>
                                                                            Pawn.AllowedToDo(wg) &&
                                                                            GetPriority(worktype, hour) != GetPriority(wg, hour))
                                                                );
        }

        public virtual void SetLoadId() {
            _loadId = PriorityManager.GetNextID();
        }

        public void SetPriority(WorkGiverDef workgiver, int priority, int hour, bool recache = true) {
            if (!Pawn.AllowedToDo(workgiver))
            {
                return;
            }

            if (priority > Settings.maxPriority) {
                priority = 0;
            }

            if (priority < 0) {
                priority = Settings.maxPriority;
            }

            this[workgiver][hour] = priority;

            if (recache) {
                InvalidateCache(workgiver);
                OnChange();
            }
        }

        public void SetPriority(WorkGiverDef workgiver, int priority, List<int> hours) {
            if (hours.NullOrEmpty()) {
                hours = TimeUtilities.WholeDay;
            }

            foreach (int hour in hours) {
                SetPriority(workgiver, priority, hour, false);
            }

            InvalidateCache(workgiver);
            OnChange();
        }

        public void SetPriority(WorkTypeDef worktype, int priority, int hour, bool recache = true) {
            if (worktype == null) {
                return;
            }

            foreach (WorkGiverDef workgiver in worktype.workGiversByPriority) {
                SetPriority(workgiver, priority, hour, false);
            }

            if (recache) {
                InvalidateCache(worktype);
                OnChange();
            }
        }

        public void SetPriority(WorkTypeDef worktype, int priority, List<int> hours) {
            if (worktype == null) {
                return;
            }

            if (hours.NullOrEmpty()) {
                hours = TimeUtilities.WholeDay;
            }

            foreach (int hour in hours) {
                SetPriority(worktype, priority, hour, false);
            }

            InvalidateCache(worktype);
            OnChange();
        }

        public bool TimeScheduled(WorkGiverDef workgiver) {
            if (!_timeScheduledWorkGiver.ContainsKey(workgiver)) {
                Recache(workgiver);
            }

            return _timeScheduledWorkGiver[workgiver];
        }

        public bool TimeScheduled(WorkTypeDef worktype) {
            if (worktype == null) {
                return false;
            }

            if (!_timeScheduledWorkType.ContainsKey(worktype)) {
                Recache(worktype);
            }

            return _timeScheduledWorkType[worktype];
        }

        public string TimeScheduledTip(WorkGiverDef workgiver) {
            if (!_timeScheduledWorkGiverTip.ContainsKey(workgiver)) {
                Recache(workgiver);
            }

            return _timeScheduledWorkGiverTip[workgiver];
        }

        public string TimeScheduledTip(WorkTypeDef worktype) {
            if (worktype == null) {
                return string.Empty;
            }

            if (!_timeScheduledWorkTypeTip.ContainsKey(worktype)) {
                Recache(worktype);
            }

            return _timeScheduledWorkTypeTip[worktype];
        }

        protected virtual void OnChange() {
        }
    }
}
