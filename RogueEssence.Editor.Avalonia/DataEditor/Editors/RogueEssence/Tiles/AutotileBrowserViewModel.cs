﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using RogueElements;
using RogueEssence.Dungeon;
using ReactiveUI;
using RogueEssence.Content;
using Avalonia.Controls;
using System.Collections.ObjectModel;
using RogueEssence.Data;

namespace RogueEssence.Dev.ViewModels
{
    public class AutotileBrowserViewModel : ViewModelBase
    {
        public AutotileBrowserViewModel()
        {
            preview = TileFrame.Empty;
            associatePreview = TileFrame.Empty;


            Autotiles = new SearchListBoxViewModel();
            Autotiles.DataName = "Autotiles:";
            Autotiles.SelectedIndexChanged += Autotiles_SelectedIndexChanged;

            AssociateAutotiles = new SearchListBoxViewModel();
            AssociateAutotiles.DataName = "Associate Autotiles:";
            AssociateAutotiles.SelectedIndexChanged += AssociateAutotiles_SelectedIndexChanged;

            Associates = new ObservableCollection<string>();

            keys = new List<string>();

            UpdateAutotilesList();
        }

        public SearchListBoxViewModel Autotiles { get; set; }

        public SearchListBoxViewModel AssociateAutotiles { get; set; }

        public ObservableCollection<string> Associates { get; }

        private int chosenAssociate;
        public int ChosenAssociate
        {
            get => chosenAssociate;
            set => this.SetIfChanged(ref chosenAssociate, value);
        }

        /// <summary>
        /// The current tile being previewed
        /// </summary>
        private TileFrame preview;
        public TileFrame Preview
        {
            get => preview;
            set => this.SetIfChanged(ref preview, value);
        }


        /// <summary>
        /// The current tile being previewed
        /// </summary>
        private TileFrame associatePreview;
        public TileFrame AssociatePreview
        {
            get => associatePreview;
            set => this.SetIfChanged(ref associatePreview, value);
        }

        private int tileSize;
        public int TileSize
        {
            get => tileSize;
            set
            {
                this.RaiseAndSetIfChanged(ref tileSize, value);
                UpdateAutotilesList();
            }
        }

        private List<string> keys;

        /// <summary>
        /// The full draw data to be used as a brush. Computed.
        /// </summary>
        public TileBrush GetBrush()
        {
            HashSet<string> associates = new HashSet<string>();
            foreach (string tile in Associates)
                associates.Add(tile);
            return new TileBrush(keys[Math.Max(Autotiles.InternalIndex, 0)], associates);
        }

        public void SetBrush(AutoTile autotile)
        {
            Autotiles.SearchText = "";
            Autotiles.SelectedSearchIndex = keys.IndexOf(autotile.AutoTileset);
            AssociateAutotiles.SearchText = "";
            AssociateAutotiles.SelectedSearchIndex = 0;
            Associates.Clear();
            foreach (string tile in autotile.Associates)
                Associates.Add(tile);
        }


        public void UpdateAutotilesList()
        {
            if (Design.IsDesignMode)
                return;

            keys.Clear();
            Autotiles.Clear();
            AssociateAutotiles.Clear();

            keys.Add("");
            Autotiles.AddItem("**EMPTY**");

            foreach(string key in DataManager.Instance.DataIndices[DataManager.DataType.AutoTile].GetOrderedKeys(false))
            {
                EntrySummary entry = DataManager.Instance.DataIndices[DataManager.DataType.AutoTile].Get(key);
                //TODO: autotiles need tile sizes too, to compare
                if (24 == TileSize)
                {
                    keys.Add(key);
                    Autotiles.AddItem(key + ": " + entry.Name.ToLocal());
                    AssociateAutotiles.AddItem(key + ": " + entry.Name.ToLocal());
                }
            }
            Associates.Clear();
        }


        private void Autotiles_SelectedIndexChanged()
        {
            if (Autotiles.InternalIndex <= 0)
            {
                Preview = TileFrame.Empty;
                return;
            }
            AutoTileData autoTile = DataManager.Instance.GetAutoTile(keys[Autotiles.InternalIndex]);
            List<TileLayer> layer = autoTile.Tiles.GetLayers(-1);
            Preview = layer[0].Frames[0];
        }

        private void AssociateAutotiles_SelectedIndexChanged()
        {
            if (AssociateAutotiles.InternalIndex <= 0)
            {
                AssociatePreview = TileFrame.Empty;
                return;
            }
            AutoTileData autoTile = DataManager.Instance.GetAutoTile(keys[AssociateAutotiles.InternalIndex]);
            List<TileLayer> layer = autoTile.Tiles.GetLayers(-1);
            AssociatePreview = layer[0].Frames[0];
        }

        public void btnAddTile_Click()
        {
            if (!Associates.Contains(keys[AssociateAutotiles.InternalIndex]))
            {
                Associates.Add(keys[AssociateAutotiles.InternalIndex]);
                ChosenAssociate = Associates.Count - 1;
            }
        }


        public void btnDeleteTile_Click()
        {
            if (ChosenAssociate > -1)
                Associates.RemoveAt(ChosenAssociate);
        }
    }
}
