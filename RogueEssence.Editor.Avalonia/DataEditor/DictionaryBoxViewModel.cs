﻿using System;
using System.Collections.Generic;
using System.Text;
using ReactiveUI;
using System.Collections.ObjectModel;
using Avalonia.Interactivity;
using Avalonia.Controls;
using RogueElements;
using System.Collections;
using RogueEssence.Dev.Views;

namespace RogueEssence.Dev.ViewModels
{
    public class DictionaryElement
    {
        private object key;
        public object Key
        {
            get { return key; }
        }
        private object value;
        public object Value
        {
            get { return value; }
        }
        public string DisplayValue
        {
            get { return conv.GetString(value); }
        }

        private StringConv conv;

        public DictionaryElement(StringConv conv, object key, object value)
        {
            this.conv = conv;
            this.key = key;
            this.value = value;
        }
    }

    public class DictionaryBoxViewModel : ViewModelBase
    {
        public ObservableCollection<DictionaryElement> Collection { get; }

        private int selectedIndex;
        public int SelectedIndex
        {
            get { return selectedIndex; }
            set { this.SetIfChanged(ref selectedIndex, value); }
        }

        public delegate void EditElementOp(object oldKey, object newKey, object element);
        public delegate void ElementOp(object key, object element, EditElementOp op);

        public event ElementOp OnEditKey;
        public event ElementOp OnEditItem;
        public event Action OnMemberChanged;

        public StringConv StringConv;

        private Window parent;

        public DictionaryBoxViewModel(Window parent, StringConv conv)
        {
            StringConv = conv;
            this.parent = parent;
            Collection = new ObservableCollection<DictionaryElement>();
        }

        public T GetDict<T>() where T : IDictionary
        {
            return (T)GetDict(typeof(T));
        }

        public IDictionary GetDict(Type type)
        {
            IDictionary result = (IDictionary)Activator.CreateInstance(type);
            foreach (DictionaryElement item in Collection)
                result.Add(item.Key, item.Value);
            return result;
        }

        public void LoadFromDict(IDictionary source)
        {
            Collection.Clear();
            foreach (object obj in source.Keys)
                Collection.Add(new DictionaryElement(StringConv, obj, source[obj]));
        }



        private void editItem(object oldKey, object key, object element)
        {
            int index = getIndexFromKey(key);
            Collection[index] = new DictionaryElement(StringConv, Collection[index].Key, element);
            OnMemberChanged?.Invoke();
        }
        private async void editKey(object oldKey, object key, object element)
        {
            int existingIndex = getIndexFromKey(key);
            if (existingIndex > -1)
            {
                await MessageBox.Show(parent, "Dictionary already contains this key!", "Error", MessageBox.MessageBoxButtons.Ok);
                return;
            }

            int index = getIndexFromKey(oldKey);
            Collection[index] = new DictionaryElement(StringConv, key, element);
            OnMemberChanged?.Invoke();
        }

        private async void insertKey(object oldKey, object key, object element)
        {
            int existingIndex = getIndexFromKey(key);
            if (existingIndex > -1)
            {
                await MessageBox.Show(parent, "Dictionary already contains this key!", "Error", MessageBox.MessageBoxButtons.Ok);
                return;
            }
            OnEditItem(key, element, insertItem);
        }

        private void insertItem(object oldKey, object key, object element)
        {
            Collection.Add(new DictionaryElement(StringConv, key, element));
            OnMemberChanged?.Invoke();
        }

        private int getIndexFromKey(object key)
        {
            int curIndex = 0;
            foreach (DictionaryElement item in Collection)
            {
                if (item.Key.Equals(key))
                    return curIndex;
                curIndex++;
            }
            return -1;
        }

        public void EditKey(int index)
        {
            if (index > -1)
            {
                DictionaryElement item = Collection[index];
                OnEditKey?.Invoke(item.Key, item.Value, editKey);
            }
        }

        public void lbxCollection_DoubleClick(object sender, RoutedEventArgs e)
        {
            //int index = lbxDictionary.IndexFromPoint(e.X, e.Y);
            int index = SelectedIndex;
            if (index > -1)
            {
                DictionaryElement item = Collection[index];
                OnEditItem?.Invoke(item.Key, item.Value, editItem);
            }
        }

        public void btnAdd_Click()
        {
            object newKey = null;
            object element = null;
            OnEditKey?.Invoke(newKey, element, insertKey);
        }

        public void btnDelete_Click()
        {
            if (SelectedIndex > -1 && SelectedIndex < Collection.Count)
            {
                Collection.RemoveAt(SelectedIndex);
                OnMemberChanged?.Invoke();
            }
        }
    }
}
