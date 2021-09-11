﻿using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Text;
using ReactiveUI;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using RogueEssence.Dungeon;
using RogueEssence.Data;
using RogueEssence.Content;
using System.IO;
using Avalonia.Media.Imaging;
using RogueElements;
using RogueEssence.Dev.Views;

namespace RogueEssence.Dev.ViewModels
{
    public class TilesetEditViewModel : ViewModelBase
    {
        private Window parent;

        private List<string> tileIndices;
        public SearchListBoxViewModel Tilesets { get; set; }

        private string cachedPath;
        public string CachedPath
        {
            get => cachedPath;
            set => this.SetIfChanged(ref cachedPath, value);
        }
        private int cachedSize;


        public TilesetEditViewModel()
        {
            tileIndices = new List<string>();

            Tilesets = new SearchListBoxViewModel();
            Tilesets.DataName = "Tilesets:";

        }

        public void LoadDataEntries(Window parent)
        {
            this.parent = parent;

            reloadFullList();
        }

        private void reloadFullList()
        {
            lock (GameBase.lockObj)
            {
                tileIndices.Clear();
                Tilesets.Clear();

                foreach (string name in GraphicsManager.TileIndex.Nodes.Keys)
                {
                    tileIndices.Add(name);
                    Tilesets.AddItem(name);
                }
            }
        }

        public async void mnuMassImport_Click()
        {
            await MessageBox.Show(parent, "Note: Importing a tileset to a slot that is already filled will automatically overwrite the old one.", "Mass Import", MessageBox.MessageBoxButtons.Ok);

            //remember addresses in registry
            string folderName = DevForm.GetConfig("TilesetDir", Directory.GetCurrentDirectory());

            //open window to choose directory
            OpenFolderDialog openFileDialog = new OpenFolderDialog();
            openFileDialog.Directory = folderName;

            string folder = await openFileDialog.ShowAsync(parent);

            if (folder == "")
                return;


            MapRetileWindow window = new MapRetileWindow();
            MapRetileViewModel viewModel = new MapRetileViewModel(GraphicsManager.TileSize, "Tile size must be divisible by 8.");
            window.DataContext = viewModel;

            bool sizeResult = await window.ShowDialog<bool>(parent);
            int size = viewModel.TileSize;

            if (!sizeResult || size == 0)
                return;

            DevForm.SetConfig("TilesetDir", folder);
            CachedPath = folder + "/";
            cachedSize = size;

            try
            {
                lock (GameBase.lockObj)
                    MassImport(CachedPath, cachedSize);
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(ex, false);
                await MessageBox.Show(parent, "Error importing from\n" + CachedPath + "\n\n" + ex.Message, "Import Failed", MessageBox.MessageBoxButtons.Ok);
                return;
            }
        }


        public async void mnuMassExport_Click()
        {
            //remember addresses in registry
            string folderName = DevForm.GetConfig("TilesetDir", Directory.GetCurrentDirectory());

            //open window to choose directory
            OpenFolderDialog openFileDialog = new OpenFolderDialog();
            openFileDialog.Directory = folderName;

            string folder = await openFileDialog.ShowAsync(parent);

            if (folder != "")
            {
                DevForm.SetConfig("TilesetDir", folder);
                CachedPath = folder + "/";

                try
                {
                    lock (GameBase.lockObj)
                        MassExport(CachedPath);
                }
                catch (Exception ex)
                {
                    DiagManager.Instance.LogError(ex, false);
                    await MessageBox.Show(parent, "Error exporting to\n" + CachedPath + "\n\n" + ex.Message, "Export Failed", MessageBox.MessageBoxButtons.Ok);
                    return;
                }
            }
        }

        public async void mnuReIndex_Click()
        {
            try
            {
                lock (GameBase.lockObj)
                    ReIndex();
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(ex, false);
                await MessageBox.Show(parent, "Error when reindexing.\n\n" + ex.Message, "Reindex Failed", MessageBox.MessageBoxButtons.Ok);
                return;
            }
        }

        public async void btnImport_Click()
        {
            //remember addresses in registry
            string folderName = DevForm.GetConfig("TilesetDir", Directory.GetCurrentDirectory());

            //open window to choose directory
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Directory = folderName;

            FileDialogFilter filter = new FileDialogFilter();
            filter.Name = "PNG Files";
            filter.Extensions.Add("png");
            openFileDialog.Filters.Add(filter);

            string[] results = await openFileDialog.ShowAsync(parent);

            if (results.Length == 0)
                return;
            
            string animName = Path.GetFileNameWithoutExtension(results[0]);

            if (tileIndices.Contains(animName))
            {
                MessageBox.MessageBoxResult result = await MessageBox.Show(parent, "Are you sure you want to overwrite the existing sheet:\n" + animName, "Sprite Sheet already exists.",
                    MessageBox.MessageBoxButtons.YesNo);
                if (result == MessageBox.MessageBoxResult.No)
                    return;
            }

            MapRetileWindow window = new MapRetileWindow();
            MapRetileViewModel viewModel = new MapRetileViewModel(GraphicsManager.TileSize, "Tile size must be divisible by 8.");
            window.DataContext = viewModel;

            bool sizeResult = await window.ShowDialog<bool>(parent);
            int size = viewModel.TileSize;

            if (!sizeResult || size == 0)
                return;

            DevForm.SetConfig("TilesetDir", Path.GetDirectoryName(results[0]));
            CachedPath = results[0];
            cachedSize = size;


            try
            {
                lock (GameBase.lockObj)
                    Import(CachedPath, cachedSize);
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(ex, false);
                await MessageBox.Show(parent, "Error importing from\n" + CachedPath + "\n\n" + ex.Message, "Import Failed", MessageBox.MessageBoxButtons.Ok);
                return;
            }
            
        }

