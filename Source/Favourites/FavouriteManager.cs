// FavouriteManager.cs
// Copyright Karel Kroeze, 2020-2020

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FluffyUI;
using FluffyUI.FloatMenu;
using UnityEngine;
using Verse;
using Widgets = Verse.Widgets;

namespace WorkTab {
    public class FavouriteManager: GameComponent {
        public static List<Favourite> Favourites = new List<Favourite>();
        private static Dictionary<Pawn, Favourite> _favourites = new Dictionary<Pawn, Favourite>();
        
        private static readonly ConcurrentQueue<Favourite> _saveQueue = new ConcurrentQueue<Favourite>();
        private static readonly HashSet<string> _pendingSet = new HashSet<string>();
        private static readonly object _lock = new object();

        private static bool _workerRunning = false;
        
        private static int _lastFlushTick = 0;
        private const int AutoFlushIntervalTicks = 60 * 5; // Automatic flush every 5 seconds (60 ticks = 1 second)

        private bool _wasInGame;

        public FavouriteManager() {
            Get = this;
        }

        public FavouriteManager(Game game) : this() {
        }

        public static FavouriteManager Get { get; private set; }

        private static string FavouriteBasePath {
            get {
                string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "WorkTab_Favourites");
                if (!Directory.Exists(path)) {
                    Directory.CreateDirectory(path);
                }

                return path;
            }
        }

        public Favourite this[Pawn pawn] {
            get => _favourites.GetValueOrDefault(pawn);
            set {
                if (value == null) {
                    _favourites.Remove(pawn);
                } else {
                    _favourites[pawn] = value;
                }
            }
        }

        public static void Delete(Favourite favourite, bool remove = false) {
            if (remove) {
                Remove(favourite);
            }

            string path = FavouritePath(favourite);
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }


        public static void FavouriteFloatMenuFor(Pawn pawn) {
            // create new
            List<FloatMenuOption> options = new List<FloatMenuOption> {
                new FloatMenuOption(
                             "Fluffy.WorkTab.CreateFavourite".Translate(),
                             () => Favourite.From(pawn)),

                // load from disk
                new FloatMenuOption(
                             "Fluffy.WorkTab.LoadFavourite".Translate(),
                             () => LoadFavouriteFloatMenu(pawn))
            };

            // clear
            if (Get[pawn] != null) {
                options.Add(new FloatMenuOption(
                                 "Fluffy.WorkTab.ClearFavourite".Translate(),
                                 () => Get[pawn] = null));
            }

            // set favourite in favourites
            foreach (Favourite favourite in Favourites) {
                options.Add(new FloatMenuOption_Aligned(
                                 favourite.Label,
                                 () => Get[pawn] = favourite,
                                 extraPartWidth: 38,
                                 extraPartOnGUI: rect => ExtraPart(rect, favourite)));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }


        public static FailReason IsValidLabel(string label, string curLabel = null) {
            if (label.NullOrEmpty()) {
                return "Fluffy.WorkTab.FavouriteLabelCannotBeEmpty".Translate().Resolve();
            }

            if (!IsValidFileName(label)) {
                return "Fluffy.WorkTab.FavouriteInvalidFilename".Translate(label).Resolve();
            }

            if (curLabel != label && File.Exists(FavouritePath(label))) {
                return "Fluffy.WorkTab.FavouriteAlreadyExists".Translate(label).Resolve();
            }

            return true;
        }

        public static void Remove(Favourite favourite) {
            Favourites.Remove(favourite);

            // remove from pawns
            Pawn[] affected = _favourites
                          .Where(f => f.Value == favourite)
                          .Select(f => f.Key)
                          .ToArray();
            foreach (Pawn pawn in affected) {
                _favourites.Remove(pawn);
            }
        }

        public static void Save(Favourite favourite, bool async = false) {
            if (async)
            {
                SaveLater(favourite);
            }
            else
            {
                Scribe.saver.InitSaving(FavouritePath(favourite), "Favourite");
                favourite.ExposeData();
                Scribe.saver.FinalizeSaving();
            }
        }

        private static bool ExtraPart(Rect rect, Favourite favourite) {
            List<FluffyUI.Grid> rects = new FluffyUI.Grid(rect, gutters: Vector2.zero).Columns(1, 1);
            float size = Mathf.Min(16f, rects[0].Rect.width, rects[0].Rect.height);
            Rect iconRect = new Rect(0f, 0f, size, size);
            GUI.DrawTexture(iconRect.CenteredIn(rects[0]), favourite.Icon);
            if (Widgets.ButtonImage(new Rect(0f, 0f, size, size).CenteredIn(rects[1]), Resources.Edit)) {
                Find.WindowStack.Add(new Dialog_EditFavourite(favourite));
            }

            return false;
        }

        private static string FavouritePath(Favourite favourite) {
            return FavouritePath(favourite.Label);
        }

        private static string FavouritePath(string label) {
            return Path.Combine(FavouriteBasePath, $"{label}.xml");
        }

        private static bool IsValidFileName(string label) {
            return GenFile.SanitizedFileName(label) == label;
        }

        private static void LoadFavourite(string path, Pawn pawn) {
            Favourite favourite = new Favourite();
            Scribe.loader.InitLoading(path);
            favourite.ExposeData();
            Scribe.loader.crossRefs.RegisterForCrossRefResolve(favourite);
            Scribe.loader.initer.RegisterForPostLoadInit(favourite);
            Scribe.loader.FinalizeLoading();

            favourite.SetLoadId();
            Favourites.Add(favourite);
            if (pawn != null) {
                Get[pawn] = favourite;
            }
        }

        private static void LoadFavouriteFloatMenu(Pawn pawn) {
            List<FloatMenuOption> options = Directory.GetFiles(FavouriteBasePath)
                                   .Select(f => new { path = f, label = Path.GetFileNameWithoutExtension(f) })
                                   .Where(f => !Favourites.Any(F => F.Label == f.label))
                                   .Select(f => new FloatMenuOption(f.label, () => LoadFavourite(f.path, pawn)))
                                   .ToList();
            if (options.Count == 0) {
                options.Add(new FloatMenuOption("Fluffy.WorkTab.NoStoredFavourites".Translate(), null));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Collections.Look(ref Favourites, "Favourites", LookMode.Deep);
            Scribe_Collections.Look(ref _favourites, "FavouriteAssignments", LookMode.Reference, LookMode.Reference);
        }
        
        public override void GameComponentTick()
        {
            Tick();
        }

        public override void GameComponentOnGUI()
        {
            switch (Current.ProgramState)
            {
                // When exiting from the game (return to the main menu)
                case ProgramState.Entry when _wasInGame:
                    _wasInGame = false;
                    FlushImmediately();
                    break;
                case ProgramState.Playing:
                    _wasInGame = true;
                    break;
                case ProgramState.MapInitializing:
                default:
                    break;
            }
        }

        private static void SaveLater(Favourite favourite)
        {
            if (favourite == null)
            {
                return;
            }
            var key = favourite.Label;

            lock (_lock)
            {
                if (!_pendingSet.Add(key))
                    return;

                _saveQueue.Enqueue(favourite);
            }

            StartWorker();
        }

        private static void StartWorker()
        {
            if (_workerRunning)
            {
                return;
            }
            
            _workerRunning = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                while (_saveQueue.TryDequeue(out var favourite))
                {
                    try
                    {
                        lock (_lock)
                        {
                            _pendingSet.Remove(favourite.Label);
                        }

                        lock (typeof(Scribe))
                        {
                            Save(favourite);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[Work Tab Favourite] Save error \n: {ex}");
                    }

                    Thread.Sleep(25);
                }

                _workerRunning = false;
            });
        }
        
        private static void Tick()
        {
            if (GenTicks.TicksGame - _lastFlushTick < AutoFlushIntervalTicks)
            {
                return;
            }
            
            _lastFlushTick = GenTicks.TicksGame;
            FlushImmediately();
        }

        private static void FlushImmediately()
        {
            lock (_lock)
            {
                while (_saveQueue.TryDequeue(out var favourite))
                {
                    try
                    {
                        _pendingSet.Remove(favourite.Label);

                        Save(favourite);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[Work Tab Favourite] Flush error: \n{ex}");
                    }
                }
            }
        }
    }
}