        public async void btnReImport_Click()
        {
            try
            {
                lock (GameBase.lockObj)
                    Import(CachedPath, cachedSize);
            }
            catch (Exception ex)
            {
                DiagManager.Instance.LogError(ex, false);
                await MessageBox.Show(parent, "Error importing from\n" + CachedPath + "\n\n" + ex.Message, "Import Failed", MessageBox.MessageBoxButtons.Ok);
                return;
            }
        }

        public async void btnExport_Click()
        {
            //get current sprite
            string animData = tileIndices[Tilesets.InternalIndex];

            //remember addresses in registry
            string folderName = DevForm.GetConfig("TilesetDir", Directory.GetCurrentDirectory());

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Directory = folderName;

            FileDialogFilter filter = new FileDialogFilter();
            filter.Name = "PNG Files";
            filter.Extensions.Add("png");
            saveFileDialog.Filters.Add(filter);

            string folder = await saveFileDialog.ShowAsync(parent);

            if (folder != null)
            {
                DevForm.SetConfig("TilesetDir", Path.GetDirectoryName(folder));
                //CachedPath = folder;
                lock (GameBase.lockObj)
                    Export(folder, animData);
            }
        }


        public async void btnDelete_Click()
        {
            //get current sprite
            int animIdx = Tilesets.InternalIndex;

            MessageBox.MessageBoxResult result = await MessageBox.Show(parent, "Are you sure you want to delete the following sheet:\n" + tileIndices[animIdx], "Delete Sprite Sheet.",
                MessageBox.MessageBoxButtons.YesNo);
            if (result == MessageBox.MessageBoxResult.No)
                return;


            lock (GameBase.lockObj)
                Delete(animIdx);

        }



        private void MassImport(string currentPath, int tileSize)
        {
            if (!Directory.Exists(Path.GetDirectoryName(PathMod.HardMod(GraphicsManager.TILE_PATTERN))))
                Directory.CreateDirectory(Path.GetDirectoryName(PathMod.HardMod(GraphicsManager.TILE_PATTERN)));

            ImportHelper.ImportAllTiles(currentPath, PathMod.HardMod(GraphicsManager.TILE_PATTERN), true, true, tileSize);

            GraphicsManager.RebuildIndices(GraphicsManager.AssetType.Tile);
            GraphicsManager.ClearCaches(GraphicsManager.AssetType.Tile);

            DiagManager.Instance.LogInfo("Mass import complete.");

            //recompute
            reloadFullList();
        }



        private void ReIndex()
        {

            GraphicsManager.RebuildIndices(GraphicsManager.AssetType.Tile);
            GraphicsManager.ClearCaches(GraphicsManager.AssetType.Tile);

            DiagManager.Instance.LogInfo("All files re-indexed.");

            reloadFullList();
        }


        private void Import(string currentPath, int tileSize)
        {
            string sheetName = Path.GetFileNameWithoutExtension(currentPath);
            string outputFile = PathMod.HardMod(String.Format(GraphicsManager.TILE_PATTERN, sheetName));

            if (!Directory.Exists(Path.GetDirectoryName(outputFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

            //load into tilesets
            using (BaseSheet tileset = BaseSheet.Import(currentPath))
            {
                List<BaseSheet> tileList = new List<BaseSheet>();
                tileList.Add(tileset);
                ImportHelper.SaveTileSheet(tileList, outputFile, tileSize);
            }

            GraphicsManager.RebuildIndices(GraphicsManager.AssetType.Tile);
            GraphicsManager.ClearCaches(GraphicsManager.AssetType.Tile);
            DevGraphicsManager.ClearCaches();

            DiagManager.Instance.LogInfo("Tiles from:\n" +
                currentPath + "\nhave been imported.");

            //recompute
            reloadFullList();
        }

        private void MassExport(string currentPath)
        {
            string[] dirs = PathMod.GetModFiles(Path.GetDirectoryName(GraphicsManager.TILE_PATTERN), String.Format(Path.GetFileName(GraphicsManager.TILE_PATTERN), "*"));
            for (int ii = 0; ii < dirs.Length; ii++)
            {
                string filename = Path.GetFileNameWithoutExtension(dirs[ii]);
                Export(currentPath + filename, filename);
            }
        }

        private void Export(string currentPath, string anim)
        {
            string animPath = PathMod.ModPath(String.Format(GraphicsManager.TILE_PATTERN, anim));
            ImportHelper.ExportTileSheet(animPath, currentPath);

            DiagManager.Instance.LogInfo("Frames from:\n" +
                anim + "\nhave been exported to:" + currentPath);
        }



        private void Delete(int animIdx)
        {
            string anim = tileIndices[animIdx];

            string animPath = PathMod.ModPath(String.Format(GraphicsManager.TILE_PATTERN, anim));
            if (File.Exists(animPath))
                File.Delete(animPath);

            GraphicsManager.RebuildIndices(GraphicsManager.AssetType.Tile);
            GraphicsManager.ClearCaches(GraphicsManager.AssetType.Tile);

            DiagManager.Instance.LogInfo("Deleted frames for:" + anim);

            tileIndices.RemoveAt(animIdx);
            Tilesets.RemoveInternalAt(animIdx);

        }

    }
}
